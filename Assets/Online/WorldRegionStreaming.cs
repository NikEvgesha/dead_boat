using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Online
{
    // Local procedural geometry only. Canonical network pages remain resident.
    public sealed class WorldRegionStreaming
    {
        private sealed class Sleeping
        {
            public WorldRegionResidency.Ticket Ticket;
            public Vector2 Range;
            public double RetryAfter;
        }
        private readonly LocationSpawner owner;
        private readonly Dictionary<int, Sleeping> sleeping = new();
        private readonly Dictionary<LocationContentSpawner, Vector2> bounds = new();
        private readonly List<float> focus = new(5);
        private readonly List<int> obsolete = new();
        private readonly float loadDistance, unloadDistance;
        private SharedDepartureState context;
        private WorldRegionArchive archive;
        private double nextAudit;
        private int cursor;
        public int SleepingCount => sleeping.Count;
        public int Suspended { get; private set; }
        public int Restored { get; private set; }
        public int Refused { get; private set; }
        public string LastRefusal { get; private set; }

        public WorldRegionStreaming(LocationSpawner owner, float loadDistance = 600, float unloadDistance = 800)
        {
            if (owner == null || !Finite(loadDistance) || !Finite(unloadDistance) || loadDistance < 0 || unloadDistance <= loadDistance)
                throw new ArgumentOutOfRangeException();
            this.owner = owner; this.loadDistance = loadDistance; this.unloadDistance = unloadDistance;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool Near(Vector2 range, IReadOnlyList<float> points, float distance)
        {
            // Missing/invalid interest information must never hide geometry.
            if (points == null || points.Count == 0 || !Finite(range.x) || !Finite(range.y) || range.y < range.x) return true;
            for (int i = 0; i < points.Count; i++)
                if (!Finite(points[i]) || points[i] >= range.x - distance && points[i] <= range.y + distance) return true;
            return false;
        }
        public void Forget(LocationContentSpawner root) => bounds.Remove(root);
        public void Tick(IReadOnlyList<LocationContentSpawner> active, bool allowSuspension = true)
        {
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || state == null || !state.BoatInitialized || Time.timeScale <= 0 ||
                Time.realtimeSinceStartupAsDouble < nextAudit) return;
            if (!CollectFocus(focus) || SharedRunContext.StreamingBudget?.TryAcquire(Time.frameCount, 0) != true) return;
            nextAudit = Time.realtimeSinceStartupAsDouble + .25;
            Audit(active, focus, state.WorldRear, allowSuspension);
        }
        public static bool CollectFocus(List<float> focus)
        {
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || state == null || !state.BoatInitialized) return false;
            focus.Clear(); focus.Add(state.BoatDistance);
            // Includes dead/ad-watching players. A live player without an avatar is
            // an incomplete snapshot: defer all suspensions/restorations this audit.
            foreach (var player in state.Runner.ActivePlayers)
            {
                if (!state.Includes(player)) continue;
                bool found = false;
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (avatar != null && avatar.Object != null && avatar.Object.IsValid &&
                        avatar.Runner == state.Runner && avatar.Object.StateAuthority == player)
                    { focus.Add(avatar.LogicalPosition.z); found = true; break; }
                if (!found) return false;
            }
            return true;
        }
        // Deterministic audit seam used by the real two-peer scene tests. At most
        // one transaction, restoring before considering another suspension.
        public bool Audit(IReadOnlyList<LocationContentSpawner> active, IReadOnlyList<float> points, float rear, bool allowSuspension = true)
        {
            var state = SharedRunContext.State;
            if (!SharedRunContext.Playing || state == null || SharedRunContext.Regions == null || active == null || points == null || points.Count == 0 || !Finite(rear)) return false;
            for (int i = 0; i < points.Count; i++) if (!Finite(points[i])) return false;
            if (context != state || archive != SharedRunContext.Regions)
            { sleeping.Clear(); bounds.Clear(); context = state; archive = SharedRunContext.Regions; }
            obsolete.Clear();
            foreach (var entry in sleeping)
                if (entry.Value.Ticket.Step.LogicalZ < rear) obsolete.Add(entry.Key);
            foreach (int index in obsolete) sleeping.Remove(index); // permanent rear retirement, never restore it
            foreach (var entry in sleeping)
            {
                var region = entry.Value;
                if (Time.realtimeSinceStartupAsDouble < region.RetryAfter || allowSuspension &&
                    !Near(region.Range, points, loadDistance) && !LiveActorNear(region.Ticket, points, loadDistance)) continue;
                if (!WorldRegionResidency.TryRestore(region.Ticket, out _, out string refusal))
                { region.RetryAfter = Time.realtimeSinceStartupAsDouble + 1; Refuse(refusal); return false; }
                sleeping.Remove(entry.Key); Restored++; LastRefusal = null;
                return true;
            }
            if (!allowSuspension) return false;
            for (int i = 0; i < active.Count; i++)
            {
                int index = (cursor + i) % active.Count;
                var root = active[index];
                if (root == null || !owner.OwnsReplica(root) || !root.gameObject.activeInHierarchy || root.ZPosition < rear) continue;
                float anchor = (root.transform.position + LobbyNetworkAvatar.Origin).z;
                if (Near(new Vector2(anchor, anchor), points, unloadDistance)) continue;
                if (!bounds.TryGetValue(root, out var range))
                {
                    float z = (root.transform.position + LobbyNetworkAvatar.Origin).z;
                    range = new Vector2(z, z);
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer.GetComponentInParent<WorldSpawnIdentity>(true) != null) continue;
                        var box = renderer.bounds;
                        range.x = Mathf.Min(range.x, box.min.z + LobbyNetworkAvatar.Origin.z);
                        range.y = Mathf.Max(range.y, box.max.z + LobbyNetworkAvatar.Origin.z);
                    }
                    // Renderer-less roots are conservatively padded as well.
                    range += new Vector2(-100, 100);
                    bounds.Add(root, range);
                }
                if (Near(range, points, unloadDistance)) continue;
                bool protectedActor = false;
                foreach (var actor in root.GetComponentsInChildren<WorldSpawnIdentity>(true))
                {
                    float z = (actor.transform.position + LobbyNetworkAvatar.Origin).z;
                    if (Near(new Vector2(z, z), points, unloadDistance) ||
                        actor.Enemy != null && state.TryEnemy(actor.Id, out var enemy) && enemy.BurnRemaining > 0)
                    { protectedActor = true; break; }
                }
                if (protectedActor) continue;
                int revision = SharedRunContext.Regions.NextRevision(root.SharedGenerationKey);
                cursor = active.Count > 0 ? (index + 1) % active.Count : 0;
                if (!WorldRegionResidency.TrySuspend(root, revision, out var ticket, out string reason))
                { Refuse(reason); return false; }
                sleeping.Add(ticket.Step.Index, new Sleeping { Ticket = ticket, Range = range });
                bounds.Remove(root); Suspended++; LastRefusal = null;
                return true;
            }
            return false;
        }
        private bool LiveActorNear(WorldRegionResidency.Ticket ticket, IReadOnlyList<float> points, float distance)
        {
            foreach (ulong id in ticket.Items)
                if (context.Items.TryGet(id, out var item) && item.Mode == 0 && item.Owner == Fusion.PlayerRef.None &&
                    Near(new Vector2(item.Position.z, item.Position.z), points, distance)) return true;
            foreach (ulong id in ticket.Enemies)
                if (context.TryEnemy(id, out var enemy) && enemy.HP > 0 &&
                    Near(new Vector2(enemy.Position.z, enemy.Position.z), points, distance)) return true;
            return false;
        }
        private void Refuse(string message) { Refused++; LastRefusal = message; }
    }
}
