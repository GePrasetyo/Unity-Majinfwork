using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Majingari.Framework.World {
    internal sealed class GameWorldSettings : ScriptableObject {
        [SerializeReference] public GameInstance classGameInstance;
        [SerializeField] private WorldConfig worldConfigObject;

        [Header("Player Setting")]
        [SerializeField] private bool editMode;

        [RuntimeInitializeOnLoadMethod]
        private static void WorldBuilderStart() {
            var obj = Resources.Load<GameWorldSettings>(nameof(GameWorldSettings));

            if(obj == null) {
                Debug.LogError("You don't have world settings, please create the world setting first");
                return;
            }

            if (obj.editMode) {
                return;
            }

            if (obj.worldConfigObject == null) {
                Debug.LogError("You don't have World Config, please attach World Config first");
                return;
            }

            obj.worldConfigObject.SetupDictionary();

            ServiceLocator.Register<GameInstance>(obj.classGameInstance);
            obj.classGameInstance.Construct(obj.worldConfigObject);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void OnEditorLoad() {
            string assetPath = "Assets/Resources/GameWorldSettings.asset";
            GameWorldSettings asset = AssetDatabase.LoadAssetAtPath<GameWorldSettings>(assetPath);

            if (asset == null) {
                Debug.LogError("WARNING : You don't have GameWorldSettings");
                CreateGameWorldAsset();
            }
        }

        [MenuItem("Game Word Settings/Get World Settings")]
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
            Debug.LogError("GameWorldSettings Created");
            string resourcesPath = "Assets/Resources";
            var asset = ScriptableObject.CreateInstance<GameWorldSettings>();

            if (!AssetDatabase.IsValidFolder(resourcesPath)) {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string assetPath = resourcesPath + "/GameWorldSettings.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }
#endif
    }
}
