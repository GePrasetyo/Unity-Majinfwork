using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Majinfwork.World {
    internal sealed class GameWorldSettings : ScriptableObject {
        [SerializeReference, ClassReference] internal GameInstance classGameInstance;
        [SerializeField] private WorldConfig worldConfigObject;
        [SerializeField] private GameScriptableObject[] preInitializeSciptableObjects = Array.Empty<GameScriptableObject>();

        [RuntimeInitializeOnLoadMethod]
        private static void WorldBuilderStart() {
            var instance = Resources.Load<GameWorldSettings>(nameof(GameWorldSettings));

            if (instance == null) {
                Debug.LogError("You don't have world settings, please create the world setting first");
                return;
            }

#if UNITY_EDITOR
            if (!SessionState.GetBool(GameWorldSession.PlayWithFrameworkKey, false)) {
                return;
            }
#endif

            if (instance.worldConfigObject == null) {
                Debug.LogError("You don't have World Config, please attach World Config first");
                return;
            }

            instance.worldConfigObject.SetupSceneConfiguration();

            ServiceLocator.Register<GameInstance>(instance.classGameInstance);
            instance.classGameInstance.Construct(instance.worldConfigObject);

            Application.quitting += instance.OnGameQuit;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeInstanceScriptableObject() {
            var instance = Resources.Load<GameWorldSettings>(nameof(GameWorldSettings));

            if (instance == null) {
                Debug.LogError("You don't have world settings, please create the world setting first");
                return;
            }

            for (int i = 0; i < instance.preInitializeSciptableObjects.Length; i++) {
                instance.preInitializeSciptableObjects[i]?.PreInitialize();
            }
        }

        private void OnGameQuit() {
            ServiceLocator.Unregister<GameInstance>(out string message);
            PlayerManager.Clear();
            classGameInstance.Deconstruct();
        }
    }

#if UNITY_EDITOR
    public static class GameWorldSession {
        public const string PlayWithFrameworkKey = "Majinfwork_PlayWithFramework";
    }
#endif
}