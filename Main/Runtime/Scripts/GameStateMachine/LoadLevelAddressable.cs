#if HAS_STATEGRAPH
using Majinfwork.StateGraph;
using Majinfwork.World;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Majinfwork {
    public class LoadLevelAddressable : StateNodeAsset {
        [SerializeField] private AssetReference sceneAddressable;
        [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Single;
        public StateTransition onComplete;
        public StateTransition onFailed;

        public override void Begin() {
            LoadSceneAsync();
        }

        public override void Tick() {
        }

        public override void End() {
        }

        private async void LoadSceneAsync() {
            var loadingStreamer = ServiceLocator.Resolve<LoadingStreamer>();

            // Fade in loading screen
            if (loadingStreamer != null) {
                await loadingStreamer.StartLoadingAsync();
            }

            // Load scene via Addressables
            var handle = Addressables.LoadSceneAsync(sceneAddressable, loadSceneMode);
            await handle.Task;

            // Fade out loading screen
            if (loadingStreamer != null) {
                await loadingStreamer.StopLoadingAsync();
            }

            if (handle.Status == AsyncOperationStatus.Succeeded) {
                TriggerExit(onComplete);
            }
            else {
                Debug.LogError($"Failed to load addressable scene: {sceneAddressable.RuntimeKey}");
                TriggerExit(onFailed);
            }
        }
    }
}
#endif
