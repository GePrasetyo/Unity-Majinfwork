using UnityEngine;

namespace Majingari.Framework.World {
    public class GameState : MonoBehaviour {
        private void Start() {
            ServiceLocator.Register<GameState>(this);
        }
    }

}