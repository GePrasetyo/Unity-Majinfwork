using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

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

        /// <summary>This platform's recordings (none: no warmup).</summary>
        public PlatformPSOEntry GetEntryForCurrentPlatform() {
            var group = GetCurrentPlatformGroup();
            for (int i = 0; i < platformCollections.Length; i++) {
                if (platformCollections[i] != null && platformCollections[i].platform == group)
                    return platformCollections[i];
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

    /// <summary>
    /// One platform's recordings. A recording references the shaders it warms, and a shader used both by what is built into
    /// the app and by what streams in Addressables bundles ships twice, as two objects: each copy is warmed only from a
    /// recording that ships beside it. So the states of what is built in go in <see cref="collection"/>, and those of what
    /// streams in <see cref="addressableCollection"/>, a recording that is itself Addressable (its shaders are the
    /// bundles' own). That one is loaded at boot and stays loaded, so the bundles holding its shaders do too, and those
    /// shaders stay warm from level to level.
    /// </summary>
    [Serializable]
    public class PlatformPSOEntry {
        public RuntimePlatformGroup platform;
        [Tooltip("The pipeline states of what is built into the app (none: nothing to warm there).")]
        public GraphicsStateCollection collection;
        [Tooltip("The pipeline states of what streams in Addressables bundles: an Addressable recording, loaded at boot and " +
                 "kept loaded (none: nothing to warm there).")]
        public AssetReferenceT<GraphicsStateCollection> addressableCollection;
    }
}
