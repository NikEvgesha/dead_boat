using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public static class SharedRecoveryHangProbe
    {
        public static string Result { get; private set; } = "not run";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Prepare()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Online/SmokeTest/RecoveryProtocolProbe.unity");
            if (scene == null) throw new InvalidOperationException("RecoveryProtocolProbe scene missing");
            SharedRewardGameplayProbe.Prepare();
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = scene;
            return "prepared empty recovery test with owner keys backed up";
        }
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            float scale = Time.timeScale;
            try
            {
                Require(LoadingManager.Instance == null && !SharedRunContext.Active, "empty isolated scene required");
                var recovery = SharedRunRecovery.Instance;
                Set(recovery, "ticket", new SharedReturnTicket { Room = "test-only", PlayerId = 9 });
                Set(recovery, "fallback", new SharedSoloSnapshot { SourceRun = "test-only", PlayerId = 9 });
                Set(recovery, "lost", true);
                Time.timeScale = 0;
                var oldReply = new TaskCompletionSource<bool>();
                int first = (int)Call(recovery, "BeginCheck");
                var wait = (Task)Call(recovery, "AwaitCheck", oldReply.Task, first, .25, "injected stalled join");
                Require(Time.timeScale == 1 && SharedRunRecovery.NeedsTransportClock, "network frames run from paused entry");
                await Until(() => !recovery.Busy, "realtime timeout while paused");
                await Canceled(wait);
                Require(recovery.CanRetry && recovery.CanSolo, "timeout retains return ticket and solo snapshot");
                await Task.Delay(100);
                Require(Time.timeScale == 0, "lost gameplay pauses again after timeout");
                var ui = recovery.GetComponent<SharedRunRecoveryUI>();
                Require(Button(ui, "resume").interactable && Button(ui, "solo").interactable && Button(ui, "exit").interactable,
                    "timeout unlocks all available buttons");

                var nextReply = new TaskCompletionSource<bool>();
                int second = (int)Call(recovery, "BeginCheck");
                var nextWait = (Task)Call(recovery, "AwaitCheck", nextReply.Task, second, 5d, "retry after timeout");
                oldReply.SetResult(true);
                Call(recovery, "EndCheck", first);
                Require(recovery.Busy, "old finally cannot unlock new attempt");
                await Task.Delay(100);
                Require(Button(ui, "exit").interactable && !Button(ui, "resume").interactable,
                    "cancel is available during connection");
                recovery.CancelCheck();
                await Canceled(nextWait);
                nextReply.SetResult(true);
                Require(!recovery.Busy && recovery.CanRetry && recovery.CanSolo, "manual cancellation retains choices");
                int third = (int)Call(recovery, "BeginCheck");
                await (Task)Call(recovery, "AwaitCheck", Task.CompletedTask, third, 5d, "successful retry");
                Call(recovery, "EndCheck", third);
                Require(!recovery.Busy, "subsequent completed attempt unlocks");
                Result = "PASS: paused entry, network clock, stalled timeout, pause restoration, UI unlock/cancel, preserved ticket/solo, late reply/finally isolation, successful retry";
                Debug.Log("[Recovery hang probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Recovery hang probe] " + Result); }
            finally { Time.timeScale = scale; }
        }
        private static Button Button(SharedRunRecoveryUI ui, string field) => (Button)typeof(SharedRunRecoveryUI).GetField(field, Private).GetValue(ui);
        private static void Set(SharedRunRecovery recovery, string field, object value) => typeof(SharedRunRecovery).GetField(field, Private).SetValue(recovery, value);
        private static object Call(SharedRunRecovery recovery, string method, params object[] args) => typeof(SharedRunRecovery).GetMethod(method, Private).Invoke(recovery, args);
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        private static async Task Canceled(Task task)
        {
            try { await task; }
            catch (OperationCanceledException) { return; }
            throw new InvalidOperationException("Stale attempt was not canceled");
        }
        private static async Task Until(Func<bool> check, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 3;
            while (!check())
            {
                if (Time.realtimeSinceStartupAsDouble >= end) throw new TimeoutException(label);
                await Task.Delay(50);
            }
        }
    }
}
