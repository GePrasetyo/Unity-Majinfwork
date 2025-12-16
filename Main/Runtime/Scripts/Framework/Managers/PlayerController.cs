namespace Majinfwork.World {
    public class PlayerController : Actor {
        protected static bool GetPawn<P>(out P pawn) where P : PlayerPawn {
            pawn = GameModeManager.playerReference?.GetPawn() as P;
            return pawn != null;
        }

        protected static bool GetState<S>(out S state) where S : PlayerState {
            state = GameModeManager.playerReference?.GetState() as S;
            return state != null;
        }

        protected static bool GetInput<I>(out I input) where I : PlayerInput {
            input = GameModeManager.playerReference?.GetInput() as I;
            return input != null;
        }
    }
}