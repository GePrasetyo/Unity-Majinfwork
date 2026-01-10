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
        // Main Player Pawn (convenience for main/first player)
        // ─────────────────────────────────────────────────────────────────

        public static T GetMainPlayerPawn<T>() where T : PlayerPawn {
            return GetPlayerController(0)?.GetCurrentPawn<T>();
        }

        public static bool TryGetMainPlayerPawn<T>(out T pawn) where T : PlayerPawn {
            pawn = GetMainPlayerPawn<T>();
            return pawn != null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Main Player Input (convenience for main/first player)
        // ─────────────────────────────────────────────────────────────────

        public static T GetMainPlayerInput<T>() where T : PlayerInput {
            return GetPlayerController(0)?.GetCurrentInput<T>();
        }

        public static bool TryGetMainPlayerInput<T>(out T input) where T : PlayerInput {
            input = GetMainPlayerInput<T>();
            return input != null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Main Player State (convenience for main/first player)
        // ─────────────────────────────────────────────────────────────────

        public static T GetMainPlayerState<T>() where T : PlayerState {
            return GetPlayerController(0)?.GetCurrentState<T>();
        }

        public static bool TryGetMainPlayerState<T>(out T state) where T : PlayerState {
            state = GetMainPlayerState<T>();
            return state != null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Utilities
        // ─────────────────────────────────────────────────────────────────

        public static int GetPlayerCount() {
            return PlayerManager.PlayerCount;
        }

        public static PlayerController GetMainPlayerController() {
            return GetPlayerController(0);
        }
    }
}
