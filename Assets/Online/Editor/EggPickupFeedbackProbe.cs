using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class EggPickupFeedbackProbe
    {
        public static string Result { get; private set; } = "not run";
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            float scale = Time.timeScale;
            try
            {
                Require(LoadingManager.Instance == null && !SharedRunContext.Active, "empty isolated scene");
                var catalog = Resources.Load<EggHatchingCatalog>("Eggs/EggHatchingCatalog");
                string eggId = catalog.Definitions.First(e => e != null).eggId;
                var state = new EggFeatureState();
                var first = EggPickupReceipt.Apply(state, eggId, 1);
                Require(first.FirstDiscovery && first.Amount == 1 && first.Total == 1, "first pickup receipt");
                var second = EggPickupReceipt.Apply(state, eggId, 2);
                Require(!second.FirstDiscovery && second.Total == 3 && state.GetEggAmount(eggId) == 3, "repeat count");
                var unique = EggPickupReceipt.Apply(state, eggId, 1, "probe-one-shot");
                Require(unique.Total == 4 && EggPickupReceipt.Apply(state, eggId, 1, "probe-one-shot").Amount == 0 &&
                    state.GetEggAmount(eggId) == 4, "one-shot replay does not count");
                Require(EggPickupReceipt.Apply(state, "", 1).Amount == 0 && EggPickupReceipt.Apply(state, eggId, 0).Amount == 0,
                    "invalid pickup rejected");
                var restored = JsonUtility.FromJson<EggFeatureState>(JsonUtility.ToJson(state));
                Require(!EggPickupReceipt.Apply(restored, eggId, 1).FirstDiscovery, "hint flag survives existing save format");
                string snapshot = JsonUtility.ToJson(state);
                Time.timeScale = 0;
                EggPickupFeedback.Show(first);
                await Task.Delay(250);
                var ui = UnityEngine.Object.FindAnyObjectByType<EggPickupFeedback>();
                Require(ui != null && ui.IsVisible && ui.ShowsFirstHint && Time.timeScale == 0, "visible on unscaled clock without pause mutation");
                Require(!ui.GetComponent<CanvasGroup>().blocksRaycasts && !ui.GetComponent<CanvasGroup>().interactable,
                    "notification cannot capture input");
                EggPickupFeedback.Show(second);
                Require(ui.DisplayedGain == 3 && ui.DisplayedTotal == 3 && ui.ShowsFirstHint, "burst coalesces and keeps first hint");
                var remaining = typeof(EggPickupFeedback).GetField("remaining", BindingFlags.Instance | BindingFlags.NonPublic);
                SharedAdProtection.Begin();
                float before = (float)remaining.GetValue(ui);
                await Task.Delay(200);
                Require(!ui.IsVisible && (float)remaining.GetValue(ui) >= before - .1f, "ad suspends notification lifetime");
                SharedAdProtection.End();
                remaining.SetValue(ui, .01f);
                await Task.Delay(150);
                Require(!ui.IsVisible, "notification expires");
                EggPickupFeedback.Show(second);
                Require(ui.DisplayedGain == 2 && !ui.ShowsFirstHint, "later pickup has no repeated tutorial");
                Require(JsonUtility.ToJson(state) == snapshot, "UI does not mutate stored state");
                remaining.SetValue(ui, .01f);
                await Task.Delay(150);
                EggPickupFeedback.Show(first); // Keep the isolated first-pickup preview visible for QA.
                var camera = new GameObject("Egg pickup preview camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .15f, .2f);
                Result = "PASS: local receipts/count/one-shot, existing save flag, burst, first hint, unscaled/input-free UI, ad suspension; no storage reads or writes";
                Debug.Log("[Egg pickup probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Egg pickup probe] " + Result); }
            finally { SharedAdProtection.End(); Time.timeScale = scale; }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
