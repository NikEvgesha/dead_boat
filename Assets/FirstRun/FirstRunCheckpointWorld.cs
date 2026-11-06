using System;
using System.Collections.Generic;
using System.Linq;
using DeadBoat.Online;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.FirstRun
{
    public sealed class FirstRunCheckpointWorld : MonoBehaviour
    {
        private readonly Dictionary<string, PickableItem> initial = new();
        private readonly Dictionary<string, StorePoint> stores = new();
        private readonly Dictionary<string, SellPoint> sales = new();
        private FirstRunAuthoredRoute route;
        public void Initialize(FirstRunAuthoredRoute owner)
        {
            route = owner;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var item in root.GetComponentsInChildren<PickableItem>(true))
                {
                    if (item.GetComponentInParent<EnemyCore>(true) || item.GetComponentInParent<StorePoint>(true)) continue;
                    string id = "initial:" + WorldSpawnIdentity.StableScenePath(item.transform);
                    item.gameObject.AddComponent<FirstRunTrackedItem>().Id = id;
                    initial.Add(id, item);
                }
                foreach (var store in root.GetComponentsInChildren<StorePoint>(true)) stores.Add(WorldSpawnIdentity.StableScenePath(store.transform), store);
                foreach (var sale in root.GetComponentsInChildren<SellPoint>(true)) sales.Add(WorldSpawnIdentity.StableScenePath(sale.transform), sale);
            }
        }
        private static FirstRunTrackedItem Track(PickableItem item)
        {
            var tracker = item.GetComponent<FirstRunTrackedItem>();
            if (tracker == null) { tracker = item.gameObject.AddComponent<FirstRunTrackedItem>(); tracker.Id = "drop:" + Guid.NewGuid().ToString("N"); }
            return tracker;
        }
        public static void MarkConsumed(PickableItem item)
        {
            if (FirstRunSession.Active && item != null) Track(item).Consumed = true;
        }
        public FirstRunCheckpoint Capture()
        {
            var board = BoardController.Instance;
            var experience = PlayerStatsManager.Instance.Experience();
            var c = new FirstRunCheckpoint {
                lesson = route.Lesson, intro = route.tutorial.CurrentStepIndex, choices = route.ChoicesMade,
                coins = CurrencyManager.Instance.Coins, exp = experience.Exp, level = experience.CurrentLevel,
                health = PlayerStatsManager.Instance.Health, distance = board.TotalDistanceTraveled, fuel = board.currentFuel,
                cards = LevelStatManager.Instance.Stats, player = PlayerMovement.Instance.transform.position,
                pistol = PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol), rifle = PlayerAmmoManager.Instance.GetAmmo(WeaponType.Rifle),
                shotgun = PlayerAmmoManager.Instance.GetAmmo(WeaponType.Shotgun), egg = route.EggCollected,
                ammo = route.AmmunitionCollected, shot = route.Fired,
                guards = route.guards.Select(g => g.IsDead ? 0 : g.SharedHP).ToArray(),
                bossHP = route.Dragon != null ? route.Dragon.SharedHP : -1
            };
            var bag = new HashSet<PickableItem>(Inventory.Instance.GetItems());
            var active = Inventory.Instance.GetComponent<ActiveItemManager>().Active;
            var seen = new HashSet<string>();
            foreach (var item in UnityEngine.Object.FindObjectsByType<PickableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool inBag = bag.Contains(item);
                if (item.GetComponent<FirstRunTrackedItem>()?.Consumed == true || item.GetComponent<FuelItem>()?.InFire == true) continue;
                if (!inBag && (item.gameObject.scene != gameObject.scene || !item.gameObject.activeInHierarchy ||
                    item.Status == ItemStatus.InInventory || item.GetComponentInParent<StorePoint>(true))) continue;
                var enemy = item.GetComponentInParent<EnemyCore>(true);
                if (enemy != null && !enemy.IsDead) continue;
                string id = Track(item).Id;
                if (!seen.Add(id)) continue;
                bool onBoat = item.transform.IsChildOf(board.transform);
                c.items.Add(new FirstRunItemData {
                    id = id, supply = item == route.EmergencyAmmunition ? 6 : Array.IndexOf(route.supplies, item), name = item.Data != null ? item.Data.Name : "", template = SharedItemCatalog.Load().Identify(item),
                    ammo = item.GetComponentInChildren<RangedWeaponController>(true)?.CurrentAmmo ?? -1,
                    bag = inBag, equipped = item == active, attached = item.Attached, boat = onBoat,
                    position = onBoat ? board.transform.InverseTransformPoint(item.transform.position) : item.transform.position,
                    rotation = onBoat ? Quaternion.Inverse(board.transform.rotation) * item.transform.rotation : item.transform.rotation
                });
            }
            foreach (var entry in initial) if (!seen.Contains(entry.Key)) c.removed.Add(entry.Key);
            foreach (var entry in stores)
                if (entry.Value.LastPurchasedItem != null) c.shops.Add(new FirstRunShopData { path = entry.Key, item = Track(entry.Value.LastPurchasedItem).Id });
            foreach (var entry in sales)
                if (entry.Value.CheckpointBag != null && !entry.Value.CheckpointBag.Collected) c.sales.Add(new FirstRunSaleData { path = entry.Key, coins = entry.Value.CheckpointBag.Money });
            return c;
        }
        public void Restore(FirstRunCheckpoint c)
        {
            LevelStatManager.Instance.RestoreRecovery(c.cards, 0);
            Inventory.Instance.SetLoadedInventory(new List<string>(), false);
            var live = new Dictionary<string, PickableItem>();
            foreach (var id in c.removed) if (initial.TryGetValue(id, out var gone) && gone != null) gone.gameObject.SetActive(false);
            foreach (var record in c.items)
            {
                if (!initial.TryGetValue(record.id, out var item) || item == null)
                {
                    var prefab = SharedItemCatalog.Load().Find(record.template);
                    if (prefab == null && !string.IsNullOrEmpty(record.name)) prefab = ItemsManager.Instance.GetItem(record.name);
                    if (prefab == null) throw new InvalidOperationException("Missing lesson item template: " + record.name);
                    item = Instantiate(prefab);
                    item.gameObject.AddComponent<FirstRunTrackedItem>().Id = record.id;
                }
                live.Add(record.id, item);
                route.BindCheckpointSupply(record.supply, item);
                item.gameObject.SetActive(true);
                item.ApplySharedWorldState(0);
                if (record.boat)
                {
                    item.transform.SetParent(BoardController.Instance.transform, false);
                    item.transform.localPosition = record.position; item.transform.localRotation = record.rotation;
                    item.ApplySharedWorldState(record.attached ? 3 : 0); item.ApplySharedPhysics(!record.attached);
                }
                else
                {
                    item.transform.position = record.position; item.transform.rotation = record.rotation;
                    item.ApplySharedPhysics(true);
                }
                if (record.bag) Inventory.Instance.AddItem(item);
                item.GetComponentInChildren<RangedWeaponController>(true)?.ApplySharedAmmo(record.ammo);
            }
            PickableItem equipped = null;
            foreach (var record in c.items)
                if (record.equipped && live.TryGetValue(record.id, out var item)) equipped = item;
            Inventory.Instance.SetActiveItem(equipped);
            foreach (var record in c.shops)
                if (stores.TryGetValue(record.path, out var store) && live.TryGetValue(record.item, out var item)) store.RestoreCheckpointPurchase(item);
            foreach (var record in c.sales)
                if (sales.TryGetValue(record.path, out var sale)) sale.RestoreCheckpointBag(record.coins);
            int delta = c.coins - CurrencyManager.Instance.Coins;
            if (delta != 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, delta);
            PlayerAmmoManager.Instance.RestoreRecovery(c.pistol, c.rifle, c.shotgun);
            PlayerStatsManager.Instance.ResetExp(); PlayerStatsManager.Instance.LoadExp(c.exp, c.level);
            PlayerStatsManager.Instance.RestoreRecoveryHealth(c.health);
            PlayerMovement.Instance.Teleport(c.player);
            for (int i = 0; i < 2; i++) route.guards[i].RestoreCheckpointHealth(c.guards[i], 12);
            route.RestoreCheckpointState(c);
            route.tutorial.ResumeAuthoredAt(c.intro);
        }
    }
}
