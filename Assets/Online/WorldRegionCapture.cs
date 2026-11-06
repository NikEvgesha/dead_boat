using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeadBoat.Online
{
    public static class WorldRegionCapture
    {
        // Read-only capture. Does not remove objects or enable dormancy.
        public static bool TryCapture(LocationContentSpawner root, int revision, out string refusal)
        {
            refusal = null;
            var state = SharedRunContext.State;
            var membership = SharedRunContext.Membership;
            var archive = SharedRunContext.Regions;
            string key = root != null ? root.SharedGenerationKey : null;
            if (!SharedRunContext.Playing || membership == null || archive == null || key == null ||
                archive.Seed != state.Seed || archive.LevelId != state.LevelId ||
                !key.StartsWith("procedural:", StringComparison.Ordinal) ||
                !int.TryParse(key.AsSpan(11), NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            { refusal = "no live procedural region"; return false; }
            return TryCaptureTree(root.gameObject, index, revision, out refusal);
        }
        public static bool TryCaptureTree(UnityEngine.GameObject root, int index, int revision, out string refusal)
        {
            refusal = null;
            var state = SharedRunContext.State; var membership = SharedRunContext.Membership; var archive = SharedRunContext.Regions;
            string key = "region:" + index;
            if (!SharedRunContext.Playing || root == null || membership == null || archive == null)
            { refusal = "no live region"; return false; }
            var itemIds = new List<ulong>(); var enemyIds = new List<ulong>();
            if (!membership.TryCopy(index, itemIds, enemyIds, out float logicalZ))
            { refusal = "incomplete membership"; return false; }
            // A late peer may never have materialized the corpse. Its deterministic
            // record (including a consumed corpse) still belongs to the source enemy.
            foreach (ulong enemy in enemyIds)
            {
                string corpseKey = "corpse:" + enemy;
                ulong corpseId = WorldSpawnIdentity.IdForKey(corpseKey);
                if (state.Items.ContainsKey(corpseId)) membership.Observe(corpseKey, corpseId, false);
            }
            if (!membership.TryCopy(index, itemIds, enemyIds, out logicalZ))
            { refusal = "incomplete corpse membership"; return false; }
            foreach (var item in root.GetComponentsInChildren<PickableItem>(true))
            {
                if (WorldReplicaRemoval.IsDormantCorpsePart(item) || WorldReplicaRemoval.IsStoreDisplay(item)) continue;
                var identity = item.GetComponent<WorldSpawnIdentity>();
                if (identity == null || !membership.BelongsTo(index, identity.Id, false))
                {
                    refusal = "untracked region item";
#if UNITY_EDITOR
                    var zombie = item.GetComponentInParent<ZombieController>(true);
                    UnityEngine.Debug.LogWarning($"[Region capture detail] {key} item={item.name} key={identity?.Key} enabled={item.enabled} status={item.Status} zombie={zombie?.name} dead={zombie?.IsDead} exactDormant={zombie?.OwnsDormantCorpse(item)} enemyBinding={(identity != null && membership.BelongsTo(index, identity.Id, true))}");
#endif
                    return false;
                }
            }
            foreach (var enemy in root.GetComponentsInChildren<EnemyCore>(true))
            {
                var identity = enemy.GetComponent<WorldSpawnIdentity>();
                if (identity == null || !membership.BelongsTo(index, identity.Id, true))
                { refusal = "untracked region enemy"; return false; }
            }
            var items = new List<KeyValuePair<ulong, SharedItemRecord>>(itemIds.Count);
            var enemies = new List<KeyValuePair<ulong, SharedEnemyRecord>>(enemyIds.Count);
            foreach (ulong id in itemIds)
            {
                if (!state.Items.TryGet(id, out var record)) { refusal = "missing canonical item"; return false; }
                items.Add(new(id, record));
            }
            foreach (ulong id in enemyIds)
            {
                if (!state.TryEnemy(id, out var record)) { refusal = "missing canonical enemy"; return false; }
                enemies.Add(new(id, record));
            }
            return archive.TryStore(index, revision, logicalZ, items, enemies, out refusal);
        }
    }
}
