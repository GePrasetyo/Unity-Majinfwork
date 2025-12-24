using UnityEngine;
using System;
using System.Reflection;
using Majinfwork.StateGraph;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Majinfwork.World {
    internal sealed class GameWorldSettings : ScriptableObject {
        [SerializeReference, ClassReference] public GameInstance classGameInstance;
        [SerializeField] private WorldConfig worldConfigObject;
        [SerializeField] private GameScriptableObject[] preInitializeSciptableObjects = Array.Empty<GameScriptableObject>(); 

        [Header("Player Setting")]
        [SerializeField] private bool editMode;

        [RuntimeInitializeOnLoadMethod]
        private static void WorldBuilderStart() {
            var instance = Resources.Load<GameWorldSettings>(nameof(GameWorldSettings));

            if(instance == null) {
                Debug.LogError("You don't have world settings, please create the world setting first");
                return;
            }

            if (instance.editMode) {
                return;
            }

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
            classGameInstance.Deconstruct();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void OnEditorLoad() {
            EditorApplication.update += RunOnceWhenScriptsReloaded;
        }

        private static void RunOnceWhenScriptsReloaded() {
            if (!EditorApplication.isCompiling) {
                EditorApplication.update -= RunOnceWhenScriptsReloaded;

                string assetPath = "Assets/Resources/GameWorldSettings.asset";
                GameWorldSettings asset = AssetDatabase.LoadAssetAtPath<GameWorldSettings>(assetPath);

                if (asset == null) {
                    Debug.LogError("WARNING : You don't have GameWorldSettings");
                    CreateGameWorldAsset();
                }
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

        private static void CreateGameWorldAsset() {
            Debug.Log("GameWorldSettings Created");
            string resourcesPath = "Assets/Resources";
            var asset = CreateInstance<GameWorldSettings>();

            if (!AssetDatabase.IsValidFolder(resourcesPath)) {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string assetPath = resourcesPath + "/GameWorldSettings.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.classGameInstance = new PersistentGameInstance();
            AssetDatabase.SaveAssets();

            SetDefaultLevelState(asset);
            Selection.activeObject = asset;
        }

        private static void SetDefaultLevelState (GameWorldSettings worldSettings) {
            FieldInfo field = typeof(GameInstance).GetField("gameStateMachine", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null) {
                Debug.LogError("Failed to find field 'gameStateMachine' via reflection. Check spelling and access level.");
                return;
            }

            GameStateMachineGraph asset = Resources.Load<GameStateMachineGraph>("GameStateMachine");

            if (asset == null) {
                string resourcesPath = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesPath)) {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                asset = ScriptableObject.CreateInstance<GameStateMachineGraph>();
                string fullPath = $"{resourcesPath}/GameStateMachine.asset";
                AssetDatabase.CreateAsset(asset, fullPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            field.SetValue(worldSettings.classGameInstance, asset);
        }
#endif
    }
}
