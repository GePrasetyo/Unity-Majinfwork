using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Majinfwork.World {
    /// <summary>
    /// Canvas-based loading screen (legacy).
    /// Use LoadingStreamerDefault for the UI Toolkit version.
    /// This version is kept for backward compatibility or when Canvas is preferred.
    /// </summary>
    [Serializable]
    public class LoadingStreamerCanvas : LoadingStreamer {
        [SerializeField, Min(0.1f)] private float fadeSpeed = 1;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private bool constructed;

        // Coalescing: track current fade operations
        private Task currentFadeIn;
        private Task currentFadeOut;

        // Internal cancellation for ForceCancel
        private CancellationTokenSource fadeCts;

        protected override void Construct() {
            canvas = new GameObject("ScreenOverlayCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.enabled = false;

            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            GameObject image = new GameObject("OverlayImage");
            image.transform.SetParent(canvas.transform);

            RectTransform rect = image.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image img = image.AddComponent<Image>();
            img.color = Color.black;

            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            canvasGroup.enabled = false;

            UnityEngine.Object.DontDestroyOnLoad(canvas.gameObject);
            constructed = true;
        }

        public override async Task StartLoadingAsync(CancellationToken cancellationToken = default) {
            if (!constructed) return;

            // If already fading in, just wait for it (with caller's cancellation)
            if (currentFadeIn != null && !currentFadeIn.IsCompleted) {
                await AwaitWithCancellation(currentFadeIn, cancellationToken);
                return;
            }

            canvas.enabled = true;
            canvasGroup.enabled = true;

            // Create new internal CTS for this fade operation
            fadeCts?.Dispose();
            fadeCts = new CancellationTokenSource();

            currentFadeIn = FadeAsync(1f, fadeCts.Token);

            try {
                await AwaitWithCancellation(currentFadeIn, cancellationToken);
            }
            catch (OperationCanceledException) when (fadeCts.IsCancellationRequested) {
                // ForceCancel was called - cleanup already done
                throw;
            }
        }

        public override async Task StopLoadingAsync(CancellationToken cancellationToken = default) {
            if (!constructed) return;

            // If already fading out, just wait for it (with caller's cancellation)
            if (currentFadeOut != null && !currentFadeOut.IsCompleted) {
                await AwaitWithCancellation(currentFadeOut, cancellationToken);
                return;
            }

            // Create new internal CTS for this fade operation
            fadeCts?.Dispose();
            fadeCts = new CancellationTokenSource();

            currentFadeOut = FadeAsync(0f, fadeCts.Token);

            try {
                await AwaitWithCancellation(currentFadeOut, cancellationToken);
                canvasGroup.enabled = false;
                canvas.enabled = false;
            }
            catch (OperationCanceledException) when (fadeCts.IsCancellationRequested) {
                // ForceCancel was called - cleanup already done
                throw;
            }
        }

        public override void ForceCancel() {
            if (!constructed) return;

            // Cancel any running fade
            fadeCts?.Cancel();
            fadeCts?.Dispose();
            fadeCts = null;

            // Reset to hidden state
            if (canvasGroup != null) {
                canvasGroup.alpha = 0;
                canvasGroup.enabled = false;
            }
            if (canvas != null) {
                canvas.enabled = false;
            }

            // Clear tracked tasks
            currentFadeIn = null;
            currentFadeOut = null;
        }

        /// <summary>
        /// Await a task while respecting caller's cancellation token.
        /// Caller's token only cancels their wait, not the underlying operation.
        /// </summary>
        private async Task AwaitWithCancellation(Task task, CancellationToken cancellationToken) {
            if (cancellationToken == default) {
                await task;
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            var completed = await Task.WhenAny(task, tcs.Task);

            if (completed == tcs.Task) {
                // Caller cancelled their wait
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Propagate any exception from the original task
            await task;
        }

        private async Task FadeAsync(float targetAlpha, CancellationToken cancellationToken) {
            while (!Mathf.Approximately(canvasGroup.alpha, targetAlpha)) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();

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
