using System;
using UnityEngine;

namespace Majingari.Framework {
    public class TickSignal : MonoBehaviour {
        internal event Action tickSubscriber;

        public void RegisterObject(Action subscriber) {
            tickSubscriber += subscriber;
        }

        public void UnRegisterObject(Action subscriber) {
            tickSubscriber -= subscriber;
        }

        private void Update() {
            tickSubscriber?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void InitService() {
            var obj = new GameObject().AddComponent<TickSignal>();
            obj.name = "[Service] Tick Signal";
            DontDestroyOnLoad(obj);
            ServiceLocator.Register<TickSignal>(obj);
        }
    }
}