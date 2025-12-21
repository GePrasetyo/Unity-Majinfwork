using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Majinfwork.TaskHelper;
using System.Threading.Tasks;

namespace Majinfwork.World {
    public class DefaultLoadingStreamer : LoadingStreamer {
        [SerializeField, Min(0.1f)] private float fadeSpeed = 1;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private ReentryCanceller reentry;

        private bool constructed;

        protected override void Construct() {
            canvas = new GameObject("ScreenOverlayCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.enabled = false;

            // Add a CanvasScaler Component and set its properties
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            // Create a new Image GameObject as a child of the Canvas
            GameObject image = new GameObject("OverlayImage");
            image.transform.SetParent(canvas.transform);

            // Add a RectTransform Component and set its properties
            RectTransform rect = image.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; // Bottom left corner
            rect.anchorMax = Vector2.one; // Top right corner
            rect.anchoredPosition = Vector2.zero; // Center in parent
            rect.sizeDelta = Vector2.zero;

            // Add an Image Component and set its properties
            Image img = image.AddComponent<Image>();
            img.color = Color.black;

            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            canvasGroup.enabled = false;

            UnityEngine.Object.DontDestroyOnLoad(canvas.gameObject);
            constructed = true;
        }

        public override async void StartLoading(Action loadingRunning) {
            if (!constructed) return;

            var ct = await reentry.Enter(default);

            try {
                canvas.enabled = true;
                canvasGroup.enabled = true;

                await FadeAsync(1f, ct);
                loadingRunning?.Invoke();
            }
            catch (OperationCanceledException) {
                Debug.Log("Fade In Canceled");
            }
            finally {
                reentry.Exit();
            }
        }

        public override async void StopLoading() {
            if (!constructed) return;

            var ct = await reentry.Enter(default);

            try {
                await FadeAsync(0f, ct);
                canvasGroup.enabled = false;
                canvas.enabled = false;
            }
            catch (OperationCanceledException) {
                Debug.Log("Fade Out Canceled");
            }
            finally {
                reentry.Exit();
            }
        }

        private async Task FadeAsync(float targetAlpha, CancellationToken ct) {
            while (!Mathf.Approximately(canvasGroup.alpha, targetAlpha)) {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();

                canvasGroup.alpha = Mathf.MoveTowards(
                    canvasGroup.alpha,
                    targetAlpha,
                    Time.unscaledDeltaTime * fadeSpeed
                );
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}