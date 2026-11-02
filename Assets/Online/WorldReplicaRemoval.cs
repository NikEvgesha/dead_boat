using UnityEngine;

namespace DeadBoat.Online
{
    // Caller MUST archive mutations before using this. No automatic streaming caller yet.
    // Destruction of a scene replica is not consumption/death in the canonical world.
    public static class WorldReplicaRemoval
    {
        public static bool TryRemoveLocalTree(GameObject root, out string refusal)
        {
            refusal = null;
            if (root == null) { refusal = "missing root"; return false; }
            if ((!SharedRunContext.Playing && !SoloRunBranch.Active) ||
                (SharedRunContext.Active && !SharedRunContext.Playing))
            { refusal = "no live world"; return false; }
            if (root.GetComponentInChildren<Fusion.NetworkObject>(true) != null ||
                root.GetComponentInChildren<PlayerMovement>(true) != null ||
                root.GetComponentInChildren<BoardController>(true) != null)
            { refusal = "persistent or network root"; return false; }
            var identities = root.GetComponentsInChildren<WorldSpawnIdentity>(true);
            // Validate the WHOLE tree before marking anything, including inactive descendants.
            foreach (var item in root.GetComponentsInChildren<PickableItem>(true))
            {
                if (item.Grabbed || item.Attached || item.Status == ItemStatus.InInventory)
                { refusal = "portable item"; return false; }
                if (item.GetComponent<WorldSpawnIdentity>() == null)
                { refusal = "untracked item"; return false; }
            }
            foreach (var enemy in root.GetComponentsInChildren<EnemyCore>(true))
                if (enemy.GetComponent<WorldSpawnIdentity>() == null)
                { refusal = "untracked enemy"; return false; }
            foreach (var identity in identities)
            {
                if (identity.Id == 0 || !SharedItemsRuntime.TryFind(identity.Id, out var registered) || registered != identity)
                { refusal = "unregistered identity"; return false; }
                if (SharedItemsRuntime.HasPendingRequest(identity.Id))
                { refusal = "pending interaction"; return false; }
                var state = SharedRunContext.State;
                if (SharedRunContext.Playing && state.Items.TryGet(identity.Id, out var record) &&
                    (record.Owner != Fusion.PlayerRef.None || record.Mode == 1 || record.Mode == 2 || record.Mode == 3))
                { refusal = "owned or attached record"; return false; }
            }
            foreach (var identity in identities) identity.RemovingReplica = true;
            foreach (var identity in identities) SharedItemsRuntime.Unregister(identity);
            root.SetActive(false);
            Object.Destroy(root); // instance flags survive Unity's deferred OnDestroy callbacks
            return true;
        }
    }
}
