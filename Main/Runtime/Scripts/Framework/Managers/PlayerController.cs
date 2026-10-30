using System;

namespace Majinfwork.World {
    /// <summary>
    /// Central controller for a player. Persists across GameMode transitions.
    /// Controller + State are persistent. Input and Pawn are equipped per GameMode. The game's UI is the HUD's, not a player's.
    /// </summary>
    public class PlayerController : Actor {
        private PlayerState localState;

        /// <summary>Player input handler (per GameMode)</summary>
        public PlayerInput Input { get; private set; }

        /// <summary>
        /// The player's state: in a network session, the online one this device plays; otherwise the local one made with
        /// this controller (none in a game whose players' states all come from its sessions).
        /// </summary>
        public PlayerState State { get; private set; }

        /// <summary>Raised when <see cref="State"/> changes: the player's online state became theirs for a session, or went with it.</summary>
        public event Action StateChanged;

        /// <summary>The local state made with this controller (null when the game has none).</summary>
        internal PlayerState LocalState => localState;

        /// <summary>Currently possessed pawn (per GameMode)</summary>
        public PlayerPawn CurrentPawn { get; private set; }

        /// <summary>Index of this player (0 = first player)</summary>
        public int PlayerIndex { get; internal set; }

        /// <summary>
        /// Initializes persistent components (Controller + State).
        /// Called once when the player is first created.
        /// </summary>
        internal void InitializePersistent(PlayerState state) {
            localState = state;
            State = state;
        }

        /// <summary>This device plays <paramref name="online"/> for the session.</summary>
        internal void BindOnline(PlayerState online) {
            if (State == online) return;

            State = online;
            StateChanged?.Invoke();
        }

        /// <summary><paramref name="online"/> is no longer this device's (it left, or the session ended): back to the local state.</summary>
        internal void UnbindOnline(PlayerState online) {
            if (State != online) return;

            State = localState;
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Sets up GameMode-owned components (Input, Pawn).
        /// Called each time a new GameMode activates.
        /// </summary>
        internal void SetupForGameMode(PlayerInput input, PlayerPawn pawn) {
            Input = input;
            Possess(pawn);
            OnGameModeSetup();
        }

        /// <summary>
        /// Cleans up GameMode-owned components. Called when a GameMode deactivates.
        /// </summary>
        internal void CleanupFromGameMode() {
            if (CurrentPawn != null) {
                OnUnPossess(CurrentPawn);
                CurrentPawn = null;
            }
            Input = null;
            OnGameModeCleanup();
        }

        /// <summary>Called after GameMode-owned components are set up. Override for custom setup.</summary>
        protected virtual void OnGameModeSetup() { }

        /// <summary>Called after GameMode-owned components are cleaned up. Override for custom cleanup.</summary>
        protected virtual void OnGameModeCleanup() { }

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
