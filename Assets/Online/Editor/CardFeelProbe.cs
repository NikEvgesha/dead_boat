using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class CardFeelProbe
    {
        public static string Result { get; private set; } = "not run";
        private static bool running;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        private static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
        private static void Complete(LevelUpButtomUI button, int version, BoostItem card, RareType rarity, bool success) =>
            typeof(LevelUpButtomUI).GetMethod("CompleteChoice", Private).Invoke(button, new object[] { version, card, rarity, success });
        private static List<ItemWithRare> Offers() => new List<ItemWithRare>
        {
            new ItemWithRare { Item = AssetDatabase.LoadAssetAtPath<BoostItem>("Assets/Scripts/LevelStat/Boosters/BoostItem_HP.asset"), Rare = RareType.Rare },
            new ItemWithRare { Item = Resources.Load<BoostItem>("BoostItem_BattleRhythm"), Rare = RareType.Uncommon },
            new ItemWithRare { Item = Resources.Load<BoostItem>("BoostItem_OnTheEdge"), Rare = RareType.Rare }
        };
        public static async void Run()
        {
            if (running) return;
            Require(EditorApplication.isPlaying && SaveManager.Instance?.PersistentProvider is SharedRunPreviewSave, "isolated real Lobby required");
            running = true; Result = "running";
            var ui = LevelUpUi.Instance;
            var manager = AdsManager.Instance;
            object providers = Field(manager, "adsProviders");
            var mockObject = new GameObject("Temporary card feedback ads");
            var mock = mockObject.AddComponent<SharedProbeAdsProvider>();
            string language = LocalizationManager.Instance.CurrentLanguage;
            try
            {
                Set(manager, "adsProviders", new List<AdsProvider> { mock });
                var stats = LevelStatManager.Instance;
                stats.RestoreRecovery(stats.StatsDefault, 2);
                int cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
                int sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length;
                ui.NewLevel(Offers()); Time.timeScale = 0;
                await Task.Delay(350);
                var buttons = ui.GetComponentsInChildren<LevelUpButtomUI>(true);
                Require(buttons.Length == 3, "real three card prefab");
                var effects = buttons.Select(b => b.GetComponent<CardFeelFeedback>()).ToArray();
                Require(effects.All(f => f != null), "feedback attached");
                int players = ui.GetComponentsInChildren<MMF_Player>(true).Length;
                int graphics = ui.GetComponentsInChildren<Graphic>(true).Length;
                Require(players == 7, "two FEEL players per card + one confirmation");
                foreach (var b in buttons)
                {
                    var target = b.transform.Find("Point/LevelUpElement");
                    Require(target != null && target.parent.name == "Point", "Animator paths changed");
                    Require(Vector3.Distance(target.localScale, Vector3.one) < .01f, "entry did not settle at timeScale0");
                    Require(Mathf.Abs(target.GetComponent<CanvasGroup>().alpha - 1) < .01f, "entry opacity not restored");
                }
                Canvas.ForceUpdateCanvases();
                double inputDeadline = Time.realtimeSinceStartupAsDouble + 5;
                while (!buttons[0].isActiveAndEnabled || !buttons[0].GetComponent<Button>().IsInteractable())
                {
                    Require(Time.realtimeSinceStartupAsDouble < inputDeadline, "card input not ready"); await Task.Delay(25);
                }
                var random = JsonUtility.ToJson(UnityEngine.Random.state);
                var press = (MMF_Player)Field(effects[0], "pulse"); int plays = press.PlayCount;
                ExecuteEvents.Execute(buttons[0].gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerDownHandler);
                Require(press.PlayCount == plays + 1, "pointer press did not start FEEL");
                Require(JsonUtility.ToJson(UnityEngine.Random.state) == random, "visual changed world RNG");
                ExecuteEvents.Execute(buttons[0].gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerUpHandler);
                EventSystem.current.SetSelectedGameObject(null);
                await Task.Delay(300);
                Require(Vector3.Distance(buttons[0].transform.Find("Point/LevelUpElement").localScale, Vector3.one) < .01f, "press did not restore scale");
                for (int i = 0; i < 30; i++) ui.RefreshLevel(Offers());
                await Task.Delay(300);
                Require(ui.GetComponentsInChildren<MMF_Player>(true).Length == players && ui.GetComponentsInChildren<Graphic>(true).Length == graphics,
                    "reroll grows feedback objects");
                SharedAdProtection.Begin(); await Task.Delay(80);
                Require(!ui.GetComponentsInChildren<MMF_Player>(true).Any(p => p.IsPlaying), "FEEL still playing behind ad");
                SharedAdProtection.End();

                // Actual Button -> AdsManager -> fake provider -> gameplay callback, without a platform ad.
                var first = buttons[0]; var chosen = Offers()[0].Item;
                Set(first, "_isAds", true);
                first.GetComponent<Button>().onClick.Invoke();
                int version = (int)Field(first, "_offerVersion");
                int gems = CurrencyManager.Instance.Gems;
                ui._RefreshClickGems();
                buttons[1].GetComponent<Button>().onClick.Invoke();
                Require(mock.requests == 1 && CurrencyManager.Instance.Gems == gems && stats.Stats.HP == 0, "pending ad permits another claim/spend");
                mock.callback(false);
                Require(stats.Stats.HP == 0 && ui.TryClaimCard(), "failed ad grants or leaves claim locked"); ui.ReleaseCardClaim();
                first.GetComponent<Button>().onClick.Invoke();
                mock.CompleteTwice();
                float expected = chosen.RareRewards.Find(r => r.Rare == RareType.Rare).Reward;
                Require(stats.Stats.HP == expected, "duplicate success did not grant exactly once");
                Require(!ui.TryClaimCard(), "double click passes into the next offer");
                Complete(first, version, chosen, RareType.Rare, true);
                Require(stats.Stats.HP == expected, "stale callback grants again");
                var confirmation = ui.GetComponent<CardChoiceFeedback>();
                Require(confirmation != null && confirmation.enabled, "confirmation did not survive panel close");
                var toast = (RectTransform)Field(confirmation, "visual");
                Require(toast.anchorMin.x > .8f, "confirmation covers the next card title");
                await Task.Delay(650);
                Require(!confirmation.enabled, "confirmation has idle update loop");

                // Close during a new ad, reopen, then deliver the old success.
                stats.RestoreRecovery(stats.StatsDefault, 2); ui.NewLevel(Offers());
                Set(first, "_isAds", true); first.GetComponent<Button>().onClick.Invoke();
                var oldCallback = mock.callback;
                ((GameObject)Field(ui, "_panel")).SetActive(false);
                ui.NewLevel(Offers()); oldCallback(true);
                Require(stats.Stats.HP == 0, "closed offer callback affected reopened UI");
                for (int i = 0; i < 10; i++) { ((GameObject)Field(ui, "_panel")).SetActive(false); ui.NewLevel(Offers()); }
                await Task.Delay(350);
                Require(ui.GetComponentsInChildren<MMF_Player>(true).Length == players, "reopen grows players");
                Require(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length == cameras &&
                    Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length == sources, "effect added camera or audio source");
                foreach (string lang in new[] { "Ru", "En" })
                {
                    LocalizationManager.Instance.ChangeLanguage(lang); ui.RefreshLevel(Offers());
                    Require(buttons.All(b => !string.IsNullOrWhiteSpace(((Text)Field(b, "_description")).text)), "localized description missing");
                }
                Result = "PASS";
                Debug.Log("[Card FEEL] PASS: real prefab, paused entry/press, RNG isolation, 30 rerolls/10 reopens stable7 players, ad gating, failed/duplicate/stale callbacks, no extra cameras/audio sources, RU/EN. Editor only.");
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError("[Card FEEL] " + Result); }
            finally
            {
                if (SharedAdProtection.IsShowing) mock.callback?.Invoke(false);
                SharedAdProtection.End(); Set(manager, "adsProviders", providers);
                LocalizationManager.Instance.ChangeLanguage(language);
                Object.Destroy(mockObject); running = false;
            }
        }
    }
}
