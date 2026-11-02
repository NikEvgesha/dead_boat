using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class WorldRegionMembershipProbe
    {
        public static string Run()
        {
            var members = new WorldRegionMembership(2, 4);
            ulong item = WorldSpawnIdentity.IdForKey("procedural:0:item:0");
            ulong enemy = WorldSpawnIdentity.IdForKey("procedural:0:enemy:0");
            var items = new List<ulong>(); var enemies = new List<ulong>();
            Require(members.Begin(0, 100), "begin");
            Require(members.Observe("procedural:0:item:0", item, false) && members.Observe("procedural:0:enemy:0", enemy, true), "bind");
            Require(!members.TryCopy(0, items, enemies, out _), "unsealed generation exposed");
            members.Seal(0);
            Require(members.TryCopy(0, items, enemies, out _) && items.Contains(item) && enemies.Contains(enemy), "sealed members");
            Require(members.Observe("procedural:0:item:0", item, false) && members.BindingCount == 2, "duplicate registration grows history");
            string corpse = "corpse:" + enemy;
            ulong corpseId = WorldSpawnIdentity.IdForKey(corpse);
            Require(members.Observe(corpse, corpseId, false) && members.BelongsTo(0, corpseId, false), "corpse origin");
            Require(members.Begin(0, 100), "repeat generation"); members.Seal(0);
            Require(members.TryCopy(0, items, enemies, out _) && items.Count == 2 && enemies.Count == 1, "repeat lost dead/consumed history");
            Require(!members.Observe("night:17", 88, true) && !members.Observe("city:1000", 89, false), "nonprocedural origin guessed");
            members.RetireBefore(100); Require(members.BindingCount == 3, "rear equality");
            members.RetireBefore(101); Require(members.BindingCount == 0 && members.RegionCount == 0, "retirement");
            Require(!members.Begin(0, 100) && !members.Observe(corpse, corpseId, false), "retired history resurrected");
            members.Clear(); Require(members.Begin(1, 200), "clear");

            var limited = new WorldRegionMembership(1, 1);
            limited.Begin(0, 100); limited.Observe("procedural:0:item:0", item, false);
            Require(!limited.Observe("procedural:0:enemy:0", enemy, true) && limited.BindingCount == 1, "binding budget");
            limited.Seal(0); Require(!limited.TryCopy(0, items, enemies, out _), "truncated membership exposed");
            Require(!limited.Begin(1, 200), "region budget");
            var conflict = new WorldRegionMembership(); conflict.Begin(0, 100); conflict.Begin(1, 200);
            conflict.Observe("procedural:0:item:0", item, false);
            Require(!conflict.Observe("procedural:1:item:0", item, false), "cross-region collision accepted");
            conflict.Seal(0); conflict.Seal(1);
            Require(!conflict.TryCopy(0, items, enemies, out _) && !conflict.TryCopy(1, items, enemies, out _), "collision did not block both regions");
            foreach (string badKey in new[] { "procedural:0:", "procedural:0:item:x", "procedural:0:unknown:0" })
            {
                var malformed = new WorldRegionMembership(); malformed.Begin(0, 100);
                Require(!malformed.Observe(badKey, 1, false), "malformed key accepted"); malformed.Seal(0);
                Require(!malformed.TryCopy(0, items, enemies, out _), "unsupported binding exposed as complete");
            }
            var authoredMembers = new WorldRegionMembership(); authoredMembers.Begin(3, 300);
            const string authoredKey = "authored:Forest:procedural:3/ActivateItem[2]/FinSword[0]/";
            ulong authoredId = WorldSpawnIdentity.IdForKey(authoredKey);
            Require(authoredMembers.Observe(authoredKey, authoredId, false), "authored procedural pickup"); authoredMembers.Seal(3);
            Require(authoredMembers.TryCopy(3, items, enemies, out _) && items.Contains(authoredId), "authored pickup not retained");
            Require(!authoredMembers.Observe("authored:Forest:city:1000/FinSword[0]/", 99, false), "city origin guessed");
            foreach (string key in new[] { "procedural:17:item:0", "corpse:981726", "город/яйцо", "scene:Forest/Tree[1]" })
            {
                ulong old = 14695981039346656037UL;
                foreach (byte b in System.Text.Encoding.UTF8.GetBytes(key)) old = unchecked((old ^ b) * 1099511628211UL);
                Require(WorldSpawnIdentity.IdForKey(key) == old, "identity protocol changed");
            }
            string result = "[Region membership] PASS seal/history/corpse origin, duplicates, bounded incomplete fallback, collisions, retirement, legacy UTF8 identity hashes";
            Debug.Log(result); System.IO.File.WriteAllText("Logs/WorldRegionMembership-20261102.log", result); return result;
        }

        public static async Task RunShared(SharedDepartureState authority, SharedDepartureState remote)
        {
            Require(UnityEditor.EditorApplication.isPlaying && LoadingManager.Instance == null && !SharedRunContext.Active && !SoloRunBranch.Active,
                "isolated shared protocol scene required");
            float scale = Time.timeScale;
            var root = new GameObject("Temporary procedural membership region"); root.transform.position = new Vector3(0, 0, 100);
            var location = root.AddComponent<LocationContentSpawner>(); location.enabled = false; location.InitializeGeneration(0);
            var owner = new GameObject("Temporary membership context");
            SharedRunContext.Begin(authority.Seed, authority.LevelId, owner, null, authority);
            var members = SharedRunContext.Membership; var archive = SharedRunContext.Regions;
            ulong first = WorldSpawnIdentity.IdForKey("procedural:0:item:0"), consumed = WorldSpawnIdentity.IdForKey("procedural:0:item:1");
            ulong enemyId = WorldSpawnIdentity.IdForKey("procedural:0:enemy:0"), corpse = WorldSpawnIdentity.IdForKey("corpse:" + enemyId);
            ulong authored = 0;
            try
            {
                CheckDormantCorpse(owner.transform);
                members.Begin(0, 100);
                var item = MakeItem("procedural:0:item:0", root.transform); authority.RegisterItem(item);
                var gone = MakeItem("procedural:0:item:1", root.transform); authority.RegisterItem(gone);
                var record = authority.Items[consumed]; record.Mode = 4; authority.Items.Set(consumed, record);
                var enemyObject = new GameObject("Temporary membership enemy"); enemyObject.SetActive(false); enemyObject.transform.SetParent(root.transform, false);
                var enemy = enemyObject.AddComponent<ReplicaProbeEnemy>(); enemy.enabled = false; enemy.SetHP(55);
                var identity = enemyObject.AddComponent<WorldSpawnIdentity>(); identity.Key = "procedural:0:enemy:0"; enemyObject.SetActive(true);
                authority.SampleEnemy(identity, enemy); authority.MarkEnemyDead(enemyId);
                authority.Items.Add(corpse, new SharedItemRecord { Mode = 4, WeaponAmmo = -1, Position = root.transform.position });
                var embedded = new GameObject("Temporary authored pickup"); embedded.SetActive(false); embedded.transform.SetParent(root.transform, false);
                embedded.AddComponent<PickableItem>().enabled = false;
                var store = new GameObject("Temporary store display"); store.SetActive(false); store.transform.SetParent(root.transform, false);
                store.AddComponent<StorePoint>().enabled = false;
                var display = new GameObject("Temporary display model"); display.transform.SetParent(store.transform, false);
                display.AddComponent<PickableItem>().enabled = false;
                var spawner = owner.AddComponent<LocationSpawner>(); spawner.enabled = false;
                root.transform.SetParent(owner.transform, true);
                var manifest = new LocationLayoutManifest(authority.Seed); manifest.Record(new LocationLayoutStep(0, 100, location));
                typeof(LocationSpawner).GetProperty("LayoutManifest").SetValue(spawner, manifest);
                location.InitializeGeneration(0);
                typeof(LocationContentSpawner).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(location, null);
                var authoredIdentity = embedded.GetComponent<WorldSpawnIdentity>();
                Require(authoredIdentity != null && authoredIdentity.Key.StartsWith("authored:", StringComparison.Ordinal), "embedded pickup not registered by initializer");
                Require(display.GetComponent<WorldSpawnIdentity>() == null, "store display treated as world pickup");
                authored = authoredIdentity.Id; authority.RegisterItem(authoredIdentity);
                members.Seal(0);
                Object.Destroy(gone.gameObject); Object.Destroy(enemyObject); Object.Destroy(store);
                await Task.Delay(200);
                Require(WorldRegionCapture.TryCapture(location, 1, out var refusal), refusal);
                Require(archive.RecordCount == 5 && archive.TryItem(0, authored, out _) && archive.TryItem(0, consumed, out var used) && used.Mode == 4 &&
                    archive.TryEnemy(0, enemyId, out var dead) && dead.HP == 0 && archive.TryItem(0, corpse, out used) && used.Mode == 4,
                    "capture lost destroyed/never-materialized records");
                record = authority.Items[first]; record.Mode = 2; record.Owner = remote.Runner.LocalPlayer; record.Position = new Vector3(0, 3, 1000);
                authority.Items.Set(first, record);
                Require(WorldRegionCapture.TryCapture(location, 2, out refusal), refusal);
                Require(archive.TryItem(0, first, out var moved) && moved.Mode == 2 && moved.Position.z == 1000 && moved.Owner == remote.Runner.LocalPlayer,
                    "capture guessed ownership/position from source root");
                var untracked = new GameObject("Temporary untracked region item"); untracked.SetActive(false); untracked.transform.SetParent(root.transform, false);
                untracked.AddComponent<PickableItem>().enabled = false;
                Require(!WorldRegionCapture.TryCapture(location, 3, out refusal) && refusal == "untracked region item" && archive.RecordCount == 5,
                    "partial root capture accepted");
                Object.Destroy(untracked); await Task.Delay(100);
                foreach (var page in SharedWorldPage.All(authority.Runner)) if (page.Object.HasStateAuthority) page.Items.Remove(first);
                Require(!WorldRegionCapture.TryCapture(location, 3, out refusal) && refusal == "missing canonical item" &&
                    archive.TryItem(0, first, out moved) && moved.Mode == 2, "missing canonical state overwrote archive");
                await Until(() => remote.Items.TryGet(consumed, out var r) && r.Mode == 4 && remote.TryEnemy(enemyId, out var e) && e.HP == 0);
                Debug.Log("[Region membership] PASS shared: authored pickup/dead/consumed/corpse captured, moved inventory preserved, incomplete root/missing record rejected atomically");
            }
            finally
            {
                foreach (var page in SharedWorldPage.All(authority.Runner)) if (page.Object.HasStateAuthority)
                { page.Items.Remove(first); page.Items.Remove(consumed); page.Items.Remove(corpse); if (authored != 0) page.Items.Remove(authored); page.Enemies.Remove(enemyId); }
                SharedRunContext.End(); Time.timeScale = scale;
                Object.Destroy(root); Object.Destroy(owner);
                Require(members.BindingCount == 0 && SharedRunContext.Membership == null, "membership survived context end");
            }
        }
        private static WorldSpawnIdentity MakeItem(string key, Transform parent)
        {
            var go = new GameObject("Temporary membership item"); go.SetActive(false); go.transform.SetParent(parent, false);
            go.AddComponent<PickableItem>().enabled = false;
            var identity = go.AddComponent<WorldSpawnIdentity>(); identity.Key = key; go.SetActive(true); return identity;
        }
        private static void CheckDormantCorpse(Transform holder)
        {
            var go = new GameObject("Temporary dormant corpse fixture"); go.SetActive(false); go.transform.SetParent(holder, false);
            var zombie = go.AddComponent<ZombieController>(); zombie.enabled = false;
            go.AddComponent<WorldSpawnIdentity>().Key = "test:membership:corpse-fixture";
            var body = new GameObject("Temporary authored corpse part"); body.SetActive(false); body.transform.SetParent(go.transform, false);
            var part = body.AddComponent<PickableItem>(); part.enabled = false;
            typeof(ZombieController).GetField("pickable", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(zombie, part);
            Require(WorldReplicaRemoval.IsDormantCorpsePart(part), "exact dormant corpse part not recognized");
            var other = new GameObject("Temporary unrelated dormant item"); other.SetActive(false); other.transform.SetParent(go.transform, false);
            var unknown = other.AddComponent<PickableItem>(); unknown.enabled = false;
            Require(!WorldReplicaRemoval.IsDormantCorpsePart(unknown), "arbitrary inactive child exempted");
            part.enabled = true; Require(!WorldReplicaRemoval.IsDormantCorpsePart(part), "activated corpse exempted"); part.enabled = false;
            typeof(PickableItem).GetField("_status", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(part, ItemStatus.InInventory);
            Require(!WorldReplicaRemoval.IsDormantCorpsePart(part), "inventory exempted as corpse part");
            typeof(PickableItem).GetField("_status", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(part, ItemStatus.Free);
            body.transform.SetParent(null); Require(!WorldReplicaRemoval.IsDormantCorpsePart(part), "detached corpse exempted"); body.transform.SetParent(go.transform, false);
            typeof(EnemyCore).GetField("isDead", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(zombie, true);
            Require(!WorldReplicaRemoval.IsDormantCorpsePart(part), "dead enemy corpse exempted");
        }
        public static string CheckLive(int revision)
        {
            Require(SharedRunContext.Playing, "live world required");
            int roots = 0, authored = 0;
            foreach (var root in Object.FindObjectsByType<LocationContentSpawner>(FindObjectsSortMode.None))
            {
                if (!root.SharedGenerationKey.StartsWith("procedural:", StringComparison.Ordinal)) continue;
                Require(WorldRegionCapture.TryCapture(root, revision, out var refusal), root.SharedGenerationKey + ": " + refusal);
                foreach (var identity in root.GetComponentsInChildren<WorldSpawnIdentity>(true))
                    if (identity.Key != null && identity.Key.StartsWith("authored:", StringComparison.Ordinal))
                    {
                        Require(SharedRunContext.State.Items.TryGet(identity.Id, out var record) && record.Template != 0, "authored pickup missing catalog template");
                        authored++;
                    }
                roots++;
            }
            Require(roots > 0, "no generated regions");
            string result = $"[Region membership live] PASS roots={roots} bindings={SharedRunContext.Membership.BindingCount} archivedRecords={SharedRunContext.Regions.RecordCount} authoredPickups={authored}; canonical procedural capture without unloading";
            Debug.Log(result); System.IO.File.AppendAllText("Logs/WorldRegionMembershipLive-20261102.log", result + "\n"); return result;
        }
        private static async Task Until(Func<bool> ready)
        {
            double limit = Time.realtimeSinceStartupAsDouble + 10;
            while (!ready()) { if (Time.realtimeSinceStartupAsDouble > limit) throw new TimeoutException("membership replication"); await Task.Delay(10); }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
