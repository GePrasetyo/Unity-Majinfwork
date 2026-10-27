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

        /// <summary>Runs the game state machine for the whole game; null without one.</summary>
        public StateRunner StateMachine { get; private set; }
#endif

        /// <summary>
        /// The LevelManager for loading scenes and managing GameModes.
        /// </summary>
        public LevelManager LevelManager { get; private set; }

        /// <summary>
        /// The currently active GameMode (delegated to LevelManager).
        /// </summary>
        public GameModeManager CurrentGameMode => LevelManager?.CurrentGameMode;

        /// <summary>
        /// Spawns an additional player (Controller + State).
        /// Use for split-screen or late-joining local players.
        /// </summary>
        public PlayerController SpawnPlayer() {
            return PlayerManager.SpawnPlayer();
        }

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
                // The runner's Start prepares the graph (a clone unless shared); preparing it here too would run the asset itself.
                StateMachine = new GameObject("[Service] State Machine").AddComponent<StateRunner>();
                StateMachine.SetGraph(gameStateMachine);
                Object.DontDestroyOnLoad(StateMachine.gameObject);
            }
#endif
        }

        public void Deconstruct() {
#if HAS_STATEGRAPH
            if (StateMachine != null) {
                Object.Destroy(StateMachine.gameObject);
                StateMachine = null;
            }
#endif

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
