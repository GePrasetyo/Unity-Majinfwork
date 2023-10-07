using Unity.Netcode;

namespace Majingari.Network {
    public class DiscoveryResponseData : INetworkSerializable {
        public ushort port;
        public string serverName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref port);
            serializer.SerializeValue(ref serverName);
        }
    }
}
