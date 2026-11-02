using System;
using System.Threading.Tasks;

namespace DeadBoat.Online
{
    // Cancels the game's wait, not the SDK operation. The owning runner is shut down
    // separately; a late completion cannot restore an invalidated connection attempt.
    public sealed class LobbyConnectionWait
    {
        private readonly TaskCompletionSource<bool> canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsCanceled => canceled.Task.IsCompleted;
        public void Cancel() => canceled.TrySetResult(true);
        public async Task<bool> WaitAsync(Task operation, TimeSpan timeout)
        {
            _ = ObserveFailure(operation);
            if (IsCanceled) return false;
            var winner = await Task.WhenAny(operation, Task.Delay(timeout), canceled.Task);
            return winner == operation && !IsCanceled;
        }
        private static async Task ObserveFailure(Task operation)
        {
            try { await operation; }
            catch (Exception) { /* Also observe a late fault after cancellation/timeout. */ }
        }
    }
}
