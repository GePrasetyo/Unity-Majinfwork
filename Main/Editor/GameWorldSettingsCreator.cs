using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// Creates a project's world settings with working defaults next to them: a persistent game instance, default
    /// PlayerController and PlayerState, and a WorldConfig whose default GameMode (made Addressable, as GameModes load) has a
    /// default GameState, HUD, PlayerInput, camera handler and pawn.
    /// </summary>
    internal static class GameWorldSettingsCreator {
        private const string DefaultFolder = "Assets/Settings";

        /// <summary>Asks where the settings go, then creates them there with their defaults and registers them.</summary>
        public static void CreateWithDialog() {
            EnsureFolder(DefaultFolder);
            string path = EditorUtility.SaveFilePanelInProject("Create World Settings", nameof(GameWorldSettings), "asset",
                "Where the project's world settings live (any folder). Their defaults are created next to them.", DefaultFolder);
            if (!string.IsNullOrEmpty(path)) {
                Create(path);
            }
        }

        /// <summary>Creates the world settings at <paramref name="settingsPath"/> with their defaults beside them, and registers them.</summary>
        public static GameWorldSettings Create(string settingsPath) {
            string folder = Path.GetDirectoryName(settingsPath).Replace('\\', '/');
            EnsureFolder(folder);

            var settings = ScriptableObject.CreateInstance<GameWorldSettings>();
            AssetDatabase.CreateAsset(settings, settingsPath);
            WorldConfig world = CreateWorldConfig(folder, CreateGameMode(folder));

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("classGameInstance").managedReferenceValue = new PersistentGameInstance();
            serialized.FindProperty("worldConfigObject").objectReferenceValue = world;
            serialized.FindProperty("playerControllerPrefab").objectReferenceValue = DefaultPrefab<PlayerController>(folder, "Default PlayerController");
            serialized.FindProperty("playerStatePrefab").objectReferenceValue = DefaultPrefab<PlayerState>(folder, "Default PlayerState");
            serialized.ApplyModifiedPropertiesWithoutUndo();
#if HAS_STATEGRAPH
            // The game instance exists only once applied; its state machine is set on it afterwards.
            serialized.Update();
            serialized.FindProperty("classGameInstance.gameStateMachine").objectReferenceValue = LoadOrCreate<GameStateMachineGraph>($"{folder}/GameStateMachine.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            GameWorldSettingsEditor.SetActive(settings);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log($"[Majingari Framework] Created the project's world settings at {settingsPath}.");
            return settings;
        }

        private static GameModeManager CreateGameMode(string folder) {
            var mode = LoadOrCreate<GameModeManager>($"{folder}/Default GameMode.asset");
            var serialized = new SerializedObject(mode);
            serialized.FindProperty("gameState").objectReferenceValue = DefaultPrefab<GameState>(folder, "Default GameState");
            serialized.FindProperty("hudPrefab").objectReferenceValue = DefaultPrefab<HUD>(folder, "Default HUD");
            serialized.FindProperty("playerInputPrefab").objectReferenceValue = DefaultPrefab<PlayerInput>(folder, "Default PlayerInput");
            serialized.FindProperty("cameraHandler").managedReferenceValue = new CameraHandlerNone();
            serialized.FindProperty("pawnProvider").managedReferenceValue = new DefaultPawnProvider();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            serialized.Update();
            serialized.FindProperty("pawnProvider.pawnPrefab").objectReferenceValue = DefaultPrefab<PlayerPawn>(folder, "Default PlayerPawn");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return mode;
        }

        private static WorldConfig CreateWorldConfig(string folder, GameModeManager defaultMode) {
            var world = LoadOrCreate<WorldConfig>($"{folder}/Default WorldConfig.asset");

            // GameModes load through Addressables, so the default one is an Addressable entry.
            AddressableAssetSettings addressables = AddressableAssetSettingsDefaultObject.GetSettings(true);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(defaultMode));
            addressables.CreateOrMoveEntry(guid, addressables.DefaultGroup);

            var serialized = new SerializedObject(world);
            serialized.FindProperty("loadingHandler").managedReferenceValue = new LoadingStreamerDefault();
            serialized.FindProperty("defaultGameMode.m_AssetGUID").stringValue = guid;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return world;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static T DefaultPrefab<T>(string folder, string name) where T : Component {
            string path = $"{folder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) {
                return existing;
            }

            var root = new GameObject(name, typeof(T));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<T>();
        }

        private static void EnsureFolder(string path) {
            if (AssetDatabase.IsValidFolder(path)) {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
