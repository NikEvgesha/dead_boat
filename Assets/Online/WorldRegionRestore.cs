using System;
using UnityEngine;

namespace DeadBoat.Online
{
    // Only the explicit inactive-root transaction calls this. No network writes.
    public static class WorldRegionRestore
    {
        public static void InitializeItem(GameObject candidate, string key, System.Collections.Generic.IReadOnlyDictionary<ulong, string> originalNames)
        {
            ulong id = WorldSpawnIdentity.IdForKey(key);
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || !state.Items.TryGet(id, out var record))
                throw new InvalidOperationException("Missing canonical restored item: " + key);
            if (record.Mode != 0 || record.Owner != Fusion.PlayerRef.None || SharedItemsRuntime.TryFind(id, out _))
            {
                candidate.SetActive(false);
                UnityEngine.Object.Destroy(candidate);
                return;
            }
            var item = candidate.GetComponent<PickableItem>();
            if (item == null || SharedItemCatalog.Load()?.Identify(item) != record.Template ||
                (record.Template == 0 && (originalNames == null || !originalNames.TryGetValue(id, out var original) || original != SourceName(candidate.name))))
                throw new InvalidOperationException("Restored item template mismatch: " + key + " name=" + candidate.name + " live=" + record.Template + " generated=" + SharedItemCatalog.Load()?.Identify(item));
            candidate.transform.SetPositionAndRotation(record.Position - LobbyNetworkAvatar.Origin, record.Rotation);
            var identity = candidate.AddComponent<WorldSpawnIdentity>();
            identity.ItemTemplate = record.Template;
            candidate.GetComponentInChildren<RangedWeaponController>(true)?.ApplySharedAmmo(record.WeaponAmmo);
            identity.Key = key;
        }

        public static string SourceName(string value) => value.Replace("(Clone)", "").TrimEnd();

        public static void InitializeEnemy(ZombieController prefab, string key, Transform root)
        {
            ulong id = WorldSpawnIdentity.IdForKey(key);
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || !state.TryEnemy(id, out var record))
                throw new InvalidOperationException("Missing canonical restored enemy: " + key);
            if (record.HP <= 0 || SharedItemsRuntime.TryFind(id, out _)) return;
            if (SharedEnemiesRuntime.RegisterTemplate(prefab) != record.Template ||
                !SharedEnemiesRuntime.TryCreateReplica(prefab, id, record, out var identity))
                throw new InvalidOperationException("Restored enemy template/state mismatch: " + key);
            identity.transform.SetParent(root, true);
            identity.Key = key;
        }
    }
}
