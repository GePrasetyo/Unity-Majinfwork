using System.Collections.Generic;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// Owns all persistent PlayerControllers and PlayerStates.
    /// These persist across GameMode transitions — only Pawn, Input, HUD are per-GameMode.
    /// </summary>
    internal static class PlayerManager {
        private static readonly List<PlayerController> players = new();

        private static PlayerController controllerPrefab;
        private static PlayerState statePrefab;

        public static int PlayerCount => players.Count;
        public static IReadOnlyList<PlayerController> AllPlayers => players;

        /// <summary>
        /// Called by GameWorldSettings during boot to provide persistent player prefabs.
        /// </summary>
        public static void Initialize(PlayerController controllerPrefab, PlayerState statePrefab) {
            PlayerManager.controllerPrefab = controllerPrefab;
            PlayerManager.statePrefab = statePrefab;
        }

        /// <summary>
        /// Creates a new persistent player (Controller + State).
        /// Called by GameWorldSettings during boot, or for additional players joining later.
        /// </summary>
        public static PlayerController SpawnPlayer(int? playerIndex = null) {
            int index = playerIndex ?? players.Count;

            var controller = Object.Instantiate(controllerPrefab);
            var state = Object.Instantiate(statePrefab);
            state.OnCreated();

            Object.DontDestroyOnLoad(controller.gameObject);
            Object.DontDestroyOnLoad(state.gameObject);

            controller.InitializePersistent(state);
            RegisterPlayer(controller);

            return controller;
        }

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

        /// <summary>
        /// Fully destroys a player — Controller, State, and any possessed Pawn/Input/HUD.
        /// Use for explicit player removal (player disconnect, game quit).
        /// </summary>
        public static void DestroyPlayer(PlayerController controller) {
            if (controller == null) return;

            UnregisterPlayer(controller);

            if (controller.State != null) Object.Destroy(controller.State.gameObject);
            Object.Destroy(controller.gameObject);
        }

        /// <summary>
        /// Destroys all players. Called during game shutdown.
        /// </summary>
        public static void DestroyAll() {
            for (int i = players.Count - 1; i >= 0; i--) {
                var controller = players[i];
                if (controller == null) continue;

                if (controller.State != null) Object.Destroy(controller.State.gameObject);
                Object.Destroy(controller.gameObject);
            }
            players.Clear();
        }

        /// <summary>
        /// Clears references without destroying objects. Only for edge cases.
        /// </summary>
        public static void Clear() {
            players.Clear();
        }
    }
}
