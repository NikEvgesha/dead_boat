using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed class SharedItemsRuntime : MonoBehaviour
    {
        private static readonly Dictionary<ulong, WorldSpawnIdentity> identities = new();
        private static readonly Dictionary<ulong, WorldSpawnIdentity> enemyIdentities = new();
        internal static int EnemyRevision { get; private set; }
        private static readonly Dictionary<ulong, Action<PickableItem>> pending = new();
        private static readonly Dictionary<ulong, double> deadlines = new();
        private readonly List<WorldSpawnIdentity> snapshot = new();
        private readonly List<WorldSpawnIdentity> interpolated = new();
        private static int registryRevision;
        private int observedRevision = -1;
        private SharedDepartureState observedState;
        private bool observedAuthority;
        private double nextSample;
        private bool capacityReported;
        internal static readonly Unity.Profiling.ProfilerMarker UpdateMarker = new("DeadBoat.SharedItems");
        public static void Register(WorldSpawnIdentity identity)
        {
            if (identities.TryGetValue(identity.Id, out var restored) && restored != null && restored != identity && restored.RecoveryReplica)
            {
                UnityEngine.Object.Destroy(identity.gameObject);
                return;
            }
            if (identities.TryGetValue(identity.Id, out var existing) && existing != null && existing.Key != identity.Key)
                throw new InvalidOperationException("Shared world identity collision");
            identities[identity.Id] = identity;
            if (identity.Enemy != null) { enemyIdentities[identity.Id] = identity; EnemyRevision++; }
            else if (enemyIdentities.Remove(identity.Id)) EnemyRevision++;
            registryRevision++;
        }

        public static void Clear() { identities.Clear(); enemyIdentities.Clear(); pending.Clear(); deadlines.Clear(); registryRevision++; EnemyRevision++; }
        public static bool TryFind(ulong id, out WorldSpawnIdentity identity) => identities.TryGetValue(id, out identity);
        public static void CopyTo(List<WorldSpawnIdentity> destination) { destination.Clear(); destination.AddRange(identities.Values); }
        internal static void CopyEnemiesTo(List<WorldSpawnIdentity> destination) { destination.Clear(); destination.AddRange(enemyIdentities.Values); }

        public static void PublishDrop(PickableItem item)
        {
            if ((!SharedRunContext.Playing && !SoloRunBranch.Active) || item.GetComponent<WorldSpawnIdentity>() != null) return;
            int template = SharedItemCatalog.Load()?.Identify(item) ?? 0;
            if (template == 0)
            {
                Debug.LogWarning("[Shared items] Dropped personal item is absent from catalog: " + item.name);
                return;
            }
            ulong id = System.BitConverter.ToUInt64(System.Guid.NewGuid().ToByteArray(), 0);
            item.transform.SetParent(null, true);
            var identity = item.gameObject.AddComponent<WorldSpawnIdentity>();
            identity.ItemTemplate = template;
            identity.InitializeRemote(id, 0);
            if (SoloRunBranch.Active && !SharedRunContext.Active) return;
            SharedRunContext.State.RPC_DropPersonalItem(id, template,
                item.transform.position + LobbyNetworkAvatar.Origin, item.transform.rotation,
                item.GetComponentInChildren<RangedWeaponController>(true)?.CurrentAmmo ?? -1);
        }

        public static bool Burn(FuelItem fuel)
        {
            if (!SharedRunContext.Active) return false;
            if (SharedRunRecovery.Restoring || SharedRecoveryInventory.Applying) return true;
            var identity = fuel.GetComponent<WorldSpawnIdentity>();
            if (identity == null) return false;
            var item = fuel.GetComponent<PickableItem>();
            if (item != null && item.Status == ItemStatus.InInventory) return true;
            if (item != null && SharedRunContext.Playing && Simulates(item))
            {
                var state = SharedRunContext.State;
                var position = item.transform.position + LobbyNetworkAvatar.Origin;
                if (state.Items.TryGet(identity.Id, out var record) && record.Owner == state.Runner.LocalPlayer)
                    state.RPC_ItemState(identity.Id, 1,
                        position, item.transform.rotation);
                else if (state.Object.HasStateAuthority) state.SampleFreeItem(identity.Id, position, item.transform.rotation);
                state.RPC_BurnItem(identity.Id);
            }
            return true;
        }

        public static void Unregister(WorldSpawnIdentity identity)
        {
            // A regenerated duplicate or stale pre-reload instance must never
            // consume the item represented by the current registered instance.
            if (!identities.TryGetValue(identity.Id, out var current) || current != identity) return;
            var state = SharedRunContext.State;
            var enemy = identity.GetComponent<EnemyCore>();
            if (SharedRunContext.Playing && enemy != null && enemy.IsDead && state.Object.HasStateAuthority)
                state.MarkEnemyDead(identity.Id);
            if (SharedRunContext.Playing && state.Items.TryGet(identity.Id, out var record) &&
                record.Owner == state.Runner.LocalPlayer && record.Mode != 4)
                state.RPC_ItemState(identity.Id, 4, record.Position, record.Rotation);
            identities.Remove(identity.Id);
            if (enemyIdentities.Remove(identity.Id)) EnemyRevision++;
            registryRevision++;
            pending.Remove(identity.Id);
            deadlines.Remove(identity.Id);
        }

        // True means this is a shared request; callers must defer their local mutation.
        public static bool Request(PickableItem item, Action<PickableItem> action)
        {
            if (SharedRecoveryInventory.Applying) return false;
            if (!SharedRunContext.Active || item == null) return false;
            var identity = item.GetComponent<WorldSpawnIdentity>();
            if (identity == null) return false; // Personal starting/shop items keep their local owner.
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || state == null || SharedLocalGameplay.Blocked) return true;
            if (state.Items.TryGet(identity.Id, out var record) && record.Owner == state.Runner.LocalPlayer)
                return false;
            if (!pending.ContainsKey(identity.Id))
            {
                pending[identity.Id] = action;
                deadlines[identity.Id] = Time.realtimeSinceStartupAsDouble + 5;
                state.RPC_ClaimItem(identity.Id);
            }
            return true;
        }

        public static bool Simulates(PickableItem item)
        {
            if (!SharedRunContext.Active) return true;
            var identity = item.GetComponent<WorldSpawnIdentity>();
            if (identity == null) return true;
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || state == null) return false;
            if (!state.Items.TryGet(identity.Id, out var record)) return state.Object.HasStateAuthority;
            return record.Mode != 4 && record.Mode != 3 && (record.Owner == state.Runner.LocalPlayer ||
                (record.Owner == PlayerRef.None && state.Object.HasStateAuthority));
        }

        private static void ApplyPhysics(WorldSpawnIdentity identity, PickableItem item, bool simulate)
        {
            item.ApplySharedPhysics(simulate);
            identity.ApplyCorpsePhysics(simulate && item.Status != ItemStatus.InInventory && !item.Attached);
        }

        private static bool NearCrew(PickableItem item, bool wasNearby)
        {
            var state = SharedRunContext.State;
            Vector3 position = item.transform.position + LobbyNetworkAvatar.Origin;
            // Small hysteresis avoids repeatedly toggling bodies at the boundary.
            float radius = item.PhysicsActivationDistance + (wasNearby ? 5 : 0);
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == state.Runner && state.Includes(avatar.Object.StateAuthority) &&
                    (avatar.LogicalPosition - position).sqrMagnitude <= radius * radius) return true;
            return false;
        }

        private void LateUpdate()
        {
            using var measured = UpdateMarker.Auto();
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || state == null) return;
            double now = Time.realtimeSinceStartupAsDouble;
            bool sample = now >= nextSample;
            if (sample) nextSample = now + 0.1;
            bool authority = state.Object.HasStateAuthority;
            // Physics and state reconciliation follow the snapshot cadence. Only
            // visible proxies need frame interpolation between samples. A pending
            // claim, registry change or takeover still gets an immediate audit.
            if (!sample && pending.Count == 0 && observedRevision == registryRevision &&
                observedState == state && observedAuthority == authority && InterpolateFrame(state)) return;
            // Discovery can follow the 10 Hz snapshot cadence; interpolation still runs every frame.
            if (sample) foreach (var entry in state.Items)
            {
                if ((!entry.Value.Dropped && entry.Value.Mode != 3) || entry.Value.Template == 0 || entry.Value.Mode == 4 || TryFind(entry.Key, out _) ||
                    (entry.Value.Prunable && entry.Value.HomeZ < state.WorldRear && entry.Value.Position.z < state.WorldRear)) continue;
                var prefab = SharedItemCatalog.Load()?.Find(entry.Value.Template);
                if (prefab == null) continue;
                var spawned = Instantiate(prefab, entry.Value.Position - LobbyNetworkAvatar.Origin, entry.Value.Rotation);
                var identity = spawned.gameObject.AddComponent<WorldSpawnIdentity>();
                identity.ItemTemplate = entry.Value.Template;
                spawned.GetComponentInChildren<RangedWeaponController>(true)?.ApplySharedAmmo(entry.Value.WeaponAmmo);
                identity.InitializeRemote(entry.Key, 0);
            }
            CopyTo(snapshot);
            observedRevision = registryRevision;
            observedState = state;
            observedAuthority = authority;
            interpolated.Clear();
            foreach (var identity in snapshot)
            {
                if (identity == null) continue;
                var item = identity.Item;
                if (item == null || identity.Enemy != null) continue;
                bool hasRecord = state.Items.TryGet(identity.Id, out var record);
                // Existing records need no registration or repeated dictionary lookup.
                if (!hasRecord && state.Object.HasStateAuthority)
                {
                    state.RegisterItem(identity);
                    hasRecord = state.Items.TryGet(identity.Id, out record);
                    if (!hasRecord && !capacityReported)
                    {
                        capacityReported = true;
                        Debug.LogWarning("[Shared items] Snapshot capacity exceeded; unregistered pickups remain locked.");
                    }
                }
                if (!hasRecord)
                {
                    ApplyPhysics(identity, item, false);
                    continue;
                }
                if (record.Prunable && record.Owner == PlayerRef.None && record.Mode != 3 &&
                    record.HomeZ < state.WorldRear && record.Position.z < state.WorldRear)
                {
                    Destroy(item.gameObject); continue;
                }
                bool owner = record.Owner == state.Runner.LocalPlayer;
                if (pending.TryGetValue(identity.Id, out var action))
                {
                    if (owner)
                    {
                        pending.Remove(identity.Id);
                        deadlines.Remove(identity.Id);
                        item.gameObject.SetActive(true);
                        identity.Weapon?.ApplySharedAmmo(record.WeaponAmmo);
                        if (item.GetComponentInParent<BoardController>() == null) item.transform.SetParent(null, true);
                        item.ApplySharedWorldState(0);
                        ApplyPhysics(identity, item, true);
                        if (SharedLocalGameplay.Blocked)
                        {
                            state.RPC_ItemState(identity.Id, 0, record.Position, record.Rotation);
                            continue;
                        }
                        action(item);
                        // Failed inventory insertion must release the claim.
                        if (item.Status == ItemStatus.Free)
                            state.RPC_ItemState(identity.Id, 0, record.Position, record.Rotation);
                    }
                    else if (record.Owner != PlayerRef.None || deadlines[identity.Id] < now)
                    {
                        pending.Remove(identity.Id);
                        deadlines.Remove(identity.Id);
                    }
                }
                if (record.Mode == 4)
                {
                    Destroy(item.gameObject);
                    continue;
                }
                if (owner)
                {
                    if (sample)
                    {
                        int mode = item.Status == ItemStatus.InInventory ? 2 : item.Attached ? 3 : item.Grabbed ? 1 : 0;
                        int ammo = identity.Weapon?.CurrentAmmo ?? -1;
                        if (mode != 2 || record.Mode != 2 || record.WeaponAmmo != ammo)
                        {
                            Vector3 position = mode == 2 && PlayerMovement.Instance != null
                                ? PlayerMovement.Instance.transform.position : item.transform.position;
                            state.RPC_ItemState(identity.Id, mode, position + LobbyNetworkAvatar.Origin,
                                item.transform.rotation, ammo);
                        }
                    }
                    ApplyPhysics(identity, item, true);
                    continue;
                }
                if (record.Mode == 3 && BoardController.Instance != null)
                {
                    item.ApplySharedWorldState(3);
                    ApplyPhysics(identity, item, false);
                    if (item.transform.parent != BoardController.Instance.transform)
                        item.transform.SetParent(BoardController.Instance.transform, false);
                    item.transform.SetLocalPositionAndRotation(record.BoatLocalPosition, record.BoatLocalRotation);
                    continue;
                }
                identity.Weapon?.ApplySharedAmmo(record.WeaponAmmo);
                if ((record.Mode == 1 || record.Mode == 2) && item.transform.parent != null)
                    item.transform.SetParent(null, true);
                item.ApplySharedWorldState(record.Mode);
                bool ownerPresent = false;
                foreach (var player in state.Runner.ActivePlayers) if (player == record.Owner) ownerPresent = true;
                bool visible = record.Mode != 2 && (record.Mode != 1 || ownerPresent);
                if (item.gameObject.activeSelf != visible) item.gameObject.SetActive(visible);
                bool simulates = record.Owner == PlayerRef.None && state.Object.HasStateAuthority;
                if (simulates && sample) identity.PhysicsNearby = NearCrew(item, identity.PhysicsNearby);
                ApplyPhysics(identity, item, simulates && identity.PhysicsNearby);
                if (simulates)
                {
                    if (sample) state.SampleFreeItem(identity.Id, item.transform.position + LobbyNetworkAvatar.Origin,
                        item.transform.rotation);
                }
                else if (visible)
                {
                    interpolated.Add(identity);
                    Interpolate(identity, record);
                }
            }
        }
        // Return false when a visible proxy changes ownership/mode so its full
        // reconciliation runs on this frame, rather than interpolating a stale mode.
        private bool InterpolateFrame(SharedDepartureState state)
        {
            foreach (var identity in interpolated)
            {
                if (identity == null || identity.Item == null) continue;
                if (!state.Items.TryGet(identity.Id, out var record) || record.Mode > 1 ||
                    record.Owner == state.Runner.LocalPlayer ||
                    (record.Owner == PlayerRef.None && state.Object.HasStateAuthority)) return false;
                Interpolate(identity, record);
            }
            return true;
        }
        private static void Interpolate(WorldSpawnIdentity identity, SharedItemRecord record)
        {
            var part = identity.Item.transform;
            Vector3 target = record.Position - LobbyNetworkAvatar.Origin;
            Vector3 position = part.position;
            if (position == target && part.rotation == record.Rotation) return;
            Vector3 next = Vector3.Lerp(position, target, 1 - Mathf.Exp(-20 * Time.unscaledDeltaTime));
            if ((next - target).sqrMagnitude < .00000001f) next = target;
            part.SetPositionAndRotation(next, record.Rotation);
        }
    }
}
