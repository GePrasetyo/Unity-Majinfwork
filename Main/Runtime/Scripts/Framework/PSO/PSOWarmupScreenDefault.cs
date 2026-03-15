using System;
using UnityEngine;

namespace Majinfwork.World {
    [Serializable]
    public class PSOWarmupScreenDefault : PSOWarmupScreen {
        [SerializeField] private Color backgroundColor = Color.black;
        [SerializeField] private Color barColor = Color.white;
        [SerializeField] private Color textColor = Color.white;

        private RuntimeWarmupPanel panel;

        protected override void Construct() {
            var go = new GameObject("WarmupPanel");
            panel = go.AddComponent<RuntimeWarmupPanel>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            panel.BuildVisualTree(backgroundColor, barColor, textColor);
        }

        public override void Show(IPSOWarmupProgress progress) {
            if (panel == null) return;
            panel.Show();
            UpdateProgress(progress);
        }

        public override void UpdateProgress(IPSOWarmupProgress progress) {
            if (panel == null) return;
            int pct = Mathf.RoundToInt(progress.NormalizedProgress * 100f);
            panel.SetProgress(progress.NormalizedProgress, $"Loading shaders... {pct}%");
        }

        public override void Hide() {
            if (panel == null) return;
            panel.Hide();
            UnityEngine.Object.Destroy(panel.gameObject);
            panel = null;
        }
    }
}
