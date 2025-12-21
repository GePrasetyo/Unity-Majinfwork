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
        [SerializeField] protected RuntimeAnimatorController gameStateMachine;

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

            var fsm = new GameObject().AddComponent<Animator>();
            fsm.name = "[Service] State Machine";
            fsm.runtimeAnimatorController = gameStateMachine;
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
            }
        }

        protected void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if(mode != LoadSceneMode.Single) {
                return;
            }

            if (worldSetting?.MapConfigList.ContainsKey(scene.name) == true) {
                worldSetting.MapConfigList[scene.name].TheGameMode.OnActive();
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