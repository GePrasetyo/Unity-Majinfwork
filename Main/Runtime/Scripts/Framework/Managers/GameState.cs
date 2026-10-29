namespace Majinfwork.World {
    /// <summary>
    /// The state of the game a GameMode runs, for everyone to read: where its rules are (a phase, its timers) and their
    /// data (score...). The GameMode makes it when it starts and removes it when it ends. In a network session the server
    /// spawns it and every peer receives it, its state replicated; outside one it is this device's alone. Resolvable from
    /// the ServiceLocator while it is here.
    /// </summary>
    public class GameState : NetworkActor {
        protected virtual void OnEnable() {
            ServiceLocator.Register<GameState>(this);
        }

        protected virtual void OnDisable() {
            if (ReferenceEquals(ServiceLocator.Resolve<GameState>(), this)) {
                ServiceLocator.Unregister<GameState>(out _);
            }
        }
    }
}
