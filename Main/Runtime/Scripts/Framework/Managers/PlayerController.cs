namespace Majinfwork.World {
    /// <summary>
    /// Central controller for a player. Holds references to Input (1:1), State (1:1), and CurrentPawn (1:many).
    /// </summary>
    public class PlayerController : Actor {
        /// <summary>Player input handler (1:1 relationship)</summary>
        public PlayerInput Input { get; private set; }

        /// <summary>Player state data (1:1 relationship)</summary>
        public PlayerState State { get; private set; }

        /// <summary>Currently possessed pawn (1:many, can switch)</summary>
        public PlayerPawn CurrentPawn { get; private set; }

        /// <summary>Index of this player (0 = first player)</summary>
        public int PlayerIndex { get; internal set; }

        /// <summary>
        /// Initializes the controller with its components.
        /// Called by GameModeManager during player spawn.
        /// </summary>
        internal void Initialize(PlayerInput input, PlayerState state, PlayerPawn initialPawn) {
            Input = input;
            State = state;
            Possess(initialPawn);
        }

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
    }
}
