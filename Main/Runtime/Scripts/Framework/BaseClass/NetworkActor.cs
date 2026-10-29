using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// An actor a network game shares between its peers. With Netcode for GameObjects in the project it is a
    /// NetworkBehaviour: in a session the server spawns it and every peer receives it, its state replicated (its prefab
    /// carries a NetworkObject, registered with the NetworkManager). Outside a session it is an actor on this device alone.
    /// </summary>
#if HAS_NETCODE
    public abstract class NetworkActor : Unity.Netcode.NetworkBehaviour {
    }
#else
    public abstract class NetworkActor : MonoBehaviour {
    }
#endif
}
