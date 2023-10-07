using Unity.Netcode;

namespace Majingari.Network {
    public class DiscoveryBroadcastData : INetworkSerializable {
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        }
    }
}
