using System.Collections.Generic;

namespace Majinfwork.World {
    /// <summary>
    /// Tracks all active PlayerControllers by index.
    /// </summary>
    internal static class PlayerManager {
        private static readonly List<PlayerController> players = new();

        public static int PlayerCount => players.Count;
        public static IReadOnlyList<PlayerController> AllPlayers => players;

        public static void RegisterPlayer(PlayerController controller) {
            if (controller == null || players.Contains(controller)) return;

            controller.PlayerIndex = players.Count;
            players.Add(controller);
        }

        public static void UnregisterPlayer(PlayerController controller) {
            if (controller == null) return;

            int index = players.IndexOf(controller);
            if (index < 0) return;

            players.RemoveAt(index);

            // Re-index remaining players
            for (int i = index; i < players.Count; i++) {
                players[i].PlayerIndex = i;
            }
        }

        public static PlayerController GetPlayer(int index = 0) {
            if (index < 0 || index >= players.Count) return null;
            return players[index];
        }

        public static T GetPlayer<T>(int index = 0) where T : PlayerController {
            return GetPlayer(index) as T;
        }

        public static void Clear() {
            players.Clear();
        }
    }
}
