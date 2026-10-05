using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    // Two real Cloud peers in one empty Editor scene. This tests the protocol,
    // not WebGL scene loading or the owner's manual acceptance criteria.
    public static class SharedReadyBarrierProbe
    {
        private static bool running;

        [MenuItem("Tools/Online/Test Crew Ready Barrier (Play Mode)")]
        public static async void Run()
        {
            if (running || !EditorApplication.isPlaying) return;
            running = true;
            float previousScale = Time.timeScale;
            try
            {
                await Case("ready", 0);
                await Case("leave", 1);
                await Case("timeout", 2);
                Debug.Log("[Ready probe] PASS: two-peer readiness, duplicate ACK, paused clock, leave, timeout.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Ready probe] FAIL: " + exception);
            }
            finally
            {
                Time.timeScale = previousScale;
                running = false;
            }
        }

        private static async Task Case(string name, int mode)
        {
            NetworkRunner first = null, second = null;
            try
            {
                string room = "ready-probe-" + Guid.NewGuid().ToString("N");
                first = NewRunner();
                await Start(first, room);
                var authority = first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                    onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(0, 2))
                    .GetComponent<SharedDepartureState>();
                second = NewRunner();
                await Start(second, room);
                await Until(() => authority.Phase == 2, "loading phase");
                await Until(() => Replica(second) != null && Replica(second).Phase == 2, "remote roster");
                var remote = Replica(second);
                Time.timeScale = 0;
                authority.RPC_SceneReady();
                authority.RPC_SceneReady();
                await Until(() => authority.IsReady(first.LocalPlayer), "first ACK");
                await Task.Delay(750);
                Require(authority.Phase == 2 && !authority.IsReady(second.LocalPlayer), "premature start");
                if (mode == 0)
                {
                    remote.RPC_SceneReady();
                    await Until(() => authority.Phase == 3 && remote.Phase == 3, "all ready");
                    Require(authority.ReadyMask == 3, "ready mask");
                }
                else if (mode == 1)
                {
                    await second.Shutdown();
                    await Until(() => authority.Phase == 4, "leave abort");
                }
                else
                {
                    typeof(SharedDepartureState).GetProperty("LoadingDeadline").SetValue(authority,
                        TickTimer.CreateFromSeconds(first, 0.2f));
                    await Until(() => authority.Phase == 4 && remote.Phase == 4, "deadline abort");
                }
                Debug.Log("[Ready probe] " + name + " PASS; phase=" + authority.Phase);
            }
            finally
            {
                if (second != null) await second.Shutdown();
                if (first != null) await first.Shutdown();
                Time.timeScale = 1;
            }
        }

        private static NetworkRunner NewRunner()
        {
            var go = new GameObject("Ready barrier probe");
            go.AddComponent<LobbySceneManager>();
            go.AddComponent<NetworkObjectProviderDefault>();
            return go.AddComponent<NetworkRunner>();
        }

        private static async Task Start(NetworkRunner runner, string room)
        {
            var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
            config.PrefabTable = NetworkProjectConfig.Global.PrefabTable;
            config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared, Config = config, SessionName = room,
                CustomLobbyName = "deadboat-ready-probe-v3", PlayerCount = 2, IsVisible = false,
                SceneManager = runner.GetComponent<LobbySceneManager>(),
                ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>()
            });
            Require(result.Ok, "connect " + result.ShutdownReason);
        }

        private static SharedDepartureState Replica(NetworkRunner runner) =>
            UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None)
                .FirstOrDefault(state => state.Runner == runner);

        private static async Task Until(Func<bool> check, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 25;
            while (!check())
            {
                Require(Time.realtimeSinceStartupAsDouble < end, "timeout: " + label);
                await Task.Delay(50);
            }
        }

        private static void Require(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label);
        }
    }
}
