using System;

namespace Majingari.Framework.World {
    [Serializable]
    public abstract class LoadingStreamer {
        protected delegate void LoadingStarted();
        protected LoadingStarted _loadingStarted;

        internal void Initialize() {
            Construct();
        }

        protected abstract void Construct();

        public abstract void StartLoading(Action loadingRunning);
        public abstract void StopLoading();
    }
}