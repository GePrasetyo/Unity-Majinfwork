#if HAS_NETCODE
using Unity.Netcode;
#endif

namespace Majinfwork.World {
    /// <summary>
    /// A player's state: who they are and what they hold in the game. The GameMode makes it with their PlayerController when
    /// the player logs in and removes it when they leave or the GameMode ends: it lives as long as its GameMode, so it holds
    /// nothing that must outlast it. Offline it is this device's alone. In a network session the server spawns it for every
    /// peer: every peer has them all (<see cref="PlayerAccessor.OnlinePlayers"/>), and the one this device plays is its
    /// PlayerController's state. An online state's subclass overriding the Netcode spawn callbacks calls the base ones first.
    /// </summary>
    public class PlayerState : NetworkActor {
#if HAS_NETCODE
        /// <summary>No client plays this player: they left, and the game keeps their state for their return.</summary>
        public const ulong NoClient = ulong.MaxValue;

        private readonly NetworkVariable<ulong> clientId = new NetworkVariable<ulong>(NoClient);

        /// <summary>The client this online player plays from (<see cref="NoClient"/> while they are away).</summary>
        public ulong ClientId => clientId.Value;

        /// <summary>A client plays this online player now.</summary>
        public bool IsConnected => clientId.Value != NoClient;

        /// <summary>This device plays this player: it is a local player's state, or the online one of this device's client.</summary>
        public bool IsLocalPlayer => !IsSpawned || (IsConnected && ClientId == NetworkManager.LocalClientId);

        public override void OnNetworkSpawn() {
            if (IsServer && !IsConnected) {
                // Spawned for the player who joined, owned by their client.
                clientId.Value = OwnerClientId;
            }

            clientId.OnValueChanged += OnClientIdChanged;
            PlayerManager.AddOnline(this);
        }

        public override void OnNetworkDespawn() {
            clientId.OnValueChanged -= OnClientIdChanged;
            PlayerManager.RemoveOnline(this);
        }

        /// <summary>Every peer: the client playing this player changed (one went away, or came back on a new connection).</summary>
        protected virtual void OnClientChanged() { }

        /// <summary>Server: <paramref name="client"/> plays this player now (one coming back on a new connection), and owns it.</summary>
        internal void AssignClient(ulong client) {
            if (OwnerClientId != client) {
                NetworkObject.ChangeOwnership(client);
            }

            clientId.Value = client;
        }

        /// <summary>Server: nobody plays this player for now (they left; the game keeps their state for their return).</summary>
        internal void ReleaseClient() {
            clientId.Value = NoClient;
        }

        private void OnClientIdChanged(ulong previous, ulong current) {
            PlayerManager.RefreshOnline(this);
            OnClientChanged();
        }
#endif
    }
}
