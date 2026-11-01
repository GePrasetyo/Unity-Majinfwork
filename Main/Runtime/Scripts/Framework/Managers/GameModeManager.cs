using System;
using System.Collections.Generic;
using UnityEngine;
#if HAS_NETCODE
using Majinfwork.Network;
using Unity.Netcode;
#endif

namespace Majinfwork.World {
    /// <summary>
    /// The rules of the game played in a level, and its players. Whoever loads the level names it (or the WorldConfig does);
    /// it runs as long as the level does, on the device with the authority: offline this one, in a network session the
    /// server. A client runs none: it gets what the server spawns.
    /// <para>
    /// It makes its GameState and logs its players in: each gets a PlayerController and a PlayerState from this GameMode's
    /// prefabs (<see cref="OnPlayerJoined"/>), which live as long as it does: when it ends, every player it made goes with
    /// it, and the next GameMode logs them in again. Anything that must outlast it lives elsewhere. Offline it logs in the
    /// first local player as it starts; a split-screen game's subclass logs in the others (<see cref="SpawnPlayer"/>). Once a
    /// player's device is in the level they have arrived (<see cref="OnPlayerArrived"/>: what they play with can be
    /// spawned); when they leave, <see cref="OnPlayerLeft"/>.
    /// </para>
    /// <para>
    /// In a network session (Netcode) the server spawns its GameState and every player's state for every peer, and each
    /// player's controller for its owner only. A connecting player may be refused first (<see cref="PreLogin"/>); each who
    /// joins gets their state (<see cref="Login"/>: a new one, or theirs kept for their return), and their device reports
    /// its arrival through its PlayerController.
    /// </para>
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "Default Game Mode Config", menuName = "MFramework/Config Object/Game Mode Config")]
    public class GameModeManager : ScriptableObject
#if HAS_NETCODE
        , IPreLogin
#endif
    {
        [Header("Game Managers")]
        [Tooltip("The game's state, made when this GameMode starts. In a network game, a NetworkObject prefab registered with the NetworkManager.")]
        [SerializeField] private GameState gameState;

        [Header("Players")]
        [Tooltip("Each player's controller, made when they log in and gone when they leave or this GameMode ends. In a network game, a NetworkObject prefab registered with the NetworkManager.")]
        [SerializeField] private PlayerController playerControllerPrefab;
        [Tooltip("Each player's state, made with their controller. In a network game, a NetworkObject prefab registered with the NetworkManager.")]
        [SerializeField] private PlayerState playerStatePrefab;

        private readonly List<PlayerController> players = new();
        private readonly List<PlayerState> states = new();
        private readonly HashSet<PlayerController> arrived = new();
        private GameState spawnedGameState;
        private string level;

        /// <summary>The level (scene) this GameMode runs in.</summary>
        protected string Level => level;

        /// <summary>The GameState this GameMode made: spawned for every peer in a network session, else local.</summary>
        protected GameState GameState => spawnedGameState;

        internal void OnActive(string sceneName) {
            level = sceneName;
#if HAS_NETCODE
            JoinSession();
#endif
            InitiateGameManager();
            StartPlay();
#if HAS_NETCODE
            if (network != null) {
                StartSessionPlay();
                return;
            }
#endif
            SpawnPlayer();
        }

        internal void OnDeactive() {
            EndPlay();
            // Every player goes with this GameMode; the next one logs them in again.
            for (int i = players.Count - 1; i >= 0; i--) {
                RemovePlayer(players[i]);
            }

            for (int i = states.Count - 1; i >= 0; i--) {
                Logout(states[i]);
            }
#if HAS_NETCODE
            LeaveSession();
#endif
            CleanupGameManager();
        }

        /// <summary>This GameMode has started, its GameState made; its players log in right after. Override to start its game.</summary>
        protected virtual void StartPlay() { }

        /// <summary>This GameMode is ending (the next level's is starting); its players go right after. Override to end its game.</summary>
        protected virtual void EndPlay() { }

        /// <summary>A player logged in: their controller and state are made (spawned, in a session). Override to welcome them.</summary>
        protected virtual void OnPlayerJoined(PlayerController player) { }

        /// <summary>The player's device is in this GameMode's level (offline at once): what they play with can be spawned there.</summary>
        protected virtual void OnPlayerArrived(PlayerController player) { }

        /// <summary>The player leaves; their controller goes right after. Their state goes too by default: override to keep it (<see cref="KeepForReturn"/>).</summary>
        protected virtual void OnPlayerLeft(PlayerController player) {
            if (player.State != null) {
                Logout(player.State);
            }
        }

        private void InitiateGameManager() {
            if (gameState == null) {
                return;
            }

            spawnedGameState = Instantiate(gameState);
            DontDestroyOnLoad(spawnedGameState.gameObject);
#if HAS_NETCODE
            if (network != null) {
                // Every peer receives it, and it outlives the level until this GameMode ends (after the next level loads).
                spawnedGameState.NetworkObject.Spawn(false);
            }
#endif
        }

        private void CleanupGameManager() {
            if (spawnedGameState == null) {
                return;
            }

#if HAS_NETCODE
            if (spawnedGameState.IsSpawned) {
                spawnedGameState.NetworkObject.Despawn();
                spawnedGameState = null;
                return;
            }
#endif
            Destroy(spawnedGameState.gameObject);
            spawnedGameState = null;
        }

        /// <summary>
        /// Offline: logs in another local player, on this device so arrived at once. The first is logged in as this GameMode
        /// starts; a split-screen game's subclass logs in the others, from its own count of players.
        /// </summary>
        protected PlayerController SpawnPlayer() {
#if HAS_NETCODE
            if (network != null) {
                Debug.LogError("[GameModeManager] In a network session players log in as their clients join.", this);
                return null;
            }
#endif
            if (playerControllerPrefab == null) {
                Debug.LogError("[GameModeManager] No PlayerController prefab to log a player in with.", this);
                return null;
            }

            PlayerState state = null;
            if (playerStatePrefab != null) {
                state = Instantiate(playerStatePrefab);
                DontDestroyOnLoad(state.gameObject);
                states.Add(state);
            }

            PlayerController controller = Instantiate(playerControllerPrefab);
            DontDestroyOnLoad(controller.gameObject);
            controller.SetState(state);
            players.Add(controller);
            controller.BeginLocal();
            OnPlayerJoined(controller);
            if (players.Contains(controller) && arrived.Add(controller)) {
                OnPlayerArrived(controller);
            }

            return controller;
        }

        /// <summary>A player leaves the game (offline: a split-screen player dropping out): <see cref="OnPlayerLeft"/>, then their controller goes.</summary>
        protected void DespawnPlayer(PlayerController player) {
            if (!players.Contains(player)) return;

            OnPlayerLeft(player);
            RemovePlayer(player);
        }

        /// <summary>The player's state goes (they are gone for good).</summary>
        protected void Logout(PlayerState player) {
            states.Remove(player);
            if (player == null) {
                // Already gone (its session ended).
                return;
            }

#if HAS_NETCODE
            if (player.IsSpawned) {
                player.NetworkObject.Despawn();
                return;
            }
#endif
            Destroy(player.gameObject);
        }

        private void RemovePlayer(PlayerController controller) {
            players.Remove(controller);
            arrived.Remove(controller);
            if (controller == null) {
                // Already gone (its session ended).
                return;
            }

#if HAS_NETCODE
            controller.Arrived -= OnArrived;
            foreach (KeyValuePair<ulong, PlayerController> entry in byClient) {
                if (entry.Value == controller) {
                    byClient.Remove(entry.Key);
                    break;
                }
            }

            if (controller.IsSpawned) {
                controller.NetworkObject.Despawn();
                return;
            }
#endif
            Destroy(controller.gameObject);
        }

#if HAS_NETCODE
        private NetworkManager network;
        // Each online player's controller by the client they joined on (its owner may already be the server as they leave).
        private readonly Dictionary<ulong, PlayerController> byClient = new();

        /// <summary>This GameMode plays in a network session: one ran when it started, and it still does.</summary>
        protected bool IsNetworked => network != null;

        /// <summary>This GameMode is the authority of a network session (its server or host).</summary>
        protected bool IsServer => network != null && network.IsServer;

        /// <summary>The session's NetworkManager while this GameMode plays in it.</summary>
        protected NetworkManager Network => network;

        string IPreLogin.PreLogin(ulong clientId, ConnectionPayload payload) => PreLogin(clientId, payload);

        /// <summary>
        /// Server: why the player connecting on <paramref name="client"/> may not join now (sent to them), or null to let them
        /// in (the default). The connection approval asks before <see cref="Login"/>, never about the host's own connection.
        /// </summary>
        protected virtual string PreLogin(ulong client, ConnectionPayload payload) => null;

        /// <summary>
        /// Server: the state of the player who joined on <paramref name="client"/>. A new one by default; override to give a
        /// returning player theirs back (<see cref="Reconnect"/>).
        /// </summary>
        protected virtual PlayerState Login(ulong client) {
            return SpawnPlayerState(client);
        }

        /// <summary>Every peer: the session this GameMode played in ended under it; what the session spawned (its GameState, its players) went with it.</summary>
        protected virtual void OnSessionEnded() { }

        /// <summary>Server: a new player's state for <paramref name="client"/>, owned by them, for every peer.</summary>
        protected PlayerState SpawnPlayerState(ulong client) {
            if (playerStatePrefab == null) {
                Debug.LogError("[GameModeManager] No PlayerState prefab to log a player in with.", this);
                return null;
            }

            PlayerState state = Instantiate(playerStatePrefab);
            DontDestroyOnLoad(state.gameObject);
            // It outlives its owner's connection: what happens when the player leaves is this GameMode's call (OnPlayerLeft).
            state.NetworkObject.DontDestroyWithOwner = true;
            state.NetworkObject.SpawnWithOwnership(client, false);
            states.Add(state);
            return state;
        }

        /// <summary>Server: a returning player plays their own state again, from <paramref name="client"/>.</summary>
        protected void Reconnect(PlayerState player, ulong client) {
            player.AssignClient(client);
        }

        /// <summary>Server: the player left, and their state is kept for their return (until this GameMode ends, or <see cref="Logout"/>).</summary>
        protected void KeepForReturn(PlayerState player) {
            player.ReleaseClient();
        }

        private void JoinSession() {
            NetworkManager running = NetworkManager.Singleton;
            if (running == null || !running.IsListening) {
                return;
            }

            network = running;
            network.OnServerStopped += OnSessionStopped;
        }

        private void StartSessionPlay() {
            ServiceLocator.Register<IPreLogin>(this);
            network.OnClientConnectedCallback += Join;
            network.OnClientDisconnectCallback += Leave;
            foreach (ulong client in network.ConnectedClientsIds) {
                Join(client);
            }
        }

        private void LeaveSession() {
            if (network == null) {
                return;
            }

            if (ReferenceEquals(ServiceLocator.Resolve<IPreLogin>(), this)) {
                ServiceLocator.Unregister<IPreLogin>(out _);
            }

            network.OnServerStopped -= OnSessionStopped;
            network.OnClientConnectedCallback -= Join;
            network.OnClientDisconnectCallback -= Leave;
            network = null;
        }

        private void OnSessionStopped(bool wasHost) {
            if (network == null) {
                return;
            }

            LeaveSession();
            players.Clear();
            byClient.Clear();
            arrived.Clear();
            states.Clear();
            OnSessionEnded();
        }

        private void Join(ulong client) {
            if (byClient.ContainsKey(client)) {
                return;
            }

            PlayerState state = Login(client);
            if (state == null || playerControllerPrefab == null) {
                if (playerControllerPrefab == null) {
                    Debug.LogError("[GameModeManager] No PlayerController prefab to log a player in with.", this);
                }

                return;
            }

            PlayerController controller = Instantiate(playerControllerPrefab);
            DontDestroyOnLoad(controller.gameObject);
            NetworkObject networkObject = controller.NetworkObject;
            // Only its owner (and the server) have it.
            networkObject.CheckObjectVisibility = observer => observer == client;
            // What happens when the player leaves is this GameMode's call (Leave).
            networkObject.DontDestroyWithOwner = true;
            controller.SetState(state);
            controller.Arrived += OnArrived;
            players.Add(controller);
            byClient.Add(client, controller);
            networkObject.SpawnWithOwnership(client, false);
            OnPlayerJoined(controller);
        }

        private void Leave(ulong client) {
            if (!byClient.TryGetValue(client, out PlayerController controller)) {
                return;
            }

            if (controller != null) {
                OnPlayerLeft(controller);
            }

            RemovePlayer(controller);
            byClient.Remove(client);
        }

        private void OnArrived(PlayerController controller, string playerLevel) {
            // A notice from the level before this one, still on its way, is not an arrival here.
            if (playerLevel == level && players.Contains(controller) && arrived.Add(controller)) {
                OnPlayerArrived(controller);
            }
        }
#endif
    }
}
