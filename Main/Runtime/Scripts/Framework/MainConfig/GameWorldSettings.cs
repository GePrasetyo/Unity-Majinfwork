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
    /// persistent PlayerController and PlayerState, the game's HUD, the save system and the PSO warmup. Like Unreal's Maps &amp; Modes project
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
        [Tooltip("A local player's state, made with their controller and kept for the whole game. None in a game whose " +
                 "players' states all come from its network sessions.")]
        [SerializeField] private PlayerState playerStatePrefab;
#if HAS_NETCODE
        [Tooltip("Each player's state in a network session, spawned by the server's GameMode for every player who joins: a " +
                 "NetworkObject prefab registered with the NetworkManager.")]
        [SerializeField] private PlayerState networkPlayerStatePrefab;
#endif

        [Header("HUD")]
        [Tooltip("The game's UI, made once at boot after the GameInstance has started, and kept for the whole game (none: a " +
                 "game without UI).")]
        [SerializeField] private HUD hudPrefab;

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
            // A Multiplayer Play Mode virtual player never gets the main editor's Play With Framework flag, and only plays
            // when the main editor does: it always boots.
            if (!SessionState.GetBool(GameWorldSession.PlayWithFrameworkKey, false) && !MultiplayerPlayMode.IsVirtualPlayer) {
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

        private static async void ContinueBoot(GameWorldSettings instance) {
            // Register core services first so anything that runs during spawn/construct can resolve them.
            Task saves = null;
            if (instance.enableSaveSystem) {
                var saveService = new SaveDataService(slotCount: instance.saveSlotCount);
                ServiceLocator.Register<ISaveDataService>(saveService);
                saves = InitializeSaveServiceAsync(saveService, instance.defaultSlotIndex);
            }
            ServiceLocator.Register<GameInstance>(instance.classGameInstance);
            Application.quitting += instance.OnGameQuit;

            // Players and the game instance read their saves as they start: they come up once the saves are loaded, so
            // nothing ever sees a stand-in for what is on disk.
            if (saves != null) {
                try {
                    await saves;
                }
                catch (System.Exception e) {
                    Debug.LogException(e);
                }
            }

            // Then do work that may depend on them (PlayerState.OnCreated resolves GameInstance).
            PlayerManager.Initialize(instance.playerControllerPrefab, instance.playerStatePrefab);
#if HAS_NETCODE
            PlayerManager.NetworkStatePrefab = instance.networkPlayerStatePrefab;
#endif
            PlayerManager.SpawnPlayer();

            instance.classGameInstance.Construct(instance.worldConfigObject);
            // After the GameInstance has started: what the HUD's components look up as they wake is registered by then.
            HUD.Create(instance.hudPrefab);
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
            HUD.Release();
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
