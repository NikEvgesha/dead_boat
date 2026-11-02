using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class WorldRegionArchiveProbe
    {
        public static string Run()
        {
            var archive = new WorldRegionArchive(17, 2, 2, 5);
            var item = new SharedItemRecord { Mode = 0, WeaponAmmo = 7, Position = new Vector3(0, 2, 100) };
            var consumed = item; consumed.Mode = 4;
            var enemy = new SharedEnemyRecord { HP = 55, MaxHP = 100, BurnRemaining = 2, BurnDamage = 3 };
            var dead = enemy; dead.HP = 0;
            var items = new Dictionary<ulong, SharedItemRecord> { [1] = item, [3] = consumed };
            var enemies = new Dictionary<ulong, SharedEnemyRecord> { [2] = enemy, [4] = dead };
            Require(archive.TryStore(0, 1, 100, items, enemies, out var refusal), refusal);
            items.Clear(); enemies.Clear(); // archive owns copies, not caller collections
            Require(archive.Seed == 17 && archive.LevelId == 2 && archive.GeneratorVersion == RunRandom.Version &&
                archive.RegionCount == 1 && archive.RecordCount == 4, "metadata/count");
            Require(archive.TryItem(0, 3, out var tombstone) && tombstone.Mode == 4 &&
                archive.TryEnemy(0, 4, out var corpse) && corpse.HP == 0 && corpse.BurnDamage == 3, "tombstones missing");
            Require(archive.AllowsItemReplica(1, item), "active region hidden");
            archive.SetDormant(0, true);
            Require(!archive.AllowsItemReplica(1, item) && !archive.AllowsEnemyReplica(2, enemy), "unchanged dormant records visible");
            Require(archive.AllowsItemReplica(999, item) && archive.AllowsEnemyReplica(999, enemy), "unknown identity hidden");
            var latest = item; latest.Owner = PlayerRef.FromIndex(0); latest.Mode = 2;
            Require(archive.AllowsItemReplica(1, latest), "inventory hidden");
            latest = item; latest.Mode = 3;
            Require(archive.AllowsItemReplica(1, latest), "boat cargo hidden");
            latest = item; latest.WeaponAmmo = 8;
            Require(archive.AllowsItemReplica(1, latest), "latest ammo hidden");
            latest = item; latest.Position += Vector3.forward * 500;
            Require(archive.AllowsItemReplica(1, latest), "moved item hidden");
            var latestEnemy = enemy; latestEnemy.HP = 54;
            Require(archive.AllowsEnemyReplica(2, latestEnemy), "latest enemy HP hidden");
            latestEnemy = enemy; latestEnemy.BurnRemaining = 1;
            Require(archive.AllowsEnemyReplica(2, latestEnemy), "latest enemy burn hidden");
            Require(archive.TryItem(0, 1, out var copy) && copy.WeaponAmmo == 7, "live comparisons changed archive");
            copy.WeaponAmmo = 99;
            Require(archive.TryItem(0, 1, out copy) && copy.WeaponAmmo == 7, "returned struct mutated archive");

            Require(!archive.TryStore(0, 1, 100, null, null, out _), "stale revision accepted");
            Require(!archive.TryStore(0, 2, 101, null, null, out _), "region anchor changed");
            Require(!archive.TryStore(1, 1, 200, new[] { Pair(1, item) }, null, out _), "cross-region duplicate accepted");
            Require(!archive.TryStore(0, 2, 100, new[] { Pair(1, item), Pair(1, item) }, null, out _), "duplicate input accepted");
            Require(archive.RecordCount == 4 && !archive.AllowsItemReplica(1, item), "failed update partially committed");
            Require(archive.TryStore(1, 1, 200, new[] { Pair(5, item) }, null, out refusal), refusal);
            Require(!archive.TryStore(2, 1, 300, null, null, out refusal) && refusal == "region budget", "region budget ignored");
            Require(!archive.TryStore(1, 2, 200, new[] { Pair(5, item), Pair(6, item) }, null, out refusal) &&
                refusal == "record budget" && archive.RecordCount == 5, "record budget/atomicity");
            Require(archive.TryStore(0, 2, 100, new[] { Pair(1, item) },
                new[] { new KeyValuePair<ulong, SharedEnemyRecord>(2, enemy) }, out refusal), refusal);
            Require(archive.RecordCount == 3 && !archive.AllowsItemReplica(1, item) && archive.AllowsItemReplica(3, consumed), "replacement membership/dormancy");
            archive.RetireBefore(100); Require(archive.RegionCount == 2, "rear equality");
            archive.RetireBefore(101); Require(archive.RegionCount == 1 && archive.RecordCount == 1 && archive.AllowsItemReplica(1, item), "retirement membership");
            archive.RetireBefore(50); Require(archive.RetiredBefore == 101, "rear moved backwards");
            Require(!archive.TryStore(0, 3, 100, null, null, out _), "retired region resurrected");
            archive.Clear(); Require(archive.RegionCount == 0 && archive.RecordCount == 0 && archive.AllowsItemReplica(5, item), "clear");

            int visited = 0;
            var bounded = new WorldRegionArchive(1, 1, 1, 2);
            Require(!bounded.TryStore(0, 1, 0, Many(), null, out refusal) && refusal == "record budget" &&
                visited == 3 && bounded.RecordCount == 0, "staging exceeded budget or partially committed");
            string result = "[Region archive] PASS copies/tombstones, stale revision/duplicate rejection, atomic bounded staging, live-state precedence, dormancy, replacement, retirement";
            Debug.Log(result); System.IO.File.WriteAllText("Logs/WorldRegionArchive-20261102.log", result); return result;
            IEnumerable<KeyValuePair<ulong, SharedItemRecord>> Many()
            { for (ulong id = 1; id < 1000000; id++) { visited++; yield return Pair(id, item); } }
        }
        private static KeyValuePair<ulong, SharedItemRecord> Pair(ulong id, SharedItemRecord value) => new(id, value);
        public static async Task<string> RunLive()
        {
            var state = SharedRunContext.State;
            var archive = SharedRunContext.Regions;
            Require(EditorApplication.isPlaying && SharedRunContext.Playing && state.Object.HasStateAuthority && archive.RecordCount == 0,
                "isolated live master with empty archive required");
            int capacity = 0;
            var entry = SharedItemCatalog.Load().entries.First(e =>
            {
                var weapon = e.prefab != null ? e.prefab.GetComponentInChildren<RangedWeaponController>(true) : null;
                if (weapon == null) return false;
                using var data = new SerializedObject(weapon);
                capacity = data.FindProperty("maxAmmo").intValue;
                return capacity >= 2;
            });
            int initialAmmo = Mathf.Min(5, capacity), changedAmmo = initialAmmo - 1;
            GameObject original = null; ulong id = 0;
            try
            {
                var item = Object.Instantiate(entry.prefab, PlayerMovement.Instance.transform.position + new Vector3(300, 10, 10), Quaternion.identity);
                original = item.gameObject;
                foreach (var body in original.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
                var identity = original.AddComponent<WorldSpawnIdentity>(); identity.ItemTemplate = entry.id;
                identity.Key = "test:region-archive:live"; id = identity.Id;
                state.RegisterItem(identity);
                await Task.Delay(250);
                var record = state.Items[id]; record.Dropped = true; record.WeaponAmmo = initialAmmo;
                identity.Weapon.ApplySharedAmmo(initialAmmo); state.Items.Set(id, record);
                await Task.Delay(250); record = state.Items[id];
                Require(record.WeaponAmmo == initialAmmo, "live weapon fixture ammo");
                Require(archive.TryStore(1000000, 1, state.WorldRear, new[] { Pair(id, record) }, null, out var refusal), refusal);
                archive.SetDormant(1000000, true);
                Require(WorldReplicaRemoval.TryRemoveLocalTree(original, out refusal), refusal);
                await Task.Delay(750);
                Require(!SharedItemsRuntime.TryFind(id, out _), "dropped-item runtime recreated dormant weapon");
                archive.SetDormant(1000000, false);
                await Until(() => SharedItemsRuntime.TryFind(id, out var restored) && restored != null);
                await Task.Delay(250); // include spawned weapon Start and the next runtime audit
                SharedItemsRuntime.TryFind(id, out var reopened);
                int copies = Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsSortMode.None).Count(i => i.Id == id);
                Require(reopened.Weapon.CurrentAmmo == initialAmmo && copies == 1,
                    $"reopen {entry.prefab.name}: ammo={reopened.Weapon.CurrentAmmo}, copies={copies}");
                archive.SetDormant(1000000, true);
                Require(WorldReplicaRemoval.TryRemoveLocalTree(reopened.gameObject, out refusal), refusal);
                await Task.Delay(250);
                var newer = state.Items[id]; newer.WeaponAmmo = changedAmmo; state.Items.Set(id, newer);
                await Until(() => SharedItemsRuntime.TryFind(id, out var restored) && restored != null);
                await Task.Delay(250);
                SharedItemsRuntime.TryFind(id, out var latest);
                Require(latest.Weapon.CurrentAmmo == changedAmmo && state.Items[id].WeaponAmmo == changedAmmo &&
                    archive.TryItem(1000000, id, out var old) && old.WeaponAmmo == initialAmmo, "stale archive overwrote live weapon");
                string result = $"[Region archive live] PASS actual dropped weapon: dormant scan blocked, reopen creates one copy/ammo{initialAmmo}, latest ammo{changedAmmo} reappears without archive rollback";
                Debug.Log(result); System.IO.File.WriteAllText("Logs/WorldRegionArchiveLive-20261102.log", result); return result;
            }
            finally
            {
                archive.Clear();
                // Remove the replica/registry first. Otherwise its next LateUpdate can
                // re-register a just-deleted test record before deferred Destroy runs.
                if (id != 0 && SharedRunContext.State == state && SharedItemsRuntime.TryFind(id, out var current) && current != null)
                {
                    if (SharedRunContext.Playing)
                        Require(WorldReplicaRemoval.TryRemoveLocalTree(current.gameObject, out var cleanupRefusal), cleanupRefusal);
                    else Object.Destroy(current.gameObject);
                }
                if (id != 0 && state != null && state.Object != null && state.Object.IsValid)
                    foreach (var page in SharedWorldPage.All(state.Runner))
                        if (page.Object.HasStateAuthority) page.Items.Remove(id);
                if (original != null) Object.Destroy(original);
                if (id != 0 && SharedRunContext.State == state && SharedRunContext.Playing)
                {
                    await Task.Delay(300);
                    Require(!state.Items.ContainsKey(id) && !SharedItemsRuntime.TryFind(id, out _) &&
                        !Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsSortMode.None).Any(i => i.Id == id),
                        "live probe cleanup left or regenerated its weapon");
                    Debug.Log("[Region archive live] cleanup PASS: own record/registry/scene replica removed");
                }
            }
        }
        private static async Task Until(Func<bool> ready)
        {
            double limit = Time.realtimeSinceStartupAsDouble + 5;
            while (!ready())
            {
                if (!EditorApplication.isPlaying || Time.realtimeSinceStartupAsDouble > limit) throw new TimeoutException("live weapon reconstruction");
                await Task.Delay(10);
            }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
