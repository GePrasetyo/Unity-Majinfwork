using System;
using UnityEngine;
#if HAS_NETCODE
using Unity.Netcode;
#endif

namespace Majinfwork.World {
    /// <summary>
    /// A player's controller: the player's will in the game. The GameMode makes it with their PlayerState when the player
    /// logs in and removes both when the player leaves or the GameMode ends: it lives exactly as long as its GameMode, so it
    /// holds nothing that must outlast it. On the device that plays it (offline, or its owner in a network session) it is
    /// local: it has the player's camera and input, and <see cref="PlayerAccessor"/> finds it. In a network session it is a
    /// network object only its owner and the server have; the server's copy is where the player's device talks to the game
    /// (its arrival, a subclass's requests, as RPCs), and the GameMode listens to it there. A subclass overriding the Netcode
    /// spawn callbacks calls the base ones first.
    /// </summary>
    public class PlayerController : NetworkActor {
        [Tooltip("Optional: the player's input, made on the device that plays them (none: the game reads its input elsewhere, such as an XR rig).")]
        [SerializeField] private PlayerInput playerInputPrefab;
        [Tooltip("Optional: the player's camera, on the device that plays them (none: the game has its own).")]
        [SerializeReference, ClassReference] private CameraHandler cameraHandler;

        private bool local;

        /// <summary>The player's input on the device that plays them, or null.</summary>
        public PlayerInput Input { get; private set; }

        /// <summary>
        /// The player's state: given by the GameMode with this controller; on the owner's device in a session, its client's
        /// online state once that is here (<see cref="StateChanged"/>).
        /// </summary>
        public PlayerState State { get; private set; }

        /// <summary>Raised when <see cref="State"/> changes.</summary>
        public event Action StateChanged;

        /// <summary>The pawn this controller possesses, or null.</summary>
        public PlayerPawn CurrentPawn { get; private set; }

        /// <summary>Index of this local player on its device (0 = first player).</summary>
        public int PlayerIndex { get; internal set; }

        /// <summary>This device plays this player (offline, or its owner in a session).</summary>
        public bool IsLocalController => local;

        internal void SetState(PlayerState state) {
            if (State == state) return;

            State = state;
            StateChanged?.Invoke();
        }

        /// <summary>This device plays the player from now on: offline when the GameMode makes it, in a session on its owner's spawn.</summary>
        internal void BeginLocal() {
            if (local) return;

            local = true;
            cameraHandler?.Construct();
            if (playerInputPrefab != null) {
                Input = Instantiate(playerInputPrefab);
                DontDestroyOnLoad(Input.gameObject);
            }

            PlayerManager.AddLocal(this);
            OnLocalStart();
        }

        private void EndLocal() {
            if (!local) return;

            local = false;
            OnLocalStop();
            PlayerManager.RemoveLocal(this);
            UnPossess();
            if (Input != null) {
                Destroy(Input.gameObject);
                Input = null;
            }

            cameraHandler?.Deconstruct();
        }

        /// <summary>This device plays the player now (camera and input are up). Override to start what only the player's device does.</summary>
        protected virtual void OnLocalStart() { }

        /// <summary>This device stops playing the player (the controller is going). Override to stop what <see cref="OnLocalStart"/> started.</summary>
        protected virtual void OnLocalStop() { }

#if HAS_NETCODE
        /// <summary>Server: the player's device has loaded the level it names, on the server's copy of their controller.</summary>
        internal event Action<PlayerController, string> Arrived;

        public override void OnNetworkSpawn() {
            if (IsOwner) {
                BeginLocal();
                // The level may have loaded here before this controller came.
                LevelLoaded(ServiceLocator.Resolve<LevelManager>()?.CurrentSceneName);
            }
        }

        public override void OnNetworkDespawn() {
            EndLocal();
        }

        public override void OnDestroy() {
            EndLocal();
            base.OnDestroy();
        }

        /// <summary>The owner's device has loaded <paramref name="level"/>: the server learns the player is there (a notice naming a level it isn't running is ignored).</summary>
        internal void LevelLoaded(string level) {
            if (IsSpawned && IsOwner && !string.IsNullOrEmpty(level)) {
                ArrivedRpc(level);
            }
        }

        // Deferred on the host, so its own player arrives after the GameMode has finished logging them in.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, DeferLocal = true)]
        private void ArrivedRpc(string level) {
            Arrived?.Invoke(this, level);
        }
#else
        protected virtual void OnDestroy() {
            EndLocal();
        }

        internal void LevelLoaded(string level) { }
#endif

        /// <summary>
        /// Possess a pawn, making it the current controlled pawn.
        /// </summary>
        public virtual void Possess(PlayerPawn pawn) {
            if (pawn == null) return;

            if (CurrentPawn != null) {
                OnUnPossess(CurrentPawn);
            }

            CurrentPawn = pawn;
            OnPossess(pawn);
        }

        /// <summary>
        /// Release control of the current pawn.
        /// </summary>
        public virtual void UnPossess() {
            if (CurrentPawn == null) return;

            OnUnPossess(CurrentPawn);
            CurrentPawn = null;
        }

        /// <summary>Called when a pawn is possessed. Override for custom logic.</summary>
        protected virtual void OnPossess(PlayerPawn pawn) { }

        /// <summary>Called when a pawn is unpossessed. Override for custom logic.</summary>
        protected virtual void OnUnPossess(PlayerPawn pawn) { }

        /// <summary>Get the current pawn cast to a specific type.</summary>
        protected T GetPawn<T>() where T : PlayerPawn => CurrentPawn as T;

        /// <summary>Get the input cast to a specific type.</summary>
        protected T GetInput<T>() where T : PlayerInput => Input as T;

        /// <summary>Get the state cast to a specific type.</summary>
        protected T GetState<T>() where T : PlayerState => State as T;

        /// <summary>Get the current pawn cast to a specific type (public access).</summary>
        public T GetCurrentPawn<T>() where T : PlayerPawn => CurrentPawn as T;

        /// <summary>Get the input cast to a specific type (public access).</summary>
        public T GetCurrentInput<T>() where T : PlayerInput => Input as T;

        /// <summary>Get the state cast to a specific type (public access).</summary>
        public T GetCurrentState<T>() where T : PlayerState => State as T;
    }
}
