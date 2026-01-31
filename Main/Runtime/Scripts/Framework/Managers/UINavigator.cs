using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork.World {
    /// <summary>
    /// Optional add-on for HUD that provides navigation stack functionality.
    /// Enables Push/Back/PopToRoot operations for menu navigation.
    /// </summary>
    [Serializable]
    public class UINavigator {
        private readonly Stack<Type> navigationStack = new();
        private Type currentWidget;

        /// <summary>
        /// Number of widgets in the navigation stack (excluding current).
        /// </summary>
        public int Count => navigationStack.Count;

        /// <summary>
        /// Push current widget to stack, show new widget.
        /// </summary>
        internal void Push(HUD hud, Type widgetType, Action showAction) {
            // Hide current and push to stack
            if (currentWidget != null) {
                navigationStack.Push(currentWidget);
                hud.HideWidgetByType(currentWidget);
            }

            // Show new widget
            currentWidget = widgetType;
            showAction?.Invoke();
        }

        /// <summary>
        /// Push current widget to stack, show new widget asynchronously.
        /// </summary>
        internal async Task PushAsync(HUD hud, Type widgetType, Func<Task> showActionAsync, CancellationToken cancellationToken = default) {
            // Hide current and push to stack
            if (currentWidget != null) {
                navigationStack.Push(currentWidget);
                await hud.HideWidgetByTypeAsync(currentWidget, cancellationToken);
            }

            // Show new widget
            currentWidget = widgetType;
            if (showActionAsync != null) {
                await showActionAsync();
            }
        }

        /// <summary>
        /// Go back to previous widget in stack.
        /// </summary>
        internal void Back(HUD hud) {
            if (navigationStack.Count == 0) return;

            // Hide current
            if (currentWidget != null) {
                hud.HideWidgetByType(currentWidget);
            }

            // Pop and show previous
            currentWidget = navigationStack.Pop();
            hud.ShowWidgetByType(currentWidget);
        }

        /// <summary>
        /// Go back to previous widget asynchronously.
        /// </summary>
        internal async Task BackAsync(HUD hud, CancellationToken cancellationToken = default) {
            if (navigationStack.Count == 0) return;

            // Hide current
            if (currentWidget != null) {
                await hud.HideWidgetByTypeAsync(currentWidget, cancellationToken);
            }

            // Pop and show previous
            currentWidget = navigationStack.Pop();
            await hud.ShowWidgetByTypeAsync(currentWidget, cancellationToken);
        }

        /// <summary>
        /// Pop all widgets and return to root.
        /// </summary>
        internal void PopToRoot(HUD hud) {
            if (navigationStack.Count == 0) return;

            // Hide current
            if (currentWidget != null) {
                hud.HideWidgetByType(currentWidget);
            }

            // Get root (bottom of stack)
            Type root = null;
            while (navigationStack.Count > 0) {
                root = navigationStack.Pop();
            }

            // Show root
            if (root != null) {
                currentWidget = root;
                hud.ShowWidgetByType(currentWidget);
            }
        }

        /// <summary>
        /// Pop to root asynchronously.
        /// </summary>
        internal async Task PopToRootAsync(HUD hud, CancellationToken cancellationToken = default) {
            if (navigationStack.Count == 0) return;

            // Hide current
            if (currentWidget != null) {
                await hud.HideWidgetByTypeAsync(currentWidget, cancellationToken);
            }

            // Get root (bottom of stack)
            Type root = null;
            while (navigationStack.Count > 0) {
                root = navigationStack.Pop();
            }

            // Show root
            if (root != null) {
                currentWidget = root;
                await hud.ShowWidgetByTypeAsync(currentWidget, cancellationToken);
            }
        }

        /// <summary>
        /// Clear the navigation stack.
        /// </summary>
        internal void Clear() {
            navigationStack.Clear();
            currentWidget = null;
        }
    }
}
