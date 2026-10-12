using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    [InitializeOnLoad]
    public static class SharedRewardGameplayProbe
    {
        private const string Backup = "DeadBoat.RewardProbe.Backup.";
        private static readonly string[] Keys = { "DeadBoat.SharedReturn.v1", "DeadBoat.FusionIdentity.v1", "DeadBoat.SoloBranch.v1" };
        public static string Result { get; private set; } = "not run";
        static SharedRewardGameplayProbe()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) Restore();
            };
        }
        public static string Prepare()
        {
            if (EditorApplication.isPlaying || SessionState.GetBool(Backup + "active", false))
                throw new InvalidOperationException("Prepare once before Play");
            foreach (var key in Keys)
            {
                SessionState.SetBool(Backup + key + ".exists", PlayerPrefs.HasKey(key));
                SessionState.SetString(Backup + key, PlayerPrefs.GetString(key));
                PlayerPrefs.DeleteKey(key);
            }
            SessionState.SetBool(Backup + "mode.exists", PlayerPrefs.HasKey("DeadBoat.OnlineMode.v1"));
            SessionState.SetInt(Backup + "mode", PlayerPrefs.GetInt("DeadBoat.OnlineMode.v1"));
            SessionState.SetString(Backup + "start", AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene));
            SessionState.SetBool(Backup + "active", true);
            SharedPerformanceTestSave.Prepare();
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/LoadingScene.unity");
            PlayerPrefs.Save();
            return "prepared isolated boot; owner keys backed up";
        }
        public static void Restore()
        {
            if (!SessionState.GetBool(Backup + "active", false)) return;
            foreach (var key in Keys)
                if (SessionState.GetBool(Backup + key + ".exists", false)) PlayerPrefs.SetString(key, SessionState.GetString(Backup + key, ""));
                else PlayerPrefs.DeleteKey(key);
            if (SessionState.GetBool(Backup + "mode.exists", false)) PlayerPrefs.SetInt("DeadBoat.OnlineMode.v1", SessionState.GetInt(Backup + "mode", 0));
            else PlayerPrefs.DeleteKey("DeadBoat.OnlineMode.v1");
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Backup + "start", ""));
            PlayerPrefs.Save();
            SessionState.SetBool(Backup + "active", false);
        }
        public static string VerifyRestored()
        {
            Require(!EditorApplication.isPlaying && !SessionState.GetBool(Backup + "active", false), "test ended");
            foreach (var key in Keys)
            {
                Require(PlayerPrefs.HasKey(key) == SessionState.GetBool(Backup + key + ".exists", false), "key presence restored");
                if (PlayerPrefs.HasKey(key)) Require(PlayerPrefs.GetString(key) == SessionState.GetString(Backup + key, ""), "key value restored");
            }
            Require(PlayerPrefs.HasKey("DeadBoat.OnlineMode.v1") == SessionState.GetBool(Backup + "mode.exists", false), "mode presence restored");
            if (PlayerPrefs.HasKey("DeadBoat.OnlineMode.v1")) Require(PlayerPrefs.GetInt("DeadBoat.OnlineMode.v1") == SessionState.GetInt(Backup + "mode", 0), "mode restored");
            Require(AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene) == SessionState.GetString(Backup + "start", ""), "start scene restored");
            return "PASS: owner return/identity/solo/mode keys and playModeStartScene restored";
        }

        public static async void Run()
        {
            if (!EditorApplication.isPlaying || Result == "running") return;
            Result = "running";
            NetworkRunner peer = null;
            var mode = NetworkProjectConfig.Global.PeerMode;
            try
            {
                await Until(() => LoadingManager.Instance?.CurrentLocation == Location.Lobby, "lobby");
                Require(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave, "isolated owner save");
                var save = SaveManager.Instance.PersistentProvider;
                int gems = save.LoadGems(), wins = save.LoadWins();
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                var lobby = UnityEngine.Object.FindAnyObjectByType<LobbyOnlineBootstrap>();
                lobby.SelectOnline();
                await Until(() => lobby.IsOnline && !lobby.IsConnecting, "visual connection");
                Require(await lobby.CreateDepartureAsync(0, 2), "create crew");
                var root = new GameObject("Reward probe transport companion");
                UnityEngine.Object.DontDestroyOnLoad(root);
                peer = root.AddComponent<NetworkRunner>();
                var config = LobbyOnlineBootstrap.CreateDepartureNetworkConfig();
                config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                var result = await peer.StartGame(new StartGameArgs {
                    GameMode = GameMode.Shared, Config = config, PlayerUniqueId = 8802,
                    CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"),
                    SessionName = lobby.DepartureSessionName, CustomLobbyName = LobbyOnlineBootstrap.DepartureDirectoryName,
                    EnableClientSessionCreation = false, SceneManager = root.AddComponent<LobbySceneManager>(),
                    ObjectProvider = root.AddComponent<NetworkObjectProviderDefault>()
                });
                Require(result.Ok, "companion connect");
                peer.SetPlayerObject(peer.LocalPlayer, peer.Spawn(AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab")));
                await Until(() => Replica(peer)?.Phase == 2, "crew sealed");
                Replica(peer).RPC_SceneReady();
                await Until(() => SharedRunContext.Playing && BoardController.Instance != null && BoardController.Instance.StartGame, "Forest loaded");
                var state = SharedRunContext.State;
                Replica(peer).RPC_BoatProfile(SharedBoatProfile.Local());
                await Until(() => state.BoatInitialized, "boat initialized");
                var fork = SharedSoloSnapshot.Capture(state);
                int reward = ProfessionService.ApplyWinGemsReward((int)typeof(GameManager).GetField("_reward", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameManager.Instance));
                typeof(SharedAdProtection).GetField("until", BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, Time.realtimeSinceStartupAsDouble + 300);
                float destination = BoardController.Instance.SharedEndDistance;
                PlayerMovement.Instance.transform.position = new Vector3(0, 5, destination - LobbyNetworkAvatar.Origin.z);
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (avatar.Runner == state.Runner || avatar.Runner == peer)
                        avatar.transform.position = new Vector3(0, 5, destination);
                // The harness skips travel, so also advance the retention range:
                // leaving the rear at the town would instantiate the whole route.
                Set(state, "WorldRear", destination - 1000);
                Set(state, "WorldFront", destination);
                Set(state, "BoatDistance", destination);
                Set(state, "BoatFinished", (NetworkBool)true);
                await Until(() => UnityEngine.Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None)
                    .Any(e => e.GetComponent<WorldSpawnIdentity>()?.Key?.StartsWith("boss:") == true), "real boss spawn");
                double end = Time.realtimeSinceStartupAsDouble + 40;
                while (!state.RunWon && Time.realtimeSinceStartupAsDouble < end)
                {
                    foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None))
                    {
                        var identity = enemy.GetComponent<WorldSpawnIdentity>();
                        if (identity == null || !identity.Key.StartsWith("boss:") || enemy.IsDead ||
                            !state.TryEnemy(identity.Id, out var record) || !record.Active) continue;
                        PlayerMovement.Instance.transform.position = enemy.transform.position + Vector3.up;
                        await Task.Delay(150);
                        if (identity != null) state.RPC_EnemyDamage(identity.Id, 5000);
                    }
                    await Task.Delay(150);
                }
                var ui = UnityEngine.Object.FindAnyObjectByType<EndGameUIManager>();
                await Until(() => state.RunWon && Replica(peer).RunWon && ui.GetState() == EndGameState.Win, "shared victory/UI");
                ui.StopAllCoroutines(); // Keep the acceptance window open during assertions.
                Require(save.LoadGems() == gems + reward && save.LoadWins() == wins + 1, "persistent single grant");
                Require(CurrencyManager.Instance.Gems == gems + reward, "reward UI balance");
                ui.ShowEndGameUI(EndGameState.Win);
                GameManager.Instance.GrantVictoryReward();
                Require(save.LoadGems() == gems + reward && save.LoadWins() == wins + 1, "repeated callbacks");
                await SharedRunLifetime.LeaveAsync();
                await Until(() => peer.IsRunning && Replica(peer).Object.HasStateAuthority && Replica(peer).RunWon, "companion survives winner exit");
                // Exercise the actual fork reward routing without repeating world
                // restoration (covered separately by the recovery gameplay probe).
                typeof(SoloRunBranch).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { fork });
                Require(SoloRunBranch.Active && SharedVictoryRewards.Apply(reward + 77), "fork routing");
                Require(save.LoadGems() == gems + reward && save.LoadWins() == wins + 1, "fork retains original receipt");
                SoloRunBranch.Clear();
                Result = "PASS: Forest boss, replicated RunWon, victory UI, persistent grant, repeated callbacks, companion survives exit, fork dedup";
                Debug.Log("[Reward gameplay probe] " + Result);
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogWarning("[Reward gameplay probe] " + Result); }
            finally
            {
                if (peer != null) await peer.Shutdown();
                if (SharedRunContext.Active) await SharedRunLifetime.LeaveAsync();
                SoloRunBranch.Clear();
                SharedAdProtection.End();
                NetworkProjectConfig.Global.PeerMode = mode;
            }
        }
        private static SharedDepartureState Replica(NetworkRunner runner) => UnityEngine.Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static void Set(SharedDepartureState state, string name, object value) => typeof(SharedDepartureState).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(state, value);
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
        private static async Task Until(Func<bool> check, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 60;
            while (!check())
            {
                if (Time.realtimeSinceStartupAsDouble >= end) throw new TimeoutException(label);
                EditorApplication.isPaused = false;
                await Task.Delay(100);
            }
        }
    }
}
