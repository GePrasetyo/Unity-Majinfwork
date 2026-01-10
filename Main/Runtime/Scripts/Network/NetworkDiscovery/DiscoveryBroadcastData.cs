using Unity.Netcode;

namespace Majinfwork.Network {
    public class DiscoveryBroadcastData : INetworkSerializable {
        public int protocolVersion;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref protocolVersion);
        }
    }
}
