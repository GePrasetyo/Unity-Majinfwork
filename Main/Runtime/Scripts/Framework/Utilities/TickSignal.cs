using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Majinfwork {
    public static partial class TickSignal {
        private static readonly List<ITickObject> tickCollection = new();
        private static readonly List<IFixedTickObject> fixedTickCollection = new();

        public static void RegisterTick(this ITickObject objectTick) {
            if (objectTick != null && !tickCollection.Contains(objectTick)) {
                tickCollection.Add(objectTick);
            }
        }

        public static void RegisterTick(this IFixedTickObject objectTick) {
            if (objectTick != null && !fixedTickCollection.Contains(objectTick)) {
                fixedTickCollection.Add(objectTick);
            }
        }

        public static void UnregisterTick(this ITickObject objectTick) => tickCollection.Remove(objectTick);

        public static void UnregisterTick(this IFixedTickObject objectFixedTick) => fixedTickCollection.Remove(objectFixedTick);

        [RuntimeInitializeOnLoadMethod]
        private static void Init() {
            AddSystem<Update>(typeof(MainTickSignalTag), Update);
            AddSystem<Update>(typeof(FixedTickSignalTag), FixedUpdate);

            var defaultSystems = PlayerLoop.GetDefaultPlayerLoop();

            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
        }

        private static void Update() {
            for (int i = tickCollection.Count - 1; i >= 0; i--) {
                var current = tickCollection[i];
                if (current == null) {
                    tickCollection.RemoveAt(i);
                    continue;
                }

                try {
                    current.Tick();
                }
                catch (Exception ex) {
                    Debug.LogException(ex);
                }
            }
        }

        private static void FixedUpdate() {
            for (int i = fixedTickCollection.Count - 1; i >= 0; i--) {
                var current = fixedTickCollection[i];
                if (current == null) {
                    fixedTickCollection.RemoveAt(i);
                    continue;
                }

                try {
                    current.FixedTick();
                }
                catch (Exception ex) {
                    Debug.LogException(ex);
                }
            }
        }

        private static void OnQuit() {
            // Restore the Unity PlayerLoop to its original state
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());

            // Clear lists to free references
            tickCollection.Clear();
            fixedTickCollection.Clear();

            Debug.Log("TickSignal: PlayerLoop restored and collections cleared.");
        }
    }

    public class MainTickSignalTag { }
    public class FixedTickSignalTag { }

    public interface ITickObject {
        public void Tick();
    }

    public interface IFixedTickObject {
        public void FixedTick();
    }
}