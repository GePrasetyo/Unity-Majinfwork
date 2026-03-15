using System;

namespace Majinfwork.World {
    [Serializable]
    public abstract class PSOWarmupScreen {
        internal void Initialize() => Construct();

        protected abstract void Construct();
        public abstract void Show(IPSOWarmupProgress progress);
        public abstract void UpdateProgress(IPSOWarmupProgress progress);
        public abstract void Hide();
    }
}
