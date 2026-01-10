using System;
using System.Collections.Generic;
using System.Net;

namespace Majinfwork.Network {
    /// <summary>
    /// Represents a discovered LAN session.
    /// Use Metadata dictionary to store custom session information from DiscoveryResponseData.
    /// </summary>
    public class DiscoveredSession {
        public IPEndPoint EndPoint { get; }
        public string SessionName { get; }
        public int CurrentPlayers { get; internal set; }
        public int MaxPlayers { get; }
        public bool HasPassword { get; }
        public int ProtocolVersion { get; }
        public int MapIndex { get; }
        public DateTime LastSeen { get; internal set; }

        /// <summary>
        /// Custom metadata dictionary for developer extensions.
        /// Populated from DiscoveryResponseData.customDataJson if present.
        /// </summary>
        public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Raw custom data JSON from discovery response.
        /// Use GetCustomData&lt;T&gt;() to deserialize.
        /// </summary>
        public string CustomDataJson { get; internal set; }

        public bool IsFull => CurrentPlayers >= MaxPlayers;
        public string Address => EndPoint.Address.ToString();
        public ushort Port => (ushort)EndPoint.Port;

        public DiscoveredSession(
            IPEndPoint endPoint,
            string sessionName,
            int currentPlayers,
            int maxPlayers,
            bool hasPassword,
            int protocolVersion,
            int mapIndex,
            string customDataJson = null) {
            EndPoint = endPoint;
            SessionName = sessionName;
            CurrentPlayers = currentPlayers;
            MaxPlayers = maxPlayers;
            HasPassword = hasPassword;
            ProtocolVersion = protocolVersion;
            MapIndex = mapIndex;
            CustomDataJson = customDataJson;
            LastSeen = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets custom data that was sent from the server.
        /// </summary>
        public T GetCustomData<T>() where T : class {
            if (string.IsNullOrEmpty(CustomDataJson)) return null;
            try {
                return UnityEngine.JsonUtility.FromJson<T>(CustomDataJson);
            }
            catch {
                return null;
            }
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

        public bool IsCompatible(int clientProtocolVersion) {
            return ProtocolVersion == clientProtocolVersion;
        }

        public override string ToString() {
            return $"{SessionName} @ {Address}:{Port} ({CurrentPlayers}/{MaxPlayers}){(HasPassword ? " [Password]" : "")}";
        }

        public override bool Equals(object obj) {
            if (obj is DiscoveredSession other) {
                return EndPoint.Equals(other.EndPoint);
            }
            return false;
        }

        public override int GetHashCode() {
            return EndPoint.GetHashCode();
        }
    }
}
