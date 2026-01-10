using Majinfwork.StateGraph;
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
        [SerializeField] protected GameStateMachineGraph gameStateMachine;

        /// <summary>
        /// The currently active GameMode for the loaded scene.
        /// </summary>
        protected GameModeManager CurrentGameMode { get; private set; }

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

            var fsm = new GameObject().AddComponent<StateRunner>();
            fsm.name = "[Service] State Machine";
            fsm.SetGraph(gameStateMachine);
            fsm.SetRuntimeGraph(gameStateMachine);
            Object.DontDestroyOnLoad(fsm.gameObject);

            Start();
            TickSignal.AddSystem<UnityEngine.PlayerLoop.Update>(typeof(GameInstance), Tick);
        }

        public void Deconstruct() {
            TickSignal.RemoveSystem<UnityEngine.PlayerLoop.Update>(typeof(GameInstance));
            Stop();
        }

        private void OnSceneUnloaded(Scene scene) {
            if (worldSetting?.MapConfigList.ContainsKey(scene.name) == true) {
                worldSetting.MapConfigList[scene.name].TheGameMode.OnDeactive();
                CurrentGameMode = null;
            }
        }

        protected void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if(mode != LoadSceneMode.Single) {
                return;
            }

            if (worldSetting?.MapConfigList.ContainsKey(scene.name) == true) {
                CurrentGameMode = worldSetting.MapConfigList[scene.name].TheGameMode;
                CurrentGameMode.OnActive();
            }

            if(stopLoadingOnSceneLoaded) {
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