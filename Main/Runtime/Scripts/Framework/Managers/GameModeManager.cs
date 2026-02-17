using System;
using System.Collections.Generic;
using UnityEngine;

namespace Majinfwork.World {
    [Serializable]
    [CreateAssetMenu(fileName = "Default Game Mode Config", menuName = "MFramework/Config Object/Game Mode Config")]
    public class GameModeManager : ScriptableObject {
        [Header("Game Managers")]
        [SerializeField] private GameState gameState;

        [Header("Player Setup")]
        [Tooltip("Automatically spawn player 0 on scene load")]
        [SerializeField] private PlayerController playerControllerPrefab;
        [SerializeField] private PlayerState playerStatePrefab;
        [SerializeReference, ClassReference] private PawnProvider pawnProvider;
        [SerializeField] private PlayerInput playerInputPrefab;
        [SerializeField] private HUD hudPrefab;

        [Header("Camera")]
        [SerializeReference, ClassReference] private CameraHandler cameraHandler;

        private readonly List<PlayerController> spawnedControllers = new();

        internal void OnActive() {
            InitiateGameManager();
            cameraHandler.Construct();
            SpawnPlayer();
        }

        internal void OnDeactive() {
            CleanupGameManager();
            cameraHandler.Deconstruct();

            // Iterate backwards since DespawnPlayer removes from list
            for (int i = spawnedControllers.Count - 1; i >= 0; i--) {
                DespawnPlayer(spawnedControllers[i]);
            }
        }

        private GameState spawnedGameState;

        internal void InitiateGameManager() {
            if (gameState != null) {
                spawnedGameState = Instantiate(gameState);
                DontDestroyOnLoad(spawnedGameState.gameObject);
            }
        }

        internal void CleanupGameManager() {
            if (spawnedGameState != null) {
                Destroy(spawnedGameState.gameObject);
                spawnedGameState = null;
            }
        }

        /// <summary>
        /// Spawns a new player on demand. Call this when a player joins (controller connects, press start, etc.)
        /// </summary>
        protected PlayerController SpawnPlayer(int? playerIndex = null) {
            int index = playerIndex ?? PlayerManager.PlayerCount;

            // Find spawn location for this player
            var playerStart = PlayerStart.FindForPlayer(index);
            Vector3 spawnPos = playerStart != null ? playerStart.transform.position : Vector3.zero;
            Quaternion spawnRot = playerStart != null ? playerStart.transform.rotation : Quaternion.identity;

            // Instantiate all player components
            var input = Instantiate(playerInputPrefab);
            var state = Instantiate(playerStatePrefab);
            state.OnCreated();
            var pawn = pawnProvider.GetPawn(state, spawnPos, spawnRot);
            var hud = hudPrefab != null ? Instantiate(hudPrefab) : null;
            var controller = Instantiate(playerControllerPrefab);

            // Make persistent so framework controls lifecycle, not Unity's scene unload
            DontDestroyOnLoad(controller.gameObject);
            DontDestroyOnLoad(input.gameObject);
            DontDestroyOnLoad(state.gameObject);
            DontDestroyOnLoad(pawn.gameObject);
            if (hud != null) DontDestroyOnLoad(hud.gameObject);

            // Initialize controller with components
            controller.Initialize(input, state, pawn, hud);

            // Register with PlayerManager
            PlayerManager.RegisterPlayer(controller);

            spawnedControllers.Add(controller);
            return controller;
        }

        /// <summary>
        /// Removes a player. Call this when a player leaves.
        /// </summary>
        protected void DespawnPlayer(PlayerController controller) {
            if (controller == null) return;

            PlayerManager.UnregisterPlayer(controller);
            spawnedControllers.Remove(controller);

            // Destroy player objects
            if (controller.Input != null) Destroy(controller.Input.gameObject);
            if (controller.State != null) Destroy(controller.State.gameObject);
            if (controller.HUD != null) Destroy(controller.HUD.gameObject);
            if (controller.CurrentPawn != null) pawnProvider.ReleasePawn(controller.CurrentPawn);
            Destroy(controller.gameObject);
        }
    }
}
