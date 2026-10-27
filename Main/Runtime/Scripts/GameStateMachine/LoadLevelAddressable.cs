#if HAS_STATEGRAPH
using System;
using System.Threading;
using System.Threading.Tasks;
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

        public override void Begin(StateContext ctx) {
            _ = LoadSceneAsync(ctx, ctx.ExitToken);
        }

        private async Task LoadSceneAsync(StateContext ctx, CancellationToken token) {
            try {
                var loadingStreamer = ServiceLocator.Resolve<LoadingStreamer>();

                if (loadingStreamer != null) {
                    await loadingStreamer.StartLoadingAsync(token);
                }

                var handle = Addressables.LoadSceneAsync(sceneAddressable, loadSceneMode);
                await handle.Task;
                token.ThrowIfCancellationRequested();

                if (loadingStreamer != null) {
                    await loadingStreamer.StopLoadingAsync(token);
                }

                if (handle.Status == AsyncOperationStatus.Succeeded) {
                    ctx.Exit(onComplete);
                }
                else {
                    Debug.LogError($"Failed to load addressable scene: {sceneAddressable.RuntimeKey}");
                    ctx.Exit(onFailed);
                }
            }
            catch (OperationCanceledException) {
                // The state ended first: whatever it went to owns the level now.
            }
            catch (Exception exception) {
                Debug.LogException(exception);
                ctx.Exit(onFailed);
            }
        }
    }
}
#endif
