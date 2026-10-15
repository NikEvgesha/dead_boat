using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedItemAmmoGameplayProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string ValidateInactiveHud()
        {
            var prefab = SharedItemCatalog.Load().entries.Select(e => e.prefab).First(p => p != null &&
                p.GetComponentInChildren<RangedWeaponController>(true) != null);
            var item = UnityEngine.Object.Instantiate(prefab);
            int calls = 0;
            Action<bool> handler = _ => calls++;
            AmmoUI.UseGun += handler;
            try
            {
                var gun = item.GetComponentInChildren<RangedWeaponController>(true);
                typeof(RangedWeaponController).GetField("_active", Private).SetValue(gun, false);
                typeof(RangedWeaponController).GetMethod("OnDisable", Private).Invoke(gun, null);
                Require(calls == 0, "inactive/remote weapon cannot hide local ammo HUD");
                return "PASS: disabling an inactive weapon does not touch the local ammo HUD";
            }
            finally { AmmoUI.UseGun -= handler; UnityEngine.Object.DestroyImmediate(item.gameObject); }
        }
        public static async Task Check(SharedDepartureState state, SharedDepartureState remote)
        {
            var inventory = Inventory.Instance;
            var prefab = SharedItemCatalog.Load().entries.Select(e => e.prefab).First(p => p != null &&
                p.GetComponentInChildren<RangedWeaponController>(true)?.GetWeaponType() == WeaponType.Pistol);
            var gunItem = UnityEngine.Object.Instantiate(prefab);
            var gun = gunItem.GetComponentInChildren<RangedWeaponController>(true);
            gun.ApplySharedAmmo(3);
            Require(inventory.AddItem(gunItem), "add personal pistol");
            int count = inventory.GetInventoryList().Count;
            Require(inventory.AddItem(gunItem) && inventory.GetInventoryList().Count == count, "duplicate insertion is idempotent");
            await Until(() => gunItem.GetComponent<WorldSpawnIdentity>() != null &&
                state.Items.TryGet(gunItem.GetComponent<WorldSpawnIdentity>().Id, out var r) && r.Owner == state.Runner.LocalPlayer && r.Mode == 2,
                "personal pistol registered before recovery snapshot");
            ulong id = gunItem.GetComponent<WorldSpawnIdentity>().Id;
            await Until(() => state.Personal.TryGet(state.Runner.LocalPlayer, out var p) && p.ItemCount == count &&
                Enumerable.Range(0, p.ItemCount).Any(i => state.PersonalItems[state.CrewSlot(state.Runner.LocalPlayer) * 32 + i].WorldId == id), "confirmed pistol snapshot");
            var oldData = state.Personal[state.Runner.LocalPlayer];
            int offset = state.CrewSlot(state.Runner.LocalPlayer) * 32;
            var oldBag = Enumerable.Range(0, oldData.ItemCount).Select(i => state.PersonalItems[offset + i]).ToArray();
            PlayerAmmoManager.Instance.RestoreRecovery(31, 17, 9);
            var slot = InventoryUI.Instance.GetComponentsInChildren<InventorySlot>(true).First(s => s.CurrentItem == gunItem);
            var drop = new GameObject("Ammo handoff drop point");
            drop.transform.position = PlayerMovement.Instance.transform.position + Vector3.forward + Vector3.up;
            try
            {
                inventory.DropOutItem(gunItem, slot, drop.transform);
                await Until(() => state.Items[id].Mode == 0, "drop releases owner");
                remote.RPC_ClaimItem(id);
                await Until(() => state.Items[id].Owner == remote.Runner.LocalPlayer, "other player claims pistol");
                remote.RPC_ItemState(id, 1, state.Items[id].Position, Quaternion.identity, 1);
                await Until(() => state.Items[id].WeaponAmmo == 1, "receiver magazine update");
                // Replay the real previous confirmed bag while the gun belongs to someone else.
                for (int i = 0; i < oldBag.Length; i++) state.PersonalItems.Set(offset + i, oldBag[i]);
                SharedRecoveryInventory.Restore(state, oldData);
                Require(!inventory.GetInventoryList().Any(p => p.GetComponent<WorldSpawnIdentity>()?.Id == id), "stale bag cannot resurrect transferred personal weapon");
                await Task.Delay(200);
                Require(oldBag.Where(e => e.WorldId != id).All(e => state.Items[e.WorldId].Mode != 4), "clearing restored inventory does not consume live records");
                remote.RPC_ItemState(id, 0, state.Items[id].Position, Quaternion.identity, 1);
                await Until(() => state.Items[id].Mode == 0, "receiver drops pistol back");
                Require(SharedItemsRuntime.TryFind(id, out var identity) && identity != null, "pistol proxy exists");
                PlayerMovement.Instance.transform.position = identity.transform.position + Vector3.up;
                await Task.Delay(300);
                inventory.AddItem(identity.Item);
                await Until(() => inventory.GetInventoryList().Any(p => p.GetComponent<WorldSpawnIdentity>()?.Id == id), "reclaim pistol");
                gunItem = inventory.GetInventoryList().First(p => p.GetComponent<WorldSpawnIdentity>()?.Id == id);
                gun = gunItem.GetComponentInChildren<RangedWeaponController>(true);
                Require(gun.CurrentAmmo == 1 && PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol) == 31, "magazine follows gun, reserve stays with player");
                await Until(() => state.Items[id].Mode == 2, "reclaimed inventory state");
                var bag = inventory.GetInventoryList().Select(p => new SharedRecoveryItem { WorldId = p.GetComponent<WorldSpawnIdentity>().Id,
                    Template = SharedItemCatalog.Load().Identify(p), Ammo = p.GetComponentInChildren<RangedWeaponController>(true)?.CurrentAmmo ?? -1 }).ToArray();
                SharedRecoveryInventory.Restore(bag.Concat(bag.Where(e => e.WorldId == id)).ToArray(), true);
                await Task.Delay(250);
                Require(state.Items[id].Mode != 4 && inventory.GetInventoryList().Count(p => p.GetComponent<WorldSpawnIdentity>()?.Id == id) == 1,
                    "duplicate recovery row yields one live pistol");
                gunItem = inventory.GetInventoryList().First(p => p.GetComponent<WorldSpawnIdentity>()?.Id == id);
                gun = gunItem.GetComponentInChildren<RangedWeaponController>(true);
                var input = typeof(PlayerInput).GetField("_reload", Private);
                input.SetValue(PlayerInput.Instance, true);
                try
                {
                    typeof(RangedWeaponController).GetMethod("Update", Private).Invoke(gun, null);
                    Require(!(bool)typeof(RangedWeaponController).GetField("_isReloading", Private).GetValue(gun), "inactive gun ignores reload key");
                }
                finally { input.SetValue(PlayerInput.Instance, false); }
                inventory.SetActiveItem(gunItem);
                gun.StartCoroutine((IEnumerator)typeof(RangedWeaponController).GetMethod("Reload", Private).Invoke(gun, null));
                await Until(() => !(bool)typeof(RangedWeaponController).GetField("_isReloading", Private).GetValue(gun), "active reload complete");
                int magazine = gun.CurrentAmmo;
                Require(PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol) == 31 - (magazine - 1), "reload conserves magazine plus reserve");
                int reserve = gun.ReserveAmmo;
                gun.ReserveAmmo += 5;
                Require(gun.ReserveAmmo == reserve + 5, "positive reserve setter adds ammo");
                int listeners = PlayerAmmoManager.Instance.NewAmmo?.GetInvocationList().Length ?? 0;
                gun.enabled = false;
                Require((PlayerAmmoManager.Instance.NewAmmo?.GetInvocationList().Length ?? 0) == listeners - 1, "disabled weapon detaches reserve listener");
                var ammoPrefab = SharedItemCatalog.Load().entries.Select(e => e.prefab).First(p => p != null && p.GetComponent<AmmoItem>() != null);
                var box = UnityEngine.Object.Instantiate(ammoPrefab, PlayerMovement.Instance.transform.position + Vector3.forward, Quaternion.identity);
                int amount = new SerializedObject(box.GetComponent<AmmoItem>()).FindProperty("AmmoCount").intValue;
                var type = (WeaponType)new SerializedObject(box.GetComponent<AmmoItem>()).FindProperty("AmmoType").enumValueIndex;
                var ammoIdentity = box.gameObject.AddComponent<WorldSpawnIdentity>(); ammoIdentity.ItemTemplate = SharedItemCatalog.Load().Identify(box); ammoIdentity.Key = "probe:ammo-once";
                await Until(() => state.Items.ContainsKey(ammoIdentity.Id), "ammo record registered");
                state.RPC_ClaimItem(ammoIdentity.Id);
                await Until(() => state.Items[ammoIdentity.Id].Owner == state.Runner.LocalPlayer, "ammo claim");
                int before = PlayerAmmoManager.Instance.GetAmmo(type);
                Require(inventory.AddItem(box) && !inventory.AddItem(box), "ammo box cannot grant twice during animation");
                Require(PlayerAmmoManager.Instance.GetAmmo(type) == before + amount && state.Items[ammoIdentity.Id].Mode == 4, "ammo grant consumes network box immediately");
                ulong boxId = ammoIdentity.Id;
                remote.RPC_ClaimItem(boxId);
                await Task.Delay(200);
                Require(state.Items[boxId].Mode == 4, "other player cannot claim consumed ammo box");
                Debug.Log("[Item ammo gameplay probe] PASS: personal ID, idempotent bag, transfer magazine/local reserve, stale snapshot rejection, restoration without consumption/duplicates, inactive reload, reserve accounting/listener, one-shot ammo pickup.");
            }
            finally { UnityEngine.Object.Destroy(drop); }
        }
        private static async Task Until(Func<bool> condition, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 45;
            while (!condition()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + label); await Task.Delay(50); }
        }
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    }
}
