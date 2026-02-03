using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Majinfwork.Network {
    /// <summary>
    /// LAN discovery service for finding and advertising sessions on the local network.
    /// Uses async enumerable pattern for consuming discovery events.
    /// Extend this class to customize discovery behavior.
    /// </summary>
    public class LANDiscoveryService : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>, ILANDiscoveryService {
        protected readonly NetworkConfig config;
        protected readonly ISessionManager sessionManager;
        protected readonly Func<ushort> getTransportPort;

        protected readonly Dictionary<IPEndPoint, DiscoveredSession> sessions = new();
        protected CancellationTokenSource scanTokenSource;

        // Unity-compatible async event queue (replaces System.Threading.Channels)
        protected ConcurrentQueue<DiscoveryEvent> eventQueue;
        protected SemaphoreSlim eventSignal;
        protected volatile bool channelCompleted;

        public IReadOnlyList<DiscoveredSession> DiscoveredSessions => sessions.Values.ToList().AsReadOnly();
        public bool IsScanning => scanTokenSource != null && !scanTokenSource.IsCancellationRequested;

        public LANDiscoveryService(NetworkConfig config, ISessionManager sessionManager, Func<ushort> getTransportPort) {
            this.config = config;
            this.sessionManager = sessionManager;
            this.getTransportPort = getTransportPort;
            this.port = config.discoveryPort;
        }

        #region Public API

        /// <summary>
        /// Scans for LAN sessions, yielding discovery events as they occur.
        /// </summary>
        public virtual async IAsyncEnumerable<DiscoveryEvent> ScanAsync(
            TimeSpan? timeout = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) {

            // Stop any existing scan
            StopScan();
            ClearSessions();

            // Create linked token source for timeout and external cancellation
            scanTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = scanTokenSource.Token;

            // Create event queue and signal (Unity-compatible replacement for Channel<T>)
            eventQueue = new ConcurrentQueue<DiscoveryEvent>();
            eventSignal = new SemaphoreSlim(0);
            channelCompleted = false;

            // Start UDP discovery
            SearchLocalSession();

            // Yield scan started event
            yield return DiscoveryEvent.ScanStarted();
            OnScanStartedInternal();

            Debug.Log($"[LANDiscovery] Started scanning{(timeout.HasValue ? $" for {timeout.Value.TotalSeconds}s" : " indefinitely")}");

            // Start the scan loop (broadcasts and prunes stale sessions)
            _ = ScanLoopAsync(timeout, token);

            // Read events from queue and yield them
            try {
                while (!channelCompleted || !eventQueue.IsEmpty) {
                    // Wait for signal or check periodically
                    try {
                        await eventSignal.WaitAsync(100, token);
                    }
                    catch (OperationCanceledException) {
                        break;
                    }

                    // Drain all available events
                    while (eventQueue.TryDequeue(out var evt)) {
                        yield return evt;
                    }
                }
            }
            finally {
                // Cleanup when enumeration completes (normally or via cancellation)
                CleanupScan();
            }
        }

        /// <summary>
        /// Finds the first session matching the predicate.
        /// Useful for "Quick Join" / matchmaking.
        /// </summary>
        public virtual async Task<DiscoveredSession> FindSessionAsync(
            Func<DiscoveredSession, bool> predicate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default) {

            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            await foreach (var evt in ScanAsync(timeout, cancellationToken)) {
                if (evt.Type == DiscoveryEventType.Discovered || evt.Type == DiscoveryEventType.Updated) {
                    if (predicate(evt.Session)) {
                        StopScan();
                        return evt.Session;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Stops the current scan.
        /// </summary>
        public virtual void StopScan() {
            if (scanTokenSource != null) {
                scanTokenSource.Cancel();
                scanTokenSource.Dispose();
                scanTokenSource = null;
            }

            // Mark channel as completed to end enumeration
            channelCompleted = true;
            eventSignal?.Release();
            eventSignal?.Dispose();
            eventSignal = null;
            eventQueue = null;

            StopDiscovery();
            Debug.Log("[LANDiscovery] Scan stopped");
        }

        /// <summary>
        /// Clears all discovered sessions.
        /// </summary>
        public virtual void ClearSessions() {
            sessions.Clear();
        }

        /// <summary>
        /// Starts hosting - responds to discovery broadcasts.
        /// </summary>
        public virtual void StartHosting() {
            StopScan();
            StartLocalSession();
            Debug.Log("[LANDiscovery] Started hosting - responding to discovery broadcasts");
        }

        #endregion

        #region Internal Methods

        private void CleanupScan() {
            scanTokenSource?.Cancel();
            scanTokenSource?.Dispose();
            scanTokenSource = null;
            channelCompleted = true;
            eventSignal?.Dispose();
            eventSignal = null;
            eventQueue = null;
            StopDiscovery();
        }

        /// <summary>
        /// Queues an event to be yielded by ScanAsync.
        /// Call this after updating the sessions dictionary.
        /// </summary>
        protected void QueueEvent(DiscoveryEvent evt) {
            if (eventQueue != null && !channelCompleted) {
                eventQueue.Enqueue(evt);
                try {
                    eventSignal?.Release();
                }
                catch (ObjectDisposedException) {
                    // Ignore if already disposed
                }
            }
        }

        /// <summary>
        /// The main scan loop - sends broadcasts and prunes stale sessions.
        /// </summary>
        protected virtual async Task ScanLoopAsync(TimeSpan? timeout, CancellationToken token) {
            var startTime = Time.realtimeSinceStartup;
            var endTime = timeout.HasValue ? startTime + (float)timeout.Value.TotalSeconds : float.MaxValue;

            while (!token.IsCancellationRequested && Time.realtimeSinceStartup < endTime) {
                // Send discovery broadcast
                var broadcastData = CreateBroadcastData();
                ClientBroadcast(broadcastData);

                try {
                    await Task.Delay((int)(config.broadcastInterval * 1000), token);
                }
                catch (OperationCanceledException) {
                    break;
                }

                // Prune stale sessions
                PruneStaleSessions();
            }

            // Queue scan complete event if we finished normally (not cancelled)
            if (!token.IsCancellationRequested) {
                QueueEvent(DiscoveryEvent.ScanComplete());
                OnScanCompletedInternal();
                Debug.Log($"[LANDiscovery] Scan complete. Found {sessions.Count} session(s)");
            }

            // Mark channel as completed to end enumeration
            channelCompleted = true;
            try {
                eventSignal?.Release();
            }
            catch (ObjectDisposedException) {
                // Ignore if already disposed
            }
        }

        /// <summary>
        /// Creates the broadcast data to send. Override to add custom data.
        /// </summary>
        protected virtual DiscoveryBroadcastData CreateBroadcastData() {
            return new DiscoveryBroadcastData {
                protocolVersion = config.protocolVersion
            };
        }

        /// <summary>
        /// Called when scan starts. Override for custom handling.
        /// </summary>
        protected virtual void OnScanStartedInternal() { }

        /// <summary>
        /// Called when scan completes. Override for custom handling.
        /// </summary>
        protected virtual void OnScanCompletedInternal() { }

        /// <summary>
        /// Removes sessions that haven't been seen recently.
        /// </summary>
        protected virtual void PruneStaleSessions() {
            var now = DateTime.UtcNow;
            var staleTimeout = TimeSpan.FromSeconds(config.sessionStaleTimeout);
            var staleEndpoints = new List<IPEndPoint>();

            foreach (var kvp in sessions) {
                if (now - kvp.Value.LastSeen > staleTimeout) {
                    staleEndpoints.Add(kvp.Key);
                }
            }

            foreach (var endpoint in staleEndpoints) {
                if (sessions.TryGetValue(endpoint, out var session)) {
                    // Update list first, then queue event
                    sessions.Remove(endpoint);
                    QueueEvent(DiscoveryEvent.Lost(session));
                    OnSessionLostInternal(session);
                    Debug.Log($"[LANDiscovery] Session lost (stale): {session.SessionName}");
                }
            }
        }

        /// <summary>
        /// Called when a session is lost. Override for custom handling.
        /// </summary>
        protected virtual void OnSessionLostInternal(DiscoveredSession session) { }

        #endregion

        #region NetworkDiscovery Overrides

        /// <summary>
        /// Processes incoming broadcast requests (server-side).
        /// </summary>
        protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadcast, out DiscoveryResponseData response) {
            response = default;

            var session = sessionManager?.CurrentSession;
            if (session == null) {
                return false;
            }

            response = CreateResponseData(session);
            OnProcessingBroadcast(sender, broadcast, response);

            Debug.Log($"[LANDiscovery] Responding to broadcast from {sender.Address}");
            return true;
        }

        /// <summary>
        /// Creates the response data for a broadcast. Override to add custom data.
        /// </summary>
        protected virtual DiscoveryResponseData CreateResponseData(SessionInfo session) {
            return new DiscoveryResponseData {
                serverName = session.SessionName,
                port = getTransportPort(),
                currentPlayers = session.CurrentPlayerCount,
                maxPlayers = session.MaxPlayers,
                hasPassword = session.HasPassword,
                protocolVersion = session.ProtocolVersion,
                mapIndex = session.MapIndex,
                customDataJson = null
            };
        }

        /// <summary>
        /// Called when processing a broadcast before sending response. Override to modify response.
        /// </summary>
        protected virtual void OnProcessingBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadcast, DiscoveryResponseData response) { }

        /// <summary>
        /// Handles received discovery responses (client-side).
        /// </summary>
        protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response) {
            // Create endpoint with the actual game port, not the discovery port
            var gameEndpoint = new IPEndPoint(sender.Address, response.port);

            if (sessions.TryGetValue(gameEndpoint, out var existingSession)) {
                // Update existing session, then queue event
                existingSession.CurrentPlayers = response.currentPlayers;
                existingSession.LastSeen = DateTime.UtcNow;
                existingSession.CustomDataJson = response.customDataJson;
                QueueEvent(DiscoveryEvent.Updated(existingSession));
                OnSessionUpdatedInternal(existingSession, response);
            }
            else {
                // Add new session, then queue event
                var discoveredSession = CreateDiscoveredSession(gameEndpoint, response);
                sessions[gameEndpoint] = discoveredSession;
                QueueEvent(DiscoveryEvent.Discovered(discoveredSession));
                OnSessionDiscoveredInternal(discoveredSession, response);

                Debug.Log($"[LANDiscovery] Discovered: {discoveredSession}");
            }
        }

        /// <summary>
        /// Creates a DiscoveredSession from response data. Override for custom session creation.
        /// </summary>
        protected virtual DiscoveredSession CreateDiscoveredSession(IPEndPoint endpoint, DiscoveryResponseData response) {
            return new DiscoveredSession(
                endpoint,
                response.serverName,
                response.currentPlayers,
                response.maxPlayers,
                response.hasPassword,
                response.protocolVersion,
                response.mapIndex,
                response.customDataJson
            );
        }

        /// <summary>
        /// Called when a new session is discovered. Override for custom handling.
        /// </summary>
        protected virtual void OnSessionDiscoveredInternal(DiscoveredSession session, DiscoveryResponseData response) { }

        /// <summary>
        /// Called when an existing session is updated. Override for custom handling.
        /// </summary>
        protected virtual void OnSessionUpdatedInternal(DiscoveredSession session, DiscoveryResponseData response) { }

        #endregion
    }
}
