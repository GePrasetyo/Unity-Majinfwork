using System;
using UnityEngine;

namespace Majinfwork.Network {
    /// <summary>
    /// Manages session lifecycle. Extend this class to customize session behavior.
    /// </summary>
    public class SessionManager : ISessionManager {
        protected readonly NetworkConfig config;

        public SessionInfo CurrentSession { get; protected set; }
        public bool IsHost => CurrentSession != null;
        public int CurrentPlayerCount => CurrentSession?.CurrentPlayerCount ?? 0;

        public event Action<SessionInfo> OnSessionCreated;
        public event Action OnSessionDestroyed;
        public event Action<int> OnPlayerCountChanged;

        public SessionManager(NetworkConfig config) {
            this.config = config;
        }

        /// <summary>
        /// Creates a new session with the given settings.
        /// </summary>
        public virtual void CreateSession(SessionSettings settings, int protocolVersion) {
            if (CurrentSession != null) {
                Debug.LogWarning("[SessionManager] Session already exists. Destroying existing session first.");
                DestroySession();
            }

            // Apply defaults if not specified
            settings = ApplyDefaults(settings);

            CurrentSession = CreateSessionInfo(settings, protocolVersion);
            OnSessionCreatedInternal();

            Debug.Log($"[SessionManager] Session created: {CurrentSession}");
            OnSessionCreated?.Invoke(CurrentSession);
        }

        /// <summary>
        /// Override to apply custom defaults to session settings.
        /// </summary>
        protected virtual SessionSettings ApplyDefaults(SessionSettings settings) {
            if (settings.maxPlayers <= 0) {
                settings.maxPlayers = config.defaultMaxPlayers;
            }

            if (string.IsNullOrEmpty(settings.sessionName)) {
                settings.sessionName = $"{config.defaultSessionNamePrefix} {UnityEngine.Random.Range(1000, 9999)}";
            }

            return settings;
        }

        /// <summary>
        /// Factory method to create SessionInfo. Override to return a custom derived SessionInfo.
        /// </summary>
        protected virtual SessionInfo CreateSessionInfo(SessionSettings settings, int protocolVersion) {
            return new SessionInfo(settings, protocolVersion);
        }

        /// <summary>
        /// Called after session is created but before event is fired. Override for custom setup.
        /// </summary>
        protected virtual void OnSessionCreatedInternal() { }

        /// <summary>
        /// Destroys the current session.
        /// </summary>
        public virtual void DestroySession() {
            if (CurrentSession == null) return;

            OnSessionDestroyingInternal();

            Debug.Log($"[SessionManager] Session destroyed: {CurrentSession.SessionName}");
            CurrentSession = null;
            OnSessionDestroyed?.Invoke();
        }

        /// <summary>
        /// Called before session is destroyed. Override for custom cleanup.
        /// </summary>
        protected virtual void OnSessionDestroyingInternal() { }

        /// <summary>
        /// Increments the player count.
        /// </summary>
        public virtual void IncrementPlayerCount() {
            if (CurrentSession == null) return;

            CurrentSession.CurrentPlayerCount++;
            OnPlayerCountChangedInternal(CurrentSession.CurrentPlayerCount);

            Debug.Log($"[SessionManager] Player joined. Count: {CurrentSession.CurrentPlayerCount}/{CurrentSession.MaxPlayers}");
            OnPlayerCountChanged?.Invoke(CurrentSession.CurrentPlayerCount);
        }

        /// <summary>
        /// Decrements the player count.
        /// </summary>
        public virtual void DecrementPlayerCount() {
            if (CurrentSession == null) return;

            CurrentSession.CurrentPlayerCount = Mathf.Max(0, CurrentSession.CurrentPlayerCount - 1);
            OnPlayerCountChangedInternal(CurrentSession.CurrentPlayerCount);

            Debug.Log($"[SessionManager] Player left. Count: {CurrentSession.CurrentPlayerCount}/{CurrentSession.MaxPlayers}");
            OnPlayerCountChanged?.Invoke(CurrentSession.CurrentPlayerCount);
        }

        /// <summary>
        /// Called when player count changes. Override for custom handling.
        /// </summary>
        protected virtual void OnPlayerCountChangedInternal(int newCount) { }

        /// <summary>
        /// Validates a join request. Override to add custom validation rules.
        /// </summary>
        public virtual bool ValidateJoinRequest(ConnectionPayload payload, out ConnectionStatus rejectionReason) {
            rejectionReason = ConnectionStatus.Success;

            if (CurrentSession == null) {
                rejectionReason = ConnectionStatus.SessionNotFound;
                Debug.LogWarning("[SessionManager] Validation failed: No active session");
                return false;
            }

            // Protocol version check
            if (!ValidateProtocolVersion(payload, out rejectionReason)) {
                return false;
            }

            // Player name validation
            if (!ValidatePlayerName(payload, out rejectionReason)) {
                return false;
            }

            // Server capacity check
            if (!ValidateCapacity(payload, out rejectionReason)) {
                return false;
            }

            // Password check
            if (!ValidatePassword(payload, out rejectionReason)) {
                return false;
            }

            // Custom validation
            if (!ValidateCustom(payload, out rejectionReason)) {
                return false;
            }

            Debug.Log($"[SessionManager] Validation passed for player: {payload.playerName}");
            return true;
        }

        /// <summary>
        /// Override to customize protocol version validation.
        /// </summary>
        protected virtual bool ValidateProtocolVersion(ConnectionPayload payload, out ConnectionStatus rejectionReason) {
            rejectionReason = ConnectionStatus.Success;

            if (payload.protocolVersion != CurrentSession.ProtocolVersion) {
                rejectionReason = ConnectionStatus.ProtocolMismatch;
                Debug.LogWarning($"[SessionManager] Protocol mismatch (client: {payload.protocolVersion}, server: {CurrentSession.ProtocolVersion})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Override to customize player name validation.
        /// </summary>
        protected virtual bool ValidatePlayerName(ConnectionPayload payload, out ConnectionStatus rejectionReason) {
            rejectionReason = ConnectionStatus.Success;

            if (string.IsNullOrWhiteSpace(payload.playerName)) {
                rejectionReason = ConnectionStatus.InvalidPlayerName;
                Debug.LogWarning("[SessionManager] Empty player name");
                return false;
            }

            if (payload.playerName.Length > 32) {
                rejectionReason = ConnectionStatus.InvalidPlayerName;
                Debug.LogWarning("[SessionManager] Player name too long");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Override to customize capacity validation.
        /// </summary>
        protected virtual bool ValidateCapacity(ConnectionPayload payload, out ConnectionStatus rejectionReason) {
            rejectionReason = ConnectionStatus.Success;

            if (CurrentSession.IsFull) {
                rejectionReason = ConnectionStatus.ServerFull;
                Debug.LogWarning($"[SessionManager] Server full ({CurrentSession.CurrentPlayerCount}/{CurrentSession.MaxPlayers})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Override to customize password validation.
        /// </summary>
        protected virtual bool ValidatePassword(ConnectionPayload payload, out ConnectionStatus rejectionReason) {
            rejectionReason = ConnectionStatus.Success;

            if (!CurrentSession.ValidatePassword(payload.password)) {
                rejectionReason = ConnectionStatus.IncorrectPassword;
                Debug.LogWarning("[SessionManager] Incorrect password");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Override to add custom validation logic.
        /// Called after all standard validations pass.
        /// </summary>
        protected virtual bool ValidateCustom(ConnectionPayload payload, out ConnectionStatus rejectionReason) {
            rejectionReason = ConnectionStatus.Success;
            return true;
        }
    }
}
