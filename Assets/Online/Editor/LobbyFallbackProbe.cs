using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class LobbyFallbackProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Result { get; private set; } = "not run";

        public static async Task CheckWaits()
        {
            var late = new TaskCompletionSource<bool>();
            var wait = new LobbyConnectionWait();
            var pending = wait.WaitAsync(late.Task, TimeSpan.FromSeconds(20));
            wait.Cancel();
            Require(!await pending && wait.IsCanceled, "cancel does not wait SDK timeout");
            late.SetResult(true);
            Require(!pending.Result, "late completion cannot revive canceled wait");
            var next = new LobbyConnectionWait();
            Require(await next.WaitAsync(Task.CompletedTask, TimeSpan.FromSeconds(1)), "new independent attempt succeeds");
            var hung = new TaskCompletionSource<bool>();
            Require(!await next.WaitAsync(hung.Task, TimeSpan.FromMilliseconds(30)), "bounded timeout");
            hung.SetException(new InvalidOperationException("expected late test fault"));
            await Task.Delay(30);
        }
        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            EditorApplication.CallbackFunction clock = () => Time.timeScale = 1;
            EditorApplication.update += clock;
            try
            {
                Require(SaveManager.Instance?.PersistentProvider is SharedRunPreviewSave, "isolated save required");
                await Until(() => LoadingManager.Instance?.CurrentLocation == Location.Lobby, "lobby");
                await CheckWaits();
                var owner = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                var save = SaveManager.Instance.PersistentProvider;
                save.SaveDistance(987); save.SaveLevelId(0); save.SaveAmmo(WeaponType.Pistol, 31);
                save.SaveGameCoin(77); save.SaveLevelUpdate(new Stats { Save = true, HP = 25 });
                PlayerAmmoManager.Instance.RestoreRecovery(31, 0, 0); // Keep the in-memory fixture consistent with its saved reserve.
                for (int i = 0; i < 3; i++)
                {
                    owner.SelectOnline();
                    var pending = (LobbyConnectionWait)Get(owner, "connectionWait");
                    owner.SelectOffline();
                    Require(pending == null || pending.IsCanceled, "offline signals pending connection");
                    await Until(() => !(bool)Get(owner, "connecting") && !(bool)Get(owner, "stopping"), "cancel cleanup");
                    Require(owner.Mode == LobbyOnlineMode.Offline && owner.Failure == LobbyConnectionFailure.None &&
                        !NetworkRunner.Instances.Any(r => r != null && r.IsConnectedToServer), "offline not undone by late result");
                    CheckSave(save);
                }
                owner.SelectOnline(); owner.SelectOffline(); owner.SelectOnline();
                await Until(() => owner.IsOnline && !owner.IsConnecting, "queued re-online after cancel");
                Require(NetworkRunner.Instances.Count(r => r != null && r.IsConnectedToServer) == 1, "single connection after repeated input");
                CheckSave(save);
                owner.SelectOffline();
                await Until(() => !(bool)Get(owner, "connecting") && !(bool)Get(owner, "stopping"), "leave visual lobby");
                // Start a genuine named-room operation, then cancel and queue ordinary online.
                typeof(LobbyOnlineBootstrap).GetField("mode", Private).SetValue(owner, LobbyOnlineMode.Online);
                var friend = owner.JoinFriendLobbyAsync("fallback-cancel-" + Guid.NewGuid().ToString("N"), "eu", true, true);
                owner.SelectOffline(); owner.SelectOnline();
                Require(!await friend, "canceled friend join does not report success");
                await Until(() => owner.IsOnline && !owner.IsConnecting, "friend finally drains queued online request");
                CheckSave(save);
                var runner = (NetworkRunner)Get(owner, "runner");
                await runner.Shutdown();
                await Until(() => !owner.IsOnline && !(bool)Get(owner, "stopping"), "transport loss fallback");
                Require(LoadingManager.Instance.CurrentLocation == Location.Lobby && owner.Failure == LobbyConnectionFailure.Other,
                    "transport loss leaves usable lobby, not a silent new run");
                CheckSave(save);
                owner.SelectOffline();
                await Until(() => !(bool)Get(owner, "connecting") && !(bool)Get(owner, "stopping"), "offline before resume");
                LoadingManager.Instance.LoadLocation(Location.Game, "LevelForest", false);
                await Until(() => LoadingManager.Instance.CurrentLocation == Location.Game && BoardController.Instance != null &&
                    BoardController.Instance.StartGame, "existing solo resume path");
                Require(!SharedRunContext.Active && !NetworkRunner.Instances.Any(r => r != null && r.IsConnectedToServer), "resumed solo without Photon");
                Require(BoardController.Instance.TotalDistanceTraveled >= 980 && PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol) == 31,
                    "saved solo distance/ammo restored");
                Result = "PASS: canceled/late/timeout waits, three real connection cancels, queued online and friend cancel retry, one runner, actual transport shutdown fallback, preserved solo save and real solo resume without Photon. Editor only; no network cable outage.";
                Debug.Log("[Lobby fallback probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError("[Lobby fallback probe] " + Result); }
            finally
            {
                var owner = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                if (owner != null) try { await (Task)typeof(LobbyOnlineBootstrap).GetMethod("StopRunnerAsync", Private).Invoke(owner, null); } catch (Exception) { }
                EditorApplication.update -= clock;
            }
        }
        private static void CheckSave(SaveProvider save)
        {
            Require(save.LoadDistance() == 987 && save.LoadAmmo(WeaponType.Pistol) == 31 && save.LoadGameCoin() == 77 &&
                save.LoadLevelUpdate().HP == 25, "connection action must preserve solo progress/ammo/coins/cards");
        }
        private static object Get(object owner, string name) => owner.GetType().GetField(name, Private).GetValue(owner);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> check, string message)
        {
            double end = Time.realtimeSinceStartupAsDouble + 90;
            while (!check()) { Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + message); await Task.Delay(100); }
        }
    }
}
