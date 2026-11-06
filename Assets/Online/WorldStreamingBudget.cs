namespace DeadBoat.Online
{
    // Automatic schedulers share one audit slot per rendered frame. Each lane
    // gets every third frame; caller order cannot starve a slower adapter.
    // This limits starts, not the duration of a synchronous Unity Instantiate.
    public sealed class WorldStreamingBudget
    {
        private int lastFrame = -1;
        public bool TryAcquire(int frame, int lane)
        {
            if (frame < 0 || lane < 0 || lane > 2 || frame == lastFrame || frame % 3 != lane) return false;
            lastFrame = frame; return true;
        }
    }
}
