using UnityEditor;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// Project Settings &gt; Majingari Framework: the project's one world settings asset, edited in place. With none it offers to
    /// create them (in a folder you pick) or to use an existing asset.
    /// </summary>
    internal sealed class GameWorldSettingsProvider : SettingsProvider {
        private static readonly GUIContent SettingsLabel = new GUIContent("World Settings", "The project's one world settings asset. It can live in any folder.");

        private Editor editor;

        private GameWorldSettingsProvider()
            : base(GameWorldSettingsEditor.SettingsPath, SettingsScope.Project, new[] { "Majingari", "World Settings", "Game Instance", "Game Mode", "World Config", "Player Controller", "PSO" }) { }

        [SettingsProvider]
        private static SettingsProvider Create() => new GameWorldSettingsProvider();

        public override void OnGUI(string searchContext) {
            GameWorldSettings settings = GameWorldSettingsEditor.Active;
            EditorGUI.BeginChangeCheck();
            var picked = (GameWorldSettings)EditorGUILayout.ObjectField(SettingsLabel, settings, typeof(GameWorldSettings), false);
            if (EditorGUI.EndChangeCheck()) {
                GameWorldSettingsEditor.SetActive(picked);
                settings = picked;
            }

            if (settings == null) {
                EditorGUILayout.HelpBox("This project has no world settings, so the framework can't boot. Create them, or pick an existing asset above.", MessageType.Warning);
                if (GUILayout.Button("Create World Settings...")) {
                    GameWorldSettingsCreator.CreateWithDialog();
                }

                return;
            }

            if (AssetDatabase.GetAssetPath(settings).Contains("/Resources/")) {
                EditorGUILayout.HelpBox("These settings sit in a Resources folder, so a build ships them twice (Resources and preloaded). Move them out of Resources.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            Editor.CreateCachedEditor(settings, null, ref editor);
            editor.OnInspectorGUI();
        }

        public override void OnDeactivate() {
            if (editor != null) {
                Object.DestroyImmediate(editor);
                editor = null;
            }
        }
    }
}
