using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// Base class for all UI widgets managed by HUD.
    /// Override Show/Hide for custom behavior, ShowAsync/HideAsync for animations.
    /// </summary>
    public abstract class UIWidget : MonoBehaviour {
        /// <summary>
        /// Show the widget. Default: SetActive(true).
        /// </summary>
        public virtual void Show() {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the widget. Default: SetActive(false).
        /// </summary>
        public virtual void Hide() {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the widget asynchronously. Override for animations.
        /// Default: calls Show() and returns completed task.
        /// </summary>
        public virtual Task ShowAsync(CancellationToken cancellationToken = default) {
            Show();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hide the widget asynchronously. Override for animations.
        /// Default: calls Hide() and returns completed task.
        /// </summary>
        public virtual Task HideAsync(CancellationToken cancellationToken = default) {
            Hide();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Generic UIWidget with typed model support.
    /// Provides structured Setup pattern for data-driven widgets.
    /// Handles UI Toolkit timing: OnSetup is deferred until Show() when visual tree exists.
    /// </summary>
    /// <typeparam name="TModel">The model type for this widget.</typeparam>
    public abstract class UIWidget<TModel> : UIWidget {
        /// <summary>
        /// Current model data.
        /// </summary>
        protected TModel Model { get; private set; }

        private bool hasModel;

        /// <summary>
        /// Setup the widget with model data.
        /// If widget is inactive, OnSetup is deferred until Show().
        /// </summary>
        public void Setup(TModel model) {
            Model = model;
            hasModel = true;

            // Only call OnSetup if active (UI Toolkit elements exist)
            if (gameObject.activeInHierarchy) {
                OnSetup(model);
            }
        }

        /// <summary>
        /// Show the widget and apply pending model setup.
        /// </summary>
        public override void Show() {
            base.Show();

            // Apply deferred setup now that visual tree exists
            if (hasModel) {
                OnSetup(Model);
            }
        }

        /// <summary>
        /// Called when Setup is invoked and widget is active.
        /// Override to update UI elements.
        /// </summary>
        protected abstract void OnSetup(TModel model);

        /// <summary>
        /// Refresh the widget with current model.
        /// Useful for updating UI after model changes externally.
        /// </summary>
        public void Refresh() {
            if (hasModel) {
                OnSetup(Model);
            }
        }
    }
}
