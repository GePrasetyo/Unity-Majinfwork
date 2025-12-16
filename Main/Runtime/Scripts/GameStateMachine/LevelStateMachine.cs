using Majinfwork.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majinfwork.FSM {
    public class LevelStateMachine : GameStateMachine {
        [SerializeField] private SceneReference map;

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
        }

        private void LoadScene() {
            SceneManager.LoadScene(map.mapName, LoadSceneMode.Single);
        }
    }
}