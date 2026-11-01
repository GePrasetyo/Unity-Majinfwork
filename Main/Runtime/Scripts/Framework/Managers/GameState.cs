using System;
using UnityEngine;
#if HAS_NETCODE
using Unity.Netcode;
#endif

namespace Majinfwork.World {
    /// <summary>
    /// The state of the game a GameMode runs, for everyone to read: where its rules are (a phase, its timers) and their
    /// data (score...). The GameMode makes it when it starts and removes it when it ends. In a network session the server
    /// spawns it and every peer receives it, its state replicated; outside one it is this device's alone. Resolvable from
    /// the ServiceLocator while it is here, and it is <see cref="Current"/>: whatever reads the game binds to it when it
    /// comes (<see cref="CurrentChanged"/>), so it never has to wait for it. In a session it comes once it is spawned, its
    /// replicated state readable; a subclass overriding the Netcode spawn callbacks calls the base ones first.
    /// </summary>
    public class GameState : NetworkActor {
        /// <summary>The GameState on this device (the server's, received here, or the local one), or null between GameModes.</summary>
        public static GameState Current { get; private set; }

        /// <summary>Raised on this device when <see cref="Current"/> changes: a GameMode's GameState came, or went.</summary>
        public static event Action CurrentChanged;

        protected virtual void OnEnable() {
#if HAS_NETCODE
            // In a session it is spawned (on the server, or received here) right after it is made: it comes then.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
                return;
            }
#endif
            Come();
        }

        protected virtual void OnDisable() {
            Go();
        }

#if HAS_NETCODE
        public override void OnNetworkSpawn() {
            Come();
        }

        public override void OnNetworkDespawn() {
            Go();
        }
#endif

        private void Come() {
            if (ReferenceEquals(Current, this)) {
                return;
            }

            ServiceLocator.Register<GameState>(this);
            Current = this;
            CurrentChanged?.Invoke();
        }

        private void Go() {
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
