using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majinfwork.World {
    /// <summary>
    /// Manages level loading and GameMode transitions.
    /// <para>
    /// Whoever loads a level names the GameMode it runs there (like Unreal's <c>?game=</c>), so one level can be played in
    /// several GameModes and one GameMode in several levels. A level loaded without one runs the WorldConfig's: the scene's
    /// own, else the default.
    /// </para>
    /// <para>
    /// A level can be loaded here (<see cref="LoadLevelAsync"/>), or by something else, such as a network scene manager, which
    /// then hands the loaded scene over with <see cref="ActivateLevel"/>.
    /// </para>
    /// </summary>
    public class LevelManager {
        private readonly WorldConfig worldConfig;
        private GameModeManager currentGameMode;

        public GameModeManager CurrentGameMode => currentGameMode;
        public bool IsLoading { get; private set; }
        public string CurrentSceneName { get; private set; }

        public LevelManager(WorldConfig worldConfig) {
            this.worldConfig = worldConfig;
        }

        /// <summary>The Addressable scenes the WorldConfig streams with <paramref name="sceneName"/>, for the <see cref="LevelStreamer"/>.</summary>
        public IReadOnlyList<AddressableSceneHandler> StreamingLevelsOf(string sceneName) {
            return worldConfig != null ? worldConfig.StreamingLevelsOf(sceneName) : Array.Empty<AddressableSceneHandler>();
        }

        /// <summary>
        /// Loads a level asynchronously with full GameMode management: it runs <paramref name="gameMode"/>, or the
        /// WorldConfig's GameMode for it when that is null.
        /// </summary>
        public async Task LoadLevelAsync(string sceneName, GameModeManager gameMode = null, CancellationToken cancellationToken = default) {
            if (IsLoading) {
                Debug.LogWarning("[LevelManager] Already loading a level. Ignoring request.");
                return;
            }

            IsLoading = true;

            try {
                GameModeManager target = ResolveGameMode(sceneName, gameMode);
                DeactivateCurrentGameMode();

                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.name == sceneName && activeScene.isLoaded) {
                    Debug.Log($"[LevelManager] Scene already loaded: {sceneName}");
                }
                else {
                    await LoadSceneInternalAsync(sceneName, cancellationToken);
                }

                ActivateGameMode(sceneName, target);
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
        /// Activates <paramref name="gameMode"/> (null: the WorldConfig's GameMode for <paramref name="sceneName"/>) in a
        /// level something else has loaded (a network scene manager, a custom loader); the current GameMode gives way to it.
        /// Asking again for the level that is running restarts its GameMode, as a freshly loaded copy of the level needs.
        /// </summary>
        public void ActivateLevel(string sceneName, GameModeManager gameMode = null) {
            GameModeManager target = ResolveGameMode(sceneName, gameMode);
            DeactivateCurrentGameMode();
            ActivateGameMode(sceneName, target);
            CurrentSceneName = sceneName;
            Debug.Log($"[LevelManager] Level ready: {sceneName}");
        }

        /// <summary>The GameMode for a scene: the one the caller named, else the WorldConfig's: the scene's own, else the default.</summary>
        private GameModeManager ResolveGameMode(string sceneName, GameModeManager named) {
            if (named != null) {
                return named;
            }

            if (worldConfig == null) {
                Debug.LogError("[LevelManager] WorldConfig is null!");
                return null;
            }

            if (worldConfig.MapConfigList.TryGetValue(sceneName, out var mapConfig) && mapConfig.gameMode != null) {
                return mapConfig.gameMode;
            }

            if (worldConfig.DefaultGameMode != null) {
                Debug.LogWarning($"[LevelManager] Scene '{sceneName}' has no GameMode; using default.");
                return worldConfig.DefaultGameMode;
            }

            Debug.LogWarning($"[LevelManager] No GameMode (default or specific) configured for scene: {sceneName}");
            return null;
        }

        private void DeactivateCurrentGameMode() {
            if (currentGameMode == null) {
                return;
            }

            Debug.Log($"[LevelManager] Deactivating GameMode: {currentGameMode.name}");
            currentGameMode.OnDeactive();
            UnityEngine.Object.Destroy(currentGameMode);
            currentGameMode = null;
        }

        private void ActivateGameMode(string sceneName, GameModeManager template) {
            if (template == null) {
                Debug.LogWarning("[LevelManager] No GameMode for this level.");
                return;
            }

            Debug.Log($"[LevelManager] Activating GameMode: {template.name}");
            currentGameMode = UnityEngine.Object.Instantiate(template);
            currentGameMode.OnActive(sceneName);
        }

        private async Task LoadSceneInternalAsync(string sceneName, CancellationToken cancellationToken) {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            if (operation == null) {
                throw new Exception($"Failed to start loading scene: {sceneName}");
            }

            while (!operation.isDone) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        /// <summary>Shuts down the LevelManager and cleans up the current GameMode.</summary>
        public void Shutdown() {
            DeactivateCurrentGameMode();
        }
    }
}
