using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Majinfwork.World {
    internal sealed class PSOWarmupConfig : ScriptableObject {
        [SerializeField] private PlatformPSOEntry[] platformCollections = Array.Empty<PlatformPSOEntry>();
        [SerializeField, Min(1)] private int batchSize = 10;
        [SerializeField, Min(0f)] private float maxFrameBudgetMs = 8f;
        [SerializeField] private bool skipInEditor = false;
        [SerializeReference, ClassReference] private PSOWarmupScreen warmupScreen;

        public int BatchSize => batchSize;
        public float MaxFrameBudgetMs => maxFrameBudgetMs;
        public bool SkipInEditor => skipInEditor;
        public PSOWarmupScreen WarmupScreen => warmupScreen;

        public GraphicsStateCollection GetCollectionForCurrentPlatform() {
            var group = GetCurrentPlatformGroup();
            for (int i = 0; i < platformCollections.Length; i++) {
                if (platformCollections[i] != null && platformCollections[i].platform == group)
                    return platformCollections[i].collection;
            }
            return null;
        }

        private static RuntimePlatformGroup GetCurrentPlatformGroup() {
#if UNITY_EDITOR
            var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            switch (target) {
                case UnityEditor.BuildTarget.Android:
                    return RuntimePlatformGroup.Android;
                case UnityEditor.BuildTarget.iOS:
                    return RuntimePlatformGroup.iOS;
                case UnityEditor.BuildTarget.WebGL:
                    return RuntimePlatformGroup.WebGL;
                case UnityEditor.BuildTarget.PS4:
                case UnityEditor.BuildTarget.PS5:
                case UnityEditor.BuildTarget.XboxOne:
                case UnityEditor.BuildTarget.GameCoreXboxSeries:
                case UnityEditor.BuildTarget.Switch:
                    return RuntimePlatformGroup.Console;
                default:
                    return RuntimePlatformGroup.Standalone;
            }
#else
            switch (Application.platform) {
                case RuntimePlatform.Android:
                    return RuntimePlatformGroup.Android;
                case RuntimePlatform.IPhonePlayer:
                    return RuntimePlatformGroup.iOS;
                case RuntimePlatform.WebGLPlayer:
                    return RuntimePlatformGroup.WebGL;
                case RuntimePlatform.PS4:
                case RuntimePlatform.PS5:
                case RuntimePlatform.XboxOne:
                case RuntimePlatform.GameCoreXboxSeries:
                case RuntimePlatform.Switch:
                    return RuntimePlatformGroup.Console;
                default:
                    return RuntimePlatformGroup.Standalone;
            }
#endif
        }
    }

    public enum RuntimePlatformGroup {
        Standalone,
        Android,
        iOS,
        Console,
        WebGL
    }

    [Serializable]
    public class PlatformPSOEntry {
        public RuntimePlatformGroup platform;
        public GraphicsStateCollection collection;
    }
}
