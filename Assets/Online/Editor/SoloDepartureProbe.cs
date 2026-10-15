using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadBoat.Online.Editor
{
    public static class SoloDepartureProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static string Result { get; private set; } = "not run";

        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            EditorApplication.CallbackFunction clock = () => Time.timeScale = 1;
            EditorApplication.update += clock;
            bool sceneLoadedWithPhoton = false;
            int gameLoads = 0;
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneWatch = (scene, mode) =>
            {
                if (!scene.name.StartsWith("Level", StringComparison.Ordinal)) return;
                gameLoads++;
                sceneLoadedWithPhoton |= NetworkRunner.Instances.Any(r => r != null && r.IsConnectedToServer);
            };
            SceneManager.sceneLoaded += sceneWatch;
            try
            {
                Require(SaveManager.Instance?.PersistentProvider is SharedRunPreviewSave, "isolated save required");
                await Until(() => LoadingManager.Instance?.CurrentLocation == Location.Lobby, "lobby");
                var owner = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                Set(owner, "mode", LobbyOnlineMode.Online);
                PlayerPrefs.SetInt("DeadBoat.OnlineMode.v1", 1);
                var save = SaveManager.Instance.PersistentProvider;
                save.SaveDistance(321);
                // Model another caller's pending shutdown without touching SDK internals.
                var gate = new TaskCompletionSource<bool>();
                Set(owner, "stopTask", gate.Task);
                var first = owner.DisconnectForSoloRunAsync();
                var duplicate = owner.DisconnectForSoloRunAsync();
                Require(!first.IsCompleted && !duplicate.IsCompleted, "both solo callers await in-flight stop");
                gate.SetResult(true); await Task.WhenAll(first, duplicate);
                await Task.Delay(100); // allow deferred mode-UI destruction
                owner.CancelSoloRunStart();
                Require(!(bool)Get(owner, "startingSoloRun") && owner.GetComponent<LobbyOnlineModeUI>() != null,
                    "load failure restores lobby controls and retry");
                Require(save.LoadDistance() == 321, "disconnect/failure does not erase solo save");
                Require(await owner.JoinFriendLobbyAsync("solo-disconnect-" + Guid.NewGuid().ToString("N"), "eu", true, true), "hidden lobby");
                var runner = (NetworkRunner)Get(owner, "runner");
                var stop = Stop(owner);
                var otherStop = Stop(owner);
                Require(ReferenceEquals(stop, otherStop), "all callers share actual shutdown task");
                await Task.WhenAll(stop, otherStop);
                Require(runner == null && !NetworkRunner.Instances.Any(r => r != null && r.IsConnectedToServer), "actual runner removed before return");
                Require(owner.Mode == LobbyOnlineMode.Online && save.LoadDistance() == 321, "teardown preserves mode/save");

                Require(await owner.JoinFriendLobbyAsync("solo-picker-" + Guid.NewGuid().ToString("N"), "eu", true, true), "picker lobby");
                await owner.LeaveDepartureForSoloSelectionAsync();
                Require(!owner.IsOnline && !(bool)Get(owner, "departureTransition") && save.LoadDistance() == 321,
                    "opening solo picker stops transport but preserves unfinished save");

                Require(await owner.CreateDepartureAsync(0, 4), "create 4-seat waiting departure");
                var interrupted = (NetworkRunner)Get(owner, "runner");
                await interrupted.Shutdown();
                owner.LaunchDepartureNow();
                await Task.Delay(200);
                Require(LoadingManager.Instance.CurrentLocation == Location.Lobby && gameLoads == 0 && save.LoadDistance() == 321,
                    "transport loss while waiting never silently starts solo");
                await Stop(owner);

                Require(await owner.CreateDepartureAsync(0, 4), "retry departure after disconnect");
                await Until(() => owner.IsDepartureLeader, "leader");
                owner.LaunchDepartureNow(); owner.LaunchDepartureNow();
                Require(owner.IsOnline && owner.DepartureCountingDown && owner.DeparturePlayerCount == 1,
                    "one member remains connected through countdown");
                await Until(() => LoadingManager.Instance.CurrentLocation == Location.Game && BoardController.Instance != null &&
                    BoardController.Instance.StartGame, "normal solo game");
                Require(!SharedRunContext.Active && !SoloRunBranch.Active && !SharedRunRecovery.HasPending,
                    "ordinary solo, not a shared recovery/fork");
                Require(gameLoads == 1 && !sceneLoadedWithPhoton && !NetworkRunner.Instances.Any(r => r != null && r.IsConnectedToServer),
                    "exactly one scene activation after all Photon runners removed");
                Require(PlayerPrefs.GetInt("DeadBoat.OnlineMode.v1") == 1 && save.LoadDistance() >= 0 && save.LoadDistance() < 100,
                    "explicit single-member start resets old run only now and preserves online preference");
                // End only this isolated run, then verify selected lobby mode reconnects.
                GameManager.Instance.EndGame(true, false);
                await Until(() => LoadingManager.Instance.CurrentLocation == Location.Lobby, "return lobby");
                await Until(() => UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>()?.IsOnline == true, "selected online mode restored");
                var returned = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                Require(returned.Mode == LobbyOnlineMode.Online && !SharedRunContext.Active, "normal lobby return");
                await Stop(returned);
                Result = "PASS: shared pending/real shutdown, duplicate calls, failure retry, picker save, disconnect without silent solo, single-member countdown, one real solo scene without Photon, normal lobby mode return. Isolated Editor world, not WebGL.";
                Debug.Log("[Solo departure probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError("[Solo departure probe] " + Result); }
            finally
            {
                var owner = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                if (owner != null) try { await Stop(owner); } catch (Exception) { }
                SceneManager.sceneLoaded -= sceneWatch;
                EditorApplication.update -= clock;
            }
        }
        private static Task Stop(LobbyOnlineBootstrap owner) => (Task)typeof(LobbyOnlineBootstrap).GetMethod("StopRunnerAsync", Private).Invoke(owner, null);
        private static object Get(object owner, string field) => owner.GetType().GetField(field, Private).GetValue(owner);
        private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> check, string label)
        {
            double until = Time.realtimeSinceStartupAsDouble + 90;
            while (!check()) { Require(Time.realtimeSinceStartupAsDouble < until, "timeout: " + label); await Task.Delay(100); }
        }
    }
}
