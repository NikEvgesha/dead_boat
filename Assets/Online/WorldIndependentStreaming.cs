using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Online
{
    // Independent actors use their LIVE canonical record, never the generation RNG.
    public sealed class WorldIndependentStreaming : MonoBehaviour
    {
        private readonly HashSet<ulong> items = new(), enemies = new();
        private readonly List<WorldSpawnIdentity> snapshot = new();
        private readonly List<ulong> obsolete = new();
        private readonly List<float> focus = new(5);
        private double next;
        public bool streamActors = true;
        public int SleepingCount => items.Count + enemies.Count;
        public int Suspended { get; private set; }
        public int Restored { get; private set; }
        public bool AllowsItem(ulong id, SharedItemRecord record) => record.Mode == 1 || record.Mode == 2 || record.Mode == 3 ||
            record.Owner != Fusion.PlayerRef.None || !items.Contains(id);
        public bool AllowsEnemy(ulong id) => !enemies.Contains(id);
        private void LateUpdate()
        {
            if (SharedRunContext.Independent != this || Time.timeScale <= 0 || Time.realtimeSinceStartupAsDouble < next ||
                !WorldRegionStreaming.CollectFocus(focus) ||
                SharedRunContext.StreamingBudget?.TryAcquire(Time.frameCount, 2) != true) return;
            next = Time.realtimeSinceStartupAsDouble + .25;
            Audit(focus, streamActors);
        }
        public bool Audit(IReadOnlyList<float> points, bool allowSuspension = true)
        {
            if (!SharedRunContext.Playing || points == null || points.Count == 0) return false;
            for (int i = 0; i < points.Count; i++) if (float.IsNaN(points[i]) || float.IsInfinity(points[i])) return false;
            var state = SharedRunContext.State;
            obsolete.Clear();
            foreach (ulong id in items) if (!state.Items.TryGet(id, out var item) || item.Mode == 4) obsolete.Add(id);
            foreach (ulong id in obsolete) items.Remove(id);
            obsolete.Clear();
            foreach (ulong id in enemies) if (!state.TryEnemy(id, out var enemy) || enemy.HP <= 0) obsolete.Add(id);
            foreach (ulong id in obsolete) enemies.Remove(id);
            foreach (ulong id in items)
            {
                var record = state.Items[id];
                if (allowSuspension && !AllowsItem(id, record) && !Near(record.Position.z, points, 600)) continue;
                items.Remove(id);
                if (SharedItemsRuntime.TryFind(id, out _)) { Restored++; return true; }
                var prefab = SharedItemCatalog.Load()?.Find(record.Template);
                if (prefab == null) { items.Add(id); return false; }
                var item = Instantiate(prefab, record.Position - LobbyNetworkAvatar.Origin, record.Rotation);
                var identity = item.gameObject.AddComponent<WorldSpawnIdentity>(); identity.ItemTemplate = record.Template;
                item.GetComponentInChildren<RangedWeaponController>(true)?.ApplySharedAmmo(record.WeaponAmmo);
                identity.InitializeRemote(id, 0); Restored++; return true;
            }
            foreach (ulong id in enemies)
            {
                state.TryEnemy(id, out var record);
                if (allowSuspension && !Near(record.Position.z, points, 600) && record.BurnRemaining <= 0) continue;
                enemies.Remove(id);
                if (SharedItemsRuntime.TryFind(id, out _)) { Restored++; return true; }
                if (!SharedEnemiesRuntime.TryTemplate(record.Template, out var prefab) ||
                    !SharedEnemiesRuntime.TryCreateReplica(prefab, id, record, out _)) { enemies.Add(id); return false; }
                Restored++; return true;
            }
            if (!allowSuspension || SleepingCount >= 4096) return false;
            SharedItemsRuntime.CopyTo(snapshot);
            foreach (var identity in snapshot)
            {
                if (!Eligible(identity) || identity.PhysicsNearby || !state.Items.TryGet(identity.Id, out var record) ||
                    record.Template == 0 || record.Mode != 0 || record.Owner != Fusion.PlayerRef.None ||
                    SharedItemCatalog.Load()?.Find(record.Template) == null || Near(record.Position.z, points, 800) ||
                    Near((identity.transform.position + LobbyNetworkAvatar.Origin).z, points, 800)) continue;
                if (!WorldReplicaRemoval.TryRemoveLocalTree(identity.gameObject, out _)) continue;
                items.Add(identity.Id); Suspended++; return true;
            }
            SharedItemsRuntime.CopyEnemiesTo(snapshot);
            foreach (var identity in snapshot)
            {
                if (!Eligible(identity) || !state.TryEnemy(identity.Id, out var record) || record.HP <= 0 || record.Template == 0 ||
                    record.BurnRemaining > 0 || !SharedEnemiesRuntime.TryTemplate(record.Template, out _) ||
                    Near(record.Position.z, points, 800) || Near((identity.transform.position + LobbyNetworkAvatar.Origin).z, points, 800)) continue;
                if (!WorldReplicaRemoval.TryRemoveLocalTree(identity.gameObject, out _)) continue;
                enemies.Add(identity.Id); Suspended++; return true;
            }
            return false;
        }
        private static bool Eligible(WorldSpawnIdentity identity) => identity != null && identity.Prunable &&
            !SharedRunContext.Membership.Contains(identity.Id) && identity.GetComponentInParent<LocationContentSpawner>(true) == null &&
            identity.GetComponentInParent<WorldCityRegion>(true) == null;
        private static bool Near(float z, IReadOnlyList<float> points, float distance) => WorldRegionStreaming.Near(new Vector2(z, z), points, distance);
    }
}
