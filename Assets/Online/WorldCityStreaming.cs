using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed class WorldCityStreaming
    {
        private sealed class Ticket
        {
            public WorldCityRegion Source;
            public GameObject Prefab;
            public Vector3 Position, Scale;
            public Quaternion Rotation;
            public int Index;
            public float Z;
            public Vector2 Range;
            public double Retry;
            public readonly List<ulong> Items = new(), Enemies = new();
        }
        private readonly CitySpawner owner;
        private readonly List<Ticket> sleeping = new();
        private readonly List<float> focus = new(5);
        private double next;
        private WorldRegionArchive archive;
        public int SleepingCount => sleeping.Count;
        public int Suspended { get; private set; }
        public int Restored { get; private set; }
        public string LastRefusal { get; private set; }
        public WorldCityStreaming(CitySpawner owner) { this.owner = owner; }
        public void Tick(List<GameObject> active, bool enabled)
        {
            if (Time.timeScale <= 0 || Time.realtimeSinceStartupAsDouble < next || !WorldRegionStreaming.CollectFocus(focus)) return;
            next = Time.realtimeSinceStartupAsDouble + .25;
            Audit(active, focus, SharedRunContext.State.WorldRear, enabled);
        }
        public bool Audit(List<GameObject> active, IReadOnlyList<float> points, float rear, bool enabled = true)
        {
            if (!SharedRunContext.Playing || points == null || points.Count == 0) return false;
            if (archive != SharedRunContext.Regions) { sleeping.Clear(); archive = SharedRunContext.Regions; }
            for (int i = 0; i < points.Count; i++) if (float.IsNaN(points[i]) || float.IsInfinity(points[i])) return false;
            sleeping.RemoveAll(t => t.Z < rear);
            foreach (var ticket in sleeping)
            {
                if (Time.realtimeSinceStartupAsDouble < ticket.Retry || enabled && !WorldRegionStreaming.Near(ticket.Range, points, 600)) continue;
                if (!Restore(ticket, active)) { ticket.Retry = Time.realtimeSinceStartupAsDouble + 1; return false; }
                sleeping.Remove(ticket); Restored++; return true;
            }
            if (!enabled) return false;
            foreach (var city in active)
            {
                var region = city != null ? city.GetComponent<WorldCityRegion>() : null;
                if (region == null || region.Prefab == null || !region.Ready || region.LogicalZ < rear) continue;
                var range = new Vector2(region.LogicalZ - 100, region.LogicalZ + 100);
                foreach (var renderer in city.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.GetComponentInParent<WorldSpawnIdentity>(true) != null) continue;
                    range.x = Mathf.Min(range.x, renderer.bounds.min.z + LobbyNetworkAvatar.Origin.z - 100);
                    range.y = Mathf.Max(range.y, renderer.bounds.max.z + LobbyNetworkAvatar.Origin.z + 100);
                }
                if (WorldRegionStreaming.Near(range, points, 800)) continue;
                foreach (var shop in city.GetComponentsInChildren<StorePoint>(true)) if (!shop.CanSuspend) return Refuse("purchase in progress");
                foreach (var bank in city.GetComponentsInChildren<BankStorePoint>(true)) if (!bank.CanSuspend) return Refuse("bank in progress");
                if (!WorldReplicaRemoval.CanRemoveLocalTree(city, out var reason)) return Refuse(reason);
                foreach (var actor in city.GetComponentsInChildren<WorldSpawnIdentity>(true))
                {
                    float z = (actor.transform.position + LobbyNetworkAvatar.Origin).z;
                    if (WorldRegionStreaming.Near(new Vector2(z, z), points, 800) || actor.Item != null && actor.PhysicsNearby ||
                        actor.Enemy != null && SharedRunContext.State.TryEnemy(actor.Id, out var enemy) && enemy.BurnRemaining > 0) return false;
                }
                var t = new Ticket { Source = region, Prefab = region.Prefab, Index = region.Index, Z = region.LogicalZ,
                    Position = city.transform.position + LobbyNetworkAvatar.Origin, Rotation = city.transform.rotation,
                    Scale = city.transform.localScale, Range = range };
                foreach (var actor in city.GetComponentsInChildren<WorldSpawnIdentity>(true))
                    if (actor.Item != null) t.Items.Add(actor.Id); else if (actor.Enemy != null) t.Enemies.Add(actor.Id);
                if (!WorldRegionCapture.TryCaptureTree(city, region.Index, SharedRunContext.Regions.NextRevision(region.Index), out reason)) return Refuse(reason);
                if (!SharedRunContext.Regions.SetReplicaDormancy(t.Index, t.Items, t.Enemies)) return Refuse("city dormancy refused");
                if (!WorldReplicaRemoval.TryRemoveLocalTree(city, out reason))
                { SharedRunContext.Regions.SetDormant(t.Index, false); return Refuse(reason); }
                active.Remove(city); sleeping.Add(t); Suspended++; LastRefusal = null; return true;
            }
            return false;
        }
        private bool Restore(Ticket ticket, List<GameObject> active)
        {
            if (!SharedRunContext.Regions.SetDormant(ticket.Index, false)) return Refuse("city archive retired");
            var staging = new GameObject("City restore staging"); staging.SetActive(false);
            GameObject city = null;
            try
            {
                city = UnityEngine.Object.Instantiate(ticket.Prefab, ticket.Position - LobbyNetworkAvatar.Origin, ticket.Rotation, staging.transform);
                city.transform.localScale = ticket.Scale;
                var region = city.AddComponent<WorldCityRegion>();
                region.Initialize(owner, ticket.Prefab, ticket.Z, true);
                city.SetActive(false); city.transform.SetParent(owner.transform, true); active.Add(city); city.SetActive(true);
                LastRefusal = null; return true;
            }
            catch (Exception e)
            {
                if (city != null)
                {
                    foreach (var identity in city.GetComponentsInChildren<WorldSpawnIdentity>(true))
                    { identity.RemovingReplica = true; SharedItemsRuntime.Unregister(identity); }
                    city.SetActive(false); UnityEngine.Object.Destroy(city);
                }
                SharedRunContext.Regions.SetReplicaDormancy(ticket.Index, ticket.Items, ticket.Enemies);
                SharedRunContext.Membership.Seal(ticket.Index);
                return Refuse("city restore: " + e.Message);
            }
            finally { UnityEngine.Object.Destroy(staging); }
        }
        private bool Refuse(string reason) { LastRefusal = reason; return false; }
    }
}
