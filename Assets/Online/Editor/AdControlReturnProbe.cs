using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class AdControlReturnProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static FieldInfo Static(Type t, string n) => t.GetField(n, BindingFlags.Static | BindingFlags.NonPublic);
        private static void Set(object o, string n, object value) => o.GetType().GetField(n, Private).SetValue(o, value);
        private static T Get<T>(object o, string n) => (T)o.GetType().GetField(n, Private).GetValue(o);
        private static void Call(object o, string n) => o.GetType().GetMethod(n, Private).Invoke(o, null);
        private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        public static void Run()
        {
            Require(!EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer, "idle Editor");
            var statics = new Dictionary<FieldInfo, object>();
            void Backup(Type t, string name) { var f = Static(t, name); statics.Add(f, f.GetValue(null)); }
            Backup(typeof(AdsManager), "_instance"); Backup(typeof(ControlManager), "_instance"); Backup(typeof(PauseManager), "_instance");
            Backup(typeof(SharedRunContext), "<Active>k__BackingField"); Backup(typeof(SharedRunContext), "<State>k__BackingField");
            foreach (string name in new[] { "showing", "deadline", "until", "token", "run" }) Backup(typeof(SharedAdProtection), name);
            var root = new GameObject("Temporary ad controls probe");
            float scale = Time.timeScale; bool audio = AudioListener.pause;
            try
            {
                Static(typeof(SharedRunContext), "<Active>k__BackingField").SetValue(null, false);
                Static(typeof(SharedRunContext), "<State>k__BackingField").SetValue(null, null);
                Static(typeof(SharedAdProtection), "showing").SetValue(null, false);
                var device = root.AddComponent<SharedProbeDeviceProvider>();
                var pauseProvider = root.AddComponent<SharedProbePauseProvider>();
                var control = root.AddComponent<ControlManager>(); Set(control, "_provider", device);
                var pause = root.AddComponent<PauseManager>(); Set(pause, "_provider", pauseProvider);
                var ads = root.AddComponent<AdsManager>();
                Static(typeof(ControlManager), "_instance").SetValue(null, control);
                Static(typeof(PauseManager), "_instance").SetValue(null, pause);
                Static(typeof(AdsManager), "_instance").SetValue(null, ads);
                var provider = root.AddComponent<SharedProbeAdsProvider>(); Set(ads, "adsProviders", new List<AdsProvider> { provider });
                int completed = 0;
                void Start() => ads.ShowRewardedAd("control-probe", _ => completed++);
                void Idle() => Require(!Get<bool>(ads, "active") && !SharedAdProtection.IsShowing && !Get<bool>(pause, "_adPause") && !Get<bool>(control, "_adCursorActive"), "request-owned state released");
                void OpenMenu() { control.CursorActive = true; pause.SetPause(true); }
                void CloseMenu() { control.CursorActive = false; pause.SetPause(false); }
                OpenMenu();
                for (int i = 0; i < 10; i++)
                {
                    Start(); Require(Get<int>(control, "_activeWindows") == 1 && control.CursorActive && control.BlocksPrimaryAction, "ad overlay does not add a menu");
                    provider.CompleteTwice(); Idle();
                    Require(Get<int>(control, "_activeWindows") == 1 && control.CursorActive && pause.IsPaused, "existing menu survives close without cursor leak");
                }
                Require(completed == 10, "one completion per show"); CloseMenu();
                Require(!control.CursorActive && !pause.IsPaused && device.Lock == CursorLockMode.Locked, "last menu close returns gameplay");
                Start(); OpenMenu(); provider.callback(true); Idle();
                Require(control.CursorActive && pause.IsPaused, "menu opened during ad survives"); CloseMenu();
                OpenMenu(); Start(); CloseMenu();
                Require(control.CursorActive && pause.IsPaused, "ad holds only its own overlay while menu closes");
                provider.callback(false); Idle(); Require(!control.CursorActive && !pause.IsPaused, "no stale pre-ad menu snapshot restored");
                OpenMenu(); Start(); control.ForceGameplayCursor(); pause.SetPause(false); OpenMenu();
                provider.callback(true); Idle(); Require(Get<int>(control, "_activeWindows") == 1 && control.CursorActive, "force reset/new menu not consumed by ad release"); CloseMenu();
                Start(); Set(control, "_useTouchControls", true); provider.callback(false); Idle();
                Require(!control.CursorActive, "touch detection during ad cannot retain desktop cursor overlay");
                Set(control, "_useTouchControls", false); control.ForceGameplayCursor();

                int before = completed;
                ads.AdClosed = () => throw new InvalidOperationException("injected observer");
                int closed = 0, shown = 0;
                ads.AdClosed += () => closed++;
                ads.ShownAdClosed += _ => throw new InvalidOperationException("injected shown observer");
                ads.ShownAdClosed += _ => shown++;
                Action<bool> callbacks = _ => throw new InvalidOperationException("injected completion"); callbacks += _ => completed++;
                ads.ShowRewardedAd("callbacks", callbacks);
                typeof(AdsProvider).GetMethod("NotifyOpened", Private).Invoke(provider, null);
                provider.callback(true); Idle();
                Require(completed == before + 1 && closed == 1 && shown == 1, "failing subscribers do not swallow completion/later observers");
                ads.AdClosed = null; Set(ads, "ShownAdClosed", null);
                provider.throwOnReady = true; before = completed; int requests = provider.requests;
                Require(!ads.IsRewardedAdReady(), "readiness exception reported as unavailable"); Start(); Idle();
                Require(completed == before + 1 && provider.requests == requests, "readiness failure completes false without opening"); provider.throwOnReady = false;
                provider.throwOnShow = true; before = completed; Start(); Idle(); Require(completed == before + 1, "show exception releases and completes"); provider.throwOnShow = false;
                Start(); var stale = provider.callback; before = completed;
                Set(ads, "deadline", Time.realtimeSinceStartupAsDouble - 1); Call(ads, "Update"); Idle();
                Require(completed == before + 1 && SharedAdProtection.Protected, "watchdog releases with grace");
                Static(typeof(SharedAdProtection), "until").SetValue(null, Time.realtimeSinceStartupAsDouble - 1);
                Require(!SharedAdProtection.Protected, "grace has finite deadline (accelerated clock)");
                Start(); stale(true); Require(Get<bool>(ads, "active"), "old provider result cannot finish new show"); provider.callback(false); Idle();

                Start(); pauseProvider.FailNextResume = true; device.FailNextLock = true; before = completed;
                provider.callback(false); Idle(); Require(completed == before + 1, "cleanup adapter errors cannot swallow result");
                pause.SetAdPause(false); control.SetAdCursor(false); // Recover intentionally broken fake adapters.
                int nestedRejected = 0;
                pauseProvider.BeforeSet = paused => { if (!paused) ads.ShowRewardedAd("during-cleanup", ok => { if (!ok) nestedRejected++; }); };
                Start(); provider.callback(false); Idle(); pauseProvider.BeforeSet = null;
                Require(nestedRejected == 1, "new request rejected during partial cleanup");
                int chained = 0;
                ads.AdClosed = () => { ads.AdClosed = null; ads.ShowRewardedAd("after-cleanup", _ => chained++); };
                Start(); var previous = provider.callback; previous(true);
                Require(Get<bool>(ads, "active") && SharedAdProtection.IsShowing && control.CursorActive && pause.IsPaused, "callback may start next ad after cleanup without old state overwrite");
                previous(true); Require(Get<bool>(ads, "active"), "duplicate old completion leaves chained show alone"); provider.callback(false); Idle(); Require(chained == 1, "chained request completes once");
                Start(); ads.enabled = false; Call(ads, "OnDisable"); Idle();
                before = completed; requests = provider.requests; Start(); Require(completed == before + 1 && provider.requests == requests, "disabled manager rejects new show and completes false"); ads.enabled = true;
                Require(Time.timeScale == scale && AudioListener.pause == audio, "fake adapters did not alter editor time/audio");
                Debug.Log("[Ad control return] PASS: menu10cycles/open/close/force reset/touch detection; cursor overlay; observer/readiness/show/cleanup errors; watchdog/stale/grace; cleanup/chained requests; disable. Fake pointer/pause/ad, no live Photon/WebGL/real ad/user save/Play/Build");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (var pair in statics) pair.Key.SetValue(null, pair.Value);
                Time.timeScale = scale; AudioListener.pause = audio;
            }
            SharedReviveCallbackProbe.Run();
        }
    }
}
