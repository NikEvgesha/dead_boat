using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public sealed partial class SharedDepartureState
    {
        [Networked] public float WorldFront { get; private set; }
        [Networked] public float WorldRear { get; private set; }
        private double nextWorldCleanup;
        private readonly List<ulong> obsolete = new();
        private readonly List<SharedWorldPage> emptyPages = new();

        private void TickWorld()
        {
            float front = BoatDistance, rear = BoatDistance;
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == Runner && Includes(avatar.Object.StateAuthority))
                {
                    front = Mathf.Max(front, avatar.transform.position.z);
                    rear = Mathf.Min(rear, avatar.transform.position.z);
                }
            WorldFront = Mathf.Max(WorldFront, front);
            // Keep a generous margin behind every crew member, including someone awaiting revival.
            WorldRear = Mathf.Max(WorldRear, rear - 1000);
            if (Time.realtimeSinceStartupAsDouble < nextWorldCleanup) return;
            nextWorldCleanup = Time.realtimeSinceStartupAsDouble + 1;
            emptyPages.Clear();
            foreach (var page in SharedWorldPage.All(Runner))
            {
                if (!page.Object.HasStateAuthority) continue;
                obsolete.Clear();
                foreach (var entry in page.Items)
                    if (entry.Value.Prunable && entry.Value.Owner == PlayerRef.None && entry.Value.Mode != 3 &&
                        entry.Value.HomeZ < WorldRear && entry.Value.Position.z < WorldRear)
                        obsolete.Add(entry.Key);
                foreach (var id in obsolete) page.Items.Remove(id);
                obsolete.Clear();
                foreach (var entry in page.Enemies)
                    if (entry.Value.Prunable && entry.Value.HomeZ < WorldRear && entry.Value.Position.z < WorldRear)
                        obsolete.Add(entry.Key);
                foreach (var id in obsolete) page.Enemies.Remove(id);
                if (page.Index != 0 && page.Items.Count == 0 && page.Enemies.Count == 0) emptyPages.Add(page);
            }
            foreach (var page in emptyPages) Runner.Despawn(page.Object);
        }
    }
}
