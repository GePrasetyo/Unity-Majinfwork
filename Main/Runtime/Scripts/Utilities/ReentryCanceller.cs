using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork {
    public class ReentryCanceller {
        private const int reentryDelay = 10;
        private CancellationTokenSource canceller;

        public async Task<CancellationToken> Enter(CancellationToken cancel) {
            canceller?.Cancel();
            while (canceller != null) {
                await Task.Delay(reentryDelay);
            }
            canceller = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            return canceller.Token;
        }

        public void Exit() {
            canceller?.Dispose();
            canceller = null;
        }

        public void Dispose() {
            canceller?.Cancel();
            canceller?.Dispose();
            canceller = null;
        }
    }
}