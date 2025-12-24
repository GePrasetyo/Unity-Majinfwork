using Majinfwork.StateGraph;
using Majinfwork.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majinfwork {
    public class LoadLevel : StateNodeAsset {
        [SerializeField] private SceneReference map;
        public StateTransition onComplete;

        public override void Begin() {
            SceneManager.sceneLoaded += OnSceneLoaded;
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
            SceneManager.LoadScene(map.mapName, LoadSceneMode.Single);
        }
    }
}