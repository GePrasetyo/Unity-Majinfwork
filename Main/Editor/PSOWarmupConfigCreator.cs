using System.IO;
using UnityEditor;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>The PSO warmup config is a field of the world settings: created next to them, and selected from them.</summary>
    internal static class PSOWarmupConfigCreator {
        private const string WarmupField = "psoWarmup";

        [MenuItem("Majingari Framework/Create PSO Warmup Config")]
        public static void CreateConfig() {
            GameWorldSettings settings = RequireSettings();
            if (settings == null) {
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty field = serialized.FindProperty(WarmupField);
            if (field.objectReferenceValue != null) {
                Debug.Log("[Majingari Framework] The world settings already have a PSO warmup config.");
                Selection.activeObject = field.objectReferenceValue;
                return;
            }

            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(settings)).Replace('\\', '/');
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{nameof(PSOWarmupConfig)}.asset");
            var config = ScriptableObject.CreateInstance<PSOWarmupConfig>();
            AssetDatabase.CreateAsset(config, path);

            var configObject = new SerializedObject(config);
            configObject.FindProperty("warmupScreen").managedReferenceValue = new PSOWarmupScreenDefault();
            configObject.ApplyModifiedPropertiesWithoutUndo();

            field.objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Debug.Log($"[Majingari Framework] PSO warmup config created at {path} and set on the world settings.");
            Selection.activeObject = config;
        }

        [MenuItem("Majingari Framework/Select PSO Warmup Config")]
        public static void SelectConfig() {
            GameWorldSettings settings = RequireSettings();
            if (settings == null) {
                return;
            }

            Object config = new SerializedObject(settings).FindProperty(WarmupField).objectReferenceValue;
            if (config != null) {
                Selection.activeObject = config;
            } else {
                Debug.LogWarning("[Majingari Framework] The world settings have no PSO warmup config. Use Majingari Framework/Create PSO Warmup Config to create one.");
            }
        }

        private static GameWorldSettings RequireSettings() {
            GameWorldSettings settings = GameWorldSettingsEditor.Active;
            if (settings == null) {
                Debug.LogWarning("[Majingari Framework] Create the world settings first (Project Settings > Majingari Framework).");
                GameWorldSettingsEditor.Open();
            }

            return settings;
        }
    }
}
