#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Majinfwork.World {
    internal static class PSOWarmupConfigCreator {
        private const string AssetPath = "Assets/Resources/PSOWarmupConfig.asset";

        [MenuItem("Majingari Framework/Create PSO Warmup Config")]
        public static void CreateConfig() {
            var existing = AssetDatabase.LoadAssetAtPath<PSOWarmupConfig>(AssetPath);
            if (existing != null) {
                Debug.Log("PSOWarmupConfig already exists");
                Selection.activeObject = existing;
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources")) {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var config = ScriptableObject.CreateInstance<PSOWarmupConfig>();

            var field = typeof(PSOWarmupConfig).GetField("warmupScreen",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) {
                field.SetValue(config, new PSOWarmupScreenDefault());
            }

            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("PSOWarmupConfig created at " + AssetPath);
            Selection.activeObject = config;
        }

        [MenuItem("Majingari Framework/Select PSO Warmup Config")]
        public static void SelectConfig() {
            var config = Resources.Load<PSOWarmupConfig>(nameof(PSOWarmupConfig));
            if (config != null) {
                Selection.activeObject = config;
            }
            else {
                Debug.LogWarning("No PSOWarmupConfig found. Use Majingari Framework/Create PSO Warmup Config to create one.");
            }
        }
    }
}
#endif
