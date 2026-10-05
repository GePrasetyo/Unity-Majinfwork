using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Majinfwork.World {
    /// <summary>
    /// Streams Addressable scenes in and out additively. Each <see cref="AddressableSceneHandler"/> takes its loads and
    /// unloads one at a time, in the order they were asked: loading a loaded scene returns it, unloading an unloaded
    /// one returns at once, and an unload asked during a load waits for the load and then unloads. Every call
    /// completes, failures included, and failures are logged here. Light probes are re-tetrahedralized after each
    /// load and unload, since additive scenes bring and take their own probes.
    /// </summary>
    public class LevelStreamer {
        /// <summary>
        /// Loads the scene additively and returns it, or an invalid scene if the load failed or ran past
        /// <paramref name="timeoutSec"/> (zero or less waits as long as it takes). Cancelling throws
        /// <see cref="OperationCanceledException"/>. A load that is cut short still lands, and is then unloaded.
        /// </summary>
        public Task<Scene> LoadAddressableSceneAsync(AddressableSceneHandler scene, CancellationToken cancellationToken = default, int timeoutSec = Timeout.Infinite) {
            if (scene == null || scene.sceneAddressable == null || !scene.sceneAddressable.RuntimeKeyIsValid()) {
                Debug.LogError($"[LevelStreamer] Can't load '{scene}': it has no valid scene reference.");
                return Task.FromResult(default(Scene));
            }

            var result = new TaskCompletionSource<Scene>(TaskCreationOptions.RunContinuationsAsynchronously);
            scene.pending = LoadQueuedAsync(scene, scene.pending, result, cancellationToken, timeoutSec);
            return result.Task;
        }

        /// <summary>Unloads the scene if it is loaded, once any load still under way on it lands.</summary>
        public Task UnloadAddressableSceneAsync(AddressableSceneHandler scene) {
            return UnloadQueued(scene);
        }

        /// <summary>Callback form of a load: <paramref name="loadComplete"/> gets the scene's path, only if it loaded.</summary>
        public async Task LoadAddressableSceneAsync(AddressableSceneHandler sceneToLoad, Action<string> loadComplete) {
            Scene scene = await LoadAddressableSceneAsync(sceneToLoad);
            if (scene.IsValid()) {
                loadComplete?.Invoke(scene.path);
            }
        }

        /// <summary>Callback form of a cancellable load: a cancelled load is logged, not thrown.</summary>
        public async Task LoadAddressableSceneAsync(AddressableSceneHandler sceneToLoad, Action<string> loadComplete, CancellationToken ct, int timeoutSec = Timeout.Infinite) {
            try {
                Scene scene = await LoadAddressableSceneAsync(sceneToLoad, ct, timeoutSec);
                if (scene.IsValid()) {
                    loadComplete?.Invoke(scene.path);
                }
            }
            catch (OperationCanceledException) {
                Debug.LogWarning($"[LevelStreamer] Load of '{sceneToLoad}' was cancelled.");
            }
        }

        /// <summary>Callback form of an unload: <paramref name="unloadComplete"/> gets the scene's path, only if it was loaded.</summary>
        public async Task UnloadAddressableSceneAsync(AddressableSceneHandler sceneToUnload, Action<string> unloadComplete) {
            string path = await UnloadQueued(sceneToUnload);
            if (path != null) {
                unloadComplete?.Invoke(path);
            }
        }

        private static Task<string> UnloadQueued(AddressableSceneHandler scene) {
            if (scene == null) {
                return Task.FromResult<string>(null);
            }

            var result = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            scene.pending = UnloadQueuedAsync(scene, scene.pending, result);
            return result.Task;
        }

        private static async Task LoadQueuedAsync(AddressableSceneHandler scene, Task previous, TaskCompletionSource<Scene> result,
            CancellationToken cancellationToken, int timeoutSec) {
            try {
                await AfterAsync(previous);
                if (cancellationToken.IsCancellationRequested) {
                    result.TrySetCanceled(cancellationToken);
                    return;
                }

                scene.Sync();
                if (scene.status == SceneLoadStatus.Loaded) {
                    result.TrySetResult(scene.loadHandle.Result.Scene);
                    return;
                }

                scene.status = SceneLoadStatus.Loading;
                AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(scene.sceneAddressable, LoadSceneMode.Additive);
                if (!await LandsInTimeAsync(handle.Task, cancellationToken, timeoutSec)) {
                    // The caller stopped waiting. An Addressables load can't be stopped, so it is undone once it lands.
                    if (cancellationToken.IsCancellationRequested) {
                        result.TrySetCanceled(cancellationToken);
                    }
                    else {
                        Debug.LogError($"[LevelStreamer] '{scene}' didn't load within {timeoutSec}s; unloading it once it lands.");
                        result.TrySetResult(default);
                    }

                    scene.status = SceneLoadStatus.Unloading;
                    await handle.Task;
                    await ReleaseAsync(handle);
                    scene.status = SceneLoadStatus.Unloaded;
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded) {
                    Debug.LogError($"[LevelStreamer] '{scene}' failed to load: {handle.OperationException?.Message}");
                    Addressables.Release(handle);
                    scene.status = SceneLoadStatus.Unloaded;
                    result.TrySetResult(default);
                    return;
                }

                Scene loaded = handle.Result.Scene;
                scene.loadHandle = handle;
                scene.status = SceneLoadStatus.Loaded;
                LightProbes.TetrahedralizeAsync();
                Debug.Log($"[LevelStreamer] Loaded '{loaded.path}'.");
                result.TrySetResult(loaded);
            }
            catch (Exception exception) {
                Debug.LogException(exception);
                if (scene.status != SceneLoadStatus.Loaded) {
                    scene.status = SceneLoadStatus.Unloaded;
                }

                result.TrySetResult(default);
            }
        }

        private static async Task UnloadQueuedAsync(AddressableSceneHandler scene, Task previous, TaskCompletionSource<string> result) {
            string path = null;
            try {
                await AfterAsync(previous);
                scene.Sync();
                if (scene.status != SceneLoadStatus.Loaded) {
                    return;
                }

                path = scene.loadHandle.Result.Scene.path;
                scene.status = SceneLoadStatus.Unloading;
                await ReleaseAsync(scene.loadHandle);
                LightProbes.TetrahedralizeAsync();
                Debug.Log($"[LevelStreamer] Unloaded '{path}'.");
            }
            catch (Exception exception) {
                Debug.LogException(exception);
            }
            finally {
                if (scene.status == SceneLoadStatus.Unloading) {
                    // The unload released the load handle with the scene.
                    scene.loadHandle = default;
                    scene.status = SceneLoadStatus.Unloaded;
                }

                result.TrySetResult(path);
            }
        }

        /// <summary>
        /// Undoes a landed load: unloads the scene it brought in, or releases it if it failed. The unload's own handle
        /// is released here, after its result is read: an auto-released one is already invalid when an await resumes.
        /// </summary>
        private static async Task ReleaseAsync(AsyncOperationHandle<SceneInstance> handle) {
            if (!handle.IsValid()) {
                return;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                Addressables.Release(handle);
                return;
            }

            AsyncOperationHandle<SceneInstance> unload = Addressables.UnloadSceneAsync(handle, autoReleaseHandle: false);
            await unload.Task;
            if (unload.Status != AsyncOperationStatus.Succeeded) {
                Debug.LogError($"[LevelStreamer] Unloading '{handle.Result.Scene.path}' failed: {unload.OperationException?.Message}");
            }

            Addressables.Release(unload);
        }

        /// <summary>Waits for the operation: false if the caller cancelled, or the timeout passed, first.</summary>
        private static async Task<bool> LandsInTimeAsync(Task operation, CancellationToken cancellationToken, int timeoutSec) {
            bool timed = timeoutSec > 0;
            if (!timed && !cancellationToken.CanBeCanceled) {
                await operation;
                return true;
            }

            using (var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                Task landed = await Task.WhenAny(operation, Task.Delay(timed ? timeoutSec * 1000 : Timeout.Infinite, stop.Token));
                stop.Cancel();
                return landed == operation;
            }
        }

        private static async Task AfterAsync(Task previous) {
            if (previous == null) {
                return;
            }

            try {
                await previous;
            }
            catch (Exception) {
                // Its own caller has been told.
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitService() {
            ServiceLocator.Register<LevelStreamer>(new LevelStreamer());
        }
    }
}
