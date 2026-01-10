#if HAS_STATEGRAPH
using Majinfwork.StateGraph;
using Majinfwork.World;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Majinfwork {
    public class LoadLevelAddressable : StateNodeAsset {
        [SerializeField] private AssetReference sceneAddressable;
        [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Single;
        public StateTransition onComplete;
        public StateTransition onFailed;

        private AsyncOperationHandle<SceneInstance> sceneHandle;

        public override void Begin() {
            ServiceLocator.Resolve<LoadingStreamer>().StartLoading(LoadScene);
        }

        public override void Tick() {

        }

        public override void End() {

        }

        private void LoadScene() {
            sceneHandle = Addressables.LoadSceneAsync(sceneAddressable, loadSceneMode);
            sceneHandle.Completed += OnSceneLoaded;
        }

        private void OnSceneLoaded(AsyncOperationHandle<SceneInstance> handle) {
            sceneHandle.Completed -= OnSceneLoaded;

            if (handle.Status == AsyncOperationStatus.Succeeded) {
                TriggerExit(onComplete);
            } else {
                Debug.LogError($"Failed to load addressable scene: {sceneAddressable.RuntimeKey}");
                TriggerExit(onFailed);
            }
        }
    }
}
#endif
