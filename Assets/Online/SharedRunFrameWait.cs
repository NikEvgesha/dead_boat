using System.Threading.Tasks;
using UnityEngine;

namespace DeadBoat.Online
{
    // Unity's player loop works on WebGL without ThreadPool/System.Timer.
    // Real time also continues while gameplay is paused for a lost connection.
    public static class SharedRunFrameWait
    {
        public static async Task Delay(double seconds)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < deadline)
                await Awaitable.NextFrameAsync();
        }
    }
}
