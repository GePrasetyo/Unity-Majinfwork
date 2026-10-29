#if HAS_NETCODE
using Unity.Netcode;
#endif

namespace Majinfwork.World {
    /// <summary>
    /// A player's state: who they are and what they hold in the game. It is a local player's or an online player's.
    /// <para>
    /// A local player's comes with its PlayerController, made at boot and kept for the whole game (the world settings'
    /// player state). In a network session every player's is spawned by the server's GameMode when they join and kept for
    /// the whole session, through every level: every peer has them all (<see cref="PlayerAccessor.OnlinePlayers"/>), and
    /// the one this device plays is its PlayerController's state while the session lasts. An online state's subclass
    /// overriding the Netcode spawn callbacks calls the base ones first.
    /// </para>
    /// </summary>
    public class PlayerState : NetworkActor {
        /// <summary>
        /// Called by PlayerManager on a local player's state after instantiation, before pawn creation.
        /// Override to initialize from persistent data (GameInstance, save system, etc.).
        /// </summary>
        public virtual void OnCreated() { }

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

        /// <summary>This device's player: its device has started the GameMode of <paramref name="level"/>, so the player is there.</summary>
        internal void Arrive(string level) {
            ArrivedRpc(level);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void ArrivedRpc(string level) {
            GameModeManager gameMode = ServiceLocator.Resolve<LevelManager>()?.CurrentGameMode;
            if (gameMode != null) {
                gameMode.PlayerArrived(this, level);
            }
        }

        private void OnClientIdChanged(ulong previous, ulong current) {
            PlayerManager.RefreshOnline(this);
            OnClientChanged();
        }
#endif
    }
}
