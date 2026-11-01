using System.Collections.Generic;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// The players on this device, as registries: the local PlayerControllers (the ones this device plays, offline or its
    /// own in a session) and, in a network session, every online player's state. The GameMode makes and removes them; they
    /// join and leave these lists themselves. The first local player plays the online state of this device's client.
    /// </summary>
    internal static class PlayerManager {
        private static readonly List<PlayerController> players = new();
        private static readonly List<PlayerState> onlinePlayers = new();

        public static int PlayerCount => players.Count;
        public static IReadOnlyList<PlayerController> AllPlayers => players;

        /// <summary>Every online player's state on this peer (empty outside a network session).</summary>
        public static IReadOnlyList<PlayerState> OnlinePlayers => onlinePlayers;

        /// <summary>A controller this device plays came: it is the next local player, and plays its client's online state if that is here.</summary>
        internal static void AddLocal(PlayerController controller) {
            if (controller == null || players.Contains(controller)) return;

            controller.PlayerIndex = players.Count;
            players.Add(controller);
            if (controller.PlayerIndex == 0) {
                foreach (PlayerState state in onlinePlayers) {
                    RefreshOnline(state);
                }
            }
        }

        /// <summary>A local controller went (its player left, or its GameMode ended).</summary>
        internal static void RemoveLocal(PlayerController controller) {
            int index = players.IndexOf(controller);
            if (index < 0) return;

            players.RemoveAt(index);
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

        /// <summary>An online player's state went (the player left, or its GameMode or the session ended).</summary>
        internal static void RemoveOnline(PlayerState state) {
            onlinePlayers.Remove(state);
            PlayerController local = GetPlayer(0);
            if (local != null && local.State == state) {
                local.SetState(null);
            }
        }

        /// <summary>The first local player plays the online state of this device's client (one local player per client online).</summary>
        internal static void RefreshOnline(PlayerState state) {
#if HAS_NETCODE
            PlayerController local = GetPlayer(0);
            if (local == null) return;

            if (state.IsLocalPlayer) local.SetState(state);
            else if (local.State == state) local.SetState(null);
#endif
        }

        // Play mode without a domain reload keeps statics: each play starts with no players.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            players.Clear();
            onlinePlayers.Clear();
        }
    }
}
