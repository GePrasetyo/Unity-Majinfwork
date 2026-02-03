using System;

namespace Majinfwork.Network {
    /// <summary>
    /// Type of discovery event during LAN scanning.
    /// </summary>
    public enum DiscoveryEventType {
        /// <summary>A new session was discovered on the network.</summary>
        Discovered,
        /// <summary>An existing session's data was updated (player count changed, etc.).</summary>
        Updated,
        /// <summary>A session is no longer responding and was removed.</summary>
        Lost,
        /// <summary>Scan has started. Session is null for this event.</summary>
        ScanStarted,
        /// <summary>Scan has completed (timeout reached). Session is null for this event.</summary>
        ScanComplete
    }

    /// <summary>
    /// Represents a discovery event during LAN scanning.
    /// Used with IAsyncEnumerable pattern for async session discovery.
    /// </summary>
    public readonly struct DiscoveryEvent {
        /// <summary>The type of event that occurred.</summary>
        public DiscoveryEventType Type { get; }

        /// <summary>
        /// The session associated with this event.
        /// Null for ScanStarted and ScanComplete events.
        /// </summary>
        public DiscoveredSession Session { get; }

        /// <summary>When this event occurred.</summary>
        public DateTime Timestamp { get; }

        public DiscoveryEvent(DiscoveryEventType type, DiscoveredSession session = null) {
            Type = type;
            Session = session;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>Creates a Discovered event.</summary>
        public static DiscoveryEvent Discovered(DiscoveredSession session)
            => new DiscoveryEvent(DiscoveryEventType.Discovered, session);

        /// <summary>Creates an Updated event.</summary>
        public static DiscoveryEvent Updated(DiscoveredSession session)
            => new DiscoveryEvent(DiscoveryEventType.Updated, session);

        /// <summary>Creates a Lost event.</summary>
        public static DiscoveryEvent Lost(DiscoveredSession session)
            => new DiscoveryEvent(DiscoveryEventType.Lost, session);

        /// <summary>Creates a ScanStarted event.</summary>
        public static DiscoveryEvent ScanStarted()
            => new DiscoveryEvent(DiscoveryEventType.ScanStarted);

        /// <summary>Creates a ScanComplete event.</summary>
        public static DiscoveryEvent ScanComplete()
            => new DiscoveryEvent(DiscoveryEventType.ScanComplete);

        public override string ToString() {
            return Session != null
                ? $"[{Type}] {Session.SessionName} @ {Timestamp:HH:mm:ss}"
                : $"[{Type}] @ {Timestamp:HH:mm:ss}";
        }
    }
}
