using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    public static class SharedRecoveryInventory
    {
        public static bool Applying { get; private set; }
        public static void Restore(SharedDepartureState state, SharedPersonalData data)
        {
            var items = new List<SharedRecoveryItem>();
            int slot = state.CrewSlot(state.Runner.LocalPlayer);
            var seen = new HashSet<ulong>();
            for (int i = 0; i < data.ItemCount; i++)
            {
                if (!state.PersonalItems.TryGet(slot * SharedDepartureState.RecoverySlots + i, out var item)) continue;
                if (item.WorldId != 0)
                {
                    if (!state.Items.TryGet(item.WorldId, out var current) || current.Owner != state.Runner.LocalPlayer ||
                        (current.Mode != 1 && current.Mode != 2)) continue;
                    item.Ammo = current.WeaponAmmo;
                    seen.Add(item.WorldId);
                }
                items.Add(item);
            }
            // Ownership RPCs may be newer than the last personal snapshot.
            foreach (var entry in state.Items)
                if (entry.Value.Owner == state.Runner.LocalPlayer && (entry.Value.Mode == 1 || entry.Value.Mode == 2) &&
                    seen.Add(entry.Key)) items.Add(new SharedRecoveryItem
                    { WorldId = entry.Key, Template = entry.Value.Template, Ammo = entry.Value.WeaponAmmo });
            Restore(items.ToArray(), true);
        }
        public static void Restore(SharedRecoveryItem[] items, bool networked)
        {
            Applying = true;
            try
            {
                Inventory.Instance.SetLoadedInventory(new List<string>());
                PickableItem active = null;
                foreach (var entry in items)
                {
                    var prefab = SharedItemCatalog.Load()?.Find(entry.Template);
                    if (prefab == null) continue;
                    PickableItem item;
                    if (networked && entry.WorldId != 0 && SharedItemsRuntime.TryFind(entry.WorldId, out var existing) && existing != null)
                        item = existing.Item;
                    else
                    {
                        item = Object.Instantiate(prefab);
                        if (networked && entry.WorldId != 0)
                        {
                            var identity = item.gameObject.AddComponent<WorldSpawnIdentity>();
                            identity.RecoveryReplica = true;
                            identity.InitializeRemote(entry.WorldId, 0);
                        }
                    }
                    item.gameObject.SetActive(true);
                    item.ApplySharedWorldState(0);
                    item.ApplySharedPhysics(true);
                    item.GetComponentInChildren<RangedWeaponController>(true)?.ApplySharedAmmo(entry.Ammo);
                    if (Inventory.Instance.AddItem(item))
                    {
                        if (entry.Equipped != 0) active = item;
                    }
                    else
                    {
                        item.transform.position = BoardController.Instance.transform.position + Vector3.up * 2;
                        if (networked && entry.WorldId != 0)
                            SharedRunContext.State.RPC_ItemState(entry.WorldId, 0,
                                item.transform.position + LobbyNetworkAvatar.Origin, item.transform.rotation, entry.Ammo);
                    }
                }
                if (active != null) Inventory.Instance.SetActiveItem(active);
            }
            finally { Applying = false; }
        }
    }
}
