using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// Editor access to the project's one <see cref="GameWorldSettings"/>, registered the way XR Plug-in Management registers
    /// its settings (an EditorBuildSettings config object, stored with the project settings). It is edited in
    /// Project Settings &gt; Majingari Framework.
    /// </summary>
    [InitializeOnLoad]
    public static class GameWorldSettingsEditor {
        public const string SettingsPath = "Project/Majingari Framework";
        private const string CheckedKey = "Majinfwork_WorldSettingsChecked";

        static GameWorldSettingsEditor() {
            EditorApplication.delayCall += CheckOnce;
        }

        /// <summary>The project's settings, or null when none are registered.</summary>
        public static GameWorldSettings Active {
            get {
                EditorBuildSettings.TryGetConfigObject(GameWorldSettings.ConfigKey, out GameWorldSettings settings);
                return settings;
            }
        }

        /// <summary>Makes <paramref name="settings"/> the project's world settings (null: none).</summary>
        public static void SetActive(GameWorldSettings settings) {
            if (settings == null) {
                EditorBuildSettings.RemoveConfigObject(GameWorldSettings.ConfigKey);
            } else {
                EditorBuildSettings.AddConfigObject(GameWorldSettings.ConfigKey, settings, true);
            }
        }

        /// <summary>Creates world settings at <paramref name="path"/> with working defaults beside them, and registers them.</summary>
        public static GameWorldSettings Create(string path) => GameWorldSettingsCreator.Create(path);

        [MenuItem("Majingari Framework/Get World Settings")]
        public static void Open() => SettingsService.OpenProjectSettings(SettingsPath);

        /// <summary>
        /// Once per editor session: a project without registered settings whose only GameWorldSettings asset predates
        /// registration (it used to be found in Resources) gets that asset registered, and its PSO warmup config linked.
        /// </summary>
        private static void CheckOnce() {
            if (SessionState.GetBool(CheckedKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            SessionState.SetBool(CheckedKey, true);
            if (Active != null) {
                return;
            }

            string[] found = AssetDatabase.FindAssets($"t:{nameof(GameWorldSettings)}").Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (found.Length == 1) {
                var settings = AssetDatabase.LoadAssetAtPath<GameWorldSettings>(found[0]);
                SetActive(settings);
                LinkPsoWarmup(settings);
                Debug.Log($"[Majingari Framework] Registered {found[0]} as the project's world settings (Project Settings > Majingari Framework). " +
                          "It no longer needs to be in a Resources folder.");
            } else if (found.Length > 1) {
                Debug.LogWarning("[Majingari Framework] Several GameWorldSettings assets and none registered: pick the project's one in " +
                                 $"Project Settings > Majingari Framework. Found: {string.Join(", ", found)}");
            } else {
                Debug.LogWarning("[Majingari Framework] This project has no world settings: create them in Project Settings > Majingari Framework.");
            }
        }

        private static void LinkPsoWarmup(GameWorldSettings settings) {
            string[] configs = AssetDatabase.FindAssets($"t:{nameof(PSOWarmupConfig)}");
            if (configs.Length != 1) {
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty field = serialized.FindProperty("psoWarmup");
            if (field.objectReferenceValue == null) {
                field.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PSOWarmupConfig>(AssetDatabase.GUIDToAssetPath(configs[0]));
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(settings);
            }
        }
    }
}
