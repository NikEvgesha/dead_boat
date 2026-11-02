using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online
{
    // Explicit prototype transaction. Nothing schedules this from distance/Update.
    // Network records stay canonical and resident; this only removes scene replicas.
    public static class WorldRegionResidency
    {
        public sealed class Ticket
        {
            internal SharedDepartureState State;
            internal WorldRegionArchive Archive;
            internal LocationSpawner Owner;
            internal LocationLayoutStep Step;
            internal Vector3 Position, Scale;
            internal Quaternion Rotation;
            internal float ZPosition;
            internal int Level;
            internal bool Completed;
            internal readonly List<ulong> Items = new(), Enemies = new();
            internal readonly Dictionary<ulong, string> ItemNames = new();
        }

        public static bool TrySuspend(LocationContentSpawner root, int revision, out Ticket ticket, out string refusal)
        {
            ticket = null;
            refusal = null;
            if (!SharedRunContext.Playing || root == null || !root.gameObject.activeInHierarchy)
            { refusal = "no live location"; return false; }
            var owner = root.GetComponentInParent<LocationSpawner>();
            if (owner == null || !owner.OwnsReplica(root) || owner.LayoutManifest == null ||
                !int.TryParse(root.SharedGenerationKey.StartsWith("procedural:", StringComparison.Ordinal)
                    ? root.SharedGenerationKey.Substring(11) : null, out int index) ||
                !owner.LayoutManifest.TryGet(index, out var step) || step.Prefab == null)
            { refusal = "missing owned layout descriptor"; return false; }
            if (!WorldReplicaRemoval.CanRemoveLocalTree(root.gameObject, out refusal)) return false;
            // A moved actor still parented under the location can be near a crew
            // member. Refuse rather than hide it based solely on its home region.
            foreach (var item in root.GetComponentsInChildren<PickableItem>(true))
            {
                if (WorldReplicaRemoval.IsDormantCorpsePart(item)) continue;
                if (item.GetComponent<WorldSpawnIdentity>().PhysicsNearby)
                { refusal = "nearby item replica"; return false; }
            }
            var staged = new Ticket { State = SharedRunContext.State, Archive = SharedRunContext.Regions,
                Owner = owner, Step = step, Position = root.transform.position + LobbyNetworkAvatar.Origin,
                Rotation = root.transform.rotation, Scale = root.transform.localScale,
                ZPosition = root.ZPosition, Level = root.GenerationLevel };
            foreach (var identity in root.GetComponentsInChildren<WorldSpawnIdentity>(true))
            {
                if (identity.Item != null) { staged.Items.Add(identity.Id); staged.ItemNames.Add(identity.Id, WorldRegionRestore.SourceName(identity.Item.name)); }
                else if (identity.Enemy != null) staged.Enemies.Add(identity.Id);
                else { refusal = "unsupported region actor"; return false; }
            }
            if (!WorldRegionCapture.TryCapture(root, revision, out refusal)) return false;
            // Complete membership was captured above. Validate selective dormancy
            // before deletion; no frame/network callback can interleave this block.
            if (!staged.Archive.SetReplicaDormancy(index, staged.Items, staged.Enemies))
            { refusal = "invalid replica membership"; return false; }
            if (!WorldReplicaRemoval.TryRemoveLocalTree(root.gameObject, out refusal))
            { staged.Archive.SetDormant(index, false); return false; }
            owner.ForgetReplica(root);
            ticket = staged;
            return true;
        }

        public static bool TryRestore(Ticket ticket, out LocationContentSpawner root, out string refusal)
        {
            root = null;
            refusal = null;
            if (ticket == null || ticket.Completed || !SharedRunContext.Playing ||
                ticket.State != SharedRunContext.State || ticket.Archive != SharedRunContext.Regions ||
                ticket.Owner == null || ticket.Owner.LayoutManifest == null ||
                !ticket.Owner.LayoutManifest.TryGet(ticket.Step.Index, out var current) ||
                current.Prefab != ticket.Step.Prefab || current.LogicalZ != ticket.Step.LogicalZ ||
                ticket.Step.LogicalZ < SharedRunContext.State.WorldRear)
            { refusal = "expired region ticket"; return false; }
            if (!ticket.Archive.SetDormant(ticket.Step.Index, false))
            { refusal = "region archive retired"; return false; }
            var staging = new GameObject("Region restore staging");
            staging.SetActive(false);
            try
            {
                // Same descriptor, no pool/layout advancement, no new seed.
                root = Object.Instantiate(ticket.Step.Prefab, ticket.Position - LobbyNetworkAvatar.Origin,
                    ticket.Rotation, staging.transform);
                root.transform.localScale = ticket.Scale;
                root.ZPosition = ticket.ZPosition;
                root.SetLevel(ticket.Level);
                root.RestoreGeneration(ticket.Step.Index, ticket.Step.LogicalZ, ticket.ItemNames);
                root.gameObject.SetActive(false);
                root.transform.SetParent(ticket.Owner.transform, true);
                ticket.Owner.TrackReplica(root);
                root.gameObject.SetActive(true);
                ticket.Completed = true;
                return true;
            }
            catch (Exception exception)
            {
                refusal = "region restore refused: " + exception.Message;
                if (root != null)
                {
                    ticket.Owner.ForgetReplica(root);
                    // Partial construction is never a gameplay consumption/death.
                    foreach (var identity in root.GetComponentsInChildren<WorldSpawnIdentity>(true))
                    { identity.RemovingReplica = true; SharedItemsRuntime.Unregister(identity); }
                    root.gameObject.SetActive(false);
                    Object.Destroy(root.gameObject);
                    root = null;
                }
                ticket.Archive.SetReplicaDormancy(ticket.Step.Index, ticket.Items, ticket.Enemies);
                SharedRunContext.Membership?.Seal(ticket.Step.Index);
                return false;
            }
            finally { Object.Destroy(staging); }
        }
    }
}
