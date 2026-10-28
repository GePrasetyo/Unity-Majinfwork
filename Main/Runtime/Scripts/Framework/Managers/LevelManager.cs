using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Majinfwork.World {
    /// <summary>
    /// Manages level loading and GameMode transitions.
    /// A GameMode is built in (it loads with the WorldConfig) or Addressable, so a mini-game's closure
    /// (HUD prefab, pawn prefab, input prefab) stays out of boot and only pulls into memory when its scene activates.
    /// <para>
    /// A level can be loaded here (<see cref="LoadLevelAsync"/>), or by something else, such as a network scene manager, which
    /// then hands the loaded scene over with <see cref="ActivateLevelAsync"/>. Either way the newest request wins: one still
    /// resolving its GameMode when another arrives is dropped.
    /// </para>
    /// </summary>
    public class LevelManager {
        private readonly WorldConfig worldConfig;
        private GameModeManager currentGameMode;
        private GameModeManager currentGameModeTemplate;
        private AsyncOperationHandle<GameModeManager> currentGameModeHandle;
        private bool hasCurrentHandle;
        /// <summary>Counts level requests: an activation finishing behind a newer request gives way to it.</summary>
        private int requests;

        public GameModeManager CurrentGameMode => currentGameMode;
        public bool IsLoading { get; private set; }
        public string CurrentSceneName { get; private set; }

        public LevelManager(WorldConfig worldConfig) {
            this.worldConfig = worldConfig;
        }

        /// <summary>
        /// Loads a level asynchronously with full GameMode management.
        /// </summary>
        public async Task LoadLevelAsync(string sceneName, CancellationToken cancellationToken = default) {
            if (IsLoading) {
                Debug.LogWarning("[LevelManager] Already loading a level. Ignoring request.");
                return;
            }

            IsLoading = true;

            try {
                // Resolve the Addressable GameMode first so we can error out early
                // without having already swapped scenes.
                var resolve = ResolveGameModeAsync(sceneName);
                var (targetTemplate, targetHandle) = await resolve;

                DeactivateCurrentGameMode();

                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.name == sceneName && activeScene.isLoaded) {
                    Debug.Log($"[LevelManager] Scene already loaded: {sceneName}");
                }
                else {
                    await LoadSceneInternalAsync(sceneName, cancellationToken);
                }

                requests++;
                ActivateGameMode(targetTemplate, targetHandle);

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
        /// Activates the GameMode of <paramref name="sceneName"/>, a level something else has loaded (a network scene manager,
        /// a custom loader); the current GameMode gives way to it. The newest call wins: one still resolving its GameMode when
        /// another arrives is dropped, so the level asked for last is the one whose GameMode ends up active. Asking again
        /// for the level that is running restarts its GameMode, as a freshly loaded copy of the level needs. True when this
        /// request's GameMode is the one now active (false: a newer request took over).
        /// </summary>
        public async Task<bool> ActivateLevelAsync(string sceneName) {
            int request = ++requests;
            IsLoading = true;
            try {
                var (targetTemplate, targetHandle) = await ResolveGameModeAsync(sceneName);
                if (request != requests) {
                    // A newer request came in meanwhile: it activates its own GameMode.
                    if (targetHandle.IsValid()) Addressables.Release(targetHandle);
                    return false;
                }

                DeactivateCurrentGameMode();
                ActivateGameMode(targetTemplate, targetHandle);
                CurrentSceneName = sceneName;
                Debug.Log($"[LevelManager] Level ready: {sceneName}");
                return true;
            }
            catch (Exception e) {
                Debug.LogError($"[LevelManager] Failed to activate level {sceneName}: {e.Message}");
                throw;
            }
            finally {
                if (request == requests) {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// Resolves the GameMode asset for a scene: the scene's own, else the default; for each, the built-in one if set,
        /// else the Addressable one. Returns both the asset and its Addressables handle so the caller can release it on
        /// deactivation. Handle is <c>default</c> (unused) for a built-in GameMode or none.
        /// </summary>
        private async Task<(GameModeManager template, AsyncOperationHandle<GameModeManager> handle)>
            ResolveGameModeAsync(string sceneName) {

            if (worldConfig == null) {
                Debug.LogError("[LevelManager] WorldConfig is null!");
                return (null, default);
            }

            GameModeManager builtIn = null;
            AssetReferenceT<GameModeManager> reference = null;

            if (worldConfig.MapConfigList.TryGetValue(sceneName, out var mapConfig)
                && (mapConfig.BuiltInGameMode != null || (mapConfig.TheGameMode != null && mapConfig.TheGameMode.RuntimeKeyIsValid()))) {
                builtIn = mapConfig.BuiltInGameMode;
                reference = mapConfig.TheGameMode;
            }
            else if (worldConfig.BuiltInDefaultGameMode != null
                     || (worldConfig.DefaultGameMode != null && worldConfig.DefaultGameMode.RuntimeKeyIsValid())) {
                Debug.LogWarning($"[LevelManager] Scene '{sceneName}' has no GameMode; using default.");
                builtIn = worldConfig.BuiltInDefaultGameMode;
                reference = worldConfig.DefaultGameMode;
            }
            else {
                Debug.LogWarning($"[LevelManager] No GameMode (default or specific) configured for scene: {sceneName}");
                return (null, default);
            }

            if (builtIn != null) {
                return (builtIn, default);
            }

            // Loaded by key, not through the reference: a reference holds one load at a time, and a level restarting its
            // GameMode loads it again before the running one is released (each load is counted and released on its own).
            var handle = Addressables.LoadAssetAsync<GameModeManager>(reference.RuntimeKey);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                Debug.LogError($"[LevelManager] Failed to load Addressable GameMode for scene '{sceneName}'.");
                Addressables.Release(handle);
                return (null, default);
            }

            return (handle.Result, handle);
        }

        /// <summary>
        /// Deactivates the current GameMode and releases its Addressables handle.
        /// </summary>
        private void DeactivateCurrentGameMode() {
            if (currentGameMode != null) {
                Debug.Log($"[LevelManager] Deactivating GameMode: {currentGameMode.name}");
                currentGameMode.OnDeactive();
                UnityEngine.Object.Destroy(currentGameMode);
                currentGameMode = null;
            }

            if (hasCurrentHandle) {
                Addressables.Release(currentGameModeHandle);
                currentGameModeHandle = default;
                hasCurrentHandle = false;
            }

            currentGameModeTemplate = null;
        }

        /// <summary>
        /// Activates a new GameMode. Claim ownership of the Addressables handle so the
        /// asset stays loaded for the lifetime of the active GameMode and is released
        /// cleanly on the next deactivation.
        /// </summary>
        private void ActivateGameMode(GameModeManager targetTemplate,
            AsyncOperationHandle<GameModeManager> targetHandle) {

            if (targetTemplate != null) {
                Debug.Log($"[LevelManager] Activating GameMode: {targetTemplate.name}");
                currentGameMode = UnityEngine.Object.Instantiate(targetTemplate);
                currentGameModeTemplate = targetTemplate;
                currentGameModeHandle = targetHandle;
                hasCurrentHandle = targetHandle.IsValid();
                currentGameMode.OnActive();
            }
            else {
                Debug.LogWarning("[LevelManager] No GameMode for this level.");
                // Nothing to own — release the (possibly default) handle if it's valid.
                if (targetHandle.IsValid()) Addressables.Release(targetHandle);
            }
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

        /// <summary>
        /// Shuts down the LevelManager and cleans up the current GameMode + its Addressables handle.
        /// </summary>
        public void Shutdown() {
            DeactivateCurrentGameMode();
        }
    }
}
