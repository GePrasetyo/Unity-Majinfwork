using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

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

            cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var screen = config.WarmupScreen;
            screen?.Initialize();

            try {
                TotalCount = collection.variantCount;
                CurrentCount = 0;

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
