using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Majinfwork.World {
    internal static class FrameworkEditorIcons {
        private const string IconPath = "Packages/com.majingari.framework/Main/Editor Default Resources/Icons/PlayFramework.png";

        private static Texture2D playFrameworkIcon;
        public static Texture2D PlayFramework => playFrameworkIcon ??= AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
    }

    [Overlay(typeof(SceneView), "Play Framework", true)]
    public class PlayFrameworkOverlay : ToolbarOverlay {
        public PlayFrameworkOverlay() : base(PlayWithFrameworkButton.Id) { }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    public class PlayWithFrameworkButton : EditorToolbarButton {
        public const string Id = "Majinfwork/PlayWithFramework";

        public PlayWithFrameworkButton() {
            text = "Play Framework";
            tooltip = "Enter Play Mode with Framework enabled";
            if (FrameworkEditorIcons.PlayFramework != null)
                icon = FrameworkEditorIcons.PlayFramework;
            clicked += OnClick;
        }

        private void OnClick() {
            if (EditorApplication.isPlaying) {
                EditorApplication.isPlaying = false;
            }
            else {
                SessionState.SetBool(GameWorldSession.PlayWithFrameworkKey, true);
                EditorApplication.isPlaying = true;
            }
        }
    }

    [InitializeOnLoad]
    public static class PlayFrameworkToolbarIntegration {
        static PlayFrameworkToolbarIntegration() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ToolbarExtender.PlayModeToolbarGUI += OnToolbarGUI;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingPlayMode) {
                SessionState.SetBool(GameWorldSession.PlayWithFrameworkKey, false);
            }
        }

        private static void OnToolbarGUI() {
            bool isEnabled = SessionState.GetBool(GameWorldSession.PlayWithFrameworkKey, false);

            GUI.backgroundColor = isEnabled ? new Color(0.4f, 0.8f, 0.4f) : Color.white;

            var icon = FrameworkEditorIcons.PlayFramework;
            var content = icon != null
                ? new GUIContent(icon, "Play with Framework enabled")
                : new GUIContent("Framework", "Play with Framework enabled");

            if (GUILayout.Button(content, EditorStyles.toolbarButton))
            {
                if (EditorApplication.isPlaying) {
                    EditorApplication.isPlaying = false;
                }
                else {
                    SessionState.SetBool(GameWorldSession.PlayWithFrameworkKey, true);
                    EditorApplication.isPlaying = true;
                }
            }

            GUI.backgroundColor = Color.white;
        }
    }

    [InitializeOnLoad]
    public static class ToolbarExtender {
        private static readonly System.Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject currentToolbar;
        private static bool initialized;

        public static System.Action PlayModeToolbarGUI;

        static ToolbarExtender() {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate() {
            if (initialized) return;

            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

            if (currentToolbar != null) {
                var root = currentToolbar.GetType()
                    .GetField("m_Root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (root != null) {
                    var rawRoot = root.GetValue(currentToolbar);
                    var mRoot = rawRoot as VisualElement;

                    if (mRoot != null) {
                        initialized = RegisterCallback(mRoot);
                    }
                }
            }
        }

        private static bool RegisterCallback(VisualElement root) {
            var playModeZone = root.Q("ToolbarZonePlayMode");
            if (playModeZone != null) {
                var container = new IMGUIContainer(() => PlayModeToolbarGUI?.Invoke());
                container.style.width = 40;
                container.style.minWidth = 40;
                container.style.height = 22;
                container.style.marginLeft = 8;
                container.style.alignSelf = Align.Center;
                playModeZone.Add(container);
                return true;
            }
            return false;
        }
    }
}
