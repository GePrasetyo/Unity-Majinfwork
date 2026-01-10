using System;
using System.Collections.Generic;
using System.Threading;

namespace Majinfwork.Network {
    public interface ILANDiscoveryService {
        IReadOnlyList<DiscoveredSession> DiscoveredSessions { get; }
        bool IsScanning { get; }

        event Action<DiscoveredSession> OnSessionDiscovered;
        event Action<DiscoveredSession> OnSessionLost;
        event Action<DiscoveredSession> OnSessionUpdated;
        event Action OnScanStarted;
        event Action OnScanComplete;

        void StartScan(CancellationToken cancellationToken = default);
        void StopScan();
        void ClearSessions();
    }
}
