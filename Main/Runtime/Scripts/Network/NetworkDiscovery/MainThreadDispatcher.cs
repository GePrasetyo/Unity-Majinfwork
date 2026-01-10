using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Majinfwork.Network {
    public class MainThreadDispatcher : MonoBehaviour {
        private static MainThreadDispatcher instance;
        private static readonly ConcurrentQueue<Action> pendingActions = new ConcurrentQueue<Action>();
        private static bool isInitialized;

        public static void Initialize() {
            if (isInitialized && instance != null) return;

            var go = new GameObject("[Network] MainThreadDispatcher");
            instance = go.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(go);
            isInitialized = true;
        }

        public static void Enqueue(Action action) {
            if (action == null) return;

            if (!isInitialized) {
                Debug.LogWarning("[MainThreadDispatcher] Not initialized. Call Initialize() first.");
                return;
            }

            pendingActions.Enqueue(action);
        }

        public static void Shutdown() {
            if (instance != null) {
                Destroy(instance.gameObject);
                instance = null;
            }

            // Clear any pending actions
            while (pendingActions.TryDequeue(out _)) { }

            isInitialized = false;
        }

        private void Update() {
            ProcessQueue();
        }

        private void ProcessQueue() {
            while (pendingActions.TryDequeue(out var action)) {
                try {
                    action();
                }
                catch (Exception ex) {
                    Debug.LogException(ex);
                }
            }
        }

        private void OnDestroy() {
            if (instance == this) {
                instance = null;
                isInitialized = false;
            }
        }
    }
}
