using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    /// <summary>Real UI/state methods, no Play/save/ads/currency and no gameplay singleton Awake.</summary>
    public static class CardOfferSafetyProbe
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
        private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Fields).Invoke(target, args);
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        private static int Begin(LevelUpUi ui)
        {
            object[] args = { 0 };
            Require((bool)Call(ui, "BeginRefresh", args), "request must begin");
            return (int)args[0];
        }
        private static void Complete(LevelUpUi ui, int revision, bool success) => Call(ui, "CompleteRefresh", revision, success);

        public static void Run()
        {
            Require(!EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer, "idle Editor");
            var oldUi = LevelUpUi.Instance; var oldManager = LevelStatManager.Instance;
            var root = new GameObject("Temporary card offer safety");
            try
            {
                var panel = new GameObject("Open choice"); panel.transform.SetParent(root.transform);
                var ui = root.AddComponent<LevelUpUi>();
                Set(ui, "_panel", panel); Set(ui, "_buttons", new List<LevelUpButtomUI>());
                var managerObject = new GameObject("Dormant stats"); managerObject.transform.SetParent(root.transform);
                managerObject.SetActive(false);
                var manager = managerObject.AddComponent<LevelStatManager>();
                LevelUpUi.Instance = ui; LevelStatManager.Instance = manager;
                Set(manager, "_countLevelUp", 3);
                // Before the fix this enters SaveManager/NewLevel and consumes a level.
                Require(!manager.CheckNewLevel() && Get<int>(manager, "_countLevelUp") == 3, "repeated hotkey must not consume another level");
                int initial = Get<int>(ui, "_offerRevision");
                ui.NewLevel(new List<ItemWithRare>());
                Require(Get<int>(ui, "_offerRevision") == initial, "open offer must not be replaced");

                int first = Begin(ui);
                object[] duplicate = { 0 };
                Require(!(bool)Call(ui, "BeginRefresh", duplicate) && !ui.TryClaimCard(), "pending reroll blocks duplicate request and selection");
                ui.Refresh();
                ui._RefreshClickGems(); // Must return before touching any real currency singleton.
                Require(Get<int>(ui, "_offerRevision") == first, "pending reroll blocks paid/manual replacement");
                Complete(ui, first, false);
                Require(!Get<bool>(ui, "_refreshPending"), "failure releases input");

                int retry = Begin(ui);
                Require(retry != first, "retry needs distinct request ID");
                Complete(ui, first, true);
                Require(Get<bool>(ui, "_refreshPending"), "late success of failed request cannot consume retry");
                Complete(ui, retry, true);
                int applied = Get<int>(ui, "_offerRevision");
                Require(!Get<bool>(ui, "_refreshPending") && applied != retry, "success refreshes real UI once");
                Complete(ui, retry, true);
                Require(Get<int>(ui, "_offerRevision") == applied, "duplicate success ignored");

                int stale = Begin(ui);
                ui.RefreshLevel(new List<ItemWithRare>());
                int changed = Get<int>(ui, "_offerRevision");
                Complete(ui, stale, true);
                Require(Get<int>(ui, "_offerRevision") == changed, "old offer callback cannot update replacement");
                int hidden = Begin(ui);
                panel.SetActive(false); Complete(ui, hidden, true);
                Require(!Get<bool>(ui, "_refreshPending"), "hidden offer cannot refresh");
                panel.SetActive(true);
                Begin(ui); Call(ui, "OnDisable");
                Require(!Get<bool>(ui, "_refreshPending"), "disable invalidates request");
                Require(ui.TryClaimCard(), "selection available after invalidation"); ui.ReleaseCardClaim();
                Require(Get<int>(manager, "_countLevelUp") == 3, "unspent levels preserved throughout guards");
                Debug.Log("[Card offer safety] PASS: repeat open/count, pending selection/gems, fail/retry/stale/duplicate success, replacement/hide/disable; no Play/ads/currency/save/Build");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                LevelUpUi.Instance = oldUi; LevelStatManager.Instance = oldManager;
            }
        }
    }
}
