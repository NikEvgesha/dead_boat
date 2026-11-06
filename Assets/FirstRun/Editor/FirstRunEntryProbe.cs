using System;
using System.Linq;
using System.Reflection;
using DeadBoat.Online;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.FirstRun.Editor
{
    public sealed class FirstRunEntrySave : SharedRunPreviewSave
    {
        public bool Existing;
        public override bool CheckProgress() => Existing;
        public override bool GetTutorialProgress() => false;
        public override int LoadGems() => 0;
        public override int LoadWins() => 0;
        public override void SetSave(bool value) => Existing = value;
    }
    [InitializeOnLoad]
    public static class FirstRunEntryProbe
    {
        private const string Key = "DeadBoat.FirstRunEntryProbe.";
        private static IDisposable profileScope;
        static FirstRunEntryProbe()
        {
            SceneManager.sceneLoaded += BeforeStart;
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredEditMode) return;
                profileScope?.Dispose(); profileScope = null;
                FirstRunSession.End();
                SessionState.SetBool(Key + "active", false);
            };
        }
        public static string Prepare(string json = "", bool existing = false, int distance = -1)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode required");
            FirstRunAuthoredTools.Prepare();
            SessionState.SetBool("DeadBoat.FirstRunAuthored.loaded", true); // Automatic boot owns routing in this probe.
            SessionState.SetString(Key + "json", json);
            SessionState.SetBool(Key + "existing", existing);
            SessionState.SetInt(Key + "distance", distance);
            SessionState.SetBool(Key + "active", true);
            return "isolated profile startup prepared";
        }
        private static void BeforeStart(Scene scene, LoadSceneMode mode)
        {
            if (!SessionState.GetBool(Key + "active", false) || profileScope != null) return;
            typeof(DeadBoat.Online.Editor.SharedPerformanceTestSave).GetMethod("OnSceneLoaded", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] {scene, mode});
            profileScope = FirstRunProfile.UseTemporaryStorage(SessionState.GetString(Key + "json", ""));
            // SharedPerformanceTestSave already installed a temporary provider before
            // LoadingManager.Start. Replace only that provider; never the owner's data.
            var manager = UnityEngine.Object.FindAnyObjectByType<SaveManager>(FindObjectsInactive.Include);
            var original = manager.PersistentProvider as SharedRunPreviewSave;
            if (original == null) throw new InvalidOperationException("Expected isolated base provider");
            var save = manager.gameObject.AddComponent<FirstRunEntrySave>();
            save.Persistent = original;
            save.Existing = SessionState.GetBool(Key + "existing", false);
            save.SaveDistance(SessionState.GetInt(Key + "distance", -1)); save.SaveLevelId(0);
            typeof(SaveManager).GetField("saveProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, save);
        }
        public static string CaptureForColdRestart()
        {
            var route = UnityEngine.Object.FindAnyObjectByType<FirstRunAuthoredRoute>();
            if (route == null || !FirstRunSession.Active || !FirstRunProfile.IsTemporary) throw new Exception("No isolated lesson");
            route.CaptureCheckpoint();
            var json = FirstRunProfile.ReadJson();
            SessionState.SetString(Key + "cold", json);
            return "checkpoint serialized for cold boot";
        }
        public static string PrepareCold() => Prepare(SessionState.GetString(Key + "cold", ""));
        public static string VerifyCold()
        {
            var r = UnityEngine.Object.FindAnyObjectByType<FirstRunAuthoredRoute>();
            Require(r != null && FirstRunSession.Active && !FirstRunSession.Restoring, "restored route ready");
            Require(r.Lesson == FirstRunLesson.SailBoss && r.ChoicesMade == 2, "phase/cards completed");
            Require(CurrencyManager.Instance.Coins == 137 && PlayerStatsManager.Instance.Health == 57, "coins and health");
            Require(LevelStatManager.Instance.Stats.InventorySlots == 1 && LevelStatManager.Instance.Stats.SteadyAimRank == 1, "card modifiers restored");
            var gun = r.supplies[3].GetComponentInChildren<RangedWeaponController>(true);
            Require(Inventory.Instance.GetItems().Contains(r.supplies[3]) && gun.CurrentAmmo == 2 &&
                PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol) == 17, "same gun and ammunition");
            Require(Mathf.Abs(BoardController.Instance.TotalDistanceTraveled - 710) < .1f && r.guards.All(g => g.IsDead), "boat and cleared guards");
            Require(r.EggCollected && r.Companion != null, "egg and temporary companion");
            Require(UnityEngine.Object.FindObjectsByType<Fusion.NetworkRunner>(FindObjectsSortMode.None).Length == 0, "no Photon");
            return "PASS: cold boot restores phase/710m/137coins/57HP/both cards/gun2+17/guards/egg/companion; no Photon";
        }
        private static object Field(object o, string field) => o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
        private static object Invoke(object o, string name, params object[] args) => o.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, args);
        private static System.Collections.Generic.List<TutorialStep> Steps(FirstRunAuthoredRoute r) => (System.Collections.Generic.List<TutorialStep>)Field(r.tutorial, "_tutorialSteps");
        public static string SaveDockSale()
        {
            var r = UnityEngine.Object.FindAnyObjectByType<FirstRunAuthoredRoute>();
            Require(FirstRunSession.Active && !FirstRunSession.Restoring && r.tutorial.CurrentStepIndex == 0, "fresh lesson");
            var steps = Steps(r); var item = (PickableItem)Field(steps[0], "_item");
            item.PickUp(PlayerMovement.Instance.transform);
            Invoke(Field(steps[1], "_sellPoint"), "OnTriggerEnter", item.GetComponent<Collider>());
            Require(r.tutorial.CurrentStepIndex == 2, "sale reaches money bag step");
            CaptureForColdRestart();
            return "PASS: fresh automatic boot, actual pickup/sale, checkpoint at money bag";
        }
        public static string VerifyDockAndBuy()
        {
            var r = UnityEngine.Object.FindAnyObjectByType<FirstRunAuthoredRoute>(); var steps = Steps(r);
            Require(!FirstRunSession.Restoring && r.tutorial.CurrentStepIndex == 2, "dock step restored");
            var original = (PickableItem)Field(steps[0], "_item");
            Require(!original.gameObject.activeInHierarchy, "sold gold not duplicated");
            var bag = (MoneyBag)Field(steps[2], "_moneyBag"); Require(bag && bag.Money > 0, "sale bag restored at goal");
            bag.GetType().GetField("_progress", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bag, 1f);
            ((System.Collections.IEnumerator)Invoke(bag, "BuyProcess")).MoveNext();
            var shop = (StorePoint)Field(steps[3], "_storePoint");
            shop.GetType().GetField("_progress", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(shop, 1f);
            ((System.Collections.IEnumerator)Invoke(shop, "BuyProcess", true)).MoveNext();
            Require(r.tutorial.CurrentStepIndex == 4 && shop.LastPurchasedItem, "cash and real purchase continue");
            CaptureForColdRestart();
            return "PASS: sold gold stays removed, cash bag restores and pays once, purchased fuel checkpoint";
        }
        public static string VerifyPurchaseAndFuel()
        {
            var r = UnityEngine.Object.FindAnyObjectByType<FirstRunAuthoredRoute>(); var steps = Steps(r);
            Require(!FirstRunSession.Restoring && r.tutorial.CurrentStepIndex == 4, "purchase step restored");
            var fuel = (PickableItem)Field(steps[4], "_item"); Require(fuel && fuel.GetComponent<FuelItem>(), "purchased fuel linked to tutorial");
            fuel.PickUp(PlayerMovement.Instance.transform);
            Invoke(Field(steps[5], "_item"), "OnTriggerEnter", fuel.GetComponent<Collider>());
            r.tutorial.QuickStopTutorial();
            Require(r.tutorial.IntroComplete && BoardController.Instance.currentFuel > 0, "refuel and steering after resume");
            return "PASS: purchased fuel survives cold boot; old tutorial continues through refueling/steering";
        }
        public static string VerifyModels()
        {
            Require(FirstRunProfile.Resolve(null, false).status == FirstRunStatus.Running, "new profile");
            Require(FirstRunProfile.Resolve(null, true).status == FirstRunStatus.Legacy, "legacy migration");
            foreach (var status in new[] {FirstRunStatus.Skipped, FirstRunStatus.Completed, FirstRunStatus.Legacy})
                Require(FirstRunProfile.Resolve(new FirstRunProfileData {status=status}, false).status == status, "terminal access");
            using (FirstRunProfile.UseTemporaryStorage("invalid")) Require(FirstRunProfile.Read().status == FirstRunStatus.Running, "corrupt recovery");
            foreach (var status in new[] { FirstRunStatus.Completed, FirstRunStatus.Skipped })
                using (FirstRunProfile.UseTemporaryStorage(JsonUtility.ToJson(new FirstRunProfileData {status=status, content=-1})))
                    Require(FirstRunProfile.Read().status == status, "content updates never reopen closed lessons");
            return "PASS: new/legacy/terminal/corrupt profile rules; closed lessons survive content version changes";
        }
        public static string VerifyStopped()
        {
            Require(!EditorApplication.isPlaying && profileScope == null && !FirstRunProfile.IsTemporary && !FirstRunSession.Active, "profile scope released");
            return FirstRunAuthoredTools.VerifyRestored();
        }
        private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    }
}
