using System;

namespace Majinfwork.Network {
    public interface INetworkService {
        SessionInfo CurrentSession { get; }
        ConnectionStatus Status { get; }
        bool IsConnected { get; }
        bool IsHost { get; }

        event Action<ConnectionStatus> OnStatusChanged;
        event Action<ulong> OnClientConnected;
        event Action<ulong, ConnectionStatus> OnClientDisconnected;

        void Initialize(NetworkConfig config);
        void Shutdown();

        void HostSession(SessionSettings settings);
        void JoinSession(string address, ushort port, string password = null);
        void JoinSession(DiscoveredSession session, string password = null);
        void LeaveSession();
    }
}
