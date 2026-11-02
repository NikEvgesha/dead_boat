using System;
using System.Collections.Generic;
using Fusion;

namespace DeadBoat.Online
{
    // Local, bounded copies only. Never writes archived data back over live network records.
    // Callers supply COMPLETE stable membership, including consumed/dead records.
    public sealed class WorldRegionArchive
    {
        private sealed class Region
        {
            public int Revision;
            public float LogicalZ;
            public bool Dormant;
            public Dictionary<ulong, SharedItemRecord> Items;
            public Dictionary<ulong, SharedEnemyRecord> Enemies;
            public HashSet<ulong> DormantItems, DormantEnemies;
            public int Count => Items.Count + Enemies.Count;
        }
        private readonly Dictionary<int, Region> regions = new();
        private readonly Dictionary<ulong, int> membership = new();
        private readonly List<int> retired = new();
        private readonly int regionLimit, recordLimit;
        public int Seed { get; }
        public int LevelId { get; }
        public int GeneratorVersion { get; } = RunRandom.Version;
        public int RegionCount => regions.Count;
        public int RecordCount => membership.Count;
        public float RetiredBefore { get; private set; } = float.NegativeInfinity;

        public WorldRegionArchive(int seed, int levelId, int regionLimit = 128, int recordLimit = 4096)
        {
            if (regionLimit < 1 || recordLimit < 1) throw new ArgumentOutOfRangeException();
            Seed = seed; LevelId = levelId; this.regionLimit = regionLimit; this.recordLimit = recordLimit;
        }

        public bool TryStore(int index, int revision, float logicalZ,
            IEnumerable<KeyValuePair<ulong, SharedItemRecord>> items,
            IEnumerable<KeyValuePair<ulong, SharedEnemyRecord>> enemies, out string refusal)
        {
            refusal = null;
            regions.TryGetValue(index, out var previous);
            if (index < 0 || revision <= 0 || float.IsNaN(logicalZ) || float.IsInfinity(logicalZ) || logicalZ < RetiredBefore ||
                (previous != null && (revision <= previous.Revision || logicalZ != previous.LogicalZ)))
            { refusal = "invalid or stale region revision"; return false; }
            if (previous == null && regions.Count >= regionLimit)
            { refusal = "region budget"; return false; }
            int available = recordLimit - membership.Count + (previous?.Count ?? 0);
            var staged = new Region { Revision = revision, LogicalZ = logicalZ, Dormant = previous?.Dormant ?? false,
                Items = new(), Enemies = new() };
            // Stop enumerating at the configured bound; even staging cannot grow without limit.
            foreach (var item in items ?? Array.Empty<KeyValuePair<ulong, SharedItemRecord>>())
            {
                if (!Accept(item.Key) || item.Value.Mode < 0 || item.Value.Mode > 4)
                { refusal = "invalid or duplicate item membership"; return false; }
                if (staged.Count >= available) { refusal = "record budget"; return false; }
                staged.Items.Add(item.Key, item.Value);
            }
            foreach (var enemy in enemies ?? Array.Empty<KeyValuePair<ulong, SharedEnemyRecord>>())
            {
                if (!Accept(enemy.Key)) { refusal = "invalid or duplicate enemy membership"; return false; }
                if (staged.Count >= available) { refusal = "record budget"; return false; }
                staged.Enemies.Add(enemy.Key, enemy.Value);
            }
            // Commit only after the whole replacement has validated.
            if (previous != null) RemoveMembership(previous);
            regions[index] = staged;
            foreach (ulong id in staged.Items.Keys) membership.Add(id, index);
            foreach (ulong id in staged.Enemies.Keys) membership.Add(id, index);
            return true;

            bool Accept(ulong id) => id != 0 && !staged.Items.ContainsKey(id) && !staged.Enemies.ContainsKey(id) &&
                (!membership.TryGetValue(id, out int owner) || owner == index);
        }

        public bool SetDormant(int index, bool dormant)
        {
            if (!regions.TryGetValue(index, out var region)) return false;
            region.Dormant = dormant;
            region.DormantItems = region.DormantEnemies = null;
            return true;
        }
        // Capture contains the entire historical region. Only removed local actors
        // sleep: detached/owned/cargo objects elsewhere must remain materializable.
        public bool SetReplicaDormancy(int index, IEnumerable<ulong> items, IEnumerable<ulong> enemies)
        {
            if (!regions.TryGetValue(index, out var region)) return false;
            var itemSet = new HashSet<ulong>(); var enemySet = new HashSet<ulong>();
            foreach (ulong id in items)
                if (!region.Items.ContainsKey(id) || !itemSet.Add(id)) return false;
            foreach (ulong id in enemies)
                if (!region.Enemies.ContainsKey(id) || !enemySet.Add(id)) return false;
            region.DormantItems = itemSet; region.DormantEnemies = enemySet;
            region.Dormant = true;
            return true;
        }
        public bool TryItem(int index, ulong id, out SharedItemRecord item)
        {
            item = default; return regions.TryGetValue(index, out var region) && region.Items.TryGetValue(id, out item);
        }
        public bool TryEnemy(int index, ulong id, out SharedEnemyRecord enemy)
        {
            enemy = default; return regions.TryGetValue(index, out var region) && region.Enemies.TryGetValue(id, out enemy);
        }

        public bool AllowsItemReplica(ulong id, SharedItemRecord live)
        {
            // Ownership/cargo always escapes dormancy. Any newer/different live state also wins.
            if (live.Owner != PlayerRef.None || live.Mode == 1 || live.Mode == 2 || live.Mode == 3) return true;
            if (!membership.TryGetValue(id, out int index) || !regions[index].Dormant ||
                (regions[index].DormantItems != null && !regions[index].DormantItems.Contains(id)) ||
                !regions[index].Items.TryGetValue(id, out var old)) return true;
            return !(live.Template == old.Template && live.WeaponAmmo == old.WeaponAmmo && live.Mode == old.Mode &&
                live.Owner == old.Owner && live.Position.Equals(old.Position) && live.Rotation.Equals(old.Rotation) &&
                live.BoatLocalPosition.Equals(old.BoatLocalPosition) && live.BoatLocalRotation.Equals(old.BoatLocalRotation) &&
                live.HomeZ == old.HomeZ && (bool)live.Prunable == (bool)old.Prunable && (bool)live.Dropped == (bool)old.Dropped);
        }
        public bool AllowsEnemyReplica(ulong id, SharedEnemyRecord live)
        {
            if (!membership.TryGetValue(id, out int index) || !regions[index].Dormant ||
                (regions[index].DormantEnemies != null && !regions[index].DormantEnemies.Contains(id)) ||
                !regions[index].Enemies.TryGetValue(id, out var old)) return true;
            return !(live.Template == old.Template && live.HP == old.HP && live.MaxHP == old.MaxHP &&
                live.Position.Equals(old.Position) && live.Rotation.Equals(old.Rotation) && live.Level == old.Level &&
                live.Animation == old.Animation && live.AnimationTime == old.AnimationTime && (bool)live.Active == (bool)old.Active &&
                live.BurnRemaining == old.BurnRemaining && live.BurnDamage == old.BurnDamage && live.BurnAccumulator == old.BurnAccumulator &&
                live.HomeZ == old.HomeZ && (bool)live.Prunable == (bool)old.Prunable);
        }

        public void RetireBefore(float rear)
        {
            if (float.IsNaN(rear) || float.IsInfinity(rear) || rear <= RetiredBefore) return;
            RetiredBefore = rear;
            retired.Clear();
            foreach (var entry in regions) if (entry.Value.LogicalZ < rear) retired.Add(entry.Key);
            foreach (int index in retired) { RemoveMembership(regions[index]); regions.Remove(index); }
        }
        private void RemoveMembership(Region region)
        {
            foreach (ulong id in region.Items.Keys) membership.Remove(id);
            foreach (ulong id in region.Enemies.Keys) membership.Remove(id);
        }
        public void Clear() { regions.Clear(); membership.Clear(); retired.Clear(); RetiredBefore = float.NegativeInfinity; }
    }
}
