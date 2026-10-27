#if HAS_STATEGRAPH
using System;
using System.Threading;
using System.Threading.Tasks;
using Majinfwork.StateGraph;
using Majinfwork.World;
using UnityEngine;

namespace Majinfwork {
    /// <summary>
    /// StateGraph node that loads a level using LevelManager.
    /// Handles both scene loading and GameMode transitions.
    /// </summary>
    public class LoadLevel : StateNodeAsset {
        [SerializeField] private SceneReference map;
        public StateTransition onComplete;

        public override void Begin(StateContext ctx) {
            _ = LoadSceneAsync(ctx, ctx.ExitToken);
        }

        private async Task LoadSceneAsync(StateContext ctx, CancellationToken token) {
            try {
                var loadingStreamer = ServiceLocator.Resolve<LoadingStreamer>();
                var levelManager = ServiceLocator.Resolve<LevelManager>();

                if (levelManager == null) {
                    Debug.LogError("[LoadLevel] LevelManager not found!");
                    return;
                }

                if (loadingStreamer != null) {
                    await loadingStreamer.StartLoadingAsync(token);
                }

                await levelManager.LoadLevelAsync(map.mapName, token);

                if (loadingStreamer != null) {
                    await loadingStreamer.StopLoadingAsync(token);
                }

                ctx.Exit(onComplete);
            }
            catch (OperationCanceledException) {
                // The state ended first: whatever it went to owns the level now.
            }
            catch (Exception exception) {
                Debug.LogException(exception);
            }
        }
    }
}
#endif
