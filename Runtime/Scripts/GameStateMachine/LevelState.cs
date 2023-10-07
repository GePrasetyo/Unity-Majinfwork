using Majingari.Framework.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majingari.FSM {
    public class LevelState : GameState {
        [SerializeField] private SceneReference map;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            SceneManager.LoadScene(map.mapName);
        }
    }
}