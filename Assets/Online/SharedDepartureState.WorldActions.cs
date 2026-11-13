using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public enum WorldActionKind { Zone = 1, Trap = 2, Switch = 3, Lever = 4, Portal = 5, Podium = 6 }
    public struct SharedWorldActionRecord : INetworkStruct
    {
        public int Kind, Flags, Spawned, Total, Region;
        public int LastSlot;
        public Vector3 Position;
        public TickTimer Due;
        public float Interval;
        public float HomeZ;
        public ulong ConsumedItem;
    }
    [Serializable]
    public struct SoloWorldAction
    {
        public ulong Id, ConsumedItem;
        public int Kind, Flags, Spawned, Total, Region;
        public int LastSlot;
        public Vector3 Position;
        public float Remaining, Interval;
        public float HomeZ;
    }
    public static class SharedWorldActionAnimation
    {
        public static void Apply(Animator animator, bool restoring)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetTrigger("Start");
            if (!restoring || !animator.gameObject.activeInHierarchy) return;
            float length = 0;
            foreach (var clip in animator.runtimeAnimatorController.animationClips) length = Mathf.Max(length, clip.length);
            animator.Update(0); animator.Update(length + 1); // evaluate the authored final curves once, not every snapshot
        }
    }
    public sealed partial class SharedDepartureState
    {
        [Networked, Capacity(4)] public NetworkArray<TickTimer> PortalCooldowns => default;
        private readonly Dictionary<ulong, SharedWorldPage> actionLocations = new();
        internal SharedWorldPage ActionPage(ulong id)
        {
            if (actionLocations.TryGetValue(id, out var found) && found != null && found.Object != null &&
                found.Object.IsValid && found.Actions.ContainsKey(id)) return found;
            actionLocations.Remove(id);
            foreach (var page in SharedWorldPage.All(Runner)) if (page.Actions.ContainsKey(id))
            { if (actionLocations.Count >= 4096) actionLocations.Clear(); actionLocations[id] = page; return page; }
            return null;
        }
        public bool TryWorldAction(ulong id, out SharedWorldActionRecord value)
        {
            var page = ActionPage(id);
            if (page != null) return page.Actions.TryGet(id, out value);
            value = default; return false;
        }
        private SharedWorldPage AvailableActionPage(int enemies)
        {
            int index = 0;
            foreach (var page in SharedWorldPage.All(Runner))
            {
                index = Mathf.Max(index, page.Index + 1);
                if (page.Object.HasStateAuthority && page.Actions.Count < page.Actions.Capacity &&
                    page.Enemies.Capacity - page.Enemies.Count >= enemies) return page;
            }
            return SpawnPage(index);
        }
        internal bool WorldActionsReady()
        {
            if (!SharedRunContext.Playing || !Object.HasStateAuthority) return false;
            bool any = false;
            foreach (var page in SharedWorldPage.All(Runner))
            { any = true; if (!page.Object.HasStateAuthority) return false; }
            return any && SharedEnemiesRuntime.EnsureAuthority(this);
        }
        private bool ActionActorNear(PlayerRef player, Component target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            foreach (var avatar in LobbyNetworkAvatar.All)
            {
                if (avatar.Runner != Runner || avatar.Object.StateAuthority != player || avatar.Health <= 0) continue;
                Vector3 position = avatar.LogicalPosition - LobbyNetworkAvatar.Origin;
                var collider = target.GetComponent<Collider>();
                return collider != null && collider.enabled ? Vector3.Distance(position, collider.ClosestPoint(position)) <= 4
                    : Vector3.Distance(position, target.transform.position) <= 8;
            }
            return false;
        }
        private SharedWorldPage CreateWorldAction(SharedWorldActionsRuntime.Node node, out SharedWorldActionRecord value)
        {
            value = new SharedWorldActionRecord { Kind = (int)node.Kind, Region = node.Region, Position = node.LogicalPosition, HomeZ = node.HomeZ };
            int count = 0;
            DungeonTrap trap = node.Component as DungeonTrap;
            if (trap != null)
            {
                count = trap.ScriptedSpawnCount;
                if (count < 0 || count > 64 || trap.ScriptedEnemyPrefab == null || trap.ScriptedSpawnPosition == null) return null;
                value.Total = count; value.Interval = Mathf.Max(.02f, trap.ScriptedCooldown);
            }
            var page = AvailableActionPage(count);
            if (page == null || !page.Object.HasStateAuthority) return null;
            if (trap != null)
            {
                ulong template = SharedEnemiesRuntime.RegisterTemplate(trap.ScriptedEnemyPrefab);
                for (int i = 0; i < count; i++)
                {
                    ulong id = SharedAmbushCatalog.GuardId(node.Id, i);
                    if (TryEnemy(id, out _)) throw new InvalidOperationException("Ambush reservation already belongs to another action page");
                    page.Enemies.Add(id, new SharedEnemyRecord { PendingSpawn = true, ActionId = node.Id, Template = template,
                        MaxHP = SharedAmbushCatalog.InitialHealth(trap.ScriptedEnemyPrefab, trap.ScriptedLevel),
                        Level = trap.ScriptedLevel, Position = trap.ScriptedSpawnPosition.position + LobbyNetworkAvatar.Origin,
                        Rotation = trap.ScriptedEnemyPrefab.transform.rotation, HomeZ = node.HomeZ, Prunable = true });
                    if (node.Region >= 0) SharedRunContext.Membership?.BindAction(node.Region, id, true);
                }
            }
            page.Actions.Add(node.Id, value); actionLocations[node.Id] = page;
            return page;
        }
        internal bool CommitWorldAction(SharedWorldActionsRuntime.Node node, int command, float delay, PlayerRef actor)
        {
            if (!WorldActionsReady() || node == null || node.Component == null || command < 1 || command > 2 ||
                float.IsNaN(delay) || float.IsInfinity(delay)) return false;
            int bit = command == 1 ? 1 : 2;
            if (node.Kind != WorldActionKind.Zone && node.Kind != WorldActionKind.Trap && command != 1) return false;
            var page = ActionPage(node.Id);
            SharedWorldActionRecord value;
            if (page == null) page = CreateWorldAction(node, out value);
            else value = page.Actions[node.Id];
            if (page == null || !page.Object.HasStateAuthority || (value.Flags & bit) != 0) return false;
            value.Flags |= bit;
            if (node.Kind == WorldActionKind.Trap && command == 2)
                value.Due = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0, ((DungeonTrap)node.Component).ScriptedFirstDelay));
            if (node.Kind == WorldActionKind.Portal) value.Due = TickTimer.CreateFromSeconds(Runner, Mathf.Clamp(delay, 0, 60));
            page.Actions.Set(node.Id, value);
            SharedWorldActionsRuntime.ApplyAccepted(node, value, this, actor, false);
            if (node.Kind == WorldActionKind.Portal && ((Portal)node.Component).NextPortal != null)
            {
                var next = SharedWorldActionsRuntime.FindNode(((Portal)node.Component).NextPortal);
                if (next != null) CommitWorldAction(next, 1, delay, actor);
            }
            return true;
        }
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_WorldAction(ulong id, int command, RpcInfo info = default)
        {
            if (!WorldActionsReady() || !CanAct(info.Source) || !SharedWorldActionsRuntime.TryNode(id, out var node) ||
                !ActionActorNear(info.Source, node.Component) || node.Kind == WorldActionKind.Portal || node.Kind == WorldActionKind.Podium) return;
            CommitWorldAction(node, command, 0, info.Source);
        }
        // A marker on the consumed item repairs a podium header if different pages
        // were observed at different times during master transfer. It never consumes a second skull.
        internal void RepairWorldOffering(SharedWorldActionsRuntime.Node node)
        {
            if (node.Kind != WorldActionKind.Podium || !WorldActionsReady()) return;
            int mask = 0, lastSlot = 0; ulong last = 0;
            foreach (var entry in Items)
                if (entry.Value.ConsumedByAction == node.Id && entry.Value.ActionSlot >= 0 && entry.Value.ActionSlot < 32)
                { mask |= 1 << entry.Value.ActionSlot; last = entry.Key; lastSlot = entry.Value.ActionSlot; }
            var page = ActionPage(node.Id);
            if (mask == 0 && page == null) return;
            SharedWorldActionRecord value;
            if (page == null) page = CreateWorldAction(node, out value);
            else value = page.Actions[node.Id];
            if (page == null) return;
            bool changed = (value.Flags | mask) != value.Flags;
            value.Flags |= mask;
            if (last != 0) { value.ConsumedItem = last; value.LastSlot = lastSlot; }
            page.Actions.Set(node.Id, value);
            if (value.ConsumedItem != 0 && Items.TryGet(value.ConsumedItem, out var item) && item.Mode != 4)
            { item.Mode = 4; item.Owner = PlayerRef.None; Items.Set(value.ConsumedItem, item); }
            if (changed) SharedWorldActionsRuntime.ApplyAccepted(node, value, this, Object.StateAuthority, true);
            SharedWorldActionsRuntime.RepairChildren(node, value.Flags, this);
        }
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_WorldOffering(ulong id, ulong itemId, RpcInfo info = default)
        {
            if (!WorldActionsReady() || !CanAct(info.Source) || !SharedWorldActionsRuntime.TryNode(id, out var node) ||
                !(node.Component is CheckItemOnLocation podium) || podium.RequiredItems.Count > 32 || !Items.TryGet(itemId, out var item) ||
                item.Mode == 2 || item.Mode == 4 || (item.Owner != info.Source &&
                !(item.Owner == PlayerRef.None && info.Source == Object.StateAuthority))) return;
            Vector3 position = item.Mode == 3 && BoardController.Instance != null
                ? BoardController.Instance.transform.TransformPoint(item.BoatLocalPosition) + LobbyNetworkAvatar.Origin : item.Position;
            if (Vector3.Distance(position, node.LogicalPosition) > 8 ||
                (item.Owner != PlayerRef.None && !ActionActorNear(info.Source, podium))) return;
            var prefab = SharedItemCatalog.Load()?.Find(item.Template);
            if (prefab == null || prefab.Data == null) return;
            RepairWorldOffering(node);
            var page = ActionPage(id); SharedWorldActionRecord value;
            if (page == null) page = CreateWorldAction(node, out value); else value = page.Actions[id];
            if (page == null) return;
            int slot = -1;
            for (int i = 0; i < podium.RequiredItems.Count; i++)
                if ((value.Flags & (1 << i)) == 0 && podium.RequiredItems[i] != null && podium.RequiredItems[i].Name == prefab.Data.Name)
                { slot = i; break; }
            if (slot < 0) return;
            item.Mode = 4; item.Owner = PlayerRef.None; item.ConsumedByAction = id; item.ActionSlot = slot;
            Items.Set(itemId, item);
            value.Flags |= 1 << slot; value.ConsumedItem = itemId; value.LastSlot = slot; page.Actions.Set(id, value);
            SharedWorldActionsRuntime.ApplyAccepted(node, value, this, info.Source, false);
        }
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_WorldPortal(ulong id, RpcInfo info = default)
        {
            int slot = CrewSlot(info.Source);
            if (!WorldActionsReady() || !CanAct(info.Source) || slot < 0 ||
                !PortalCooldowns[slot].ExpiredOrNotRunning(Runner) || !SharedWorldActionsRuntime.TryNode(id, out var node) ||
                !(node.Component is Portal portal) || portal.NextPortal == null || !ActionActorNear(info.Source, portal) ||
                !TryWorldAction(id, out var value) || (value.Flags & 1) == 0 || !value.Due.ExpiredOrNotRunning(Runner)) return;
            PortalCooldowns.Set(slot, TickTimer.CreateFromSeconds(Runner, 1));
            RPC_WorldPortalConfirmed(info.Source, portal.NextPortal.transform.position + LobbyNetworkAvatar.Origin);
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_WorldPortalConfirmed(PlayerRef actor, Vector3 logicalDestination)
        {
            if (Runner.LocalPlayer == actor && SharedRunContext.State == this && PlayerMovement.Instance != null)
                PlayerMovement.Instance.Teleport(logicalDestination - LobbyNetworkAvatar.Origin);
        }
        internal void TickWorldAmbushes()
        {
            if (!WorldActionsReady()) return;
            foreach (var page in SharedWorldPage.All(Runner))
            foreach (var entry in page.Actions)
            {
                var action = entry.Value;
                if (action.Kind != (int)WorldActionKind.Trap || (action.Flags & 2) == 0 || action.Spawned >= action.Total ||
                    !action.Due.ExpiredOrNotRunning(Runner)) continue;
                ulong id = SharedAmbushCatalog.GuardId(entry.Key, action.Spawned);
                if (!page.Enemies.TryGet(id, out var enemy)) continue; // no new ID/actor on incomplete state
                if (enemy.PendingSpawn)
                {
                    if (!SharedEnemiesRuntime.TryTemplate(enemy.Template, out var prefab) || SharedItemsRuntime.TryFind(id, out _)) continue;
                    var actor = Instantiate(prefab, enemy.Position - LobbyNetworkAvatar.Origin, enemy.Rotation);
                    actor.InitializeLevel(enemy.Level);
                    var identity = actor.gameObject.AddComponent<WorldSpawnIdentity>();
                    identity.InitializeRemote(id, enemy.Template);
                    if (action.Region >= 0) SharedRunContext.Membership?.BindAction(action.Region, id, true);
                    SampleEnemy(identity, actor);
                    if (!page.Enemies.TryGet(id, out var published) || published.PendingSpawn || published.MaxHP <= 0)
                    { identity.RemovingReplica = true; actor.gameObject.SetActive(false); Destroy(actor.gameObject); continue; }
                }
                action.Spawned++;
                action.Due = action.Spawned < action.Total ? TickTimer.CreateFromSeconds(Runner, action.Interval) : default;
                page.Actions.Set(entry.Key, action); // wave cursor and released actor share one page snapshot
            }
        }
    }
}
