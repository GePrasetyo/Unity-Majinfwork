using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Majinfwork.World {
    public class LevelStreamer {
        public async Task LoadAddressableSceneAsync(AddressableSceneHandler sceneToLoad, Action<string> loadComplete) {
            if (sceneToLoad.status != SceneLoadStatus.Unloaded) {
                return;
            }

            sceneToLoad.status = SceneLoadStatus.Loading;
            RegisterLoadCallbacks(sceneToLoad, (scenePath) => LoadSceneComplete(scenePath, sceneToLoad, loadComplete));
            var sceneHandle = Addressables.LoadSceneAsync(sceneToLoad.sceneAddressable, LoadSceneMode.Additive);
            await sceneHandle.Task;
            sceneToLoad.UpdateHandler(sceneHandle);
        }

        public async Task LoadAddressableSceneAsync(AddressableSceneHandler sceneToLoad, Action<string> loadComplete, CancellationToken ct, int timeoutSec = Timeout.Infinite) {
            if (sceneToLoad.status != SceneLoadStatus.Unloaded) {
                return;
            }

            sceneToLoad.status = SceneLoadStatus.Loading;
            RegisterLoadCallbacks(sceneToLoad, (scenePath) => LoadSceneComplete(scenePath, sceneToLoad, loadComplete));
            var sceneHandle = Addressables.LoadSceneAsync(sceneToLoad.sceneAddressable, LoadSceneMode.Additive);

            try {
                await AwaitHandleWithCancellation(sceneHandle, ct, timeoutSec);
                sceneToLoad.UpdateHandler(sceneHandle);
            }
            catch (OperationCanceledException) {
                Debug.LogWarning("Scene load was CANCELLED by the user/system.");

                if (sceneHandle.IsValid()) {
                    Addressables.UnloadSceneAsync(sceneHandle);
                    Debug.Log("Scene handle is being safely unloaded/abandoned.");
                }
            }
            catch (Exception e) {
                Debug.LogError($"Scene load failed for other reasons: {e.Message}");
            }
        }

        internal async Task UnloadAddressableSceneAsync(AddressableSceneHandler sceneToUnload, Action<string> unloadComplete) {
            if (sceneToUnload.status != SceneLoadStatus.Loaded) {
                return;
            }

            sceneToUnload.status = SceneLoadStatus.Loading;
            RegisterUnloadCallbacks(sceneToUnload, (scenePath) => UnloadSceneComplete(scenePath, sceneToUnload, unloadComplete));
            var sceneHandle = Addressables.UnloadSceneAsync(sceneToUnload.streamHandler);
            await sceneHandle.Task;
            sceneToUnload.UpdateHandler(sceneHandle);
        }

        private void RegisterLoadCallbacks(AddressableSceneHandler sceneAddressableObject, Action<string> loadComplete) {
            sceneAddressableObject.streamHandlerCompleted = loadComplete;
        }

        private void RegisterUnloadCallbacks(AddressableSceneHandler sceneAddressableObject, Action<string> unloadComplete) {
            sceneAddressableObject.streamHandlerCompleted = unloadComplete;
        }

        private void LoadSceneComplete(string scenePath, AddressableSceneHandler loadedSceneObject, Action<string> loadCompleteCallback) {
            loadCompleteCallback?.Invoke(scenePath);

            Debug.Log(loadedSceneObject + "loaded");
        }

        private void UnloadSceneComplete(string scenePath, AddressableSceneHandler unloadedSceneObject, Action<string> unloadCompleteCallback) {
            unloadCompleteCallback?.Invoke(scenePath);

            Debug.Log(unloadedSceneObject + " unloaded");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitService() {
            ServiceLocator.Register<LevelStreamer>(new LevelStreamer());
        }

        private async Task<T> AwaitHandleWithCancellation<T>(AsyncOperationHandle<T> handle, CancellationToken cancellationToken, int timeoutSec = Timeout.Infinite) {
            Task<T> operationTask = handle.Task;
            Task cancellationTask = Task.Delay(timeoutSec == Timeout.Infinite ? Timeout.Infinite : timeoutSec * 1000, cancellationToken);
            Task completedTask = await Task.WhenAny(operationTask, cancellationTask);

            if (completedTask == cancellationTask) {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return await operationTask;
        }
    }
}