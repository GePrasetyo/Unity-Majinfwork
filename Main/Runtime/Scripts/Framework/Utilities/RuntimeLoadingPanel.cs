using UnityEngine;
using UnityEngine.UIElements;

namespace Majinfwork.World {
    /// <summary>
    /// Runtime-created UI Toolkit panel for loading screen overlay.
    /// Creates PanelSettings and UIDocument at runtime - no assets required.
    /// Used internally by LoadingStreamerDefault.
    /// </summary>
    internal class RuntimeLoadingPanel : MonoBehaviour {
        private UIDocument uiDocument;
        private PanelSettings panelSettings;
        private VisualElement overlay;
        private bool initialized;

        private void Awake() {
            // Create PanelSettings at runtime - no asset required
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "LoadingPanelSettings";
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            // High sort order to render on top
            panelSettings.sortingOrder = 10000;

            // Create UIDocument
            uiDocument = gameObject.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;
        }

        private void Start() {
            // rootVisualElement is available after Start
            BuildVisualTree();
        }

        private void BuildVisualTree() {
            if (initialized) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) {
                Debug.LogError("[RuntimeLoadingPanel] rootVisualElement is null - UI Toolkit panel failed to initialize");
                return;
            }

            // Make root fill the screen
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;

            overlay = new VisualElement {
                name = "loading-overlay",
                pickingMode = PickingMode.Ignore,
                style = {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    right = 0,
                    bottom = 0,
                    backgroundColor = Color.black,
                    opacity = 0,
                    display = DisplayStyle.None
                }
            };

            root.Add(overlay);
            initialized = true;
        }

        public void Show() {
            EnsureInitialized();
            if (overlay != null) {
                overlay.style.display = DisplayStyle.Flex;
            }
        }

        public void Hide() {
            if (overlay != null) {
                overlay.style.display = DisplayStyle.None;
            }
        }

        public void SetOpacity(float opacity) {
            EnsureInitialized();
            if (overlay != null) {
                overlay.style.opacity = opacity;
            }
        }

        public float GetOpacity() {
            EnsureInitialized();
            if (overlay != null) {
                return overlay.resolvedStyle.opacity;
            }
            return 0f;
        }

        private void EnsureInitialized() {
            if (!initialized) {
                BuildVisualTree();
            }
        }
    }
}
