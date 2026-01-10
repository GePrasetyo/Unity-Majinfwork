using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majinfwork.Network {
    /// <summary>
    /// Data sent from client to server during connection.
    /// Use CustomData dictionary to send additional information.
    /// </summary>
    [Serializable]
    public class ConnectionPayload {
        public string clientGUID;
        public string clientScene;
        public string playerName;
        public int protocolVersion;
        public string password;

        /// <summary>
        /// Custom string data that can be sent over network.
        /// Use this for additional connection parameters.
        /// Values must be serializable to JSON (primitives, strings, simple objects).
        /// </summary>
        public string customDataJson;

        /// <summary>
        /// Creates a new connection payload with the specified parameters.
        /// </summary>
        public static ConnectionPayload Create(string playerName, int protocolVersion, string password = null) {
            return new ConnectionPayload {
                clientGUID = Guid.NewGuid().ToString(),
                clientScene = SceneManager.GetActiveScene().name,
                playerName = playerName,
                protocolVersion = protocolVersion,
                password = password,
                customDataJson = null
            };
        }

        /// <summary>
        /// Sets custom data that will be serialized and sent to server.
        /// </summary>
        public void SetCustomData<T>(T data) where T : class {
            customDataJson = JsonUtility.ToJson(data);
        }

        /// <summary>
        /// Gets custom data that was sent from client.
        /// Returns null if no custom data or deserialization fails.
        /// </summary>
        public T GetCustomData<T>() where T : class {
            if (string.IsNullOrEmpty(customDataJson)) return null;
            try {
                return JsonUtility.FromJson<T>(customDataJson);
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// Serializes the payload to bytes for network transmission.
        /// </summary>
        public byte[] ToBytes() {
            var json = JsonUtility.ToJson(this);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Deserializes a payload from bytes.
        /// </summary>
        public static ConnectionPayload FromBytes(byte[] data) {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<ConnectionPayload>(json);
        }

        /// <summary>
        /// Safely attempts to deserialize a payload from bytes.
        /// </summary>
        public static bool TryFromBytes(byte[] data, out ConnectionPayload payload) {
            try {
                payload = FromBytes(data);
                return payload != null;
            }
            catch {
                payload = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Example custom connection data class.
    /// Developers can create their own classes following this pattern.
    /// </summary>
    [Serializable]
    public class CustomConnectionData {
        public string skinId;
        public string teamId;
        public int characterIndex;

        // Add any serializable fields you need
    }
}
