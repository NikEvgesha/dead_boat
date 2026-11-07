using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class CardProgressRecoveryProbe
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static void Set(object o, string n, object v) => o.GetType().GetField(n, Private).SetValue(o, v);
        private static T Get<T>(object o, string n) => (T)o.GetType().GetField(n, Private).GetValue(o);
        private static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, Private).Invoke(o, a);
        private static FieldInfo Static(Type type, string name) => type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly SaveManager Save;
            public readonly LevelStatManager Manager;
            public readonly LevelUpUi UI;
            public readonly SharedRunPreviewSave Memory;
            private readonly LevelUpUi oldUi = LevelUpUi.Instance;
            private readonly LevelStatManager oldManager = LevelStatManager.Instance;
            private readonly PlayerAmmoManager oldAmmo = PlayerAmmoManager.Instance;
            private readonly PlayerStatsManager oldPlayer = PlayerStatsManager.Instance;
            private readonly object oldSave = Static(typeof(SaveManager), "_instance").GetValue(null);
            private readonly object oldPause = Static(typeof(PauseManager), "_instance").GetValue(null);
            private readonly object oldControl = Static(typeof(ControlManager), "_instance").GetValue(null);
            private readonly FieldInfo contextSave = Static(typeof(SharedRunContext), "<Save>k__BackingField");
            private readonly object oldContext;
            public Fixture(bool shared)
            {
                oldContext = contextSave.GetValue(null);
                Root = new GameObject("Temporary card progress recovery");
                Memory = Root.AddComponent<SharedRunPreviewSave>();
                Save = Root.AddComponent<SaveManager>(); Set(Save, "saveProvider", Memory);
                Static(typeof(SaveManager), "_instance").SetValue(null, Save);
                contextSave.SetValue(null, shared ? Memory : null);
                var pause = Root.AddComponent<PauseManager>(); Static(typeof(PauseManager), "_instance").SetValue(null, pause);
                var control = Root.AddComponent<ControlManager>(); Set(control, "_useTouchControls", true);
                Static(typeof(ControlManager), "_instance").SetValue(null, control);
                Manager = Root.AddComponent<LevelStatManager>(); LevelStatManager.Instance = Manager;
                var ammo = Root.AddComponent<PlayerAmmoManager>();
                Set(ammo, "_ammo", new Dictionary<WeaponType, int>()); PlayerAmmoManager.Instance = ammo;
                PlayerStatsManager.Instance = Root.AddComponent<PlayerStatsManager>();
                UI = Root.AddComponent<LevelUpUi>(); LevelUpUi.Instance = UI;
                var panel = Child("Choice"); panel.SetActive(false); Set(UI, "_panel", panel);
                Set(UI, "_panelAdsChoise", Child("Upgrade ad"));
                Set(UI, "_buttonLevelUp", Child("Level button"));
                Set(UI, "_anim", Child("Animator").AddComponent<Animator>());
                Set(UI, "_countLevelUpUi", Child("Count", true).AddComponent<Text>());
                Set(UI, "_textPrice", Child("Price", true).AddComponent<Text>());
                Set(UI, "_buttons", new List<LevelUpButtomUI>());
                Manager._boostItems.Clear(); Manager.Stats = Manager.StatsDefault;
            }
            private GameObject Child(string name, bool rect = false)
            {
                var go = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
                go.transform.SetParent(Root.transform); return go;
            }
            public void Seed(int count)
            {
                Save.SaveLevelUpdate(Manager.StatsDefault); Save.SaveLevelUp(count);
                Call(Manager, "LoadStats"); UI.AddNewLevelCount(count);
            }
            public int ReloadCount()
            {
                var go = Child("Reloaded manager"); var fresh = go.AddComponent<LevelStatManager>();
                Call(fresh, "LoadStats"); return Get<int>(fresh, "_countLevelUp");
            }
            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                LevelUpUi.Instance = oldUi; LevelStatManager.Instance = oldManager;
                PlayerAmmoManager.Instance = oldAmmo; PlayerStatsManager.Instance = oldPlayer;
                Static(typeof(SaveManager), "_instance").SetValue(null, oldSave);
                Static(typeof(PauseManager), "_instance").SetValue(null, oldPause);
                Static(typeof(ControlManager), "_instance").SetValue(null, oldControl);
                contextSave.SetValue(null, oldContext);
            }
        }

        public static void Observe()
        {
            Require(!EditorApplication.isPlaying, "idle Editor required");
            foreach (bool shared in new[] { false, true })
            using (var f = new Fixture(shared))
            {
                f.Seed(1); Require(f.Manager.CheckNewLevel(), "open earned choice");
                Debug.Log("[Card progress observation] " + (shared ? "shared RAM route" : "solo RAM route") +
                    ": opened before selection; saved=" + f.Save.LoadLevelUp() + ", new manager=" + f.ReloadCount());
            }
        }

        public static void Run()
        {
            Require(!EditorApplication.isPlaying, "idle Editor required");
            foreach (bool shared in new[] { false, true })
            using (var f = new Fixture(shared))
            {
                f.Seed(1); Require(f.Manager.CheckNewLevel(), "open final choice");
                Require(!f.Manager.HaveLevelUp(), "last choice retains previous interstitial rule");
                Require(f.Save.LoadLevelUp() == 1 && f.ReloadCount() == 1, "open unselected choice survives fresh manager");
                f.UI.Close();
                Require(!f.UI.IsChoiceOpen && f.Save.LoadLevelUp() == 1, "dismiss without reward keeps choice");
                f.Seed(3); Require(f.Manager.CheckNewLevel() && f.Manager.HaveLevelUp(), "other pending choices skip final interstitial");
                Require(!f.Manager.CheckNewLevel() && f.Save.LoadLevelUp() == 3, "repeat open does not spend");
                var card = ScriptableObject.CreateInstance<BoostItem>();
                try
                {
                    Set(card, "_boostType", BoostType.RangeDamage);
                    Set(card, "_Rare", new List<RareReward> { new RareReward { Rare = RareType.Common, Reward = 5 } });
                    Require(f.UI.TryClaimCard(), "claim current offer");
                    Require(f.UI.ApplyNormalChoice(card, RareType.Common), "apply earned choice");
                    Require(!f.UI.ApplyNormalChoice(card, RareType.Common), "same offer cannot apply twice");
                    Require(f.Save.LoadLevelUp() == 2 && f.Save.LoadLevelUpdate().RangeDamage == 5, "bonus and count committed together");
                    // Simulate a stale legacy counter (e.g. interruption after canonical write).
                    f.Memory.SaveLevelUp(999);
                    Require(f.Save.LoadLevelUp() == 2 && f.ReloadCount() == 2, "canonical count wins over stale mirror");
                    var captured = SharedPersonalData.Capture();
                    Require(captured.Upgrades == 2 && captured.RangeDamage == 5, "actual personal capture uses live coherent progress, not stale legacy counter");
                    var json = JsonUtility.ToJson(f.Save.LoadLevelUpdate());
                    var restored = JsonUtility.FromJson<Stats>(json);
                    Require(restored.HasUpgradeCount && restored.UnspentUpgrades == 2 && restored.RangeDamage == 5, "coherent JSON reload");
                    f.UI.Close();
                    Require(f.UI.IsChoiceOpen && f.Manager.PendingUpgrades == 2 && !f.UI.ApplyNormalChoice(card, RareType.Common), "next offer opens without extra debit or old grant");

                    // Same Cards/Upgrades sources and actual SeedSave used by network return / solo fork.
                    var data = SharedPersonalData.Capture();
                    var forkObject = new GameObject("Temporary return/solo seed");
                    forkObject.transform.SetParent(f.Root.transform);
                    var fork = forkObject.AddComponent<SharedRunPreviewSave>();
                    data.SeedSave(fork, 0, 0, 0, 10);
                    Require(fork.LoadLevelUpdate().UnspentUpgrades == 2 && fork.LoadLevelUpdate().RangeDamage == 5 && fork.LoadLevelUp() == 2,
                        "actual recovery/solo seed carries coherent progress");
                    f.Manager.ScriptedOffersOnly = true;
                    Require(!f.Manager.ApplyEarnedChoice(card, RareType.Common) && f.Manager.PendingUpgrades == 2, "scripted run does not consume ordinary choices");
                    f.Manager.ScriptedOffersOnly = false;
                    f.Manager.RestoreRecovery(data.Cards, data.Upgrades);
                    Require(f.Save.LoadLevelUp() == 2 && f.Save.LoadLevelUpdate().RangeDamage == 5 && !f.UI.IsChoiceOpen,
                        "actual RestoreRecovery pair invalidates the old open choice");
                    Require(f.Manager.CheckNewLevel() && f.Manager.PendingUpgrades == 2, "recovery can reopen a choice without debit");
                    f.UI.Close();
                    f.Manager.DeleteProgress();
                    Require(f.Save.LoadLevelUp() == 0 && f.Save.LoadLevelUpdate().RangeDamage == 0, "new run reset is coherent");
                    f.Memory.SaveLevelUpdate(new Stats { Save = true, RangeDamage = 7 }); f.Memory.SaveLevelUp(4);
                    Require(f.Save.LoadLevelUp() == 4 && f.ReloadCount() == 4, "legacy save without marker reads old count");
                    f.Save.SaveLevelUp(5);
                    Require(f.Save.LoadLevelUpdate().HasUpgradeCount && f.Save.LoadLevelUp() == 5 && f.Save.LoadLevelUpdate().RangeDamage == 7, "legacy mutation migrates without losing stats");
                }
                finally { UnityEngine.Object.DestroyImmediate(card); }
                Debug.Log("[Card progress recovery] PASS " + (shared ? "shared RAM route" : "solo RAM route") +
                    ": open/reload/count+bonus/duplicate/next offer/stale mirror/JSON/SeedSave/RestoreRecovery/scripted/reset/legacy; no Play/network/user save/ads/Build");
            }
            CardOfferSafetyProbe.Run();
            Debug.Log("[Card progress recovery] Math regression: " + CombatCardProbe.RunMath());
            Debug.Log("[Card progress recovery] Protocol: " + AvatarNamesProbe.Validate());
        }
    }
}
