using Majingari.Framework.World;

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Majingari.Framework {
    [Serializable]
    public class GameInstance {
        protected WorldConfig worldSetting;
        [SerializeField] protected RuntimeAnimatorController gameStateMachine;

        public GameInstance() {
            Debug.Log($"Game Instance generated : {this.GetType()}");

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        public void Construct(WorldConfig _worldSetting) {
            worldSetting = _worldSetting;

            var fsm = new GameObject().AddComponent<Animator>();
            fsm.name = "[Service] State Machine";
            fsm.runtimeAnimatorController = gameStateMachine;
            Object.DontDestroyOnLoad(fsm);

            Start();
        }

        protected void OnActiveSceneChanged(Scene prevScene, Scene nextScene) {

        }

        protected void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if(mode != LoadSceneMode.Single) {
                return;
            }

            if (worldSetting.MapConfigList.ContainsKey(scene.name)) {
                worldSetting.MapConfigList[scene.name].TheGameMode.InitiateGameManager();
                worldSetting.MapConfigList[scene.name].TheGameMode.InstantiatePlayer();
            }
        }

        protected virtual void Start() {
            ServiceLocator.Resolve<TickSignal>().RegisterObject(Tick);
        }

        protected virtual void Tick() {

        }
    }
}