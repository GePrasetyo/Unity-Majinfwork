using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork.Network {
    /// <summary>
    /// Service for discovering LAN sessions using async enumerable pattern.
    /// Sessions are automatically cleared when a new scan starts.
    /// </summary>
    public interface ILANDiscoveryService {
        /// <summary>
        /// Currently discovered sessions. Updated in real-time during scanning.
        /// Cleared automatically when ScanAsync() is called.
        /// </summary>
        IReadOnlyList<DiscoveredSession> DiscoveredSessions { get; }

        /// <summary>
        /// Whether a scan is currently in progress.
        /// </summary>
        bool IsScanning { get; }

        /// <summary>
        /// Scans for LAN sessions, yielding discovery events as they occur.
        /// Sessions are cleared when scan starts. The DiscoveredSessions list is
        /// updated before each event is yielded, so it's always consistent.
        /// </summary>
        /// <param name="timeout">
        /// How long to scan before completing. Null means scan indefinitely until cancelled.
        /// </param>
        /// <param name="cancellationToken">Token to cancel the scan early.</param>
        /// <returns>
        /// Async enumerable of discovery events. Yields ScanStarted first,
        /// then Discovered/Updated/Lost events, and finally ScanComplete (if not cancelled).
        /// </returns>
        /// <example>
        /// <code>
        /// await foreach (var evt in lanDiscovery.ScanAsync(TimeSpan.FromSeconds(10), ct)) {
        ///     switch (evt.Type) {
        ///         case DiscoveryEventType.Discovered:
        ///             AddToUI(evt.Session);
        ///             break;
        ///         case DiscoveryEventType.Lost:
        ///             RemoveFromUI(evt.Session);
        ///             break;
        ///     }
        /// }
        /// </code>
        /// </example>
        IAsyncEnumerable<DiscoveryEvent> ScanAsync(
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Convenience method for matchmaking - finds the first session matching a predicate.
        /// Useful for "Quick Join" functionality.
        /// </summary>
        /// <param name="predicate">Filter to match sessions (e.g., not full, compatible version).</param>
        /// <param name="timeout">How long to search before giving up. Null means search indefinitely.</param>
        /// <param name="cancellationToken">Token to cancel the search.</param>
        /// <returns>First matching session, or null if timeout/cancelled with no match.</returns>
        /// <example>
        /// <code>
        /// var session = await lanDiscovery.FindSessionAsync(
        ///     s => !s.IsFull &amp;&amp; s.IsCompatible(protocolVersion),
        ///     timeout: TimeSpan.FromSeconds(10)
        /// );
        /// if (session != null) {
        ///     networkService.JoinSession(session);
        /// }
        /// </code>
        /// </example>
        Task<DiscoveredSession> FindSessionAsync(
            Func<DiscoveredSession, bool> predicate,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Stops the current scan if one is in progress.
        /// The async enumerable will complete after yielding any pending events.
        /// </summary>
        void StopScan();

        /// <summary>
        /// Clears all discovered sessions from the list.
        /// This is called automatically when ScanAsync() starts.
        /// </summary>
        void ClearSessions();
    }
}
