using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majinfwork.World {
    /// <summary>
    /// Manages level loading and GameMode transitions.
    /// Provides explicit control over when scenes load and GameModes activate.
    /// </summary>
    public class LevelManager {
        private readonly WorldConfig worldConfig;
        private GameModeManager currentGameMode;
        private GameModeManager currentGameModeTemplate;

        /// <summary>
        /// The currently active GameMode instance.
        /// </summary>
        public GameModeManager CurrentGameMode => currentGameMode;

        /// <summary>
        /// Whether a level is currently loading.
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// The currently loaded scene name.
        /// </summary>
        public string CurrentSceneName { get; private set; }

        public LevelManager(WorldConfig worldConfig) {
            this.worldConfig = worldConfig;
        }

        /// <summary>
        /// Initializes GameMode for a scene that is already loaded.
        /// Use this at startup when scene is already active.
        /// </summary>
        public void InitializeGameModeForScene(string sceneName) {
            GameModeManager targetTemplate = GetGameModeForScene(sceneName);

            if (targetTemplate == null) {
                Debug.LogWarning($"[LevelManager] No GameMode found for scene: {sceneName}");
                return;
            }

            // Activate GameMode
            currentGameMode = UnityEngine.Object.Instantiate(targetTemplate);
            currentGameModeTemplate = targetTemplate;
            currentGameMode.OnActive();

            CurrentSceneName = sceneName;
            Debug.Log($"[LevelManager] Initialized GameMode for scene: {sceneName}");
        }

        /// <summary>
        /// Loads a level asynchronously with full GameMode management.
        /// </summary>
        /// <param name="sceneName">The scene to load.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public async Task LoadLevelAsync(string sceneName, CancellationToken cancellationToken = default) {
            if (IsLoading) {
                Debug.LogWarning("[LevelManager] Already loading a level. Ignoring request.");
                return;
            }

            IsLoading = true;

            try {
                // Determine target GameMode
                GameModeManager targetTemplate = GetGameModeForScene(sceneName);

                // Handle GameMode transition
                await TransitionGameModeAsync(targetTemplate, cancellationToken);

                // Check if scene is already loaded
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.name == sceneName && activeScene.isLoaded) {
                    Debug.Log($"[LevelManager] Scene already loaded: {sceneName}");
                }
                else {
                    // Load the scene
                    await LoadSceneInternalAsync(sceneName, cancellationToken);
                }

                CurrentSceneName = sceneName;
                Debug.Log($"[LevelManager] Level ready: {sceneName}");
            }
            catch (OperationCanceledException) {
                Debug.Log($"[LevelManager] Level load cancelled: {sceneName}");
                throw;
            }
            catch (Exception e) {
                Debug.LogError($"[LevelManager] Failed to load level {sceneName}: {e.Message}");
                throw;
            }
            finally {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Gets the GameMode template for a scene from WorldConfig.
        /// </summary>
        private GameModeManager GetGameModeForScene(string sceneName) {
            if (worldConfig == null) {
                Debug.LogError("[LevelManager] WorldConfig is null!");
                return null;
            }

            // Check if scene is registered
            if (worldConfig.MapConfigList.TryGetValue(sceneName, out var mapConfig)) {
                if (mapConfig.TheGameMode != null) {
                    return mapConfig.TheGameMode;
                }
                Debug.LogWarning($"[LevelManager] Scene '{sceneName}' registered but has no GameMode. Using default.");
            }

            // Fall back to default
            return worldConfig.DefaultGameMode;
        }

        /// <summary>
        /// Handles GameMode transition - deactivates old, activates new if different.
        /// </summary>
        private Task TransitionGameModeAsync(GameModeManager targetTemplate, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();

            // Same template - no transition needed
            if (targetTemplate == currentGameModeTemplate) {
                Debug.Log("[LevelManager] Same GameMode, no transition needed.");
                return Task.CompletedTask;
            }

            // Deactivate current GameMode
            if (currentGameMode != null) {
                Debug.Log($"[LevelManager] Deactivating GameMode: {currentGameMode.name}");
                currentGameMode.OnDeactive();
                UnityEngine.Object.Destroy(currentGameMode);
                currentGameMode = null;
            }

            // Activate new GameMode
            if (targetTemplate != null) {
                Debug.Log($"[LevelManager] Activating GameMode: {targetTemplate.name}");
                currentGameMode = UnityEngine.Object.Instantiate(targetTemplate);
                currentGameModeTemplate = targetTemplate;
                currentGameMode.OnActive();
            }
            else {
                currentGameModeTemplate = null;
                Debug.LogWarning("[LevelManager] No GameMode for this level.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Loads a scene asynchronously.
        /// </summary>
        private async Task LoadSceneInternalAsync(string sceneName, CancellationToken cancellationToken) {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            if (operation == null) {
                throw new Exception($"Failed to start loading scene: {sceneName}");
            }

            // Wait for scene to load
            while (!operation.isDone) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        /// <summary>
        /// Shuts down the LevelManager and cleans up the current GameMode.
        /// </summary>
        public void Shutdown() {
            if (currentGameMode != null) {
                currentGameMode.OnDeactive();
                UnityEngine.Object.Destroy(currentGameMode);
                currentGameMode = null;
                currentGameModeTemplate = null;
            }
        }
    }
}
