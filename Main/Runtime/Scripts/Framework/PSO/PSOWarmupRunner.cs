using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Majinfwork.World {
    internal class PSOWarmupRunner : IPSOWarmupProgress {
        public int TotalCount { get; private set; }
        public int CurrentCount { get; private set; }
        public float NormalizedProgress => TotalCount > 0 ? (float)CurrentCount / TotalCount : 1f;
        public bool IsComplete { get; private set; }

        private readonly PSOWarmupConfig config;
        private CancellationTokenSource cts;

        public PSOWarmupRunner(PSOWarmupConfig config) {
            this.config = config;
        }

        public async Task RunAsync(CancellationToken token = default) {
            var collection = config.GetCollectionForCurrentPlatform();
            if (collection == null) {
                IsComplete = true;
                return;
            }

            // A collection holds the pipeline states of the graphics API it was recorded on: it warms up nothing on another.
            if (collection.graphicsDeviceType != SystemInfo.graphicsDeviceType) {
                Debug.Log($"[PSOWarmupRunner] {collection.name} was recorded on {collection.graphicsDeviceType}; this device runs {SystemInfo.graphicsDeviceType}: no warmup");
                IsComplete = true;
                return;
            }

            cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var screen = config.WarmupScreen;
            screen?.Initialize();

            try {
                // Warmup counts graphics states (a shader variant has one per way it was drawn), not shader variants.
                TotalCount = collection.totalGraphicsStateCount;
                CurrentCount = 0;
                var clock = Stopwatch.StartNew();

                screen?.Show(this);

                int batchSize = config.BatchSize;
                JobHandle dependency = default;

                while (!collection.isWarmedUp) {
                    cts.Token.ThrowIfCancellationRequested();

                    dependency = collection.WarmUpProgressively(batchSize, dependency);
                    dependency.Complete();

                    CurrentCount = collection.completedWarmupCount;
                    screen?.UpdateProgress(this);

                    await Task.Yield();
                }

                CurrentCount = TotalCount;
                IsComplete = true;
                screen?.UpdateProgress(this);
                Debug.Log($"[PSOWarmupRunner] Warmed up {collection.completedWarmupCount} graphics states of {collection.variantCount} shader variants in {clock.ElapsedMilliseconds} ms");
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

        public void Cancel() {
            cts?.Cancel();
        }
    }
}
