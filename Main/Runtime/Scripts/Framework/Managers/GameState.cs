using System;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// The state of the game a GameMode runs, for everyone to read: where its rules are (a phase, its timers) and their
    /// data (score...). The GameMode makes it when it starts and removes it when it ends. In a network session the server
    /// spawns it and every peer receives it, its state replicated; outside one it is this device's alone. Resolvable from
    /// the ServiceLocator while it is here, and it is <see cref="Current"/>: whatever reads the game binds to it when it
    /// comes (<see cref="CurrentChanged"/>), so it never has to wait for it.
    /// </summary>
    public class GameState : NetworkActor {
        /// <summary>The GameState on this device (the server's, received here, or the local one), or null between GameModes.</summary>
        public static GameState Current { get; private set; }

        /// <summary>Raised on this device when <see cref="Current"/> changes: a GameMode's GameState came, or went.</summary>
        public static event Action CurrentChanged;

        protected virtual void OnEnable() {
            ServiceLocator.Register<GameState>(this);
            Current = this;
            CurrentChanged?.Invoke();
        }

        protected virtual void OnDisable() {
            if (ReferenceEquals(ServiceLocator.Resolve<GameState>(), this)) {
                ServiceLocator.Unregister<GameState>(out _);
            }

            if (ReferenceEquals(Current, this)) {
                Current = null;
                CurrentChanged?.Invoke();
            }
        }

        // Play mode without a domain reload keeps statics: each play starts with no GameState and no listeners.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            Current = null;
            CurrentChanged = null;
        }
    }
}
