#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Majinfwork.World {
    [InitializeOnLoad]
    internal static class GameWorldSettingsCreator {
        private const string ResourcesPath = "Assets/Resources";
        private const string PrefabsPath = "Assets/Resources/Prefabs";

        static GameWorldSettingsCreator() {
            EditorApplication.update += RunOnceWhenScriptsReloaded;
        }

        private static void RunOnceWhenScriptsReloaded() {
            if (EditorApplication.isCompiling) return;

            EditorApplication.update -= RunOnceWhenScriptsReloaded;

            var asset = AssetDatabase.LoadAssetAtPath<GameWorldSettings>("Assets/Resources/GameWorldSettings.asset");
            if (asset == null) {
                Debug.LogWarning("WARNING: You don't have GameWorldSettings");
                CreateGameWorldAsset();
            }
        }

        [MenuItem("Majingari Framework/Get World Settings")]
        public static void GetTheInstance() {
            var obj = Resources.Load<GameWorldSettings>(nameof(GameWorldSettings));

            if (obj != null) {
                Selection.activeObject = obj;
            }
            else {
                CreateGameWorldAsset();
            }
        }

        [MenuItem("Majingari Framework/Create World Settings")]
        public static void CreateGameWorldAsset() {
            Debug.Log("GameWorldSettings Created");

            EnsureFolderExists(ResourcesPath);
            EnsureFolderExists(PrefabsPath);

            // Create GameWorldSettings
            var worldSettings = ScriptableObject.CreateInstance<GameWorldSettings>();
            worldSettings.classGameInstance = new PersistentGameInstance();
            AssetDatabase.CreateAsset(worldSettings, $"{ResourcesPath}/GameWorldSettings.asset");

            // Create default GameModeManager with all prefabs
            var gameMode = CreateDefaultGameMode();

            // Create default WorldConfig
            var worldConfig = CreateDefaultWorldConfig(gameMode);

            // Assign WorldConfig to GameWorldSettings
            SetPrivateField(worldSettings, "worldConfigObject", worldConfig);

#if HAS_STATEGRAPH
            SetDefaultLevelState(worldSettings);
#endif

            EditorUtility.SetDirty(worldSettings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = worldSettings;
        }

        private static GameModeManager CreateDefaultGameMode() {
            var existing = Resources.Load<GameModeManager>("Default Game Mode Config");
            if (existing != null) return existing;

            var gameMode = ScriptableObject.CreateInstance<GameModeManager>();

            SetPrivateField(gameMode, "cameraHandler", new CameraHandlerNone());
            SetPrivateField(gameMode, "gameState", CreateDefaultPrefab<GameState>("Default GameState"));
            SetPrivateField(gameMode, "hudPrefab", CreateDefaultPrefab<HUD>("Default HUD"));
            SetPrivateField(gameMode, "playerControllerPrefab", CreateDefaultPrefab<PlayerController>("Default PlayerController"));
            SetPrivateField(gameMode, "playerStatePrefab", CreateDefaultPrefab<PlayerState>("Default PlayerState"));
            SetPrivateField(gameMode, "playerPawnPrefab", CreateDefaultPrefab<PlayerPawn>("Default PlayerPawn"));
            SetPrivateField(gameMode, "playerInputPrefab", CreateDefaultPrefab<PlayerInput>("Default PlayerInput"));

            AssetDatabase.CreateAsset(gameMode, $"{ResourcesPath}/Default Game Mode Config.asset");
            Debug.Log("Default Game Mode Config Created");
            return gameMode;
        }

        private static T CreateDefaultPrefab<T>(string prefabName) where T : Component {
            string prefabPath = $"{PrefabsPath}/{prefabName}.prefab";

            var existingPrefab = AssetDatabase.LoadAssetAtPath<T>(prefabPath);
            if (existingPrefab != null) return existingPrefab;

            var go = new GameObject(prefabName);
            go.AddComponent<T>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"Created prefab: {prefabName}");
            return prefab.GetComponent<T>();
        }

        private static WorldConfig CreateDefaultWorldConfig(GameModeManager defaultGameMode) {
            var existing = Resources.Load<WorldConfig>("Default World Config");
            if (existing != null) return existing;

            var worldConfig = ScriptableObject.CreateInstance<WorldConfig>();

            SetPrivateField(worldConfig, "loadingHandler", new LoadingStreamerDefault());
            SetPrivateField(worldConfig, "defaultGameMode", defaultGameMode);

            AssetDatabase.CreateAsset(worldConfig, $"{ResourcesPath}/Default World Config.asset");
            Debug.Log("Default World Config Created");
            return worldConfig;
        }

#if HAS_STATEGRAPH
        private static void SetDefaultLevelState(GameWorldSettings worldSettings) {
            var field = typeof(GameInstance).GetField("gameStateMachine", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null) {
                Debug.LogError("Failed to find field 'gameStateMachine' via reflection.");
                return;
            }

            var asset = Resources.Load<GameStateMachineGraph>("GameStateMachine");

            if (asset == null) {
                asset = ScriptableObject.CreateInstance<GameStateMachineGraph>();
                AssetDatabase.CreateAsset(asset, $"{ResourcesPath}/GameStateMachine.asset");
                Debug.Log("GameStateMachine Created");
            }

            field.SetValue(worldSettings.classGameInstance, asset);
        }
#endif

        private static void SetPrivateField(object target, string fieldName, object value) {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) {
                field.SetValue(target, value);
            }
            else {
                Debug.LogWarning($"Could not find field '{fieldName}' on {target.GetType().Name}");
            }
        }

        private static void EnsureFolderExists(string path) {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            var folder = System.IO.Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parent)) {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
