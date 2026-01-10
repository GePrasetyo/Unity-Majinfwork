namespace Majinfwork.World {
    /// <summary>
    /// Static utility class for easy access to player components.
    /// </summary>
    public static class PlayerAccessor {

        // ─────────────────────────────────────────────────────────────────
        // Player Controller
        // ─────────────────────────────────────────────────────────────────

        public static PlayerController GetPlayerController(int index = 0) {
            return PlayerManager.GetPlayer(index);
        }

        public static T GetPlayerController<T>(int index = 0) where T : PlayerController {
            return PlayerManager.GetPlayer(index) as T;
        }

        public static bool TryGetPlayerController<T>(out T controller, int index = 0) where T : PlayerController {
            controller = GetPlayerController<T>(index);
            return controller != null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Player Pawn
        // ─────────────────────────────────────────────────────────────────

        public static PlayerPawn GetPlayerPawn(int index = 0) {
            return PlayerManager.GetPlayer(index)?.CurrentPawn;
        }

        public static T GetPlayerPawn<T>(int index = 0) where T : PlayerPawn {
            return GetPlayerPawn(index) as T;
        }

        public static bool TryGetPlayerPawn<T>(out T pawn, int index = 0) where T : PlayerPawn {
            pawn = GetPlayerPawn<T>(index);
            return pawn != null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Player Input
        // ─────────────────────────────────────────────────────────────────

        public static PlayerInput GetPlayerInput(int index = 0) {
            return PlayerManager.GetPlayer(index)?.Input;
        }

        public static T GetPlayerInput<T>(int index = 0) where T : PlayerInput {
            return GetPlayerInput(index) as T;
        }

        public static bool TryGetPlayerInput<T>(out T input, int index = 0) where T : PlayerInput {
            input = GetPlayerInput<T>(index);
            return input != null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Player State
        // ─────────────────────────────────────────────────────────────────

        public static PlayerState GetPlayerState(int index = 0) {
            return PlayerManager.GetPlayer(index)?.State;
        }

        public static T GetPlayerState<T>(int index = 0) where T : PlayerState {
            return GetPlayerState(index) as T;
        }

        public static bool TryGetPlayerState<T>(out T state, int index = 0) where T : PlayerState {
            state = GetPlayerState<T>(index);
            return state != null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Utilities
        // ─────────────────────────────────────────────────────────────────

        public static int GetPlayerCount() {
            return PlayerManager.PlayerCount;
        }

        public static PlayerController GetFirstPlayerController() {
            return GetPlayerController(0);
        }
    }
}
