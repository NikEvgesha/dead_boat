using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace DeadBoat.Online
{
    // Scene bindings are local presentation only. No scene scans in the steady-state loop.
    public sealed class SharedWorldActionsRuntime : MonoBehaviour
    {
        public sealed class Node
        {
            public Component Component;
            public ulong Id;
            public WorldActionKind Kind;
            public int Region;
            internal bool Observed, Applied;
            internal int Flags;
            internal bool PortalActive;
            internal bool RestoredScene;
            public Vector3 LogicalPosition => Component.transform.position + LobbyNetworkAvatar.Origin;
            public float HomeZ;
        }
        private sealed class SoloAction { public SoloWorldAction Value; public float Due; }
        private static SharedWorldActionsRuntime instance;
        private static SharedDepartureState cascade;
        private static PlayerRef cascadeActor;
        private static int applying;
        private readonly Dictionary<Component, Node> components = new();
        private readonly Dictionary<ulong, Node> nodes = new();
        private readonly Dictionary<(ulong, int), double> requests = new();
        private readonly Dictionary<ulong, SoloAction> solo = new();
        private readonly Dictionary<ulong, SharedWorldActionRecord> received = new();
        private readonly Dictionary<(ulong, int), double> pending = new();
        private readonly List<(ulong, int)> pendingSnapshot = new();
        private readonly List<Node> snapshot = new();
        private int revision, observedRevision = -1;
        private bool soloMode;
        private double nextTick;
        private float soloPortalUntil;
        private SharedDepartureState observedState;
        private int observedAuthority;
        private bool wasReady;
        public static bool IsApplying => applying > 0;
        public static void Begin(GameObject owner, SharedDepartureState state, SoloWorldAction[] saved = null, SoloWorldItem[] items = null)
        {
            End();
            instance = owner.AddComponent<SharedWorldActionsRuntime>();
            instance.soloMode = state == null;
            foreach (var record in saved ?? Array.Empty<SoloWorldAction>())
                if (record.Id != 0) instance.solo[record.Id] = new SoloAction { Value = record,
                    Due = record.Remaining >= 0 ? Time.time + record.Remaining : -1 };
            if (instance.soloMode)
            {
                foreach (var item in items ?? Array.Empty<SoloWorldItem>())
                    if (item.ConsumedByAction != 0 && item.ActionSlot >= 0 && item.ActionSlot < 32)
                    {
                        if (!instance.solo.TryGetValue(item.ConsumedByAction, out var accepted))
                            instance.solo[item.ConsumedByAction] = accepted = new SoloAction { Value = new SoloWorldAction
                                { Id = item.ConsumedByAction, Kind = (int)WorldActionKind.Podium, Position = item.Position, Region = -1 }, Due = -1 };
                        accepted.Value.Flags |= 1 << item.ActionSlot; accepted.Value.ConsumedItem = item.Id; accepted.Value.LastSlot = item.ActionSlot;
                    }
                foreach (var value in instance.solo.Values)
                    if (value.Value.Kind == (int)WorldActionKind.Podium && value.Value.ConsumedItem != 0)
                        SoloRunBranch.ConsumeActionItemId(value.Value.ConsumedItem, value.Value.Id, value.Value.LastSlot);
            }
        }
        public static void End()
        {
            var old = instance; instance = null; cascade = null; applying = 0;
            if (old != null) { old.enabled = false; old.components.Clear(); old.nodes.Clear(); old.solo.Clear(); Destroy(old); }
        }
        public static void Stop(GameObject owner) { if (instance != null && instance.gameObject == owner) End(); }
        private void OnDestroy() { if (instance == this) { instance = null; cascade = null; applying = 0; } }
        public static void Register(Component component) { FindNode(component); }
        public static Node FindNode(Component component)
        {
            if (instance == null || component == null) return null;
            if (instance.components.TryGetValue(component, out var found)) return found;
            WorldActionKind kind;
            if (component is ActivateZone) kind = WorldActionKind.Zone;
            else if (component is DungeonTrap) kind = WorldActionKind.Trap;
            else if (component is ActivateObject) kind = WorldActionKind.Switch;
            else if (component is Lever) kind = WorldActionKind.Lever;
            else if (component is Portal) kind = WorldActionKind.Portal;
            else if (component is CheckItemOnLocation) kind = WorldActionKind.Podium;
            else return null;
            int ordinal = 0;
            foreach (var sibling in component.GetComponents(component.GetType()))
            { if (sibling == component) break; ordinal++; }
            string path = WorldSpawnIdentity.StablePath(component.transform);
            ulong id = WorldSpawnIdentity.IdForKey("action:" + ((int)kind).ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ":" + path + "@" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (instance.nodes.TryGetValue(id, out var other) && other.Component != null && other.Component != component)
                throw new InvalidOperationException("Duplicate live world action ID: " + path);
            int region = -1;
            var content = component.GetComponentInParent<LocationContentSpawner>(true);
            string origin = content != null ? content.SharedGenerationKey : component.GetComponentInParent<WorldGenerationAnchor>(true)?.Key;
            if (origin != null && origin.StartsWith("procedural:", StringComparison.Ordinal))
            { if (!int.TryParse(origin.Substring(11), out region) || region < 0) region = -1; }
            else if (!WorldCityRegion.TryIndex(origin, out region)) region = -1;
            var city = component.GetComponentInParent<WorldCityRegion>(true);
            var node = new Node { Id = id, Kind = kind, Region = region, Component = component,
                HomeZ = city != null ? city.LogicalZ : (content != null ? content.transform.position.z : component.transform.position.z) + LobbyNetworkAvatar.Origin.z };
            instance.components[component] = node; instance.nodes[id] = node; instance.revision++;
            if (component is DungeonTrap trap && trap.ScriptedEnemyPrefab != null) SharedEnemiesRuntime.RegisterTemplate(trap.ScriptedEnemyPrefab);
            return node;
        }
        public static bool TryNode(ulong id, out Node node)
        { node = null; return instance != null && instance.nodes.TryGetValue(id, out node) && node.Component != null; }
        public static void Remove(Component component)
        {
            if (instance == null || !instance.components.TryGetValue(component, out var node)) return;
            instance.components.Remove(component);
            if (instance.nodes.TryGetValue(node.Id, out var current) && current == node) instance.nodes.Remove(node.Id);
            instance.revision++;
        }
        private static bool Throttled(ulong id, int command)
        {
            var key = (id, command); double now = Time.realtimeSinceStartupAsDouble;
            if (instance.requests.TryGetValue(key, out double next) && now < next) return true;
            if (instance.requests.Count >= 4096) instance.requests.Clear();
            instance.requests[key] = now + .5; return false;
        }
        public static bool Request(Component component, int command, float delay = 0)
        {
            if (!SharedRunContext.GenerationActive) return false;
            var node = FindNode(component);
            if (node == null) return true;
            if (IsApplying)
            {
                if (cascade != null) cascade.CommitWorldAction(node, command, delay, cascadeActor);
                else if (instance.soloMode) instance.CommitSolo(node, command, delay);
                return true;
            }
            if (SharedItemInteraction.Blocked) return true;
            if (SharedRunContext.Active)
            {
                if (SharedRunContext.Playing)
                {
                    int bit = command == 1 ? 1 : 2;
                    if (SharedRunContext.State.TryWorldAction(node.Id, out var known) && (known.Flags & bit) != 0) return true;
                    var key = (node.Id, command);
                    if (instance.pending.Count < 128 && !instance.pending.ContainsKey(key))
                        instance.pending[key] = Time.realtimeSinceStartupAsDouble + 5;
                    if (!Throttled(node.Id, command)) SharedRunContext.State.RPC_WorldAction(node.Id, command);
                }
            }
            else if (SoloRunBranch.ReadyForWorldActions) instance.CommitSolo(node, command, delay);
            return true;
        }
        public static bool UseButton(UseButtonShow button, UnityEvent accepted)
        {
            if (!SharedRunContext.GenerationActive || accepted == null) return false;
            for (int i = 0; i < accepted.GetPersistentEventCount(); i++)
                if (accepted.GetPersistentTarget(i) is Lever lever)
                { Request(lever, 1); return true; }
            return false;
        }
        public static bool Offer(CheckItemOnLocation podium, PickableItem item)
        {
            if (!SharedRunContext.GenerationActive) return false;
            if (item == null || item.Status == ItemStatus.InInventory || SharedItemInteraction.Blocked) return true;
            var node = FindNode(podium);
            if (node == null || podium.RequiredItems.Count > 32) return true;
            var identity = item.GetComponent<WorldSpawnIdentity>();
            if (identity == null) { SharedItemsRuntime.PublishDrop(item); identity = item.GetComponent<WorldSpawnIdentity>(); }
            if (identity == null) return true;
            if (SharedRunContext.Active)
            {
                var state = SharedRunContext.State;
                if (!SharedRunContext.Playing || !state.Items.TryGet(identity.Id, out var value) || value.Mode == 4 || value.Mode == 2 ||
                    (value.Owner != state.Runner.LocalPlayer && !(value.Owner == PlayerRef.None && state.Object.HasStateAuthority)) ||
                    Throttled(identity.Id, 100)) return true;
                Vector3 position = item.transform.position + LobbyNetworkAvatar.Origin;
                if (value.Owner == state.Runner.LocalPlayer) state.RPC_ItemState(identity.Id, value.Mode, position, item.transform.rotation);
                else state.SampleFreeItem(identity.Id, position, item.transform.rotation);
                state.RPC_WorldOffering(node.Id, identity.Id);
            }
            else if (SoloRunBranch.ReadyForWorldActions)
            {
                var value = instance.GetSolo(node);
                for (int i = 0; i < podium.RequiredItems.Count; i++)
                {
                    if ((value.Value.Flags & (1 << i)) != 0 || podium.RequiredItems[i] == null || item.Data == null || podium.RequiredItems[i].Name != item.Data.Name) continue;
                    value.Value.Flags |= 1 << i; value.Value.ConsumedItem = identity.Id; value.Value.LastSlot = i;
                    SoloRunBranch.ConsumeWorldActionItem(identity, node.Id, i);
                    ApplySolo(node, value, false, item);
                    identity.RemovingReplica = true; Destroy(item.gameObject); break;
                }
            }
            return true;
        }
        public static bool Travel(Portal portal, PlayerMovement player)
        {
            if (!SharedRunContext.GenerationActive) return false;
            var node = FindNode(portal);
            if (node == null || player == null || player != PlayerMovement.Instance || SharedItemInteraction.Blocked) return true;
            if (SharedRunContext.Active)
            { if (SharedRunContext.Playing && !Throttled(node.Id, 200)) SharedRunContext.State.RPC_WorldPortal(node.Id); }
            else if (SoloRunBranch.ReadyForWorldActions && instance.solo.TryGetValue(node.Id, out var value) &&
                (value.Value.Flags & 1) != 0 && (value.Due < 0 || value.Due <= Time.time) && instance.soloPortalUntil <= Time.time && portal.NextPortal != null)
            { instance.soloPortalUntil = Time.time + 1; player.Teleport(portal.NextPortal.transform.position); }
            return true;
        }
        internal static void ApplyAccepted(Node node, SharedWorldActionRecord value, SharedDepartureState state, PlayerRef actor, bool restoring)
        {
            if (instance == null || node.Component == null) return;
            var before = cascade; var previousActor = cascadeActor;
            cascade = state; cascadeActor = actor; applying++;
            try
            {
                ApplyPresentation(node, value.Flags, restoring, value.ConsumedItem,
                    value.Due.ExpiredOrNotRunning(state.Runner));
            }
            finally { applying--; cascade = before; cascadeActor = previousActor; }
            node.Flags = value.Flags; node.Applied = true; node.Observed = true;
            node.RestoredScene |= restoring;
        }
        internal static void RepairChildren(Node node, int flags, SharedDepartureState state)
        {
            // Canonical dependencies are repaired independently of once-only
            // presentation callbacks, which a proxy may have applied pre-takeover.
            if (flags == 0) return;
            if (node.Component is Lever lever && (flags & 1) != 0 && lever._activeSubject != null)
                state.CommitWorldAction(FindNode(lever._activeSubject), 1, 0, state.Runner.LocalPlayer);
            if (node.Component is CheckItemOnLocation podium) RepairEvents(podium.PutItem, state);
            if (node.Component is Portal portal && (flags & 1) != 0 && portal.NextPortal != null)
            {
                var next = FindNode(portal.NextPortal);
                if (next != null && (!state.TryWorldAction(next.Id, out var other) || (other.Flags & 1) == 0))
                {
                    state.TryWorldAction(node.Id, out var current);
                    state.CommitWorldAction(next, 1, current.Due.RemainingTime(state.Runner) ?? 0, state.Runner.LocalPlayer);
                }
            }
            if (node.Component is ActivateZone zone)
            {
                if ((flags & 1) != 0) RepairEvents(zone.PlayerOnTriggerEnter, state);
                if ((flags & 2) != 0) RepairEvents(zone.PlayerOnTriggerExit, state);
            }
        }
        private static void RepairEvents(UnityEventBase events, SharedDepartureState state)
        {
            if (events == null) return;
            for (int i = 0; i < events.GetPersistentEventCount(); i++)
            {
                var target = events.GetPersistentTarget(i) as Component;
                string method = events.GetPersistentMethodName(i);
                int command = target is DungeonTrap && method == "_InTrap" ? 2 :
                    target is DungeonTrap && method == "_InDungeons" || target is Portal && method == "_ActivateTeleport" ||
                    target is ActivateObject && method == "Activate" || target is Lever && method == "Use" ? 1 : 0;
                if (command != 0) state.CommitWorldAction(FindNode(target), command, 0, state.Runner.LocalPlayer);
            }
        }
        private static void ApplyPresentation(Node node, int flags, bool restoring, ulong offered, bool due)
        {
            switch (node.Component)
            {
                case ActivateZone zone: zone.SharedApply(flags, restoring); break;
                case DungeonTrap trap: trap.SharedApply(flags, restoring); break;
                case ActivateObject subject: if ((flags & 1) != 0) subject.SharedApply(restoring); break;
                case Lever lever: if ((flags & 1) != 0) lever.SharedApply(restoring); break;
                case Portal portal:
                    bool active = (flags & 1) != 0 && due;
                    portal.SharedApply(active); node.PortalActive = active; break;
                case CheckItemOnLocation podium:
                    SharedItemsRuntime.TryFind(offered, out var identity);
                    podium.SharedApply(flags, identity != null ? identity.Item : null, restoring); break;
            }
        }
        private static void ApplyProxy(Node node, SharedWorldActionRecord value, bool restoring, SharedDepartureState state)
        {
            // Replayed author events may update ordinary GO/collider visuals. They
            // never enqueue fresh requests or start a local wave/portal coroutine.
            applying++;
            try { ApplyPresentation(node, value.Flags, restoring, value.ConsumedItem, value.Due.ExpiredOrNotRunning(state.Runner)); }
            finally { applying--; }
            node.Flags = value.Flags; node.Applied = true; node.Observed = true;
            node.RestoredScene |= restoring;
        }
        private void Update()
        {
            if (Time.realtimeSinceStartupAsDouble < nextTick) return;
            nextTick = Time.realtimeSinceStartupAsDouble + .1;
            if (observedRevision != revision)
            { snapshot.Clear(); snapshot.AddRange(nodes.Values); observedRevision = revision; }
            if (soloMode)
            {
                if (!SoloRunBranch.ReadyForWorldActions) return;
                TickSolo();
                for (int phase = 0; phase < 3; phase++) foreach (var node in snapshot)
                {
                    if (node.Component == null || Phase(node.Kind) != phase) continue;
                    if (!solo.TryGetValue(node.Id, out var value))
                    {
                        node.Observed = true;
                        if (node.Component is Portal initial && initial.InitiallyActive) CommitSolo(node, 1, 0);
                        continue;
                    }
                    bool due = value.Due < 0 || value.Due <= Time.time;
                    if (!node.Applied || node.Flags != value.Value.Flags || node.Kind == WorldActionKind.Portal &&
                        ((value.Value.Flags & 1) != 0 && due) != node.PortalActive)
                        ApplySolo(node, value, !node.Observed);
                }
                return;
            }
            if (!SharedRunContext.Playing) return;
            var state = SharedRunContext.State;
            RetryPending(state);
            bool authority = state.WorldActionsReady();
            bool roleChanged = state != observedState || state.Object.StateAuthority.RawEncoded != observedAuthority || authority && !wasReady;
            observedState = state; observedAuthority = state.Object.StateAuthority.RawEncoded;
            wasReady = authority;
            if (authority) state.TickWorldAmbushes();
            SharedRunContext.Membership?.ObserveActions(state);
            received.Clear();
            foreach (var page in SharedWorldPage.All(state.Runner))
                foreach (var entry in page.Actions) received[entry.Key] = entry.Value;
            bool structuralChange = false;
            for (int phase = 0; phase < 3; phase++) foreach (var node in snapshot)
            {
                if (node.Component == null || Phase(node.Kind) != phase) continue;
                if (authority && node.Kind == WorldActionKind.Podium && (!node.Observed || roleChanged)) state.RepairWorldOffering(node);
                if (!received.TryGetValue(node.Id, out var value))
                {
                    node.Observed = true;
                    if (authority && node.Component is Portal initial && initial.InitiallyActive) state.CommitWorldAction(node, 1, 0, state.Runner.LocalPlayer);
                    continue;
                }
                bool due = value.Due.ExpiredOrNotRunning(state.Runner);
                if (authority && (!node.Observed || roleChanged)) RepairChildren(node, value.Flags, state);
                if (!node.Applied || node.Flags != value.Flags || node.Kind == WorldActionKind.Portal && node.PortalActive != ((value.Flags & 1) != 0 && due))
                {
                    structuralChange |= node.Kind != WorldActionKind.Portal;
                    if (authority) ApplyAccepted(node, value, state, state.Runner.LocalPlayer, !node.Observed);
                    else ApplyProxy(node, value, !node.Observed, state);
                }
            }
            if (structuralChange)
                foreach (var node in snapshot)
                    if (node.Component != null && node.Kind == WorldActionKind.Switch && node.RestoredScene &&
                        received.TryGetValue(node.Id, out var final) && (final.Flags & 1) != 0)
                        ApplyProxy(node, final, true, state);
            MaterializeReleased(state, authority);
        }
        private static int Phase(WorldActionKind kind) => kind == WorldActionKind.Portal ? 2 :
            kind == WorldActionKind.Switch || kind == WorldActionKind.Lever ? 1 : 0;
        private void RetryPending(SharedDepartureState state)
        {
            if (pending.Count == 0) return;
            pendingSnapshot.Clear(); pendingSnapshot.AddRange(pending.Keys);
            foreach (var key in pendingSnapshot)
            {
                int bit = key.Item2 == 1 ? 1 : 2;
                if (SharedItemInteraction.Blocked || Time.realtimeSinceStartupAsDouble > pending[key] ||
                    !TryNode(key.Item1, out var node) || PlayerMovement.Instance == null ||
                    state.TryWorldAction(key.Item1, out var known) && (known.Flags & bit) != 0)
                { pending.Remove(key); continue; }
                var collider = node.Component.GetComponent<Collider>();
                Vector3 position = PlayerMovement.Instance.transform.position;
                float distance = collider != null && collider.enabled && collider.gameObject.activeInHierarchy
                    ? Vector3.Distance(position, collider.ClosestPoint(position)) : Vector3.Distance(position, node.Component.transform.position);
                if (distance > 8 || !node.Component.gameObject.activeInHierarchy) { pending.Remove(key); continue; }
                if (!Throttled(key.Item1, key.Item2)) state.RPC_WorldAction(key.Item1, key.Item2);
            }
        }
        private static void MaterializeReleased(SharedDepartureState state, bool authority)
        {
            // On takeover the authority may have no local copy of a living guard.
            // Ordinary proxies are also handled by SharedEnemiesRuntime.
            if (!authority) return;
            foreach (var page in SharedWorldPage.All(state.Runner)) foreach (var entry in page.Enemies)
            {
                var record = entry.Value;
                if (record.ActionId == 0 || record.PendingSpawn || record.HP <= 0 || SharedItemsRuntime.TryFind(entry.Key, out _) ||
                    !SharedEnemiesRuntime.TryTemplate(record.Template, out var prefab)) continue;
                bool nearby = false;
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (avatar.Runner == state.Runner && state.Includes(avatar.Object.StateAuthority) &&
                        Mathf.Abs(avatar.LogicalPosition.z - record.Position.z) <= 800) { nearby = true; break; }
                if (nearby) SharedEnemiesRuntime.TryCreateReplica(prefab, entry.Key, record, out _);
            }
        }
        private SoloAction GetSolo(Node node)
        {
            if (solo.TryGetValue(node.Id, out var found)) return found;
            found = new SoloAction { Value = new SoloWorldAction { Id = node.Id, Kind = (int)node.Kind,
                Region = node.Region, Position = node.LogicalPosition, HomeZ = node.HomeZ, Remaining = -1 }, Due = -1 };
            if (node.Component is DungeonTrap trap)
            {
                found.Value.Total = trap.ScriptedSpawnCount; found.Value.Interval = Mathf.Max(.02f, trap.ScriptedCooldown);
                ulong template = SharedEnemiesRuntime.RegisterTemplate(trap.ScriptedEnemyPrefab);
                for (int i = 0; i < found.Value.Total; i++)
                {
                    ulong id = SharedAmbushCatalog.GuardId(node.Id, i);
                    if (!SoloRunBranch.TryActionEnemy(id, out _)) SoloRunBranch.StoreActionEnemy(new SoloWorldEnemy
                    { Id = id, ActionId = node.Id, PendingSpawn = true, Template = template, Level = trap.ScriptedLevel,
                        MaxHP = SharedAmbushCatalog.InitialHealth(trap.ScriptedEnemyPrefab, trap.ScriptedLevel),
                        Position = trap.ScriptedSpawnPosition.position + LobbyNetworkAvatar.Origin,
                        Rotation = trap.ScriptedEnemyPrefab.transform.rotation, Prunable = true });
                }
            }
            solo.Add(node.Id, found); return found;
        }
        private void CommitSolo(Node node, int command, float delay)
        {
            if (command < 1 || command > 2 || node.Kind != WorldActionKind.Zone && node.Kind != WorldActionKind.Trap && command != 1) return;
            if (node.Component is DungeonTrap trap && (trap.ScriptedEnemyPrefab == null || trap.ScriptedSpawnPosition == null || trap.ScriptedSpawnCount > 64)) return;
            var value = GetSolo(node); int bit = command == 1 ? 1 : 2;
            if ((value.Value.Flags & bit) != 0) return;
            value.Value.Flags |= bit;
            if (node.Component is DungeonTrap dungeon && command == 2) value.Due = Time.time + dungeon.ScriptedFirstDelay;
            if (node.Component is Portal) value.Due = Time.time + Mathf.Clamp(delay, 0, 60);
            ApplySolo(node, value, false);
            if (node.Component is Portal portal && portal.NextPortal != null)
            { var next = FindNode(portal.NextPortal); if (next != null) CommitSolo(next, 1, delay); }
        }
        private static void ApplySolo(Node node, SoloAction value, bool restoring, PickableItem item = null)
        {
            applying++;
            try
            {
                if (node.Component is CheckItemOnLocation podium) podium.SharedApply(value.Value.Flags, item, restoring);
                else ApplyPresentation(node, value.Value.Flags, restoring, value.Value.ConsumedItem, value.Due < 0 || value.Due <= Time.time);
            }
            finally { applying--; }
            node.Flags = value.Value.Flags; node.Applied = true; node.Observed = true;
        }
        private void TickSolo()
        {
            if (Time.timeScale <= 0) return;
            foreach (var value in solo.Values)
            {
                var action = value.Value;
                if (action.Kind != (int)WorldActionKind.Trap || (action.Flags & 2) == 0 || action.Spawned >= action.Total || value.Due > Time.time) continue;
                ulong id = SharedAmbushCatalog.GuardId(action.Id, action.Spawned);
                if (!SoloRunBranch.TryActionEnemy(id, out var record)) continue;
                if (record.PendingSpawn)
                {
                    if (!SharedEnemiesRuntime.TryTemplate(record.Template, out var prefab) || SharedItemsRuntime.TryFind(id, out _)) continue;
                    var actor = Instantiate(prefab, record.Position - LobbyNetworkAvatar.Origin, record.Rotation);
                    actor.InitializeLevel(record.Level);
                    var identity = actor.gameObject.AddComponent<WorldSpawnIdentity>(); identity.InitializeRemote(id, record.Template);
                    record.PendingSpawn = false; record.HP = actor.SharedHP; record.MaxHP = actor.SharedMaxHP;
                    SoloRunBranch.StoreActionEnemy(record);
                }
                value.Value.Spawned++;
                value.Due = value.Value.Spawned < value.Value.Total ? Time.time + value.Value.Interval : -1;
            }
        }
        public static SoloWorldAction[] Capture(SharedDepartureState state)
        {
            var list = new List<SoloWorldAction>();
            foreach (var page in SharedWorldPage.All(state.Runner)) foreach (var pair in page.Actions)
            {
                var value = pair.Value;
                list.Add(new SoloWorldAction { Id = pair.Key, Kind = value.Kind, Flags = value.Flags, Spawned = value.Spawned,
                    Total = value.Total, Region = value.Region, Position = value.Position, HomeZ = value.HomeZ, Interval = value.Interval,
                    ConsumedItem = value.ConsumedItem, LastSlot = value.LastSlot, Remaining = value.Due.RemainingTime(state.Runner) ?? -1 });
            }
            return list.ToArray();
        }
        public static SoloWorldAction[] CaptureSolo()
        {
            if (instance == null || !instance.soloMode) return Array.Empty<SoloWorldAction>();
            var list = new List<SoloWorldAction>(instance.solo.Count);
            foreach (var pair in instance.solo)
            { var value = pair.Value.Value; value.Remaining = pair.Value.Due >= 0 ? Mathf.Max(0, pair.Value.Due - Time.time) : -1; list.Add(value); }
            return list.ToArray();
        }
    }
}
