namespace Majinfwork.World {
    public class PlayerState : Actor {
        /// <summary>
        /// Called by GameModeManager after instantiation, before pawn creation.
        /// Override to initialize from persistent data (GameInstance, save system, etc.).
        /// </summary>
        public virtual void OnCreated() { }
    }
}
