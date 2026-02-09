using System;
using UnityEngine;

namespace Majinfwork.Settings {
    /// <summary>
    /// Built-in graphics/quality settings.
    /// Directly applies to Unity's QualitySettings and Screen APIs.
    /// </summary>
    [Serializable]
    public class GraphicsSettings : SettingsData<GraphicsSettings> {
        public override string FileName => "Graphics";
        public override string DisplayName => "Graphics";
        public override int Order => 0;

        public int qualityLevel = -1;
        public int resolutionWidth;
        public int resolutionHeight;
        public int refreshRate;
        public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
        public bool vsync = true;
        public int targetFrameRate = -1;

        public override void ResetToDefaults() {
            qualityLevel = QualitySettings.GetQualityLevel();
            var res = Screen.currentResolution;
            resolutionWidth = res.width;
            resolutionHeight = res.height;
            refreshRate = Mathf.RoundToInt((float)res.refreshRateRatio.value);
            fullScreenMode = Screen.fullScreenMode;
            vsync = QualitySettings.vSyncCount > 0;
            targetFrameRate = -1;
        }

        public override void Apply() {
            if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length) {
                QualitySettings.SetQualityLevel(qualityLevel, applyExpensiveChanges: true);
            }

            if (resolutionWidth > 0 && resolutionHeight > 0) {
                Screen.SetResolution(resolutionWidth, resolutionHeight, fullScreenMode);
            }
            else {
                Screen.fullScreenMode = fullScreenMode;
            }

            QualitySettings.vSyncCount = vsync ? 1 : 0;
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
