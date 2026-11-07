using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class SharedReviveCallbackProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static FieldInfo Static(Type t, string n) => t.GetField(n, BindingFlags.Static | BindingFlags.NonPublic);
        private static void Set(object o, string n, object v) => o.GetType().GetField(n, Private).SetValue(o, v);
        private static T Get<T>(object o, string n) => (T)o.GetType().GetField(n, Private).GetValue(o);
        private static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, Private).Invoke(o, args);
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        private static int Begin(EndGameUIManager ui)
        {
            object[] args = { 0 }; Require((bool)Call(ui, "TryBeginRevive", args), "begin revive"); return (int)args[0];
        }
        private static bool Accept(EndGameUIManager ui, int id, int revision, bool success, bool shared, GameManager game) =>
            (bool)Call(ui, "TryAcceptRevive", id, revision, success, shared, null, game);

        public static void Run()
        {
            Require(!EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer, "idle Editor");
            var active = Static(typeof(SharedRunContext), "<Active>k__BackingField");
            var state = Static(typeof(SharedRunContext), "<State>k__BackingField");
            var gameInstance = Static(typeof(GameManager), "_instance");
            var adsInstance = Static(typeof(AdsManager), "_instance");
            object oldActive = active.GetValue(null), oldState = state.GetValue(null);
            object oldGame = gameInstance.GetValue(null), oldAds = adsInstance.GetValue(null);
            var adFields = new Dictionary<FieldInfo, object>();
            foreach (var name in new[] { "showing", "deadline", "until", "token", "run" })
            {
                var field = Static(typeof(SharedAdProtection), name); adFields.Add(field, field.GetValue(null));
            }
            var root = new GameObject("Temporary revive callback safety");
            try
            {
                active.SetValue(null, false); state.SetValue(null, null);
                Static(typeof(SharedAdProtection), "showing").SetValue(null, false);
                var game = root.AddComponent<GameManager>(); gameInstance.SetValue(null, game);
                var ui = root.AddComponent<EndGameUIManager>();
                var buttonGo = new GameObject("Revive", typeof(RectTransform), typeof(Button)); buttonGo.transform.SetParent(root.transform);
                var button = buttonGo.GetComponent<Button>(); Set(ui, "reviveButton", button);
                Set(ui, "currentState", EndGameState.Faint); Set(ui, "_presentationRevision", 10);
                int request = Begin(ui);
                object[] duplicate = { 0 };
                Require(!(bool)Call(ui, "TryBeginRevive", duplicate) && !button.interactable, "pending blocks duplicate click before provider/ACK");
                Call(ui, "SetButtonsInteractable", true);
                Require(!button.interactable, "enable-delay callback cannot reenable pending revive");
                ui.ShowEndGameUI(EndGameState.Faint);
                Require(Get<int>(ui, "_presentationRevision") == 10 && Get<bool>(ui, "_revivePending"), "duplicate faint does not reset request/timer");
                Require(Accept(ui, request, 10, true, false, game) && !Accept(ui, request, 10, true, false, game), "valid result is consumed once");
                request = Begin(ui); Require(!Accept(ui, request, 10, false, false, game) && button.interactable, "failure allows retry");
                int retry = Begin(ui);
                Require(retry != request && !Accept(ui, request, 10, true, false, game) && Get<bool>(ui, "_revivePending"), "old success cannot consume retry");
                Require(Accept(ui, retry, 10, true, false, game), "retry accepts its own success");
                request = Begin(ui); Call(ui, "InvalidateRevive"); Set(ui, "_presentationRevision", 11);
                int nextDeath = Begin(ui);
                Require(!Accept(ui, request, 10, true, false, game) && Get<bool>(ui, "_revivePending") && Accept(ui, nextDeath, 11, true, false, game), "old death cannot revive a new faint");
                request = Begin(ui); Set(ui, "currentState", EndGameState.Win);
                Require(!Accept(ui, request, 11, true, false, game), "victory rejects revive");
                Set(ui, "currentState", EndGameState.Faint); request = Begin(ui); active.SetValue(null, true);
                Require(!Accept(ui, request, 11, true, false, game), "offline callback cannot enter new shared run");
                duplicate[0] = 0;
                Require(!(bool)Call(ui, "TryBeginRevive", duplicate), "disconnected/loading shared context cannot request ads");
                active.SetValue(null, false); request = Begin(ui);
                Require(!Accept(ui, request, 11, true, true, game), "old shared callback cannot revive after transition to solo");
                request = Begin(ui); game.isEndGame = true;
                Require(!Accept(ui, request, 11, true, false, game), "explicit exit rejects result"); game.isEndGame = false;
                request = Begin(ui); var other = root.AddComponent<GameManager>(); gameInstance.SetValue(null, other);
                Require(!Accept(ui, request, 11, true, false, game), "new GameManager rejects old run callback"); gameInstance.SetValue(null, game);
                Begin(ui); Call(ui, "OnDisable"); Require(button.interactable && !Get<bool>(ui, "_revivePending"), "disable releases pending UI and invalidates request");

                // Actual button -> AdsManager -> fake provider -> failure/stale success. Never a real ad or heal.
                var ads = root.AddComponent<AdsManager>(); adsInstance.SetValue(null, ads);
                var provider = root.AddComponent<SharedProbeAdsProvider>(); Set(ads, "adsProviders", new List<AdsProvider> { provider });
                ui.OnReviveButtonClicked(); ui.OnReviveButtonClicked();
                Require(provider.requests == 1 && Get<bool>(ui, "_revivePending"), "actual button submits once");
                provider.callback(false);
                Require(!Get<bool>(ui, "_revivePending") && button.interactable, "actual provider failure releases UI");
                provider.callback(true); Require(ui.GetState() == EndGameState.Faint, "provider duplicate success after failure cannot revive");
                ui.OnReviveButtonClicked(); Set(ui, "currentState", EndGameState.Win); provider.callback(true);
                Require(ui.GetState() == EndGameState.Win && !Get<bool>(ui, "_revivePending"), "actual late ad success preserves victory");
                Debug.Log("[Revive callback safety] PASS: once/fail/retry/new faint/duplicate faint/UI delay/mode/game/exit/disable; actual AdsManager fake-provider failure+late Win. Valid gate tested without actual healing; no Play/live Photon/real ad/user save/Build");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                gameInstance.SetValue(null, oldGame); adsInstance.SetValue(null, oldAds);
                active.SetValue(null, oldActive); state.SetValue(null, oldState);
                foreach (var pair in adFields) pair.Key.SetValue(null, pair.Value);
            }
            SharedVictoryPresentationProbe.Run();
        }
    }
}
