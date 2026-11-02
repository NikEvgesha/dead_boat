using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Real prefab + actual Start, two live Cloud peers, no scene/save bootstrap.
    public static class WorldEnemyRehydrationProbe
    {
        public static async Task RunShared(SharedDepartureState authority, SharedDepartureState remote)
        {
            Require(!SharedRunContext.Active && !SoloRunBranch.Active && LoadingManager.Instance == null, "isolated scene required");
            const ulong id = 7301;
            var owner = new GameObject("Temporary enemy rehydration context");
            WorldSpawnIdentity actor = null;
            ZombieController ordinary = null;
            float scale = Time.timeScale;
            int deaths = 0;
            SharedRunContext.Begin(authority.Seed, authority.LevelId, owner, null, authority);
            try
            {
                Time.timeScale = 0;
                var prefab = AssetDatabase.LoadAssetAtPath<ZombieController>("Assets/Prefabs/Enemy/Zombie.prefab");
                Require(prefab != null, "real zombie prefab missing");
                var record = new SharedEnemyRecord { Template = SharedEnemiesRuntime.RegisterTemplate(prefab),
                    Level = 4, HP = 17, MaxHP = 150, Position = new Vector3(4000, 3, 4000), Rotation = Quaternion.identity };
                int popups = Object.FindObjectsByType<DamagePopup>(FindObjectsSortMode.None).Length;
                Require(SharedEnemiesRuntime.TryCreateReplica(prefab, id, record, out actor), "create wounded replica");
                var zombie = (ZombieController)actor.Enemy;
                zombie.enabled = false; zombie.Death += () => deaths++;
                Require(zombie.SharedHP == 17 && zombie.SharedMaxHP == 150 && zombie.mobLevel == 4, "initial HP/level mismatch");
                Start(zombie);
                Require(zombie.SharedHP == 17 && zombie.SharedMaxHP == 150 && !zombie.IsDead, "Start healed replica");
                Require(!SharedEnemiesRuntime.TryCreateReplica(prefab, id, record, out _), "duplicate replica created");
                Require(SharedItemsRuntime.TryFind(id, out var found) && found == actor, "duplicate replaced existing actor");
                authority.SampleEnemy(actor, zombie);
                await Until(() => remote.TryEnemy(id, out var e) && e.HP == 17 && e.MaxHP == 150 && e.Level == 4);
                foreach (var page in SharedWorldPage.All(authority.Runner))
                    if (page.Object.HasStateAuthority && page.Enemies.TryGet(id, out var e))
                    { e.BurnRemaining = 3; e.BurnDamage = 5; e.BurnAccumulator = .5f; page.Enemies.Set(id, e); }
                authority.SampleEnemy(actor, zombie);
                Require(authority.TryEnemy(id, out var burning) && burning.BurnRemaining == 3 && burning.BurnAccumulator == .5f,
                    "initial sample reset canonical burn");
                // The network burn clock advances even when gameplay timeScale is zero.
                // Clear this injected effect before waiting; the immediate sample above
                // tests preservation, not elapsed burn damage in this isolated fixture.
                foreach (var page in SharedWorldPage.All(authority.Runner))
                    if (page.Object.HasStateAuthority && page.Enemies.TryGet(id, out var e))
                    { e.BurnRemaining = 0; page.Enemies.Set(id, e); }
                Require(WorldReplicaRemoval.TryRemoveLocalTree(actor.gameObject, out var refusal), refusal);
                await Task.Delay(200); actor = null;
                record.HP = 0;
                Require(!SharedEnemiesRuntime.TryCreateReplica(prefab, id, record, out _) && !SharedItemsRuntime.TryFind(id, out _),
                    "dead enemy resurrected");
                foreach (int bad in new[] { -1, 151 })
                { record.HP = bad; Require(!SharedEnemiesRuntime.TryCreateReplica(prefab, id, record, out _), "invalid HP accepted"); }
                record.HP = 11;
                var invalid = record; invalid.Level = 0;
                Require(!SharedEnemiesRuntime.TryCreateReplica(prefab, id, invalid, out _), "invalid level accepted");
                invalid = record; invalid.Position.x = float.NaN;
                Require(!SharedEnemiesRuntime.TryCreateReplica(prefab, id, invalid, out _), "invalid position accepted");
                invalid = record; invalid.Rotation = default;
                Require(!SharedEnemiesRuntime.TryCreateReplica(prefab, id, invalid, out _), "invalid rotation accepted");
                foreach (var page in SharedWorldPage.All(authority.Runner))
                    if (page.Object.HasStateAuthority && page.Enemies.TryGet(id, out var e))
                    { e.HP = 11; page.Enemies.Set(id, e); }
                Require(authority.TryEnemy(id, out record), "latest canonical record missing");
                Require(SharedEnemiesRuntime.TryCreateReplica(prefab, id, record, out actor), "recreate latest wounded replica");
                zombie = (ZombieController)actor.Enemy; zombie.enabled = true; zombie.Death += () => deaths++;
                // Unlike the first case, let Unity deliver Start on its own frame.
                await Until(() => (bool)typeof(ZombieController).GetField("started", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(zombie));
                zombie.enabled = false;
                Require(zombie.SharedHP == 11 && zombie.SharedMaxHP == 150, "automatic Start healed replica");
                authority.SampleEnemy(actor, zombie);
                await Until(() => remote.TryEnemy(id, out var e) && e.HP == 11 && e.MaxHP == 150);
                Require(deaths == 0 && Object.FindObjectsByType<DamagePopup>(FindObjectsSortMode.None).Length == popups,
                    "initialization emitted combat effects");
                ordinary = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity); ordinary.enabled = false;
                ordinary.mobLevel = 4; Start(ordinary);
                int normalHP = Mathf.RoundToInt(Mathf.Lerp(ordinary.minHP, ordinary.maxHP, 3f / 9));
                Require(ordinary.SharedHP == normalHP && ordinary.SharedMaxHP == normalHP, "ordinary spawn health changed");
                var init = typeof(ZombieController).GetMethod("TryInitializeReplica", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(!(bool)init.Invoke(ordinary, new object[] { 4, 11, 150 }) && ordinary.SharedHP == normalHP,
                    "running actor accepted initialization override");
                string result = "[Enemy rehydration] PASS: real prefab wounded HP survives manual and automatic Start; duplicate/dead/invalid HP/level/pose refused; two-peer HP/level and canonical burn preserved; repeat latest HP; no damage popup/death; ordinary spawn unchanged";
                Debug.Log(result); System.IO.File.WriteAllText("Logs/EnemyRehydration-20261102.log", result);
            }
            finally
            {
                if (actor != null) Require(WorldReplicaRemoval.TryRemoveLocalTree(actor.gameObject, out _), "replica cleanup");
                if (ordinary != null) Object.Destroy(ordinary.gameObject);
                foreach (var page in SharedWorldPage.All(authority.Runner))
                    if (page.Object.HasStateAuthority) page.Enemies.Remove(id);
                SharedRunContext.End(); Time.timeScale = scale; Object.Destroy(owner);
            }
        }
        private static void Start(ZombieController zombie) => typeof(ZombieController)
            .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(zombie, null);
        private static async Task Until(Func<bool> check)
        {
            double end = Time.realtimeSinceStartupAsDouble + 8;
            while (!check()) { if (Time.realtimeSinceStartupAsDouble > end) throw new TimeoutException("replica HP replication"); await Task.Delay(25); }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
