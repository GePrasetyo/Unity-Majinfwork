using UnityEngine;
using System;
using System.Threading.Tasks;
using Majinfwork.SaveSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Majinfwork.World {
    /// <summary>
    /// The project's world settings: the GameInstance that runs the app, the WorldConfig (each scene's GameMode), the
    /// persistent PlayerController and PlayerState, the save system and the PSO warmup. Like Unreal's Maps &amp; Modes project
    /// settings there is exactly one per project: the asset registered in Project Settings &gt; Majingari Framework, which can
    /// live in any folder. A build carries it as a preloaded asset, so it is loaded before the first scene and the framework
    /// boots from it; in the editor the registered asset is used directly.
    /// </summary>
    public sealed class GameWorldSettings : ScriptableObject {
        /// <summary>The key the project's settings are registered under (EditorBuildSettings config objects).</summary>
        public const string ConfigKey = "com.majingari.framework.worldsettings";

        [SerializeReference, ClassReference] internal GameInstance classGameInstance;
        [SerializeField] private WorldConfig worldConfigObject;
        [SerializeField] private GameScriptableObject[] preInitializeSciptableObjects = Array.Empty<GameScriptableObject>();

        [Header("Player Setup")]
        [SerializeField] private PlayerController playerControllerPrefab;
        [SerializeField] private PlayerState playerStatePrefab;

        [Header("Save System")]
        [SerializeField] private bool enableSaveSystem = true;
        [SerializeField] private int saveSlotCount = 1;
        [SerializeField] private int defaultSlotIndex = 0;

        [Header("PSO Warmup")]
        [Tooltip("Warms up the pipeline states before the first scene (none: no warmup).")]
        [SerializeField] private PSOWarmupConfig psoWarmup;

#if !UNITY_EDITOR
        private static GameWorldSettings preloaded;

        // A player loads the build's settings (its one preloaded GameWorldSettings) before the first scene.
        private void OnEnable() {
            preloaded = this;
        }
#endif

        /// <summary>The project's settings: the registered asset in the editor, the preloaded one in a player.</summary>
        internal static GameWorldSettings Active {
            get {
#if UNITY_EDITOR
                EditorBuildSettings.TryGetConfigObject(ConfigKey, out GameWorldSettings registered);
                return registered;
#else
                return preloaded;
#endif
            }
        }

        private static GameWorldSettings Require() {
            GameWorldSettings settings = Active;
            if (settings == null) {
                Debug.LogError("[Majingari Framework] This project has no world settings: create or pick them in Project Settings > Majingari Framework.");
            }

            return settings;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void WorldBuilderStart() {
            var instance = Require();
            if (instance == null) {
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

            PSOWarmupConfig psoConfig = instance.psoWarmup;
            bool shouldWarmup = psoConfig != null;
#if UNITY_EDITOR
            if (shouldWarmup && psoConfig.SkipInEditor)
                shouldWarmup = false;
#endif
            if (shouldWarmup) {
                RunWarmupThenBoot(instance, psoConfig);
                return;
            }

            ContinueBoot(instance);
        }

        private static async void RunWarmupThenBoot(GameWorldSettings instance, PSOWarmupConfig psoConfig) {
            try {
                var runner = new PSOWarmupRunner(psoConfig);
                await runner.RunAsync();
            }
            catch (System.Exception e) {
                Debug.LogWarning($"[GameWorldSettings] PSO warmup failed, continuing boot: {e.Message}");
            }

            ContinueBoot(instance);
        }

        private static void ContinueBoot(GameWorldSettings instance) {
            // Register core services first so anything that runs during spawn/construct can resolve them.
            if (instance.enableSaveSystem) {
                var saveService = new SaveDataService(slotCount: instance.saveSlotCount);
                ServiceLocator.Register<ISaveDataService>(saveService);
                _ = InitializeSaveServiceAsync(saveService, instance.defaultSlotIndex);
            }
            ServiceLocator.Register<GameInstance>(instance.classGameInstance);

            // Then do work that may depend on them (PlayerState.OnCreated resolves GameInstance).
            PlayerManager.Initialize(instance.playerControllerPrefab, instance.playerStatePrefab);
            PlayerManager.SpawnPlayer();

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
            var instance = Require();
            if (instance == null) {
                return;
            }

            for (int i = 0; i < instance.preInitializeSciptableObjects.Length; i++) {
                instance.preInitializeSciptableObjects[i]?.PreInitialize();
            }
        }

        private void OnGameQuit() {
            ServiceLocator.Unregister<GameInstance>(out string message);
            PlayerManager.DestroyAll();
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
