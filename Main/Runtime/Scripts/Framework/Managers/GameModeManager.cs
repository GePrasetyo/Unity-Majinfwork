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
        [SerializeReference, ClassReference] private PawnProvider pawnProvider;
        [SerializeField] private PlayerInput playerInputPrefab;
        [SerializeField] private HUD hudPrefab;

        [Header("Camera")]
        [SerializeReference, ClassReference] private CameraHandler cameraHandler;

        private readonly List<PlayerController> spawnedControllers = new();

        internal void OnActive() {
            InitiateGameManager();
            cameraHandler.Construct();

            // Setup all existing persistent players for this GameMode
            foreach (var controller in PlayerManager.AllPlayers) {
                SetupPlayerForGameMode(controller);
                spawnedControllers.Add(controller);
            }
        }

        internal void OnDeactive() {
            CleanupGameManager();
            cameraHandler.Deconstruct();

            // Cleanup GameMode-owned components (Pawn, Input, HUD) but keep Controller + State alive
            for (int i = spawnedControllers.Count - 1; i >= 0; i--) {
                CleanupPlayerFromGameMode(spawnedControllers[i]);
            }
            spawnedControllers.Clear();
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
            var input = Instantiate(playerInputPrefab);
            var pawn = pawnProvider.GetPawn(controller.State, spawnPos, spawnRot);
            var hud = hudPrefab != null ? Instantiate(hudPrefab) : null;

            // Mark as DontDestroyOnLoad so we control lifecycle, not scene unload
            DontDestroyOnLoad(input.gameObject);
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
    }
}
