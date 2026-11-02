using System;
using System.Collections.Generic;

namespace DeadBoat.Online
{
    // Local shadow of generated layout, NOT a network/persistent world snapshot.
    // Prefab references are local assets; future wire descriptors need a versioned catalog.
    public sealed class LocationLayoutManifest
    {
        private readonly Dictionary<int, LocationLayoutStep> entries = new();
        private readonly Queue<int> order = new();
        public int Seed { get; }
        public int GeneratorVersion { get; } = RunRandom.Version;
        public int NextIndex { get; private set; }
        public float LastLogicalZ { get; private set; }
        public float RetiredBefore { get; private set; } = float.NegativeInfinity;
        public int Count => entries.Count;

        public LocationLayoutManifest(int seed) { Seed = seed; }

        public void Record(LocationLayoutStep step)
        {
            if (step.Index != NextIndex || float.IsNaN(step.LogicalZ) || float.IsInfinity(step.LogicalZ) ||
                step.LogicalZ < LastLogicalZ)
                throw new InvalidOperationException("Layout manifest must advance in logical generation order");
            NextIndex++;
            LastLogicalZ = step.LogicalZ;
            // Skipped history must advance the cursor, but need not stay in memory.
            if (step.LogicalZ < RetiredBefore) return;
            entries.Add(step.Index, step);
            order.Enqueue(step.Index);
        }

        public bool TryGet(int index, out LocationLayoutStep step) => entries.TryGetValue(index, out step);

        // Permanent retirement only. Never use a camera/interest window here:
        // an unloaded but revisitable region still needs its descriptor and mutations.
        public void RetireBefore(float rear)
        {
            if (float.IsNaN(rear) || float.IsInfinity(rear))
                throw new ArgumentOutOfRangeException(nameof(rear));
            if (rear <= RetiredBefore) return;
            RetiredBefore = rear;
            while (order.Count > 0)
            {
                int index = order.Peek();
                if (entries[index].LogicalZ >= rear) break;
                order.Dequeue();
                entries.Remove(index);
            }
        }
    }
}
