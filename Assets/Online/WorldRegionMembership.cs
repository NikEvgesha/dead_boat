using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeadBoat.Online
{
    // Procedural layout regions only. Cities/night spawns require separate membership adapters.
    // Ownership here means generation origin, NOT inventory owner or current spatial interest.
    public sealed class WorldRegionMembership
    {
        private sealed class Region
        {
            public float LogicalZ;
            public bool Sealed, Complete = true;
            public readonly HashSet<ulong> Items = new(), Enemies = new();
        }
        private readonly Dictionary<int, Region> regions = new();
        private readonly Dictionary<ulong, (int region, bool enemy)> owners = new();
        private readonly List<int> retired = new();
        private readonly int regionLimit, bindingLimit;
        public int RegionCount => regions.Count;
        public int BindingCount => owners.Count;
        public float RetiredBefore { get; private set; } = float.NegativeInfinity;
        public WorldRegionMembership(int regionLimit = 128, int bindingLimit = 8192)
        {
            if (regionLimit < 1 || bindingLimit < 1) throw new ArgumentOutOfRangeException();
            this.regionLimit = regionLimit; this.bindingLimit = bindingLimit;
        }
        public bool Begin(int index, float logicalZ)
        {
            if (index < 0 || float.IsNaN(logicalZ) || float.IsInfinity(logicalZ) || logicalZ < RetiredBefore) return false;
            if (regions.TryGetValue(index, out var region))
            {
                if (region.LogicalZ != logicalZ) { region.Complete = false; return false; }
                region.Sealed = false; return true; // retain consumed/dead and late corpse history
            }
            if (regions.Count >= regionLimit) return false;
            regions.Add(index, new Region { LogicalZ = logicalZ }); return true;
        }
        public void Seal(int index) { if (regions.TryGetValue(index, out var region)) region.Sealed = true; }

        public bool Observe(string key, ulong id, bool enemy)
        {
            if (key == null) return false;
            if (key.StartsWith("authored:", StringComparison.Ordinal))
            {
                int sceneEnd = key.IndexOf(':', 9);
                if (sceneEnd < 0 || string.CompareOrdinal(key, sceneEnd + 1, "procedural:", 0, 11) != 0) return false;
                int start = sceneEnd + 12, slash = key.IndexOf('/', start);
                if (slash < 0 || !int.TryParse(key.AsSpan(start, slash - start), NumberStyles.None, CultureInfo.InvariantCulture, out int origin)) return false;
                if (enemy) { if (regions.TryGetValue(origin, out var invalidAuthored)) invalidAuthored.Complete = false; return false; }
                return Bind(origin, id, false);
            }
            if (key.StartsWith("corpse:", StringComparison.Ordinal))
            {
                if (enemy || !ulong.TryParse(key.AsSpan(7), NumberStyles.None, CultureInfo.InvariantCulture, out ulong source) ||
                    !owners.TryGetValue(source, out var parent) || !parent.enemy) return false;
                return Bind(parent.region, id, false);
            }
            if (!key.StartsWith("procedural:", StringComparison.Ordinal)) return false;
            int separator = key.IndexOf(':', 11);
            if (separator < 0 || !int.TryParse(key.AsSpan(11, separator - 11), NumberStyles.None, CultureInfo.InvariantCulture, out int index)) return false;
            int offset;
            if (string.CompareOrdinal(key, separator + 1, "item:", 0, 5) == 0) offset = separator + 6;
            else if (string.CompareOrdinal(key, separator + 1, "enemy:", 0, 6) == 0) offset = separator + 7;
            else
            {
                if (regions.TryGetValue(index, out var unsupported)) unsupported.Complete = false;
                return false;
            }
            bool keyEnemy = offset == separator + 7;
            if (offset >= key.Length || !int.TryParse(key.AsSpan(offset), NumberStyles.None, CultureInfo.InvariantCulture, out _) || enemy != keyEnemy)
            {
                if (regions.TryGetValue(index, out var invalid)) invalid.Complete = false;
                return false;
            }
            return Bind(index, id, enemy);
        }
        private bool Bind(int index, ulong id, bool enemy)
        {
            if (!regions.TryGetValue(index, out var region)) return false;
            if (owners.TryGetValue(id, out var owner))
            {
                if (owner.region == index && owner.enemy == enemy) return true;
                region.Complete = false; regions[owner.region].Complete = false; return false;
            }
            if (id == 0 || owners.Count >= bindingLimit) { region.Complete = false; return false; }
            owners.Add(id, (index, enemy));
            (enemy ? region.Enemies : region.Items).Add(id); return true;
        }
        public bool BelongsTo(int index, ulong id, bool enemy) => owners.TryGetValue(id, out var owner) && owner.region == index && owner.enemy == enemy;
        public bool TryCopy(int index, List<ulong> items, List<ulong> enemies, out float logicalZ)
        {
            items.Clear(); enemies.Clear(); logicalZ = 0;
            if (!regions.TryGetValue(index, out var region) || !region.Sealed || !region.Complete) return false;
            items.AddRange(region.Items); enemies.AddRange(region.Enemies); logicalZ = region.LogicalZ; return true;
        }
        public void RetireBefore(float rear)
        {
            if (float.IsNaN(rear) || float.IsInfinity(rear) || rear <= RetiredBefore) return;
            RetiredBefore = rear; retired.Clear();
            foreach (var entry in regions) if (entry.Value.LogicalZ < rear) retired.Add(entry.Key);
            foreach (int index in retired)
            {
                foreach (ulong id in regions[index].Items) owners.Remove(id);
                foreach (ulong id in regions[index].Enemies) owners.Remove(id);
                regions.Remove(index);
            }
        }
        public void Clear() { regions.Clear(); owners.Clear(); retired.Clear(); RetiredBefore = float.NegativeInfinity; }
    }
}
