using Majingari.Framework.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majingari.FSM {
    public class LevelState : GameState {
        [SerializeField] private SceneReference map;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateEnter(animator, stateInfo, layerIndex);
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            ServiceLocator.Resolve<LoadingStreamer>().StartLoading(LoadScene);
        }

        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void LoadScene() {
            SceneManager.LoadScene(map.mapName, LoadSceneMode.Single);
        }
    }
}