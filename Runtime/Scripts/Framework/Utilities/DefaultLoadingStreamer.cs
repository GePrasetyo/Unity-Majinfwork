using System;
using UnityEngine;
using UnityEngine.UI;

namespace Majingari.Framework.World {
    public class DefaultLoadingStreamer : LoadingStreamer {
        [SerializeField, Min(0.1f)] private float _fadeSpeed = 1;

        private bool _fading;
        private bool _constructed;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        internal override void Construct() {
            _canvas = new GameObject("ScreenOverlayCanvas").AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            _canvas.enabled = false;

            // Add a CanvasScaler Component and set its properties
            CanvasScaler scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            // Create a new Image GameObject as a child of the Canvas
            GameObject image = new GameObject("OverlayImage");
            image.transform.SetParent(_canvas.transform);

            // Add a RectTransform Component and set its properties
            RectTransform rect = image.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; // Bottom left corner
            rect.anchorMax = Vector2.one; // Top right corner
            rect.anchoredPosition = Vector2.zero; // Center in parent
            rect.sizeDelta = Vector2.zero;

            // Add an Image Component and set its properties
            Image img = image.AddComponent<Image>();
            img.color = Color.black;

            _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.enabled = false;

            GameObject.DontDestroyOnLoad(_canvas.gameObject);
            _constructed = true;
        }

        public override void StartLoading(Action loadingRunning) {
            if (!_constructed) {
                return;
            }

            if (_fading) {
                return;
            }

            _canvasGroup.enabled = true;
            _canvas.enabled = true;
            _loadingStarted = () => loadingRunning.Invoke();

            if (_canvasGroup.alpha > 0) {
                _canvasGroup.alpha = 1;
                _loadingStarted.Invoke();
                return;
            }

            _fading = true;
            ServiceLocator.Resolve<TickSignal>().RegisterObject(TickFadeIn);
        }

        private void TickFadeIn() {
            _canvasGroup.alpha += Time.unscaledDeltaTime * _fadeSpeed;

            if (_canvasGroup.alpha >= 1) {
                _fading = false;

                _loadingStarted?.Invoke();
                _loadingStarted = null;
                ServiceLocator.Resolve<TickSignal>().UnRegisterObject(TickFadeIn);
            }
        }

        private void TickFadeOut() {
            _canvasGroup.alpha -= Time.unscaledDeltaTime * _fadeSpeed;

            if (_canvasGroup.alpha <= 0) {
                _fading = false;
                _canvasGroup.enabled = false;
                _canvas.enabled = false;

                ServiceLocator.Resolve<TickSignal>().UnRegisterObject(TickFadeOut);
            }
        }

        public override void StopLoading() {
            if (!_constructed) {
                return;
            }

            if (_fading) {
                return;
            }

            if (_canvasGroup.alpha < 1) {
                _canvasGroup.alpha = 0;
                _canvasGroup.enabled = false;
                _canvas.enabled = false;
                return;
            }

            _fading = true;
            ServiceLocator.Resolve<TickSignal>().RegisterObject(TickFadeOut);
        }
    }
}