using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Majinfwork.World {
    /// <summary>
    /// Every player build carries the project's world settings as a preloaded asset (loaded before the first scene, where the
    /// framework boots from it), added for the build and taken out again after it; a build without settings fails.
    /// </summary>
    internal sealed class GameWorldSettingsBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            GameWorldSettings settings = GameWorldSettingsEditor.Active;
            if (settings == null) {
                throw new BuildFailedException("[Majingari Framework] This project has no world settings: create or pick them in Project Settings > Majingari Framework.");
            }

            List<Object> preloaded = Others().ToList();
            preloaded.Add(settings);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }

        public void OnPostprocessBuild(BuildReport report) {
            PlayerSettings.SetPreloadedAssets(Others().ToArray());
        }

        /// <summary>The preloaded assets without any world settings (one left by a build that failed is dropped too).</summary>
        private static IEnumerable<Object> Others() => PlayerSettings.GetPreloadedAssets().Where(asset => asset != null && !(asset is GameWorldSettings));
    }
}
