using UnityEngine;
using UnityEngine.UIElements;

namespace Majinfwork.World {
    internal class RuntimeWarmupPanel : MonoBehaviour {
        private UIDocument uiDocument;
        private PanelSettings panelSettings;
        private VisualElement overlay;
        private VisualElement barFill;
        private Label progressLabel;
        private bool initialized;

        private void Awake() {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "WarmupPanelSettings";
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 10001;

            uiDocument = gameObject.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;
        }

        private void Start() {
            BuildVisualTree(Color.black, Color.white, Color.white);
        }

        internal void BuildVisualTree(Color backgroundColor, Color barColor, Color textColor) {
            if (initialized) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) {
                Debug.LogError("[RuntimeWarmupPanel] rootVisualElement is null");
                return;
            }

            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;

            overlay = new VisualElement {
                name = "warmup-overlay",
                pickingMode = PickingMode.Ignore,
                style = {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    right = 0,
                    bottom = 0,
                    backgroundColor = backgroundColor,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center
                }
            };

            var container = new VisualElement {
                name = "warmup-container",
                style = {
                    width = 400,
                    alignItems = Align.Center
                }
            };

            progressLabel = new Label("Loading shaders...") {
                name = "warmup-label",
                style = {
                    color = textColor,
                    fontSize = 18,
                    marginBottom = 12,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };

            var barTrack = new VisualElement {
                name = "warmup-bar-track",
                style = {
                    width = Length.Percent(100),
                    height = 8,
                    backgroundColor = new Color(1f, 1f, 1f, 0.15f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            barFill = new VisualElement {
                name = "warmup-bar-fill",
                style = {
                    width = Length.Percent(0),
                    height = Length.Percent(100),
                    backgroundColor = barColor,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            barTrack.Add(barFill);
            container.Add(progressLabel);
            container.Add(barTrack);
            overlay.Add(container);
            root.Add(overlay);

            initialized = true;
        }

        public void SetProgress(float normalized, string text) {
            EnsureInitialized();
            if (barFill != null)
                barFill.style.width = Length.Percent(normalized * 100f);
            if (progressLabel != null)
                progressLabel.text = text;
        }

        public void Show() {
            EnsureInitialized();
            if (overlay != null)
                overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide() {
            if (overlay != null)
                overlay.style.display = DisplayStyle.None;
        }

        private void EnsureInitialized() {
            if (!initialized)
                BuildVisualTree(Color.black, Color.white, Color.white);
        }
    }
}
