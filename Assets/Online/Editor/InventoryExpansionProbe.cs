using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class InventoryExpansionProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Run()
        {
            if (!EditorApplication.isPlaying || Inventory.Instance != null || InventoryUI.Instance != null || LevelStatManager.Instance != null)
                throw new InvalidOperationException("Isolated empty Play scene required");
            var root = new GameObject("Temporary inventory expansion fixture"); root.SetActive(false);
            var inventory = root.AddComponent<Inventory>();
            var ui = root.AddComponent<InventoryUI>();
            var stats = root.AddComponent<LevelStatManager>();
            typeof(Inventory).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, inventory);
            typeof(InventoryUI).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, ui);
            LevelStatManager.Instance = stats;
            try
            {
                Set(inventory, "_baseBagCapacity", 10);
                var bag = new List<PickableItem>(); Set(inventory, "_bagItems", bag);
                Set(ui, "_initialized", true);
                Set(ui, "_InventorySlotPrefab", AssetDatabase.LoadAssetAtPath<InventorySlot>("Assets/Prefabs/Inventory/InventorySlotPrefab.prefab"));
                Set(ui, "_inventorySlotsParent", root.transform);
                var label = new GameObject("Temporary capacity label", typeof(RectTransform)); label.transform.SetParent(root.transform);
                var text = label.AddComponent<Text>(); Set(ui, "_capacityText", text); Set(ui, "_capacityButtonText", text);
                stats.Stats.InventorySlots = 0; inventory.RefreshCapacity();
                int baseline = Mathf.Clamp(10 + ProfessionService.GetCurrentPassiveBonuses().inventorySlots + EggAnimalBuffService.InventorySlots, 1, 27);
                Require(inventory.BagCapacity == baseline, "base profession/animal sum");
                ui.ResizeBag(baseline);
                var slots = (List<InventorySlot>)typeof(InventoryUI).GetField("_mainSlots", Private).GetValue(ui);
                var first = slots[0];
                stats.Stats.InventorySlots = 10; inventory.RefreshCapacity();
                Require(inventory.BagCapacity == Mathf.Min(27, baseline + 10) && slots[0] == first, "growth rebuilt slots or wrong sum");
                stats.Stats.InventorySlots = 1000; inventory.RefreshCapacity();
                Require(inventory.BagCapacity == 27 && slots.Count == 27, "capacity cap");
                var dummy = new GameObject("Temporary retained item"); dummy.SetActive(false); dummy.transform.SetParent(root.transform);
                var item = dummy.AddComponent<PickableItem>(); bag.Add(item);
                typeof(InventorySlot).GetField("_item", Private).SetValue(slots[26], item);
                stats.Stats.InventorySlots = 0; inventory.RefreshCapacity();
                Require(inventory.BagCapacity == 27 && slots[26].CurrentItem == item && bag.Contains(item), "reduction lost high occupied slot");
                Require((int)typeof(Inventory).GetField("_admissionCapacity", Private).GetValue(inventory) == baseline, "reduction still permits excess pickups");
                typeof(InventorySlot).GetField("_item", Private).SetValue(slots[26], null); bag.Clear(); inventory.RefreshCapacity();
                Require(inventory.BagCapacity == baseline && slots[0] == first, "capacity not reduced after emptying");
                var legacy = JsonUtility.FromJson<Stats>("{\"Save\":true}"); Require(legacy.InventorySlots == 0, "legacy save");
                var personal = new SharedPersonalData { InventorySlots = 17 };
                Require(personal.Cards.InventorySlots == 17 && JsonUtility.FromJson<Stats>(JsonUtility.ToJson(personal.Cards)).InventorySlots == 17, "save/recovery mapping");
                var buffs = new AnimalRunBuffs { inventorySlots = 2 }; buffs.Add(new AnimalRunBuffs { inventorySlots = 4 });
                Require(buffs.inventorySlots == 6, "animals not additive");
                var card = Resources.Load<BoostItem>("BoostItem_InventorySlots");
                Require(card != null && card.BoostType == BoostType.InventorySlots && card.RareRewards.Count == 6 &&
                    card.RareRewards[0].Reward == 1 && card.RareRewards[5].Reward == 10, "card asset");
                string result = "[Inventory expansion] PASS: real slot prefab; additive capacity, stable IDs, cap27, high occupied slot retained, admission limit after reduction, legacy/new JSON, personal recovery mapping, animal sum, card1..10";
                Debug.Log(result); return result;
            }
            finally
            {
                typeof(Inventory).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
                typeof(InventoryUI).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
                LevelStatManager.Instance = null;
                UnityEngine.Object.Destroy(root);
            }
        }
        public static async Task RunShared(SharedDepartureState authority, SharedDepartureState remote)
        {
            authority.RPC_RecoveryProgress(new SharedPersonalData { InventorySlots = 11 });
            remote.RPC_RecoveryProgress(new SharedPersonalData { InventorySlots = 3 });
            double deadline = Time.realtimeSinceStartupAsDouble + 8;
            while (!remote.Personal.TryGet(authority.Runner.LocalPlayer, out var first) || first.Cards.InventorySlots != 11 ||
                !remote.Personal.TryGet(remote.Runner.LocalPlayer, out var second) || second.Cards.InventorySlots != 3)
            {
                if (Time.realtimeSinceStartupAsDouble > deadline) throw new TimeoutException("personal capacity replication");
                await Task.Delay(25);
            }
            Debug.Log("[Inventory expansion] PASS: two Photon players retain separate card slot bonuses11/3");
        }
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
