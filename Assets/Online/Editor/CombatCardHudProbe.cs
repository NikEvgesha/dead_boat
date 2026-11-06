using System;
using System.Reflection;
using System.Threading.Tasks;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class CombatCardHudProbe
    {
        public static string Result { get; private set; } = "not run";
        private static bool running;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        private static RectTransform Root(CombatCardHud hud) => (RectTransform)typeof(CombatCardHud).GetField("root", Private).GetValue(hud);
        private static void Refresh(CombatCardHud hud) => typeof(CombatCardHud).GetMethod("Refresh", Private).Invoke(hud, null);
        private static string Text(CombatCardHud hud, BoostType type) => Root(hud).Find(type + "/Label").GetComponent<Text>().text;
        private static MMF_Player Pulse(CombatCardHud hud, BoostType type) => Root(hud).Find(type.ToString()).GetComponentInChildren<MMF_Player>(true);
        public static async void Run()
        {
            if (running) return;
            Require(SaveManager.Instance?.PersistentProvider is SharedRunPreviewSave && LoadingManager.Instance.CurrentLocation == Location.Lobby, "isolated real Lobby required");
            running = true; Result = "running";
            GameObject victim = null;
            string language = LocalizationManager.Instance.CurrentLanguage;
            try
            {
                var lobbyHud = LevelUpUi.Instance.GetComponent<CombatCardHud>();
                Require(lobbyHud != null && !Root(lobbyHud).gameObject.activeSelf, "combat HUD shown in social lobby");
                SaveManager.Instance.PersistentProvider.SaveLevelId(0);
                LoadingManager.Instance.LoadLocation(Location.Game, "LevelForest", false);
                await Until(() => LoadingManager.Instance.CurrentLocation == Location.Game && BoardController.Instance != null && LevelUpUi.Instance != null);
                Time.timeScale = 1;
                var hud = LevelUpUi.Instance.GetComponent<CombatCardHud>(); Require(hud != null, "HUD not attached in real level");
                var cards = LevelStatManager.Instance; var player = PlayerStatsManager.Instance;
                var stats = cards.StatsDefault; stats.BloodPactRank = 1; stats.BattleRhythmRank = 3; stats.OnTheEdgeRank = 2;
                cards.RestoreRecovery(stats, 0); player.RestoreRecoveryHealth(player.MaxHealth * .5f);
                PauseManager.Instance.SetPause(false, false); Time.timeScale = 1;
                Refresh(hud);
                Require(Root(hud).gameObject.activeSelf && Text(hud, BoostType.BattleRhythm).Contains("0/3"), "owned cards/initial combo");
                Require(!Text(hud, BoostType.OnTheEdge).Contains("+35%"), "edge active above threshold");
                await Task.Delay(1150); // Conservative post-recovery healing lock expires.
                victim = new GameObject("Temporary combat HUD victim"); victim.SetActive(false);
                var enemy = victim.AddComponent<SharedProbeEnemy>(); enemy.SetHP(20000);
                int players = hud.GetComponentsInChildren<MMF_Player>(true).Length;
                int cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
                var first = CombatCards.Begin(true); CombatCards.Hit(first, enemy, 10); CombatCards.End(first); Refresh(hud);
                Require(Text(hud, BoostType.BattleRhythm).Contains("1/3"), "first hit counter");
                Require(Text(hud, BoostType.BloodPact).Contains("+0.4 HP"), "actual heal display");
                var second = CombatCards.Begin(true); CombatCards.Hit(second, enemy, 10); CombatCards.End(second); Refresh(hud);
                Require(Text(hud, BoostType.BattleRhythm).Contains("2/3"), "second hit counter");
                var third = CombatCards.Begin(true); CombatCards.Hit(third, enemy, 10); CombatCards.End(third); Refresh(hud);
                Require(CombatCards.ThirdHitRevision == 1 && Pulse(hud, BoostType.BattleRhythm).PlayCount == 1, "confirmed third pulse");
                Require(CombatCards.HealingTotal > 0 && Pulse(hud, BoostType.BloodPact).PlayCount == 1, "heals were not coalesced");
                for (int i = 0; i < 20; i++) Refresh(hud);
                Require(Pulse(hud, BoostType.BattleRhythm).PlayCount == 1, "polling repeats third pulse");
                CombatCards.Confirm(999999, 100); Refresh(hud);
                Require(CombatCards.ThirdHitRevision == 1, "unknown confirmation creates a cue");
                CombatCards.End(CombatCards.Begin(true));
                await Task.Delay(850); Refresh(hud);
                Require(Text(hud, BoostType.BattleRhythm).Contains("0/3"), "miss did not reset");
                CombatCards.Hit(CombatCards.Begin(true), enemy, 10);
                await Task.Delay(2150); Refresh(hud);
                Require(CombatCards.ComboHits == 0 && Text(hud, BoostType.BattleRhythm).Contains("0/3"), "combo timeout");
                player.RestoreRecoveryHealth(player.MaxHealth * .4f); Refresh(hud);
                int edgePlays = Pulse(hud, BoostType.OnTheEdge).PlayCount;
                Require(Text(hud, BoostType.OnTheEdge).Contains("+35%") && edgePlays > 0, "inclusive HP threshold");
                for (int i = 0; i < 20; i++) Refresh(hud);
                Require(Pulse(hud, BoostType.OnTheEdge).PlayCount == edgePlays, "low HP pulses every poll");
                player.RestoreRecoveryHealth(player.MaxHealth * .41f); Refresh(hud);
                Require(!Text(hud, BoostType.OnTheEdge).Contains("+35%"), "edge did not deactivate");
                Time.timeScale = 0; Refresh(hud); Require(!Root(hud).gameObject.activeSelf, "pause not hidden"); Time.timeScale = 1;
                SharedAdProtection.Begin(); Refresh(hud); Require(!Root(hud).gameObject.activeSelf, "ad not hidden");
                SharedAdProtection.End(); Refresh(hud);
                int thirdPlays = Pulse(hud, BoostType.BattleRhythm).PlayCount;
                Require(thirdPlays == 1, "hidden old hit replayed on resume");
                typeof(PlayerStatsManager).GetField("_health", Private).SetValue(player, 0f); Refresh(hud);
                Require(!Root(hud).gameObject.activeSelf, "dead HUD visible"); player.RestoreRecoveryHealth(player.MaxHealth);
                stats.BloodPactRank = 0; cards.Stats = stats; Refresh(hud);
                Require(!Root(hud).Find("BloodPact").gameObject.activeSelf, "unowned card visible");
                foreach (string lang in new[] { "Ru", "En" })
                {
                    LocalizationManager.Instance.ChangeLanguage(lang); Refresh(hud);
                    Require(!Text(hud, BoostType.BattleRhythm).Contains("{"), "text parameter unresolved");
                    Require(!CombatCardStatusText.Status(BoostType.BloodPact, 1, 1, .001).Contains("+0 HP"), "tiny actual heal rounds to zero");
                }
                stats.BloodPactRank = 1; cards.RestoreRecovery(stats, 0); Refresh(hud);
                Require(CombatCards.HealingTotal == 0 && CombatCards.ThirdHitRevision == 0 && Text(hud, BoostType.BattleRhythm).Contains("0/3"), "recovery presentation not reset");
                Require(hud.GetComponentsInChildren<MMF_Player>(true).Length == players && Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length == cameras,
                    "status updates create players/cameras");
                LoadingManager.Instance.LoadLocation(Location.Lobby, default, false);
                await Until(() => LoadingManager.Instance.CurrentLocation == Location.Lobby && LevelUpUi.Instance != null);
                Refresh(LevelUpUi.Instance.GetComponent<CombatCardHud>());
                Require(!Root(LevelUpUi.Instance.GetComponent<CombatCardHud>()).gameObject.activeSelf, "return leaked combat HUD");
                Require(Object.FindObjectsByType<CombatCardHud>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1, "return duplicated HUD");
                Result = "PASS";
                Debug.Log("[Combat card HUD] PASS: real Lobby/Forest/return, local owned rows, actual heal/coalescing, combo/miss/timeout/confirmed third, HP threshold, pause/ad/death, recovery reset, RU/EN, stable players/cameras. Editor only.");
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError("[Combat card HUD] " + Result); }
            finally
            {
                SharedAdProtection.End(); if (LocalizationManager.Instance != null) LocalizationManager.Instance.ChangeLanguage(language);
                if (victim != null) Object.Destroy(victim); running = false;
            }
        }
        private static async Task Until(Func<bool> check)
        {
            double end = Time.realtimeSinceStartupAsDouble + 90;
            while (!check()) { Require(Time.realtimeSinceStartupAsDouble < end, "scene transition timeout"); Time.timeScale = 1; await Task.Delay(100); }
        }
    }
}
