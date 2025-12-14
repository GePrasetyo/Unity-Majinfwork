using Unity.Netcode;

namespace Majinfwork.Network {
    public class DiscoveryResponseData : INetworkSerializable {
        public ushort port;
        public string serverName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref port);
            serializer.SerializeValue(ref serverName);
        }
    }
}
