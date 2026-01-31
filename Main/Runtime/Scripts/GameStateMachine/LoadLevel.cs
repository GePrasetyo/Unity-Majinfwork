#if HAS_STATEGRAPH
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

        public override void Begin() {
            LoadSceneAsync();
        }

        public override void Tick() {
        }

        public override void End() {
        }

        private async void LoadSceneAsync() {
            var loadingStreamer = ServiceLocator.Resolve<LoadingStreamer>();
            var levelManager = ServiceLocator.Resolve<LevelManager>();

            if (levelManager == null) {
                Debug.LogError("[LoadLevel] LevelManager not found!");
                return;
            }

            // Fade in loading screen
            if (loadingStreamer != null) {
                await loadingStreamer.StartLoadingAsync();
            }

            // Load the level
            await levelManager.LoadLevelAsync(map.mapName);

            // Fade out loading screen
            if (loadingStreamer != null) {
                await loadingStreamer.StopLoadingAsync();
            }

            TriggerExit(onComplete);
        }
    }
}
#endif
