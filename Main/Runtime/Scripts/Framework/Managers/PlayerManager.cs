using System.Collections.Generic;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// Owns all persistent PlayerControllers and PlayerStates.
    /// These persist across GameMode transitions — only Pawn, Input, HUD are per-GameMode.
    /// In a network session it also keeps every online player's state, for as long as the session spawns it.
    /// </summary>
    internal static class PlayerManager {
        private static readonly List<PlayerController> players = new();
        private static readonly List<PlayerState> onlinePlayers = new();

        private static PlayerController controllerPrefab;
        private static PlayerState statePrefab;

        public static int PlayerCount => players.Count;
        public static IReadOnlyList<PlayerController> AllPlayers => players;

        /// <summary>Every online player's state on this peer (empty outside a network session).</summary>
        public static IReadOnlyList<PlayerState> OnlinePlayers => onlinePlayers;

        /// <summary>Each player's state in a network session, spawned by the server's GameMode for every player who joins.</summary>
        public static PlayerState NetworkStatePrefab { get; set; }

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
            Object.DontDestroyOnLoad(controller.gameObject);

            // A game whose players' states all come from its network sessions has no local one.
            var state = statePrefab != null ? Object.Instantiate(statePrefab) : null;
            if (state != null) {
                state.OnCreated();
                Object.DontDestroyOnLoad(state.gameObject);
            }

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

            if (controller.LocalState != null) Object.Destroy(controller.LocalState.gameObject);
            Object.Destroy(controller.gameObject);
        }

        /// <summary>
        /// Destroys all players. Called during game shutdown.
        /// </summary>
        public static void DestroyAll() {
            for (int i = players.Count - 1; i >= 0; i--) {
                var controller = players[i];
                if (controller == null) continue;

                if (controller.LocalState != null) Object.Destroy(controller.LocalState.gameObject);
                Object.Destroy(controller.gameObject);
            }
            players.Clear();
        }

        /// <summary>The online player <paramref name="clientId"/> plays, or null.</summary>
        public static PlayerState FindOnline(ulong clientId) {
#if HAS_NETCODE
            foreach (PlayerState state in onlinePlayers) {
                if (state.IsConnected && state.ClientId == clientId) return state;
            }
#endif
            return null;
        }

        /// <summary>An online player's state spawned here.</summary>
        internal static void AddOnline(PlayerState state) {
            onlinePlayers.Add(state);
            RefreshOnline(state);
        }

        /// <summary>An online player's state went (the player left for good, or the session ended).</summary>
        internal static void RemoveOnline(PlayerState state) {
            onlinePlayers.Remove(state);
            GetPlayer(0)?.UnbindOnline(state);
        }

        /// <summary>The first local player plays the online state of this device's client (one local player per client online).</summary>
        internal static void RefreshOnline(PlayerState state) {
#if HAS_NETCODE
            PlayerController local = GetPlayer(0);
            if (local == null) return;

            if (state.IsLocalPlayer) local.BindOnline(state);
            else local.UnbindOnline(state);
#endif
        }

        /// <summary>
        /// Clears references without destroying objects. Only for edge cases.
        /// </summary>
        public static void Clear() {
            players.Clear();
        }
    }
}
