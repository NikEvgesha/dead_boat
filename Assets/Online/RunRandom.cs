using System;

namespace DeadBoat.Online
{
    // Version 1: integer arithmetic is identical in Editor and WebAssembly.
    // A new generator per stable key prevents unrelated calls from changing the map.
    public sealed class RunRandom
    {
        public const int Version = 1;
        private uint state;

        public RunRandom(int seed, string stream, int index)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in stream) hash = (hash ^ c) * 16777619;
                state = hash ^ (uint)seed ^ ((uint)index * 0x9e3779b9u);
                if (state == 0) state = 0x6d2b79f5u;
            }
        }

        private uint Next()
        {
            unchecked
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
        }

        public float Value => (Next() >> 8) * (1f / 16777216f);
        public float Range(float min, float max) => min + (max - min) * Value;
        public int Range(int min, int max)
        {
            if (max <= min) throw new ArgumentOutOfRangeException(nameof(max));
            // Rejection avoids modulo bias.
            uint size = (uint)((long)max - min);
            uint threshold = unchecked(0u - size) % size;
            uint value;
            do { value = Next(); } while (value < threshold);
            return (int)(min + (long)(value % size));
        }
    }
}
