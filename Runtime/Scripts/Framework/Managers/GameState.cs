namespace Majingari.Framework.World {
    public class GameState : GameComponent {
        private void Start() {
            ServiceLocator.Register<GameState>(this);
        }

        private void OnDestroy() {
            ServiceLocator.Unregister<GameState>(out string message);
        }
    }

}