using Unity.Netcode;

namespace Majinfwork.Network {
    /// <summary>
    /// Data sent in response to a discovery broadcast.
    /// Use customDataJson to send additional session information.
    /// </summary>
    public class DiscoveryResponseData : INetworkSerializable {
        public ushort port;
        public string serverName;
        public int currentPlayers;
        public int maxPlayers;
        public bool hasPassword;
        public int protocolVersion;
        public int mapIndex;

        /// <summary>
        /// Custom JSON data for developer extensions.
        /// Serialize any additional session info you need to broadcast.
        /// </summary>
        public string customDataJson;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref port);
            serializer.SerializeValue(ref serverName);
            serializer.SerializeValue(ref currentPlayers);
            serializer.SerializeValue(ref maxPlayers);
            serializer.SerializeValue(ref hasPassword);
            serializer.SerializeValue(ref protocolVersion);
            serializer.SerializeValue(ref mapIndex);
            serializer.SerializeValue(ref customDataJson);
        }

        /// <summary>
        /// Sets custom data that will be broadcast to searching clients.
        /// </summary>
        public void SetCustomData<T>(T data) where T : class {
            customDataJson = UnityEngine.JsonUtility.ToJson(data);
        }

        /// <summary>
        /// Gets custom data from the response.
        /// </summary>
        public T GetCustomData<T>() where T : class {
            if (string.IsNullOrEmpty(customDataJson)) return null;
            try {
                return UnityEngine.JsonUtility.FromJson<T>(customDataJson);
            }
            catch {
                return null;
            }
        }
    }

    /// <summary>
    /// Example custom discovery data class.
    /// Developers can create their own classes following this pattern.
    /// </summary>
    [System.Serializable]
    public class CustomDiscoveryData {
        public string gameMode;
        public string mapName;
        public int ping;
        public string region;

        // Add any serializable fields you need
    }
}
