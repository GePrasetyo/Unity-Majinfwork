using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// Lightweight HUD locator owned by PlayerController.
    /// Provides static API for easy access with optional player index.
    /// </summary>
    public class HUD : GameComponent {
        [SerializeField] private UIWidget[] widgetPrefabs;

        [SerializeReference, ClassReference]
        private UINavigator navigator;

        // Widget pool - caches UIWidget directly, no GetComponent needed after first access
        private readonly Dictionary<Type, UIWidget> widgetPool = new();

        #region Static API - Core

        /// <summary>
        /// Get a widget by type for specified player.
        /// </summary>
        public static T Get<T>(int playerIndex = 0) where T : UIWidget {
            var hud = GetHUD(playerIndex);
            return hud?.GetWidget<T>();
        }

        /// <summary>
        /// Show a widget for specified player.
        /// </summary>
        public static void Show<T>(Action<T> setup = null, int playerIndex = 0) where T : UIWidget {
            var hud = GetHUD(playerIndex);
            hud?.ShowWidget(setup);
        }

        /// <summary>
        /// Show a widget asynchronously.
        /// </summary>
        public static Task ShowAsync<T>(Action<T> setup = null, int playerIndex = 0, CancellationToken cancellationToken = default) where T : UIWidget {
            var hud = GetHUD(playerIndex);
            return hud?.ShowWidgetAsync(setup, cancellationToken) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Hide a widget for specified player.
        /// </summary>
        public static void Hide<T>(Action<T> onHide = null, int playerIndex = 0) where T : UIWidget {
            var hud = GetHUD(playerIndex);
            hud?.HideWidget(onHide);
        }

        /// <summary>
        /// Hide a widget asynchronously.
        /// </summary>
        public static Task HideAsync<T>(Action<T> onHide = null, int playerIndex = 0, CancellationToken cancellationToken = default) where T : UIWidget {
            var hud = GetHUD(playerIndex);
            return hud?.HideWidgetAsync(onHide, cancellationToken) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Hide all widgets for specified player.
        /// </summary>
        public static void HideAll(int playerIndex = 0) {
            var hud = GetHUD(playerIndex);
            hud?.HideAllWidgets();
        }

        /// <summary>
        /// Hide all widgets asynchronously.
        /// </summary>
        public static Task HideAllAsync(int playerIndex = 0, CancellationToken cancellationToken = default) {
            var hud = GetHUD(playerIndex);
            return hud?.HideAllWidgetsAsync(cancellationToken) ?? Task.CompletedTask;
        }

        #endregion

        #region Static API - Generic Widget with Model

        /// <summary>
        /// Show a widget and setup with model data.
        /// </summary>
        public static void Show<TWidget, TModel>(TModel model, int playerIndex = 0)
            where TWidget : UIWidget<TModel> {
            var hud = GetHUD(playerIndex);
            hud?.ShowWidgetWithModel<TWidget, TModel>(model);
        }

        /// <summary>
        /// Show a widget with model asynchronously.
        /// </summary>
        public static Task ShowAsync<TWidget, TModel>(TModel model, int playerIndex = 0, CancellationToken cancellationToken = default)
            where TWidget : UIWidget<TModel> {
            var hud = GetHUD(playerIndex);
            return hud?.ShowWidgetWithModelAsync<TWidget, TModel>(model, cancellationToken) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Push a widget with model onto navigation stack.
        /// </summary>
        public static void Push<TWidget, TModel>(TModel model, int playerIndex = 0)
            where TWidget : UIWidget<TModel> {
            var hud = GetHUD(playerIndex);
            hud?.PushWidgetWithModel<TWidget, TModel>(model);
        }

        /// <summary>
        /// Push a widget with model asynchronously.
        /// </summary>
        public static Task PushAsync<TWidget, TModel>(TModel model, int playerIndex = 0, CancellationToken cancellationToken = default)
            where TWidget : UIWidget<TModel> {
            var hud = GetHUD(playerIndex);
            return hud?.PushWidgetWithModelAsync<TWidget, TModel>(model, cancellationToken) ?? Task.CompletedTask;
        }

        #endregion

        #region Static API - Navigator

        /// <summary>
        /// Push a new widget onto the navigation stack.
        /// </summary>
        public static void Push<T>(Action<T> setup = null, int playerIndex = 0) where T : UIWidget {
            var hud = GetHUD(playerIndex);
            hud?.PushWidget(setup);
        }

        /// <summary>
        /// Push a new widget asynchronously.
        /// </summary>
        public static Task PushAsync<T>(Action<T> setup = null, int playerIndex = 0, CancellationToken cancellationToken = default) where T : UIWidget {
            var hud = GetHUD(playerIndex);
            return hud?.PushWidgetAsync(setup, cancellationToken) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Go back to the previous widget in the navigation stack.
        /// </summary>
        public static void Back(int playerIndex = 0) {
            var hud = GetHUD(playerIndex);
            hud?.GoBack();
        }

        /// <summary>
        /// Go back asynchronously.
        /// </summary>
        public static Task BackAsync(int playerIndex = 0, CancellationToken cancellationToken = default) {
            var hud = GetHUD(playerIndex);
            return hud?.GoBackAsync(cancellationToken) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Pop to the root widget in the navigation stack.
        /// </summary>
        public static void PopToRoot(int playerIndex = 0) {
            var hud = GetHUD(playerIndex);
            hud?.PopToRootWidget();
        }

        /// <summary>
        /// Pop to root asynchronously.
        /// </summary>
        public static Task PopToRootAsync(int playerIndex = 0, CancellationToken cancellationToken = default) {
            var hud = GetHUD(playerIndex);
            return hud?.PopToRootWidgetAsync(cancellationToken) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Clear the navigation stack.
        /// </summary>
        public static void ClearStack(int playerIndex = 0) {
            var hud = GetHUD(playerIndex);
            hud?.ClearNavigationStack();
        }

        /// <summary>
        /// Get the navigation stack count.
        /// </summary>
        public static int StackCount(int playerIndex = 0) {
            var hud = GetHUD(playerIndex);
            return hud?.GetStackCount() ?? 0;
        }

        #endregion

        #region Instance Methods - Core

        private static HUD GetHUD(int playerIndex) {
            var controller = PlayerAccessor.GetPlayerController(playerIndex);
            return controller?.HUD;
        }

        internal T GetWidget<T>() where T : UIWidget {
            var type = typeof(T);

            if (widgetPool.TryGetValue(type, out var widget)) {
                return (T)widget;
            }

            var prefab = FindPrefab<T>();
            if (prefab == null) return null;

            var newWidget = Instantiate(prefab);
            newWidget.Hide(); // Start hidden
            widgetPool[type] = newWidget;

            return newWidget;
        }

        internal void ShowWidget<T>(Action<T> setup = null) where T : UIWidget {
            var widget = GetWidget<T>();
            if (widget == null) return;

            setup?.Invoke(widget);
            widget.Show();
        }

        internal Task ShowWidgetAsync<T>(Action<T> setup = null, CancellationToken cancellationToken = default) where T : UIWidget {
            var widget = GetWidget<T>();
            if (widget == null) return Task.CompletedTask;

            setup?.Invoke(widget);
            return widget.ShowAsync(cancellationToken);
        }

        internal void HideWidget<T>(Action<T> onHide = null) where T : UIWidget {
            var type = typeof(T);
            if (!widgetPool.TryGetValue(type, out var widget)) return;

            onHide?.Invoke((T)widget);
            widget.Hide();
        }

        internal Task HideWidgetAsync<T>(Action<T> onHide = null, CancellationToken cancellationToken = default) where T : UIWidget {
            var type = typeof(T);
            if (!widgetPool.TryGetValue(type, out var widget)) return Task.CompletedTask;

            onHide?.Invoke((T)widget);
            return widget.HideAsync(cancellationToken);
        }

        internal void HideAllWidgets() {
            foreach (var widget in widgetPool.Values) {
                if (widget != null && widget.gameObject.activeSelf) {
                    widget.Hide();
                }
            }
        }

        internal async Task HideAllWidgetsAsync(CancellationToken cancellationToken = default) {
            var tasks = new List<Task>();

            foreach (var widget in widgetPool.Values) {
                if (widget != null && widget.gameObject.activeSelf) {
                    tasks.Add(widget.HideAsync(cancellationToken));
                }
            }

            if (tasks.Count > 0) {
                await Task.WhenAll(tasks);
            }
        }

        #endregion

        #region Instance Methods - Generic Widget with Model

        internal void ShowWidgetWithModel<TWidget, TModel>(TModel model) where TWidget : UIWidget<TModel> {
            var widget = GetWidget<TWidget>();
            if (widget == null) return;

            widget.Setup(model);
            widget.Show();
        }

        internal Task ShowWidgetWithModelAsync<TWidget, TModel>(TModel model, CancellationToken cancellationToken = default)
            where TWidget : UIWidget<TModel> {
            var widget = GetWidget<TWidget>();
            if (widget == null) return Task.CompletedTask;

            widget.Setup(model);
            return widget.ShowAsync(cancellationToken);
        }

        internal void PushWidgetWithModel<TWidget, TModel>(TModel model) where TWidget : UIWidget<TModel> {
            if (navigator == null) {
                Debug.LogWarning("HUD: Navigator not configured. Using Show instead.");
                ShowWidgetWithModel<TWidget, TModel>(model);
                return;
            }
            navigator.Push(this, typeof(TWidget), () => ShowWidgetWithModel<TWidget, TModel>(model));
        }

        internal Task PushWidgetWithModelAsync<TWidget, TModel>(TModel model, CancellationToken cancellationToken = default)
            where TWidget : UIWidget<TModel> {
            if (navigator == null) {
                Debug.LogWarning("HUD: Navigator not configured. Using ShowAsync instead.");
                return ShowWidgetWithModelAsync<TWidget, TModel>(model, cancellationToken);
            }
            return navigator.PushAsync(this, typeof(TWidget), () => ShowWidgetWithModelAsync<TWidget, TModel>(model, cancellationToken), cancellationToken);
        }

        #endregion

        #region Instance Methods - Navigator

        internal void PushWidget<T>(Action<T> setup = null) where T : UIWidget {
            if (navigator == null) {
                Debug.LogWarning("HUD: Navigator not configured. Using Show instead.");
                ShowWidget(setup);
                return;
            }
            navigator.Push(this, typeof(T), () => ShowWidget(setup));
        }

        internal async Task PushWidgetAsync<T>(Action<T> setup = null, CancellationToken cancellationToken = default) where T : UIWidget {
            if (navigator == null) {
                Debug.LogWarning("HUD: Navigator not configured. Using ShowAsync instead.");
                await ShowWidgetAsync(setup, cancellationToken);
                return;
            }
            await navigator.PushAsync(this, typeof(T), () => ShowWidgetAsync(setup, cancellationToken), cancellationToken);
        }

        internal void GoBack() {
            if (navigator == null) {
                Debug.LogWarning("HUD: Navigator not configured. Back operation ignored.");
                return;
            }
            navigator.Back(this);
        }

        internal Task GoBackAsync(CancellationToken cancellationToken = default) {
            if (navigator == null) {
                Debug.LogWarning("HUD: Navigator not configured. Back operation ignored.");
                return Task.CompletedTask;
            }
            return navigator.BackAsync(this, cancellationToken);
        }

        internal void PopToRootWidget() {
            if (navigator == null) {
                Debug.LogWarning("HUD: Navigator not configured. PopToRoot operation ignored.");
                return;
            }
            navigator.PopToRoot(this);
        }

        internal Task PopToRootWidgetAsync(CancellationToken cancellationToken = default) {
            if (navigator == null) {
                Debug.LogWarning("HUD: Navigator not configured. PopToRoot operation ignored.");
                return Task.CompletedTask;
            }
            return navigator.PopToRootAsync(this, cancellationToken);
        }

        internal void ClearNavigationStack() {
            navigator?.Clear();
        }

        internal int GetStackCount() {
            return navigator?.Count ?? 0;
        }

        #endregion

        #region Internal - Used by UINavigator

        internal void HideWidgetByType(Type type) {
            if (widgetPool.TryGetValue(type, out var widget) && widget != null) {
                widget.Hide();
            }
        }

        internal Task HideWidgetByTypeAsync(Type type, CancellationToken cancellationToken) {
            if (widgetPool.TryGetValue(type, out var widget) && widget != null) {
                return widget.HideAsync(cancellationToken);
            }
            return Task.CompletedTask;
        }

        internal void ShowWidgetByType(Type type) {
            if (widgetPool.TryGetValue(type, out var widget) && widget != null) {
                widget.Show();
            }
        }

        internal Task ShowWidgetByTypeAsync(Type type, CancellationToken cancellationToken) {
            if (widgetPool.TryGetValue(type, out var widget) && widget != null) {
                return widget.ShowAsync(cancellationToken);
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Helpers

        private T FindPrefab<T>() where T : UIWidget {
            if (widgetPrefabs == null) return null;

            foreach (var prefab in widgetPrefabs) {
                if (prefab is T typedPrefab) {
                    return typedPrefab;
                }
            }
            return null;
        }

        private void Cleanup() {
            foreach (var widget in widgetPool.Values) {
                if (widget != null) {
                    Destroy(widget.gameObject);
                }
            }
            widgetPool.Clear();
            navigator?.Clear();
        }

        private void OnDestroy() {
            Cleanup();
        }

        #endregion
    }
}
