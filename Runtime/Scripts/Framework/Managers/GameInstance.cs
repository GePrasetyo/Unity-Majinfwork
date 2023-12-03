using Majingari.Framework.World;

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Majingari.Framework {
    [Serializable]
    public class GameInstance {
        protected WorldConfig worldSetting;
        [SerializeField] protected bool stopLoadingOnSceneLoaded;
        [SerializeField] protected RuntimeAnimatorController gameStateMachine;

        public GameInstance() {
            Debug.Log($"Game Instance generated : {this.GetType()}");

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Construct(WorldConfig _worldSetting) {
            worldSetting = _worldSetting;

            var fsm = new GameObject().AddComponent<Animator>();
            fsm.name = "[Service] State Machine";
            fsm.runtimeAnimatorController = gameStateMachine;
            Object.DontDestroyOnLoad(fsm.gameObject);

            Start();
            ServiceLocator.Resolve<TickSignal>().RegisterObject(Tick);
        }

        public void Deconstruct() {
            ServiceLocator.Resolve<TickSignal>().UnRegisterObject(Tick);
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
            ServiceLocator.Resolve<TickSignal>().RegisterObject(Tick);
        }

        protected virtual void Tick() {

        }

        protected virtual void Stop() {

        }
    }
}