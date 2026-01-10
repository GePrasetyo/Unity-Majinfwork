#if HAS_STATEGRAPH
using Majinfwork.StateGraph;
using Majinfwork.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majinfwork {
    public class LoadLevel : StateNodeAsset {
        [SerializeField] private SceneReference map;
        public StateTransition onComplete;

        public override void Begin() {
            ServiceLocator.Resolve<LoadingStreamer>().StartLoading(LoadScene);
        }

        public override void Tick() {
            
        }

        public override void End() {
            
        }

        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            TriggerExit(onComplete);
        }

        private void LoadScene() {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(map.mapName, LoadSceneMode.Single);
        }
    }
}
#endif
