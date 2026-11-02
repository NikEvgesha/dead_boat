using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class WorldRegionResidencyProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static async Task RunShared(SharedDepartureState authority, SharedDepartureState remote)
        {
            Require(!SharedRunContext.Active && LoadingManager.Instance == null, "isolated scene required");
            var ownerObject = new GameObject("Temporary residency owner");
            var staging = new GameObject("Temporary residency source"); staging.SetActive(false);
            LocationContentSpawner root = null, template = null;
            WorldSpawnIdentity detached = null;
            var balance = ScriptableObject.CreateInstance<LocationBalanceProfile>();
            var itemIds = new List<ulong>(); var enemyIds = new List<ulong>();
            float scale = Time.timeScale;
            SharedRunContext.Begin(authority.Seed, authority.LevelId, ownerObject, null, authority);
            try
            {
                Time.timeScale = 0;
                var source = AssetDatabase.LoadAssetAtPath<LocationContentSpawner>("Assets/Prefabs/Locations/Enemy/ruins_1.prefab");
                Require(source != null, "real ruins prefab missing");
                template = Object.Instantiate(source, staging.transform);
                template.name = "Temporary ruins residency template";
                typeof(LocationContentSpawner).GetField("_balanceProfile", Private).SetValue(template, balance);
                template._enemyPrefabs = new() { AssetDatabase.LoadAssetAtPath<ZombieController>("Assets/Prefabs/Enemy/Zombie.prefab") };
                template.enemySpawnPoints = new();
                for (int i = 0; i < 2; i++)
                {
                    var point = new GameObject("Controlled enemy point " + i); point.transform.SetParent(template.transform, false);
                    point.transform.localPosition = new Vector3(i * 4, 0, 0); template.enemySpawnPoints.Add(point.transform);
                }
                var weaponPrefab = SharedItemCatalog.Load().entries.First(e => e.prefab != null &&
                    e.prefab.GetComponentInChildren<RangedWeaponController>(true) is RangedWeaponController w &&
                    new SerializedObject(w).FindProperty("maxAmmo").intValue >= 3).prefab;
                Object.Instantiate(weaponPrefab, template.transform);
                Quiet(template);
                var owner = ownerObject.AddComponent<LocationSpawner>(); owner.enabled = false;
                var manifest = new LocationLayoutManifest(authority.Seed);
                manifest.Record(new LocationLayoutStep(0, 1000, template));
                typeof(LocationSpawner).GetProperty("LayoutManifest").SetValue(owner, manifest);
                root = Object.Instantiate(template, new Vector3(10, 0, 1000), Quaternion.identity, staging.transform);
                root.ZPosition = 1000; root.SetLevel(4); root.InitializeGeneration(0);
                typeof(LocationContentSpawner).GetMethod("Start", Private).Invoke(root, null);
                Quiet(root); root.transform.SetParent(owner.transform, true);
                typeof(LocationSpawner).GetMethod("TrackReplica", Private).Invoke(owner, new object[] { root });
                var actors = root.GetComponentsInChildren<WorldSpawnIdentity>(true);
                foreach (var actor in actors)
                {
                    actor.PhysicsNearby = false;
                    if (actor.Item != null) authority.RegisterItem(actor);
                    if (actor.Enemy != null) authority.SampleEnemy(actor, actor.Enemy);
                }
                Require(SharedRunContext.Membership.TryCopy(0, itemIds, enemyIds, out _) && itemIds.Count >= 3 && enemyIds.Count == 2,
                    "controlled ruins membership");
                var weapon = actors.First(a => a.Weapon != null);
                var otherItems = actors.Where(a => a.Item != null && a != weapon).ToArray();
                var consumed = otherItems[0]; detached = otherItems[1];
                var killed = actors.First(a => a.Enemy != null);
                var wounded = actors.First(a => a.Enemy != null && a != killed);
                ulong weaponId = weapon.Id, usedId = consumed.Id, deadId = killed.Id, woundedId = wounded.Id, detachedId = detached.Id;
                weapon.PhysicsNearby = true;
                Require(!WorldRegionResidency.TrySuspend(root, 1, out _, out var reason) && reason == "nearby item replica" &&
                    root.gameObject.activeInHierarchy && SharedRunContext.Regions.RegionCount == 0, "nearby item guard was partial");
                weapon.PhysicsNearby = false;
                var used = authority.Items[usedId]; used.Mode = 4; authority.Items.Set(usedId, used);
                Require(WorldReplicaRemoval.TryRemoveLocalTree(consumed.gameObject, out reason), reason);
                authority.MarkEnemyDead(deadId);
                Require(WorldReplicaRemoval.TryRemoveLocalTree(killed.gameObject, out reason), reason);
                detached.transform.SetParent(null, true);
                var moved = authority.Items[detachedId]; moved.Dropped = true; moved.Position += Vector3.right * 300;
                authority.Items.Set(detachedId, moved); detached.transform.position = moved.Position - LobbyNetworkAvatar.Origin;
                weapon.Weapon.ApplySharedAmmo(2);
                var ammo = authority.Items[weaponId]; ammo.WeaponAmmo = 2; authority.Items.Set(weaponId, ammo);
                typeof(EnemyCore).GetField("currentHP", Private).SetValue(wounded.Enemy, 17);
                authority.SampleEnemy(wounded, wounded.Enemy);
                await Task.Delay(150);
                Require(WorldRegionResidency.TrySuspend(root, 1, out var ticket, out reason), reason);
                await Task.Delay(150);
                Require(root == null && SharedItemsRuntime.TryFind(detachedId, out var kept) && kept == detached,
                    "detached actor deleted with home region");
                Require(SharedRunContext.Regions.AllowsItemReplica(detachedId, moved), "detached dropped record hidden by dormancy");
                Require(!SharedItemsRuntime.TryFind(weaponId, out _) && !SharedItemsRuntime.TryFind(woundedId, out _), "removed replicas remain registered");
                ammo = authority.Items[weaponId]; ammo.WeaponAmmo = 1; authority.Items.Set(weaponId, ammo);
                foreach (var page in SharedWorldPage.All(authority.Runner))
                    if (page.Object.HasStateAuthority && page.Enemies.TryGet(woundedId, out var e))
                    { e.HP = 13; page.Enemies.Set(woundedId, e); }
                Require(WorldRegionResidency.TryRestore(ticket, out root, out reason), reason);
                Quiet(root);
                Check(root, manifest, weaponId, usedId, deadId, woundedId, detachedId, detached);
                Require(!WorldRegionResidency.TryRestore(ticket, out _, out _), "ticket restored twice");
                await Task.Delay(150); // Unity completes deferred removal of skipped prefab children.
                Require(WorldRegionResidency.TrySuspend(root, 2, out ticket, out reason), reason);
                await Task.Delay(150);
                // Missing canonical state must roll back partial replicas, not infer a
                // pristine item from the seed or overwrite the live world from archive.
                foreach (var page in SharedWorldPage.All(authority.Runner)) if (page.Object.HasStateAuthority) page.Items.Remove(weaponId);
                Require(!WorldRegionResidency.TryRestore(ticket, out root, out reason) && root == null &&
                    !SharedItemsRuntime.TryFind(woundedId, out _) && SharedItemsRuntime.TryFind(detachedId, out kept) && kept == detached,
                    "failed restore left partial actors or erased external item");
                authority.Items.Add(weaponId, ammo);
                await Task.Delay(150);
                Require(WorldRegionResidency.TryRestore(ticket, out root, out reason), reason);
                Quiet(root);
                Check(root, manifest, weaponId, usedId, deadId, woundedId, detachedId, detached);
                await Until(() => remote.Items.TryGet(weaponId, out var w) && w.WeaponAmmo == 1 &&
                    remote.Items.TryGet(usedId, out var c) && c.Mode == 4 &&
                    remote.TryEnemy(deadId, out var d) && d.HP == 0 && remote.TryEnemy(woundedId, out var e) && e.HP == 13);
                string result = "[Region residency] PASS: real ruins geometry + controlled seeded content; two suspend/restore cycles; consumed/dead absent; wounded HP13/ammo1 latest; detached dropped actor retained; layout cursor unchanged; nearby guard; partial failure rollback/retry; two-peer records unchanged";
                Debug.Log(result); System.IO.File.WriteAllText("Logs/WorldRegionResidency-20261102.log", result);
            }
            finally
            {
                if (root != null) foreach (var identity in root.GetComponentsInChildren<WorldSpawnIdentity>(true))
                { typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", Private).SetValue(identity, true); SharedItemsRuntime.Unregister(identity); }
                if (detached != null)
                { typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", Private).SetValue(detached, true); SharedItemsRuntime.Unregister(detached); Object.Destroy(detached.gameObject); }
                foreach (var page in SharedWorldPage.All(authority.Runner)) if (page.Object.HasStateAuthority)
                { foreach (ulong id in itemIds) page.Items.Remove(id); foreach (ulong id in enemyIds) page.Enemies.Remove(id); }
                SharedRunContext.End(); Time.timeScale = scale;
                Object.Destroy(ownerObject); Object.Destroy(staging); Object.Destroy(balance);
            }
        }
        private static void Quiet(LocationContentSpawner root)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null && !(behaviour is WorldSpawnIdentity)) behaviour.enabled = false;
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            foreach (var identity in root.GetComponentsInChildren<WorldSpawnIdentity>(true)) identity.PhysicsNearby = false;
        }
        private static void Check(LocationContentSpawner root, LocationLayoutManifest manifest, ulong weapon, ulong used,
            ulong dead, ulong wounded, ulong detachedId, WorldSpawnIdentity detached)
        {
            Require(root != null && root.gameObject.activeInHierarchy && manifest.NextIndex == 1 && manifest.LastLogicalZ == 1000,
                "restore advanced layout");
            Require(!SharedItemsRuntime.TryFind(used, out _) && !SharedItemsRuntime.TryFind(dead, out _), "consumed/dead actor resurrected");
            Require(SharedItemsRuntime.TryFind(weapon, out var gun) && gun.Weapon.CurrentAmmo == 1 &&
                SharedItemsRuntime.TryFind(wounded, out var enemy) && enemy.Enemy.SharedHP == 13, "archive overwrote latest HP/ammo");
            Require(SharedItemsRuntime.TryFind(detachedId, out var kept) && kept == detached && !kept.transform.IsChildOf(root.transform),
                "detached duplicate/reparent");
            foreach (ulong id in new[] { weapon, wounded, detachedId })
                Require(Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(a => a.Id == id) == 1, "duplicate world actor");
        }
        private static async Task Until(Func<bool> check)
        {
            double end = Time.realtimeSinceStartupAsDouble + 8;
            while (!check()) { if (Time.realtimeSinceStartupAsDouble > end) throw new TimeoutException("region records"); await Task.Delay(25); }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
