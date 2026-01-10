using System.Collections.Generic;

namespace Majinfwork.World {
    /// <summary>
    /// Tracks all active PlayerControllers by index.
    /// </summary>
    internal static class PlayerManager {
        private static readonly List<PlayerController> _players = new();

        public static int PlayerCount => _players.Count;
        public static IReadOnlyList<PlayerController> AllPlayers => _players;

        public static void RegisterPlayer(PlayerController controller) {
            if (controller == null || _players.Contains(controller)) return;

            controller.PlayerIndex = _players.Count;
            _players.Add(controller);
        }

        public static void UnregisterPlayer(PlayerController controller) {
            if (controller == null) return;

            int index = _players.IndexOf(controller);
            if (index < 0) return;

            _players.RemoveAt(index);

            // Re-index remaining players
            for (int i = index; i < _players.Count; i++) {
                _players[i].PlayerIndex = i;
            }
        }

        public static PlayerController GetPlayer(int index = 0) {
            if (index < 0 || index >= _players.Count) return null;
            return _players[index];
        }

        public static T GetPlayer<T>(int index = 0) where T : PlayerController {
            return GetPlayer(index) as T;
        }

        public static void Clear() {
            _players.Clear();
        }
    }
}
