using System;
using UnityEngine;

namespace Majinfwork {
    public class TickSignal : MonoBehaviour {
        private Action tickSubscriber;
        private Action fixedTickSubscriber;

        /// <summary>
        /// Be careful with redundant subscriber, make sure it's not duplicated by removing the existing if any
        /// </summary>
        /// <param name="subscriber"></param>
        public void RegisterObject(Action subscriber) {
            tickSubscriber += subscriber;
        }

        public void RegisterPhysicObject(Action subscriber) {
            fixedTickSubscriber += subscriber;
        }

        /// <summary>
        /// It is safe enough when you unregiester subscriber that is not exist in the subscriber list;
        /// </summary>
        /// <param name="subscriber"></param>
        public void UnRegisterObject(Action subscriber) {
            tickSubscriber -= subscriber;
        }

        /// <summary>
        /// It is safe enough when you unregiester subscriber that is not exist in the subscriber list;
        /// </summary>
        /// <param name="subscriber"></param>
        public void UnRegisterPhysicObject(Action subscriber) {
            fixedTickSubscriber -= subscriber;
        }

        private void Update() {
            tickSubscriber?.Invoke();
        }

        private void FixedUpdate() {
            fixedTickSubscriber?.Invoke();
        }

        private void OnDestroy() {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitService() {
            var obj = new GameObject().AddComponent<TickSignal>();
            obj.name = "[Service] Tick Signal";
            DontDestroyOnLoad(obj.gameObject);
            ServiceLocator.Register<TickSignal>(obj);
        }
    }
}