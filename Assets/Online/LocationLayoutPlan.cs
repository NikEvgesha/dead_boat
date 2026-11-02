namespace DeadBoat.Online
{
    public readonly struct LocationLayoutStep
    {
        public readonly int Index;
        public readonly float LogicalZ;
        public readonly LocationContentSpawner Prefab;
        public LocationLayoutStep(int index, float z, LocationContentSpawner prefab)
        {
            Index = index; LogicalZ = z; Prefab = prefab;
        }
    }

    // Advances layout/pool history without instantiating geometry or content.
    // A fresh random stream at index N is NOT random access: the collection also
    // remembers unused choices. Replay with an owned pool clone in index order.
    public static class LocationLayoutPlan
    {
        public static LocationLayoutStep Advance(int index, float previousZ, float min, float max,
            LocationSpawnCollection collection, RunRandom random)
        {
            float distance = random != null ? random.Range(min, max) : UnityEngine.Random.Range(min, max);
            float z = previousZ + distance;
            var prefab = collection != null ? collection.GetRandomSpawnPrefab(z, random) : null;
            return new LocationLayoutStep(index, z, prefab);
        }
    }
}
