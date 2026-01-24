using UnityEngine;
using System;
using System.Threading.Tasks;
using Majinfwork.SaveSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Majinfwork.World {
    internal sealed class GameWorldSettings : ScriptableObject {
        [SerializeReference, ClassReference] internal GameInstance classGameInstance;
        [SerializeField] private WorldConfig worldConfigObject;
        [SerializeField] private GameScriptableObject[] preInitializeSciptableObjects = Array.Empty<GameScriptableObject>();

        [Header("Save System")]
        [SerializeField] private bool enableSaveSystem = true;
        [SerializeField] private int saveSlotCount = 1;
        [SerializeField] private int defaultSlotIndex = 0;

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

            // Initialize save system (async, consumers await when needed)
            if (instance.enableSaveSystem) {
                var saveService = new SaveDataService(slotCount: instance.saveSlotCount);
                ServiceLocator.Register<ISaveDataService>(saveService);
                _ = InitializeSaveServiceAsync(saveService, instance.defaultSlotIndex);
            }

            ServiceLocator.Register<GameInstance>(instance.classGameInstance);
            instance.classGameInstance.Construct(instance.worldConfigObject);

            Application.quitting += instance.OnGameQuit;
        }

        private static async Task InitializeSaveServiceAsync(SaveDataService saveService, int slotIndex) {
            await saveService.InitializeAsync();
            saveService.SetCurrentSlot(slotIndex);
            await saveService.PreloadAllAsync();
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

            var saveService = ServiceLocator.Resolve<ISaveDataService>();
            if (saveService != null) {
                saveService.Shutdown();
                ServiceLocator.Unregister<ISaveDataService>(out _);
            }
        }
    }

#if UNITY_EDITOR
    public static class GameWorldSession {
        public const string PlayWithFrameworkKey = "Majinfwork_PlayWithFramework";
    }
#endif
}