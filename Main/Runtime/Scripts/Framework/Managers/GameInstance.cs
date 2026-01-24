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
        [SerializeField] protected bool stopLoadingOnSceneLoaded;
#if HAS_STATEGRAPH
        [SerializeField] protected GameStateMachineGraph gameStateMachine;
#endif

        /// <summary>
        /// The currently active GameMode for the loaded scene (cloned instance).
        /// </summary>
        protected GameModeManager CurrentGameMode { get; private set; }

        /// <summary>
        /// The template GameModeManager that CurrentGameMode was cloned from.
        /// Used for resolving cross-scene references.
        /// </summary>
        private GameModeManager currentGameModeTemplate;

        public GameInstance() {
            Debug.Log($"Game Instance generated : {GetType()}");

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public static bool TryGetGameInstance <T> (out T gameInstance) where T : GameInstance{
            gameInstance = ServiceLocator.Resolve<GameInstance>() as T;
            return gameInstance != null;
        }

        public static T Instance<T> () where T : GameInstance => ServiceLocator.Resolve<GameInstance>() as T;

        public void Construct(WorldConfig _worldSetting) {
            worldSetting = _worldSetting;
            Start();
            TickSignal.AddSystem<UnityEngine.PlayerLoop.Update>(typeof(GameInstance), Tick);

#if HAS_STATEGRAPH
            var fsm = new GameObject().AddComponent<StateRunner>();
            fsm.name = "[Service] State Machine";
            fsm.SetGraph(gameStateMachine);
            fsm.SetRuntimeGraph(gameStateMachine);
            Object.DontDestroyOnLoad(fsm.gameObject);
#endif
        }

        public void Deconstruct() {
            TickSignal.RemoveSystem<UnityEngine.PlayerLoop.Update>(typeof(GameInstance));
            Stop();
        }

        private void OnSceneUnloaded(Scene scene) {
            // Handled in OnSceneLoaded - we only deactivate when replacing
        }

        protected void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (mode != LoadSceneMode.Single) {
                return;
            }

            // Get the GameMode template for this scene (or use default)
            GameModeManager template = null;
            if (worldSetting?.MapConfigList.ContainsKey(scene.name) == true) {
                template = worldSetting.MapConfigList[scene.name].TheGameMode;
            }
            template ??= worldSetting?.DefaultGameMode;

            if (template != null) {
                // Deactivate and destroy current cloned GameMode
                if (CurrentGameMode != null) {
                    CurrentGameMode.OnDeactive();
                    Object.Destroy(CurrentGameMode);
                    CurrentGameMode = null;
                    currentGameModeTemplate = null;
                }

                // Clone fresh instance from template
                CurrentGameMode = Object.Instantiate(template);
                currentGameModeTemplate = template;
                CurrentGameMode.ResolveCrossSceneReferences(template);
                CurrentGameMode.OnActive();
            }

            if (stopLoadingOnSceneLoaded) {
                ServiceLocator.Resolve<LoadingStreamer>().StopLoading();
            }
        }

        protected virtual void Start() {

        }

        protected virtual void Tick() {

        }

        protected virtual void Stop() {

        }
    }
}
