using System;
using System.Collections.Generic;
using UnityEngine;
#if HAS_NETCODE
using Unity.Netcode;
#endif

namespace Majinfwork.World {
    /// <summary>
    /// The rules of the game played in a level, and the setup of its players there. Whoever loads the level names it (or
    /// the WorldConfig does); it runs as long as the level does.
    /// <para>
    /// On every peer it makes its GameState and camera handler and equips this device's players for it: their input, pawn
    /// and HUD (their controllers and states are kept across GameModes).
    /// </para>
    /// <para>
    /// A GameMode started while a network session runs (Netcode) plays in it, the server as its authority: the server
    /// spawns its GameState for every peer and handles the session's players. Each player who joins gets their
    /// PlayerState (<see cref="Login"/>; it is kept for the whole session, through every level) and is welcomed into the
    /// game (<see cref="OnPlayerJoined"/>); once their device has started this GameMode too, they have arrived
    /// (<see cref="OnPlayerArrived"/>: what they play with can be spawned); when they leave, <see cref="OnPlayerLeft"/>.
    /// A GameMode started outside a session plays on this device alone, its GameState local: start the session first.
    /// </para>
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "Default Game Mode Config", menuName = "MFramework/Config Object/Game Mode Config")]
    public class GameModeManager : ScriptableObject {
        [Header("Game Managers")]
        [Tooltip("The game's state, made when this GameMode starts. In a network game, a NetworkObject prefab registered with the NetworkManager.")]
        [SerializeField] private GameState gameState;

        [Header("Player Setup")]
        [SerializeReference, ClassReference] private PawnProvider pawnProvider;
        [Tooltip("Optional: none when the game reads its input elsewhere (an XR rig's own input).")]
        [SerializeField] private PlayerInput playerInputPrefab;
        [SerializeField] private HUD hudPrefab;

        [Header("Camera")]
        [SerializeReference, ClassReference] private CameraHandler cameraHandler;

        private readonly List<PlayerController> spawnedControllers = new();
        private GameState spawnedGameState;
        private string level;

        /// <summary>The level (scene) this GameMode runs in.</summary>
        protected string Level => level;

        /// <summary>The GameState this GameMode made: the server's in a network session (a client receives it instead), or the local one.</summary>
        protected GameState GameState => spawnedGameState;

        internal void OnActive(string sceneName) {
            level = sceneName;
#if HAS_NETCODE
            JoinSession();
#endif
            InitiateGameManager();
            cameraHandler.Construct();

            // Setup all existing persistent players for this GameMode
            foreach (var controller in PlayerManager.AllPlayers) {
                SetupPlayerForGameMode(controller);
                spawnedControllers.Add(controller);
            }

            StartPlay();
#if HAS_NETCODE
            StartSessionPlay();
#endif
        }

        internal void OnDeactive() {
            EndPlay();
#if HAS_NETCODE
            LeaveSession();
#endif
            CleanupGameManager();
            cameraHandler.Deconstruct();

            // Cleanup GameMode-owned components (Pawn, Input, HUD) but keep Controller + State alive
            for (int i = spawnedControllers.Count - 1; i >= 0; i--) {
                CleanupPlayerFromGameMode(spawnedControllers[i]);
            }
            spawnedControllers.Clear();
        }

        /// <summary>Every peer: this GameMode has started, its GameState and players set up. Override to start its game.</summary>
        protected virtual void StartPlay() { }

        /// <summary>Every peer: this GameMode is ending (the next level's is starting). Override to end its game.</summary>
        protected virtual void EndPlay() { }

        internal void InitiateGameManager() {
            if (gameState == null) {
                return;
            }

#if HAS_NETCODE
            if (network != null && !network.IsServer) {
                // A client receives the server's.
                return;
            }
#endif
            spawnedGameState = Instantiate(gameState);
            DontDestroyOnLoad(spawnedGameState.gameObject);
#if HAS_NETCODE
            if (network != null) {
                // Every peer receives it, and it outlives the level until this GameMode ends (after the next level loads).
                spawnedGameState.NetworkObject.Spawn(false);
            }
#endif
        }

        internal void CleanupGameManager() {
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
        /// Sets up a persistent PlayerController with this GameMode's Input, Pawn, and HUD.
        /// Called on GameMode activation for existing players.
        /// </summary>
        private void SetupPlayerForGameMode(PlayerController controller) {
            int index = controller.PlayerIndex;

            // Find spawn location
            var playerStart = PlayerStart.FindForPlayer(index);
            Vector3 spawnPos = playerStart != null ? playerStart.transform.position : Vector3.zero;
            Quaternion spawnRot = playerStart != null ? playerStart.transform.rotation : Quaternion.identity;

            // Create GameMode-owned components
            var input = playerInputPrefab != null ? Instantiate(playerInputPrefab) : null;
            var pawn = pawnProvider.GetPawn(controller.State, spawnPos, spawnRot);
            var hud = hudPrefab != null ? Instantiate(hudPrefab) : null;

            // Mark as DontDestroyOnLoad so we control lifecycle, not scene unload
            if (input != null) DontDestroyOnLoad(input.gameObject);
            DontDestroyOnLoad(pawn.gameObject);
            if (hud != null) DontDestroyOnLoad(hud.gameObject);

            controller.SetupForGameMode(input, pawn, hud);
        }

        /// <summary>
        /// Cleans up GameMode-owned components (Input, Pawn, HUD) from a controller.
        /// Controller and State remain alive.
        /// </summary>
        private void CleanupPlayerFromGameMode(PlayerController controller) {
            if (controller == null) return;

            if (controller.Input != null) Destroy(controller.Input.gameObject);
            if (controller.HUD != null) Destroy(controller.HUD.gameObject);
            if (controller.CurrentPawn != null) pawnProvider.ReleasePawn(controller.CurrentPawn);

            controller.CleanupFromGameMode();
        }

        /// <summary>
        /// Fully removes a player (destroys everything including persistent components).
        /// Call this when a player disconnects or leaves entirely.
        /// </summary>
        protected void DespawnPlayer(PlayerController controller) {
            if (controller == null) return;

            CleanupPlayerFromGameMode(controller);
            spawnedControllers.Remove(controller);
            PlayerManager.DestroyPlayer(controller);
        }

#if HAS_NETCODE
        private NetworkManager network;
        private readonly HashSet<PlayerState> arrived = new();
        /// <summary>This device's player, until its online state is here to arrive with.</summary>
        private PlayerController arriving;

        /// <summary>This GameMode plays in a network session: one ran when it started, and it still does.</summary>
        protected bool IsNetworked => network != null;

        /// <summary>This peer is the authority of the session this GameMode plays in (its server or host).</summary>
        protected bool IsServer => network != null && network.IsServer;

        /// <summary>The session's NetworkManager while this GameMode plays in it.</summary>
        protected NetworkManager Network => network;

        /// <summary>
        /// Server: the state of the player who joined on <paramref name="client"/>. A new one by default; override to give a
        /// returning player theirs back (<see cref="Reconnect"/>).
        /// </summary>
        protected virtual PlayerState Login(ulong client) {
            return SpawnPlayerState(client);
        }

        /// <summary>Server: a player is in the session while this GameMode runs (they joined, or were there when it started).</summary>
        protected virtual void OnPlayerJoined(PlayerState player) { }

        /// <summary>Server: the player's device has started this GameMode in its level: what they play with can be spawned there.</summary>
        protected virtual void OnPlayerArrived(PlayerState player) { }

        /// <summary>Server: the player left the session. Their state goes by default; override to keep it for their return (<see cref="KeepForReturn"/>).</summary>
        protected virtual void OnPlayerLeft(PlayerState player) {
            Logout(player);
        }

        /// <summary>Every peer: the session this GameMode played in ended under it (what the session spawned went with it); it plays on alone.</summary>
        protected virtual void OnSessionEnded() { }

        /// <summary>Server: a new player's state for <paramref name="client"/>, owned by them and kept for the whole session.</summary>
        protected PlayerState SpawnPlayerState(ulong client) {
            PlayerState prefab = PlayerManager.NetworkStatePrefab;
            if (prefab == null) {
                Debug.LogError("[GameModeManager] The world settings have no network player state to spawn.", this);
                return null;
            }

            PlayerState state = Instantiate(prefab);
            // Lives outside every level, and outlives its owner's connection: what happens when the player leaves is this
            // GameMode's call (OnPlayerLeft).
            DontDestroyOnLoad(state.gameObject);
            state.NetworkObject.DontDestroyWithOwner = true;
            state.NetworkObject.SpawnWithOwnership(client, false);
            return state;
        }

        /// <summary>Server: a returning player plays their own state again, from <paramref name="client"/>.</summary>
        protected static void Reconnect(PlayerState player, ulong client) {
            player.AssignClient(client);
        }

        /// <summary>Server: the player left, and their state is kept for their return.</summary>
        protected static void KeepForReturn(PlayerState player) {
            player.ReleaseClient();
        }

        /// <summary>Server: the player is gone for good, and so is their state.</summary>
        protected static void Logout(PlayerState player) {
            if (player.IsSpawned) {
                player.NetworkObject.Despawn();
            }
        }

        /// <summary>Server: <paramref name="player"/>'s device started the GameMode of <paramref name="playerLevel"/>.</summary>
        internal void PlayerArrived(PlayerState player, string playerLevel) {
            // A notice from the level before this one, still on its way, is not an arrival here.
            if (IsServer && playerLevel == level && player.IsConnected && arrived.Add(player)) {
                OnPlayerArrived(player);
            }
        }

        private void JoinSession() {
            NetworkManager running = NetworkManager.Singleton;
            if (running == null || !running.IsListening) {
                return;
            }

            network = running;
            network.OnServerStopped += OnSessionStopped;
            network.OnClientStopped += OnSessionStopped;
        }

        private void StartSessionPlay() {
            if (network == null) {
                return;
            }

            if (network.IsServer) {
                network.OnClientConnectedCallback += Join;
                network.OnClientDisconnectCallback += Leave;
                foreach (ulong client in network.ConnectedClientsIds) {
                    Join(client);
                }
            }

            // This device's player arrives once its online state is here (a joining client's may come after the GameMode).
            arriving = PlayerManager.GetPlayer(0);
            if (arriving != null) {
                arriving.StateChanged += ArriveLocalPlayer;
                ArriveLocalPlayer();
            }
        }

        private void LeaveSession() {
            if (arriving != null) {
                arriving.StateChanged -= ArriveLocalPlayer;
                arriving = null;
            }

            if (network == null) {
                return;
            }

            network.OnServerStopped -= OnSessionStopped;
            network.OnClientStopped -= OnSessionStopped;
            network.OnClientConnectedCallback -= Join;
            network.OnClientDisconnectCallback -= Leave;
            arrived.Clear();
            network = null;
        }

        private void OnSessionStopped(bool wasHost) {
            if (network == null) {
                return;
            }

            LeaveSession();
            OnSessionEnded();
        }

        private void Join(ulong client) {
            PlayerState player = PlayerManager.FindOnline(client) ?? Login(client);
            if (player != null) {
                OnPlayerJoined(player);
            }
        }

        private void Leave(ulong client) {
            PlayerState player = PlayerManager.FindOnline(client);
            if (player == null) {
                return;
            }

            arrived.Remove(player);
            OnPlayerLeft(player);
        }

        private void ArriveLocalPlayer() {
            PlayerState state = arriving.State;
            if (state == null || !state.IsSpawned || !state.IsLocalPlayer) {
                return;
            }

            arriving.StateChanged -= ArriveLocalPlayer;
            arriving = null;
            state.Arrive(level);
        }
#endif
    }
}
