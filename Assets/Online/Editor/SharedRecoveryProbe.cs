using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    public static class SharedRecoveryProbe
    {
        public static string Result { get; private set; } = "not run";
        private static bool running;
        [MenuItem("Tools/Online/Test Live Run Recovery (isolated Play Mode)")]
        public static async void Run()
        {
            if (running || !EditorApplication.isPlaying) return;
            running = true; Result = "running";
            NetworkRunner first = null, second = null, returned = null, outsider = null;
            try
            {
                string room = "recovery-test-" + Guid.NewGuid().ToString("N");
                first = await Connect(room, 1001, true);
                SpawnAvatar(first);
                var state = first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                    onBeforeSpawned: (_, o) => o.GetComponent<SharedDepartureState>().Initialize(0, 2))
                    .GetComponent<SharedDepartureState>();
                second = await Connect(room, 1002, false);
                SpawnAvatar(second);
                await Until(() => state.Phase == 2 && Replica(second)?.Phase == 2, "loading");
                var remote = Replica(second);
                state.RPC_SceneReady(); remote.RPC_SceneReady();
                await Until(() => state.Phase == 3 && remote.Phase == 3, "playing");
                typeof(SharedDepartureState).GetProperty("DayHour").SetValue(state, 22.5f);
                typeof(SharedDepartureState).GetProperty("DayClockInitialized").SetValue(state, (NetworkBool)true);
                await Until(() => remote.DayClockInitialized && Mathf.Abs(remote.DayHour - 22.5f) < .01f, "night clock replication");
                var player = second.LocalPlayer;
                int template = SharedItemCatalog.Load().entries.First(e => e.prefab != null).id;
                var item = new SharedRecoveryItem { Template = template, Ammo = 7, WorldId = 9281, Equipped = 1 };
                state.Items.Add(9281, new SharedItemRecord { Template = template, Owner = player, Mode = 2, WeaponAmmo = 7 });
                remote.RPC_RecoveryItem(1, 0, item);
                remote.RPC_RecoveryCommit(new SharedPersonalData { Revision = 1, ItemCount = 1, Health = 80, Pistol = 31 });
                await Until(() => state.Personal.TryGet(player, out var data) && data.Revision == 1, "snapshot");
                remote.RPC_RecoveryCommit(new SharedPersonalData { Revision = 2, ItemCount = 2, Health = 1 });
                await Task.Delay(350);
                Require(state.Personal[player].Revision == 1 && state.Personal[player].Health == 80, "partial snapshot discarded");
                await second.Shutdown(); second = null;
                await Task.Delay(1200);
                Require(state.Items[9281].Owner == player && state.Items[9281].Mode == 2, "disconnected bag reserved");
                Require(state.Phase == 3, "remaining crew keeps run alive");
                outsider = await Connect(room, 9999, false);
                await Until(() => Replica(outsider) != null, "outsider state");
                Require(!Replica(outsider).CanReturn(outsider.LocalPlayer), "outsider rejected by roster");
                await outsider.Shutdown(); outsider = null;
                returned = await Connect(room, 1002, false);
                await Until(() => Replica(returned) != null, "return state");
                Require(returned.LocalPlayer == player, "PlayerUniqueId restores PlayerRef");
                var restored = Replica(returned);
                Require(restored.DayClockInitialized && Mathf.Abs(restored.DayHour - 22.5f) < .01f, "night clock restored on return");
                Require(restored.CanReturn(player) && restored.Personal[player].Pistol == 31, "personal state survives reconnect");
                int slot = restored.CrewSlot(player);
                Require(restored.PersonalItems[slot * SharedDepartureState.RecoverySlots].Ammo == 7, "weapon magazine preserved");
                SpawnAvatar(returned);
                await Until(() => state.HasOtherPlayingCrew(first.LocalPlayer), "returned avatar registered");
                await first.Shutdown(); first = null;
                await Until(() => restored.Object.HasStateAuthority, "master transfer");
                Require(restored.DayClockInitialized && Mathf.Abs(restored.DayHour - 22.5f) < .01f, "night clock survives master transfer");
                Require(restored.Personal[player].Health == 80 && restored.Items[9281].Owner == player, "recovery data survives master transfer");
                first = await Connect(room, 1001, false); // Probe only; no gameplay avatar.
                await Until(() => Replica(first) != null, "probe state");
                await returned.Shutdown(); returned = null;
                await Until(() => Replica(first)?.Phase == 4, "last gameplay participant left");
                Require(!Replica(first).CanReturn(first.LocalPlayer), "probe cannot resurrect empty run");
                await first.Shutdown(); first = null;
                await Task.Delay(500);
                var gone = NewRunner();
                var result = await gone.StartGame(Args(gone, room, 1001, false));
                Require(!result.Ok, "empty room cannot be recreated by return");
                if (gone != null) await gone.Shutdown();
                var json = JsonUtility.ToJson(new SharedSoloSnapshot
                {
                    Seed = 42, Bag = new[] { item }, Personal = new SharedPersonalData { Health = 80, Pistol = 31 },
                    Items = Array.Empty<SoloWorldItem>(), Enemies = Array.Empty<SoloWorldEnemy>()
                });
                var copy = JsonUtility.FromJson<SharedSoloSnapshot>(json);
                Require(copy.Bag[0].WorldId == 9281 && copy.Bag[0].Equipped == 1 && copy.Personal.Pistol == 31, "solo data JSON round trip");
                Result = "PASS";
                Debug.Log("[Recovery probe] PASS: partial snapshot, bag reservation, stable PlayerRef, roster rejection, master transfer, last crew exit, no recreation, JSON.");
            }
            catch (Exception e) { Result = "FAIL: " + e.Message; Debug.LogError("[Recovery probe] " + Result); }
            finally
            {
                foreach (var runner in new[] { first, second, returned, outsider })
                    if (runner != null) try { await runner.Shutdown(); } catch (Exception) { }
                running = false;
            }
        }
        private static NetworkRunner NewRunner()
        {
            var root = new GameObject("Recovery test runner");
            root.AddComponent<LobbySceneManager>(); root.AddComponent<NetworkObjectProviderDefault>();
            return root.AddComponent<NetworkRunner>();
        }
        private static StartGameArgs Args(NetworkRunner runner, string room, long id, bool create)
        {
            var config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig();
            config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            return new StartGameArgs
            {
                GameMode = GameMode.Shared, Config = config, PlayerUniqueId = id,
                CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"), SessionName = room,
                CustomLobbyName = "deadboat-recovery-probe-v1", PlayerCount = 2,
                EnableClientSessionCreation = create, IsVisible = false,
                SceneManager = runner.GetComponent<LobbySceneManager>(), ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>()
            };
        }
        private static async Task<NetworkRunner> Connect(string room, long id, bool create)
        {
            var runner = NewRunner(); var result = await runner.StartGame(Args(runner, room, id, create));
            Require(result.Ok, "connect: " + result.ShutdownReason); return runner;
        }
        private static void SpawnAvatar(NetworkRunner runner)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
            runner.SetPlayerObject(runner.LocalPlayer, runner.Spawn(prefab));
        }
        private static SharedDepartureState Replica(NetworkRunner runner) =>
            UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static async Task Until(Func<bool> test, string label)
        {
            double until = Time.realtimeSinceStartupAsDouble + 25;
            while (!test()) { Require(Time.realtimeSinceStartupAsDouble < until, "timeout: " + label); await Task.Delay(50); }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
