using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Majinfwork.Network {
    /// <summary>
    /// LAN discovery service for finding and advertising sessions on the local network.
    /// Extend this class to customize discovery behavior.
    /// </summary>
    public class LANDiscoveryService : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>, ILANDiscoveryService {
        protected readonly NetworkConfig config;
        protected readonly ISessionManager sessionManager;
        protected readonly Func<ushort> getTransportPort;

        protected readonly Dictionary<IPEndPoint, DiscoveredSession> sessions = new Dictionary<IPEndPoint, DiscoveredSession>();
        protected CancellationTokenSource scanTokenSource;

        public IReadOnlyList<DiscoveredSession> DiscoveredSessions => sessions.Values.ToList().AsReadOnly();
        public bool IsScanning => scanTokenSource != null && !scanTokenSource.IsCancellationRequested;

        public event Action<DiscoveredSession> OnSessionDiscovered;
        public event Action<DiscoveredSession> OnSessionLost;
        public event Action<DiscoveredSession> OnSessionUpdated;
        public event Action OnScanStarted;
        public event Action OnScanComplete;

        public LANDiscoveryService(NetworkConfig config, ISessionManager sessionManager, Func<ushort> getTransportPort) {
            this.config = config;
            this.sessionManager = sessionManager;
            this.getTransportPort = getTransportPort;
            this.port = config.discoveryPort;
        }

        /// <summary>
        /// Starts scanning for LAN sessions.
        /// </summary>
        public virtual void StartScan(CancellationToken externalToken = default) {
            StopScan();
            ClearSessions();

            scanTokenSource = externalToken == default
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(externalToken);

            SearchLocalSession();
            OnScanStarted?.Invoke();
            OnScanStartedInternal();

            _ = ScanLoopAsync(scanTokenSource.Token);

            Debug.Log($"[LANDiscovery] Started scanning for {config.discoveryTimeout} seconds");
        }

        /// <summary>
        /// Called when scan starts. Override for custom handling.
        /// </summary>
        protected virtual void OnScanStartedInternal() { }

        /// <summary>
        /// Stops the current scan.
        /// </summary>
        public virtual void StopScan() {
            if (scanTokenSource != null) {
                scanTokenSource.Cancel();
                scanTokenSource.Dispose();
                scanTokenSource = null;
            }

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

        /// <summary>
        /// The main scan loop. Override to customize scan behavior.
        /// </summary>
        protected virtual async Task ScanLoopAsync(CancellationToken token) {
            var startTime = Time.realtimeSinceStartup;
            var endTime = startTime + config.discoveryTimeout;

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

            if (!token.IsCancellationRequested) {
                OnScanComplete?.Invoke();
                OnScanCompletedInternal();
                Debug.Log($"[LANDiscovery] Scan complete. Found {sessions.Count} session(s)");
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
                    sessions.Remove(endpoint);
                    OnSessionLost?.Invoke(session);
                    OnSessionLostInternal(session);
                    Debug.Log($"[LANDiscovery] Session lost (stale): {session.SessionName}");
                }
            }
        }

        /// <summary>
        /// Called when a session is lost. Override for custom handling.
        /// </summary>
        protected virtual void OnSessionLostInternal(DiscoveredSession session) { }

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
                // Update existing session
                existingSession.CurrentPlayers = response.currentPlayers;
                existingSession.LastSeen = DateTime.UtcNow;
                existingSession.CustomDataJson = response.customDataJson;
                OnSessionUpdated?.Invoke(existingSession);
                OnSessionUpdatedInternal(existingSession, response);
            }
            else {
                // New session discovered
                var discoveredSession = CreateDiscoveredSession(gameEndpoint, response);
                sessions[gameEndpoint] = discoveredSession;
                OnSessionDiscovered?.Invoke(discoveredSession);
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
    }
}
