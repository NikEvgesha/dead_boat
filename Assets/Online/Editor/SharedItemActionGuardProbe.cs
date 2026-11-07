using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class SharedItemActionGuardProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static FieldInfo Static(Type t, string n) => t.GetField(n, BindingFlags.Static | BindingFlags.NonPublic);
        private static void Set(object o, string n, object v) => o.GetType().GetField(n, Private).SetValue(o, v);
        private static T Get<T>(object o, string n) => (T)o.GetType().GetField(n, Private).GetValue(o);
        private static object Call(object o, string n) => o.GetType().GetMethod(n, Private).Invoke(o, null);
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run()
        {
            Require(!EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer, "idle Editor");
            var saved = new Dictionary<FieldInfo, object>();
            void Backup(Type t, string name) { var f = Static(t, name); saved.Add(f, f.GetValue(null)); }
            Backup(typeof(SharedRunContext), "<Active>k__BackingField"); Backup(typeof(SharedRunContext), "<State>k__BackingField");
            Backup(typeof(SharedRecoveryInventory), "<Applying>k__BackingField");
            Backup(typeof(Inventory), "_instance"); Backup(typeof(InventoryUI), "_instance");
            foreach (string name in new[] { "showing", "deadline", "until", "token", "run" }) Backup(typeof(SharedAdProtection), name);
            var pending = (Dictionary<ulong, Action<PickableItem>>)Static(typeof(SharedItemsRuntime), "pending").GetValue(null);
            var deadlines = (Dictionary<ulong, double>)Static(typeof(SharedItemsRuntime), "deadlines").GetValue(null);
            var cancelled = (HashSet<ulong>)Static(typeof(SharedItemsRuntime), "cancelledPending").GetValue(null);
            var oldPending = new Dictionary<ulong, Action<PickableItem>>(pending);
            var oldDeadlines = new Dictionary<ulong, double>(deadlines); var oldCancelled = new HashSet<ulong>(cancelled);
            var root = new GameObject("Temporary item action guard"); root.SetActive(false);
            try
            {
                Static(typeof(SharedRunContext), "<Active>k__BackingField").SetValue(null, false);
                Static(typeof(SharedRunContext), "<State>k__BackingField").SetValue(null, null);
                Static(typeof(SharedRecoveryInventory), "<Applying>k__BackingField").SetValue(null, false);
                Static(typeof(SharedAdProtection), "run").SetValue(null, null);
                Static(typeof(SharedAdProtection), "token").SetValue(null, 0L);
                var inventory = root.AddComponent<Inventory>(); Static(typeof(Inventory), "_instance").SetValue(null, inventory);
                var bag = new List<PickableItem>(); Set(inventory, "_bagItems", bag); Set(inventory, "_quickPanelItems", new List<PickableItem>());
                var itemGo = new GameObject("Personal item without network ID", typeof(Rigidbody), typeof(BoxCollider)); itemGo.transform.SetParent(root.transform);
                var item = itemGo.AddComponent<PickableItem>();
                var pickup = root.AddComponent<PlayerItemPickUp>(); Set(pickup, "_raycastHitItem", item);
                var slotGo = new GameObject("Slot", typeof(RectTransform), typeof(Image)); slotGo.transform.SetParent(root.transform);
                var slot = slotGo.AddComponent<InventorySlot>();
                SharedAdProtection.Begin();
                Require(SharedItemInteraction.Blocked && SharedItemsRuntime.Request(item, _ => throw new Exception("must defer")), "personal item cannot bypass ad before identity lookup");
                Require(!inventory.AddItem(item) && bag.Count == 0 && item.Status == ItemStatus.Free, "blocked add has no local inventory mutation");
                inventory.DropOutItem(item, null); inventory.TrySwitch(null, null); inventory.SetActiveItem(item);
                Require(!(bool)Call(pickup, "TryPickupObject"), "direct pickup is blocked"); Call(pickup, "DropItem"); Call(pickup, "TryAttach");
                slot.OnBeginDrag(null); slot.OnDrag(null); slot.OnPointerClick(null); slot.OnDrop(null);
                Require(!Get<bool>(slot, "_isDragging"), "blocked UI events cannot begin mutation");
                int actions = 0; const ulong id = 0xD001;
                pending[id] = _ => actions++; deadlines[id] = Time.realtimeSinceStartupAsDouble + 5;
                typeof(SharedItemsRuntime).GetMethod("CancelBlockedRequests", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                Require(actions == 0 && pending.ContainsKey(id) && deadlines.ContainsKey(id) && cancelled.Contains(id), "cancel intent retains its pending residency guard until normal reply/timeout");

                var ui = root.AddComponent<InventoryUI>(); Static(typeof(InventoryUI), "_instance").SetValue(null, ui);
                var dropPanel = new GameObject("Drop overlay"); dropPanel.transform.SetParent(root.transform); Set(ui, "_dropOutPanel", dropPanel);
                var home = new GameObject("Original slot", typeof(RectTransform)); home.transform.SetParent(root.transform);
                var iconGo = new GameObject("Dragged icon", typeof(RectTransform), typeof(InventoryIcon)); iconGo.transform.SetParent(root.transform);
                Set(slot, "_icon", slotGo.GetComponent<Image>()); Set(slot, "_container", iconGo.GetComponent<InventoryIcon>()); Set(slot, "_slot", home.transform);
                Set(slot, "_newParent", null); Set(slot, "_isDragging", true); Set(slot, "_item", item);
                Set(item, "_status", ItemStatus.InInventory); bag.Add(item);
                slot.OnEndDrag(null);
                Require(iconGo.transform.parent == home.transform && slot.CurrentItem == item && bag.Count == 1 && item.Status == ItemStatus.InInventory,
                    "ad starting mid-drag returns icon without dropping/removing item");

                SharedAdProtection.End();
                Static(typeof(SharedRunContext), "<Active>k__BackingField").SetValue(null, true);
                Require(SharedItemInteraction.Blocked && !inventory.AddItem(item), "disconnected shared context cannot mutate local bag");
                Static(typeof(SharedRecoveryInventory), "<Applying>k__BackingField").SetValue(null, true);
                Require(!SharedItemInteraction.Blocked && !SharedItemsRuntime.Request(item, _ => actions++), "explicit recovery population bypasses user action guard");
                Static(typeof(SharedRecoveryInventory), "<Applying>k__BackingField").SetValue(null, false);
                Static(typeof(SharedRunContext), "<Active>k__BackingField").SetValue(null, false);
                Require(!SharedItemInteraction.Blocked && !SharedItemsRuntime.Request(item, _ => actions++), "ordinary local ownership after ad is unchanged (grace is not an action lock)");
                Require(cancelled.Contains(id) && actions == 0, "cancelled intent cannot silently become active after ad");
                var actor = new Vector3(1000, 10, 8000); var nearby = actor + Vector3.right * 20;
                Require(SharedItemInteraction.ReleasePosition(nearby, actor) == nearby, "normal drop position unchanged");
                var released = SharedItemInteraction.ReleasePosition(actor + Vector3.right * 1000, actor);
                Require(released == actor + Vector3.up && Vector3.Distance(released, actor) < 20, "late far-away claim can release within existing authority range");
                Require(SharedItemInteraction.ReleasePosition(new Vector3(float.NaN, 0, 0), actor) == actor + Vector3.up, "invalid candidate falls back to owner");
                var origin = new Vector3(100000, 0, 100000);
                Require(SharedItemInteraction.ReleasePosition(nearby + origin, actor + origin) - origin == nearby, "logical origin is applied once");
                bag.Clear(); Set(item, "_status", ItemStatus.Free); Set(item, "_componentsInitialized", true); Set(item, "_rb", itemGo.GetComponent<Rigidbody>());
                item.ApplySharedPhysics(false); Require(itemGo.GetComponent<Rigidbody>().isKinematic, "waiting free proxy can freeze physics");
                item.ApplySharedPhysics(true); Require(!itemGo.GetComponent<Rigidbody>().isKinematic, "ordinary free simulation can resume");
                var identity = itemGo.AddComponent<WorldSpawnIdentity>(); identity.InitializeRemote(0xD002, 0);
                var sync = typeof(WorldSpawnIdentity).GetMethod("SyncFreeAuthorityPose", Private);
                var pose = actor + origin;
                Require((bool)sync.Invoke(identity, new object[] { pose, Quaternion.identity, origin }) &&
                    item.transform.position == actor && itemGo.GetComponent<Rigidbody>().position == actor, "new free authority starts at canonical logical pose before physics sampling");
                item.transform.position = actor + Vector3.right;
                Require(!(bool)sync.Invoke(identity, new object[] { pose, Quaternion.identity, origin }) && item.transform.position == actor + Vector3.right,
                    "ongoing authority physics is not reset to old snapshot every audit");
                Call(identity, "ResetFreeAuthorityPose");
                Require((bool)sync.Invoke(identity, new object[] { pose + Vector3.forward * 5, Quaternion.identity, origin }) && item.transform.position == actor + Vector3.forward * 5,
                    "new ownership/role interval reapplies canonical pose once");
                Debug.Log("[Item action guard] PASS: personal/shared-disconnected/UI/add/drop/switch/drag return; recovery bypass; cancelled intent retains guard; release distance/origin; real Rigidbody freeze/resume/canonical authority pose once+reset. No live claim race/reconnect/held/drop network ACK/positive full bag load/Play/Build/user saves");
            }
            finally
            {
                Static(typeof(Inventory), "_instance").SetValue(null, null); Static(typeof(InventoryUI), "_instance").SetValue(null, null);
                UnityEngine.Object.DestroyImmediate(root);
                pending.Clear(); foreach (var p in oldPending) pending.Add(p.Key, p.Value);
                deadlines.Clear(); foreach (var p in oldDeadlines) deadlines.Add(p.Key, p.Value);
                cancelled.Clear(); cancelled.UnionWith(oldCancelled);
                foreach (var pair in saved) pair.Key.SetValue(null, pair.Value);
            }
            AdControlReturnProbe.Run();
        }
    }
}
