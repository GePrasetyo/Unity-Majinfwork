using Unity.Netcode;

namespace Majinfwork.Network {
    public class DiscoveryBroadcastData : INetworkSerializable {
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        }
    }
}
