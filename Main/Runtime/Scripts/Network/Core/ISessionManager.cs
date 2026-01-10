using System;

namespace Majinfwork.Network {
    public interface ISessionManager {
        SessionInfo CurrentSession { get; }
        bool IsHost { get; }
        int CurrentPlayerCount { get; }

        event Action<SessionInfo> OnSessionCreated;
        event Action OnSessionDestroyed;
        event Action<int> OnPlayerCountChanged;

        void CreateSession(SessionSettings settings, int protocolVersion);
        void DestroySession();
        void IncrementPlayerCount();
        void DecrementPlayerCount();
        bool ValidateJoinRequest(ConnectionPayload payload, out ConnectionStatus rejectionReason);
    }
}
