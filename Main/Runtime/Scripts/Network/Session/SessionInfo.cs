using System;
using System.Collections.Generic;

namespace Majinfwork.Network {
    /// <summary>
    /// Runtime information about an active session.
    /// Developers can extend this class to add custom session data.
    /// </summary>
    public class SessionInfo {
        public string SessionId { get; }
        public string SessionName { get; protected set; }
        public int MaxPlayers { get; protected set; }
        public int CurrentPlayerCount { get; internal set; }
        public bool HasPassword { get; protected set; }
        public int MapIndex { get; protected set; }
        public int ProtocolVersion { get; protected set; }
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Custom metadata dictionary for developer extensions.
        /// Use this to store additional session data.
        /// </summary>
        public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        internal string Password { get; }

        public bool IsFull => CurrentPlayerCount >= MaxPlayers;

        public SessionInfo(SessionSettings settings, int protocolVersion) {
            SessionId = Guid.NewGuid().ToString();
            SessionName = string.IsNullOrEmpty(settings.sessionName)
                ? $"Session {UnityEngine.Random.Range(1000, 9999)}"
                : settings.sessionName;
            MaxPlayers = settings.maxPlayers;
            CurrentPlayerCount = 0;
            HasPassword = settings.HasPassword;
            Password = settings.password;
            MapIndex = settings.mapIndex;
            ProtocolVersion = protocolVersion;
            CreatedAt = DateTime.UtcNow;

            OnCreated(settings);
        }

        /// <summary>
        /// Protected constructor for derived classes.
        /// </summary>
        protected SessionInfo() {
            SessionId = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Called after the session is created. Override to add custom initialization.
        /// </summary>
        protected virtual void OnCreated(SessionSettings settings) { }

        /// <summary>
        /// Validates the provided password against the session password.
        /// Override to implement custom password validation (e.g., hashing).
        /// </summary>
        public virtual bool ValidatePassword(string inputPassword) {
            if (!HasPassword) return true;
            return Password == inputPassword;
        }

        /// <summary>
        /// Gets a metadata value or default if not found.
        /// </summary>
        public T GetMetadata<T>(string key, T defaultValue = default) {
            if (!Metadata.TryGetValue(key, out var value)) {
                return defaultValue;
            }
            return value is T typedValue ? typedValue : defaultValue;
        }

        /// <summary>
        /// Sets a metadata value.
        /// </summary>
        public void SetMetadata<T>(string key, T value) {
            Metadata[key] = value;
        }

        public override string ToString() {
            return $"{SessionName} ({CurrentPlayerCount}/{MaxPlayers}){(HasPassword ? " [Password]" : "")}";
        }
    }
}
