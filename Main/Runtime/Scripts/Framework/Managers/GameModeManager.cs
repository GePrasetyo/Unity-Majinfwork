using System;
using System.Collections.Generic;
using UnityEngine;

namespace Majinfwork.World {
    [Serializable]
    [CreateAssetMenu(fileName = "Default Game Mode Config", menuName = "MFramework/Config Object/Game Mode Config")]
    public class GameModeManager : ScriptableObject {
        [Header("Game Managers")]
        [SerializeField] private GameState gameState;
        [SerializeField] private HUDManager hudManager;

        [Header("Player Setup")]
        [Tooltip("Automatically spawn player 0 on scene load")]
        [SerializeField] private PlayerController playerControllerPrefab;
        [SerializeField] private PlayerState playerStatePrefab;
        [SerializeField] private PlayerPawn playerPawnPrefab;
        [SerializeField] private PlayerInput playerInputPrefab;

        [Header("Camera")]
        [SerializeReference, ClassReference] private CameraHandler cameraHandler;

        private readonly List<PlayerController> spawnedControllers = new();

        internal void OnActive() {
            InitiateGameManager();
            cameraHandler.Construct();
            SpawnPlayer();
        }

        internal void OnDeactive() {
            cameraHandler.Deconstruct();

            // Iterate backwards since DespawnPlayer removes from list
            for (int i = spawnedControllers.Count - 1; i >= 0; i--) {
                DespawnPlayer(spawnedControllers[i]);
            }
        }

        internal void InitiateGameManager() {
            Instantiate(gameState);
            Instantiate(hudManager);
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
            var pawn = Instantiate(playerPawnPrefab, spawnPos, spawnRot);
            var controller = Instantiate(playerControllerPrefab);

            // Initialize controller with components
            controller.Initialize(input, state, pawn);

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
            if (controller.CurrentPawn != null) Destroy(controller.CurrentPawn.gameObject);
            Destroy(controller.gameObject);
        }
    }
}
