using System;
using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork.World {
    [Serializable]
    public abstract class LoadingStreamer {
        internal void Initialize() {
            Construct();
        }

        protected abstract void Construct();

        /// <summary>
        /// Start loading screen (fade in). Awaitable.
        /// If already fading in, waits for current fade to complete.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel this caller's wait. Does not stop the fade for other waiters.</param>
        public abstract Task StartLoadingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop loading screen (fade out). Awaitable.
        /// If already fading out, waits for current fade to complete.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel this caller's wait. Does not stop the fade for other waiters.</param>
        public abstract Task StopLoadingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Force cancel the current fade operation and reset to hidden state.
        /// Use this for cleanup scenarios (e.g., scene force-unload, error recovery).
        /// </summary>
        public abstract void ForceCancel();
    }
}
