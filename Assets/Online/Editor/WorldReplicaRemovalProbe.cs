using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public sealed class ReplicaProbeEnemy : EnemyCore { }

    public static class WorldReplicaRemovalProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<GameObject> owned = new();
        private static WorldSpawnIdentity Item(ulong id, Transform parent = null)
        {
            var go = new GameObject("Temporary replica item"); owned.Add(go); go.SetActive(false);
            go.transform.SetParent(parent);
            go.AddComponent<PickableItem>().enabled = false;
            var identity = go.AddComponent<WorldSpawnIdentity>(); identity.InitializeRemote(id, 0);
            go.SetActive(true); return identity;
        }
        private static WorldSpawnIdentity Enemy(ulong id, Transform parent)
        {
            var go = new GameObject("Temporary replica enemy"); owned.Add(go); go.SetActive(false);
            go.transform.SetParent(parent);
            var enemy = go.AddComponent<ReplicaProbeEnemy>(); enemy.enabled = false; enemy.SetHP(55);
            var identity = go.AddComponent<WorldSpawnIdentity>(); identity.InitializeRemote(id, 0);
            go.SetActive(true); return identity;
        }
        public static async Task<string> RunSolo()
        {
            Require(EditorApplication.isPlaying && LoadingManager.Instance == null && !SharedRunContext.Active && !SoloRunBranch.Active,
                "empty isolated Play required");
            var saved = new SharedSoloSnapshot {
                Seed = 17, Items = new[] { new SoloWorldItem { Id = 7101, Ammo = 7, Mode = 0 } },
                Enemies = new[] { new SoloWorldEnemy { Id = 7102, HP = 55, MaxHP = 55 } }
            };
            try
            {
                typeof(SoloRunBranch).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { saved });
                var branch = (SoloRunBranch)typeof(SoloRunBranch).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                branch.enabled = false; // no game/save bootstrap in the empty scene
                var items = (Dictionary<ulong, SoloWorldItem>)typeof(SoloRunBranch).GetField("items", Private).GetValue(branch);
                var enemies = (Dictionary<ulong, SoloWorldEnemy>)typeof(SoloRunBranch).GetField("enemies", Private).GetValue(branch);
                var root = new GameObject("Temporary replica region"); owned.Add(root);
                var item = Item(7101, root.transform); Enemy(7102, root.transform);
                Require(WorldReplicaRemoval.TryRemoveLocalTree(root, out var reason), reason);
                Require(!root.activeSelf && !SharedItemsRuntime.TryFind(7101, out _), "replica did not leave registry immediately");
                // A replacement may be registered before deferred destruction of the old tree.
                var replacement = Item(7101);
                item.InitializeRemote(7101, 0); // stale registration after removal must not replace it
                await Frames();
                Require(root == null && SharedItemsRuntime.TryFind(7101, out var current) && current == replacement, "stale OnDestroy/registration erased replacement");
                Require(items[7101].Mode == 0 && items[7101].Ammo == 7 && enemies[7102].HP == 55, "replica removal wrote solo tombstone");

                var ordinaryItem = Item(7103); var ordinaryEnemy = Enemy(7104, null);
                Object.Destroy(ordinaryItem.gameObject); Object.Destroy(ordinaryEnemy.gameObject);
                await Frames();
                Require(items[7103].Mode == 4 && enemies[7104].HP == 0, "ordinary destruction no longer records outcome");

                var guarded = new GameObject("Temporary guarded region"); owned.Add(guarded);
                var free = Item(7110, guarded.transform); var portable = Item(7111, guarded.transform);
                typeof(PickableItem).GetField("_status", Private).SetValue(portable.Item, ItemStatus.InInventory);
                Require(!WorldReplicaRemoval.TryRemoveLocalTree(guarded, out _) && guarded.activeSelf &&
                    SharedItemsRuntime.TryFind(free.Id, out _), "portable guard partially removed tree");
                typeof(PickableItem).GetField("_status", Private).SetValue(portable.Item, ItemStatus.Free);
                // No object was marked on refusal: ordinary destruction must still create a tombstone.
                Object.Destroy(free.gameObject); await Frames();
                Require(items[7110].Mode == 4, "refused tree was partially marked");

                var pending = (Dictionary<ulong, Action<PickableItem>>)typeof(SharedItemsRuntime).GetField("pending", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                pending[portable.Id] = _ => { };
                Require(!WorldReplicaRemoval.TryRemoveLocalTree(guarded, out reason) && reason == "pending interaction", "pending request lost");
                pending.Remove(portable.Id);
                var untracked = new GameObject("Temporary untracked child"); owned.Add(untracked); untracked.SetActive(false);
                untracked.transform.SetParent(guarded.transform);
                var untrackedItem = untracked.AddComponent<PickableItem>(); untrackedItem.enabled = false;
                typeof(PickableItem).GetField("_status", Private).SetValue(untrackedItem, ItemStatus.InInventory);
                Require(!WorldReplicaRemoval.TryRemoveLocalTree(guarded, out reason) && reason == "portable item", "inactive untracked inventory lost");
                typeof(PickableItem).GetField("_status", Private).SetValue(untrackedItem, ItemStatus.Free);
                Require(!WorldReplicaRemoval.TryRemoveLocalTree(guarded, out reason) && reason == "untracked item", "untracked free item lost");
                Object.Destroy(untracked); await Frames();
                var unregistered = new GameObject("Temporary unregistered identity"); owned.Add(unregistered);
                unregistered.transform.SetParent(guarded.transform); unregistered.AddComponent<WorldSpawnIdentity>();
                Require(!WorldReplicaRemoval.TryRemoveLocalTree(guarded, out reason) && reason == "unregistered identity", "unregistered identity lost");
                Object.Destroy(unregistered); await Frames();
                Require(WorldReplicaRemoval.TryRemoveLocalTree(guarded, out reason), reason); await Frames();
                string result = "[Replica removal] PASS solo: live item/ammo/enemy retained, ordinary tombstones, delayed destroy/replacement, atomic portable/pending guards";
                Debug.Log(result); System.IO.File.WriteAllText("Logs/ReplicaRemovalSolo-20261102.log", result); return result;
            }
            finally
            {
                SoloRunBranch.Suspend();
                foreach (var go in owned) if (go != null) Object.Destroy(go);
                owned.Clear(); SharedItemsRuntime.Clear();
            }
        }
        private static async Task Frames()
        {
            int frame = Time.frameCount;
            double limit = Time.realtimeSinceStartupAsDouble + 5;
            while (Time.frameCount < frame + 2)
            {
                if (!EditorApplication.isPlaying || Time.realtimeSinceStartupAsDouble > limit) throw new TimeoutException("deferred destruction frames");
                await Task.Delay(1);
            }
        }
        public static async Task RunShared(SharedDepartureState authority, SharedDepartureState remote)
        {
            Require(!SharedRunContext.Active && !SoloRunBranch.Active && LoadingManager.Instance == null, "isolated shared protocol scene required");
            float scale = Time.timeScale;
            var owner = new GameObject("Temporary replica context"); owned.Add(owner);
            SharedRunContext.Begin(authority.Seed, authority.LevelId, owner, null, authority);
            try
            {
                Require(SharedRunContext.Playing, "shared context not playing");
                var root = new GameObject("Temporary shared region"); owned.Add(root);
                var item = Item(7201, root.transform); var enemy = Enemy(7202, root.transform);
                authority.RegisterItem(item); var record = authority.Items[7201]; record.WeaponAmmo = 7; authority.Items.Set(7201, record);
                authority.SampleEnemy(enemy, enemy.Enemy);
                await Until(() => remote.Items.TryGet(7201, out var r) && r.WeaponAmmo == 7 && remote.TryEnemy(7202, out var e) && e.HP == 55);
                // Exercise the dead-enemy teardown branch while canonical state is still alive.
                typeof(EnemyCore).GetField("isDead", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy.Enemy, true);
                Require(WorldReplicaRemoval.TryRemoveLocalTree(root, out var refusal), refusal);
                await Frames(); await Task.Delay(500);
                Require(authority.Items.TryGet(7201, out record) && record.Mode == 0 && record.WeaponAmmo == 7 &&
                    remote.Items.TryGet(7201, out var replicated) && replicated.Mode == 0 && replicated.WeaponAmmo == 7 &&
                    authority.TryEnemy(7202, out var localEnemy) && localEnemy.HP == 55 &&
                    remote.TryEnemy(7202, out var remoteEnemy) && remoteEnemy.HP == 55,
                    "replica removal changed network item/ammo/enemy");

                var ordinary = Item(7210); authority.RegisterItem(ordinary);
                record = authority.Items[7210]; record.Mode = 3; authority.Items.Set(7210, record);
                Require(!WorldReplicaRemoval.TryRemoveLocalTree(ordinary.gameObject, out refusal) && refusal == "owned or attached record",
                    "network attachment guard failed");
                record.Owner = remote.Runner.LocalPlayer; record.Mode = 2; authority.Items.Set(7210, record);
                Require(!WorldReplicaRemoval.TryRemoveLocalTree(ordinary.gameObject, out refusal) && refusal == "owned or attached record",
                    "remote inventory guard failed");
                record.Owner = authority.Runner.LocalPlayer; record.Mode = 1; authority.Items.Set(7210, record);
                Require(!WorldReplicaRemoval.TryRemoveLocalTree(ordinary.gameObject, out refusal) && refusal == "owned or attached record",
                    "network ownership guard failed");
                Object.Destroy(ordinary.gameObject);
                await Until(() => remote.Items.TryGet(7210, out var consumed) && consumed.Mode == 4);
                Debug.Log("[Replica removal] PASS shared: two peers retain item/ammo/enemy, ownership guard, ordinary destruction replicates consumption");
            }
            finally
            {
                foreach (var page in SharedWorldPage.All(authority.Runner))
                    if (page.Object.HasStateAuthority)
                    {
                        page.Items.Remove((ulong)7201); page.Items.Remove((ulong)7210);
                        page.Enemies.Remove((ulong)7202);
                    }
                SharedRunContext.End(); Time.timeScale = scale;
                foreach (var go in owned) if (go != null) Object.Destroy(go);
                owned.Clear();
            }
        }
        private static async Task Until(Func<bool> ready)
        {
            double limit = Time.realtimeSinceStartupAsDouble + 10;
            while (!ready())
            {
                if (!EditorApplication.isPlaying || Time.realtimeSinceStartupAsDouble > limit) throw new TimeoutException("replica network condition");
                await Task.Delay(10);
            }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
