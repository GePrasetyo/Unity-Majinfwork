using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Majinfwork.World {
    /// <summary>
    /// Warms up this platform's recordings before the first scene: the built-in one, then the Addressable one (loaded here
    /// and kept loaded for the whole game, so its bundles' shaders stay warm). A recording made on another graphics API is
    /// skipped. Logs what it warmed and how long it took.
    /// </summary>
    internal class PSOWarmupRunner : IPSOWarmupProgress {
        public int TotalCount { get; private set; }
        public int CurrentCount { get; private set; }
        public float NormalizedProgress => TotalCount > 0 ? (float)CurrentCount / TotalCount : 1f;
        public bool IsComplete { get; private set; }

        // The Addressable recording stays loaded for the whole game: released, its bundles and their shaders would unload,
        // and the shaders loaded again later would be new objects, cold again.
        private static AsyncOperationHandle<GraphicsStateCollection> kept;

        private readonly PSOWarmupConfig config;
        private CancellationTokenSource cts;

        public PSOWarmupRunner(PSOWarmupConfig config) {
            this.config = config;
        }

        public async Task RunAsync(CancellationToken token = default) {
            PlatformPSOEntry entry = config.GetEntryForCurrentPlatform();
            if (entry == null) {
                IsComplete = true;
                return;
            }

            cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var screen = config.WarmupScreen;
            screen?.Initialize();

            try {
                screen?.Show(this);
                var collections = new List<GraphicsStateCollection>(2);
                Add(collections, entry.collection);
                if (entry.addressableCollection != null && entry.addressableCollection.RuntimeKeyIsValid()) {
                    if (!kept.IsValid()) {
                        kept = Addressables.LoadAssetAsync<GraphicsStateCollection>(entry.addressableCollection.RuntimeKey);
                    }

                    await kept.Task;
                    if (kept.Status == AsyncOperationStatus.Succeeded) {
                        Add(collections, kept.Result);
                    } else {
                        Debug.LogWarning($"[PSOWarmupRunner] The Addressable recording didn't load: {kept.OperationException?.Message}");
                    }
                }

                // Warmup counts graphics states (a shader variant has one per way it was drawn), not shader variants.
                TotalCount = 0;
                foreach (GraphicsStateCollection collection in collections) {
                    TotalCount += collection.totalGraphicsStateCount;
                }

                CurrentCount = 0;
                screen?.UpdateProgress(this);
                int batchSize = config.BatchSize;
                int done = 0;
                foreach (GraphicsStateCollection collection in collections) {
                    var clock = Stopwatch.StartNew();
                    JobHandle dependency = default;
                    while (!collection.isWarmedUp) {
                        cts.Token.ThrowIfCancellationRequested();

                        dependency = collection.WarmUpProgressively(batchSize, dependency);
                        dependency.Complete();

                        CurrentCount = done + collection.completedWarmupCount;
                        screen?.UpdateProgress(this);

                        await Task.Yield();
                    }

                    done += collection.totalGraphicsStateCount;
                    Debug.Log($"[PSOWarmupRunner] Warmed up {collection.name}: {collection.completedWarmupCount} graphics states of {collection.variantCount} shader variants in {clock.ElapsedMilliseconds} ms");
                }

                CurrentCount = TotalCount;
                IsComplete = true;
                screen?.UpdateProgress(this);
            }
            catch (System.OperationCanceledException) {
                Debug.LogWarning("[PSOWarmupRunner] Warmup cancelled");
            }
            catch (System.Exception e) {
                Debug.LogWarning($"[PSOWarmupRunner] Warmup failed: {e.Message}");
            }
            finally {
                IsComplete = true;
                screen?.Hide();
                cts?.Dispose();
                cts = null;
            }
        }

        // A collection holds the pipeline states of the graphics API it was recorded on: it warms up nothing on another.
        private static void Add(List<GraphicsStateCollection> collections, GraphicsStateCollection collection) {
            if (collection == null) {
                return;
            }

            if (collection.graphicsDeviceType != SystemInfo.graphicsDeviceType) {
                Debug.Log($"[PSOWarmupRunner] {collection.name} was recorded on {collection.graphicsDeviceType}; this device runs {SystemInfo.graphicsDeviceType}: skipped");
                return;
            }

            collections.Add(collection);
        }

        public void Cancel() {
            cts?.Cancel();
        }
    }
}
