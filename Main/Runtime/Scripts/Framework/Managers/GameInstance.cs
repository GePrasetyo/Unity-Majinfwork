#if HAS_STATEGRAPH
using Majinfwork.StateGraph;
#endif
using Majinfwork.CrossRef;
using Majinfwork.World;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Majinfwork {
    [Serializable]
    public class GameInstance {
        protected WorldConfig worldSetting;

#if HAS_STATEGRAPH
        [Header("State Machine")]
        [SerializeField] protected GameStateMachineGraph gameStateMachine;
#endif

        /// <summary>
        /// The LevelManager for loading scenes and managing GameModes.
        /// </summary>
        public LevelManager LevelManager { get; private set; }

        /// <summary>
        /// The currently active GameMode (delegated to LevelManager).
        /// </summary>
        public GameModeManager CurrentGameMode => LevelManager?.CurrentGameMode;

        public GameInstance() {
            // Note: Constructor may be called multiple times by Unity serialization.
            // Do NOT put anything here.
        }

        public static bool TryGetGameInstance<T>(out T gameInstance) where T : GameInstance {
            gameInstance = ServiceLocator.Resolve<GameInstance>() as T;
            return gameInstance != null;
        }

        public static T Instance<T>() where T : GameInstance => ServiceLocator.Resolve<GameInstance>() as T;

        public void Construct(WorldConfig _worldSetting) {
            worldSetting = _worldSetting;

            // Create LevelManager
            LevelManager = new LevelManager(worldSetting);
            ServiceLocator.Register<LevelManager>(LevelManager);

            Start();
            TickSignal.AddSystem<UnityEngine.PlayerLoop.Update>(typeof(GameInstance), Tick);

#if HAS_STATEGRAPH
            if (gameStateMachine != null) {
                var fsm = new GameObject().AddComponent<StateRunner>();
                fsm.name = "[Service] State Machine";
                fsm.SetGraph(gameStateMachine);
                fsm.SetRuntimeGraph(gameStateMachine);
                Object.DontDestroyOnLoad(fsm.gameObject);
            }
#endif
        }

        public void Deconstruct() {
            // Shutdown LevelManager
            if (LevelManager != null) {
                LevelManager.Shutdown();
                ServiceLocator.Unregister<LevelManager>(out _);
                LevelManager = null;
            }

            TickSignal.RemoveSystem<UnityEngine.PlayerLoop.Update>(typeof(GameInstance));
            Stop();
        }

        protected virtual void Start() {
        }

        protected virtual void Tick() {
        }

        protected virtual void Stop() {
        }
    }
}
