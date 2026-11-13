using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Protocol payload/lifecycle check. No sale-point visuals, owner scenes or persistence writes.
    public static class SharedSalesMigrationProbe
    {
        [Serializable] private sealed class Report
        {
            public string unityVersion, result, skippedReason;
            public string scope = "Isolated native Play, two real Cloud peers, one hidden unique room. Sales/credit/personal payload is seeded before the second peer joins, then validated after cold delivery and authority takeover. Actual DTO Capture runs only with an already-valid read-only identity. Controlled TickWorld prune is invoked by reflection; this is not physical travel, a rendered browser rejoin, sale interaction or owner-save acceptance.";
            public bool actualCaptureTested, jsonRoundtripTested, legacyNullableSales, pruneTested, cleanupComplete;
            public int coldSales, takeoverSales, firstCredit, secondCredit, firstCoins, firstAcknowledgedCredits;
            public float rearAfterPrune;
            public double elapsedSeconds;
            public List<string> passed = new List<string>();
            public List<string> errors = new List<string>();
        }
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string IdentityKey = "DeadBoat.FusionIdentity.v1";
        private static bool running;
        private static Stopwatch clock;
        public static string Result { get; private set; } = "not run";

        public static async Task RunAsync()
        {
            Require(!running && EditorApplication.isPlaying && SaveManager.Instance == null && LoadingManager.Instance == null &&
                GameManager.Instance == null && PlayerMovement.Instance == null && !SharedRunContext.Active && !SoloRunBranch.Active &&
                Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length == 0, "Empty isolated Play without owner managers/runners required.");
            running = true; Result = "running"; clock = Stopwatch.StartNew();
            var report = new Report { unityVersion = Application.unityVersion };
            var previousMode = NetworkProjectConfig.Global.PeerMode;
            float previousScale = Time.timeScale;
            var previousSocial = Object.FindAnyObjectByType<MirraSocialService>();
            string identityBefore = PlayerPrefs.GetString(IdentityKey, ""); // Read only: LocalId would create/write a missing identity.
            NetworkRunner first = null, second = null; GameObject fixture = null;
            Exception failure = null;
            try
            {
                Time.timeScale = 0; NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                var statePrefab = Resources.Load<NetworkObject>("Online/SharedDepartureState");
                var avatarPrefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
                Require(statePrefab != null && avatarPrefab != null, "Production network prefabs missing.");
                // Awake-only fixture, matching the existing scale protocol probe. Start and saves never run.
                fixture = new GameObject("Sales271 migration transient fixture");
                fixture.AddComponent<GameManager>().enabled = false;
                var localization = fixture.AddComponent<LocalizationManager>(); localization.enabled = false;
                var managers = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers.prefab");
                var localizationData = managers.GetComponentInChildren<LocalizationManager>(true).LocalizationData;
                Require(localizationData != null, "Production localization missing.");
                typeof(LocalizationManager).GetField("localizationData", Private).SetValue(localization, localizationData);
                typeof(LocalizationManager).GetField("currentLanguage", Private).SetValue(localization, "Ru");

                string room = "sales271-migration-" + Guid.NewGuid().ToString("N");
                ulong retired = WorldSpawnIdentity.IdForKey(room + ":retired"), current = WorldSpawnIdentity.IdForKey(room + ":current");
                first = SharedReadyBarrierProbe.NewRunner(); await Budget(Start(first, room), "first Cloud connection");
                var state = first.Spawn(statePrefab,
                    onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(0, 2)).GetComponent<SharedDepartureState>();
                PlayerRef originalPlayer = first.LocalPlayer;
                state.Sales.Add(retired, new SharedSaleRecord { Coins = 111, Revision = 7, Position = new Vector3(3, 0, 100) });
                state.Sales.Add(current, new SharedSaleRecord { Coins = 222, Revision = 9, Position = new Vector3(5, 0, 9000) });
                state.SaleCredits.Set(0, 321);
                state.Personal.Add(originalPlayer, new SharedPersonalData { Coins = 421, SaleCredits = 321, Health = 100, Revision = 1 });
                Require(state.Phase == 0, "Fixture must seed before the second connection, not after warm replication.");
                second = SharedReadyBarrierProbe.NewRunner(); await Budget(Start(second, room), "cold second Cloud connection");
                await Until(() => state.Phase == 2 && Replica(second) != null && Replica(second).Phase == 2, "cold sealed roster");
                var remote = Replica(second);
                await Until(() => SamePayload(remote, retired, current, originalPlayer), "cold Sales/credits/Personal payload");
                Require(remote.CrewSlot(originalPlayer) == 0, "First player no longer maps to seeded crew slot0.");
                report.coldSales = remote.Sales.Count;
                report.passed.Add("Second peer cold received both seeded balances/revisions/positions, credit321 and first Personal coins421/ack321.");

                first.SetPlayerObject(first.LocalPlayer, first.Spawn(avatarPrefab, Vector3.zero));
                second.SetPlayerObject(second.LocalPlayer, second.Spawn(avatarPrefab, Vector3.zero));
                foreach (var companions in Object.FindObjectsByType<AvatarCompanions>(FindObjectsSortMode.None)) companions.enabled = false;
                await Until(() => first.ActivePlayers.All(p => first.TryGetPlayerObject(p, out var obj) && obj != null) &&
                    second.ActivePlayers.All(p => second.TryGetPlayerObject(p, out var obj) && obj != null), "valid crew player objects");
                state.RPC_SceneReady(); remote.RPC_SceneReady();
                await Until(() => state.Phase == 3 && remote.Phase == 3, "phase3 ready barrier");
                await Budget(first.Shutdown(), "former authority shutdown");
                await Until(() => remote != null && remote.Object.IsValid && remote.Object.HasStateAuthority &&
                    second.IsSharedModeMasterClient && remote.Phase == 3, "real master takeover");
                Require(SamePayload(remote, retired, current, originalPlayer), "Takeover changed seeded sale payload.");
                report.takeoverSales = remote.Sales.Count;
                report.passed.Add("Former authority shutdown; remaining peer became master and kept canonical balances, credits and first-player receipt.");

                // Only this runner's own sender identity is used. Do not forge the departed player's RPC source.
                int ownSlot = remote.CrewSlot(second.LocalPlayer); Require(ownSlot >= 0 && ownSlot != 0, "Second crew slot missing.");
                remote.SaleCredits.Set(ownSlot, 27);
                var staleOwnProgress = new SharedPersonalData { Coins = 50, Health = 100, SaleCredits = 0 };
                remote.RPC_RecoveryProgress(staleOwnProgress);
                await Until(() => remote.Personal.TryGet(second.LocalPlayer, out var p) && p.Coins == 77 && p.SaleCredits == 27, "own stale ack reconciliation");
                remote.RPC_RecoveryProgress(staleOwnProgress);
                await Task.Delay(100); // Let the repeated RPC run before asserting idempotence.
                Require(remote.Personal[second.LocalPlayer].Coins == 77 && remote.Personal[second.LocalPlayer].SaleCredits == 27,
                    "Repeated stale own receipt was credited twice.");
                Require(SamePayload(remote, retired, current, originalPlayer), "Own progress changed departed player's receipt.");
                report.passed.Add("Own-source progress with ack0 is reconciled to coins77/ack27; repeat does not add again or alter first player's receipt.");

                SharedSoloSnapshot snapshot;
                if (long.TryParse(identityBefore, out long existingId) && existingId != 0)
                {
                    snapshot = SharedSoloSnapshot.Capture(remote);
                    report.actualCaptureTested = true;
                    Require(snapshot.PlayerId == existingId, "Actual Capture changed existing identity.");
                    Require(snapshot.Personal.Coins == 77 && snapshot.Personal.SaleCredits == 27, "Actual Capture lost own receipt.");
                }
                else
                {
                    report.skippedReason = "Actual SharedSoloSnapshot.Capture skipped: no valid existing identity; its LocalId getter would write PlayerPrefs. No identity was created or changed.";
                    snapshot = new SharedSoloSnapshot { Sales = remote.Sales.Select(e => new SoloSaleState { Id = e.Key, Coins = e.Value.Coins }).ToArray(),
                        Personal = remote.Personal[second.LocalPlayer] };
                }
                var restored = JsonUtility.FromJson<SharedSoloSnapshot>(JsonUtility.ToJson(snapshot));
                Require(restored.Sales != null && restored.Sales.Length == 2 && restored.Sales.Single(s => s.Id == retired).Coins == 111 &&
                    restored.Sales.Single(s => s.Id == current).Coins == 222 && restored.Personal.Coins == 77 && restored.Personal.SaleCredits == 27,
                    "Actual solo/recovery DTO JSON lost sale balances or receipt.");
                report.jsonRoundtripTested = true;
                var legacy = JsonUtility.FromJson<SharedSoloSnapshot>("{\"Seed\":271,\"Personal\":{\"Coins\":50},\"Bag\":[],\"Items\":[],\"Enemies\":[]}");
                Require(legacy.Sales == null && (legacy.Sales ?? Array.Empty<SoloSaleState>()).Length == 0 && legacy.Personal.SaleCredits == 0,
                    "Legacy missing Sales/credit fields are not nullable/zero-compatible.");
                report.legacyNullableSales = true;
                report.passed.Add("Network-derived sale balances and receipt survive actual DTO JSON; missing legacy Sales remains nullable and ack defaults0.");

                // Controlled focus at10km, canonical boat10km: strict WorldRear9km prunes100m, retains exactly9000m.
                bool ownAvatarPresent = false;
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (avatar.Runner == second && avatar.Object.HasStateAuthority)
                    { avatar.transform.position = Vector3.forward * 10000; ownAvatarPresent = true; }
                Require(ownAvatarPresent, "Live second-player focus avatar missing.");
                await Until(() => FocusReady(remote), "departed avatar removed and surviving focus at10km");
                typeof(SharedDepartureState).GetProperty("BoatDistance").SetValue(remote, 10000f);
                typeof(SharedDepartureState).GetField("nextWorldCleanup", Private).SetValue(remote, 0d);
                var tickWorld = typeof(SharedDepartureState).GetMethod("TickWorld", Private); Require(tickWorld != null, "TickWorld seam missing.");
                tickWorld.Invoke(remote, null);
                Require(Mathf.Abs(remote.WorldRear - 9000f) < .01f && !remote.Sales.ContainsKey(retired) && remote.Sales.ContainsKey(current),
                    "Rear prune did not remove retired balance and retain strict boundary balance.");
                Require(remote.Sales[current].Coins == 222 && remote.SaleCredits[0] == 321 && remote.SaleCredits[ownSlot] == 27 &&
                    remote.Personal[originalPlayer].Coins == 421 && remote.Personal[originalPlayer].SaleCredits == 321,
                    "Rear prune changed surviving balance or personal credit ledger.");
                report.pruneTested = true; report.rearAfterPrune = remote.WorldRear;
                report.firstCredit = remote.SaleCredits[0]; report.secondCredit = remote.SaleCredits[ownSlot];
                report.firstCoins = remote.Personal[originalPlayer].Coins; report.firstAcknowledgedCredits = remote.Personal[originalPlayer].SaleCredits;
                report.passed.Add("Production TickWorld prunes retired100m balance, retains exactly rear9000m balance and preserves both credit ledgers/first receipt.");
                Require(PlayerPrefs.GetString(IdentityKey, "") == identityBefore, "Read-only test changed identity storage.");
                report.result = "PASS";
            }
            catch (Exception error) { failure = error; report.result = "FAIL"; report.errors.Add(error.ToString()); }
            finally
            {
                await Stop(second, report); await Stop(first, report);
                if (fixture != null) Object.Destroy(fixture);
                if (previousSocial == null) { var created = Object.FindAnyObjectByType<MirraSocialService>(); if (created != null) Object.Destroy(created.gameObject); }
                NetworkProjectConfig.Global.PeerMode = previousMode; Time.timeScale = previousScale;
                await Task.Delay(50);
                report.cleanupComplete = Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length == 0 &&
                    !SharedRunContext.Active && !SoloRunBranch.Active && SaveManager.Instance == null && GameManager.Instance == null &&
                    NetworkProjectConfig.Global.PeerMode == previousMode && Time.timeScale == previousScale && PlayerPrefs.GetString(IdentityKey, "") == identityBefore;
                if (!report.cleanupComplete) report.errors.Add("Fixture cleanup or identity/settings restoration failed.");
                report.elapsedSeconds = clock.Elapsed.TotalSeconds;
                if (report.errors.Count > 0) report.result = "FAIL";
                Result = report.result + ": cold sales payload, takeover, own receipt reconcile, JSON/legacy and rear prune; actualCaptureTested=" + report.actualCaptureTested;
                string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "Loot265-20261114"); Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "migration271.json"), JsonUtility.ToJson(report, true));
                running = false;
                UnityEngine.Debug.Log("[Sales migration271] " + Result);
            }
            if (failure != null || report.errors.Count > 0) throw new InvalidOperationException(Result, failure);
        }
        private static bool SamePayload(SharedDepartureState s, ulong retired, ulong current, PlayerRef first)
            => s.Sales.Count == 2 && s.Sales.TryGet(retired, out var a) && a.Coins == 111 && a.Revision == 7 && a.Position.z == 100 &&
                s.Sales.TryGet(current, out var b) && b.Coins == 222 && b.Revision == 9 && b.Position.z == 9000 && s.SaleCredits[0] == 321 &&
                s.Personal.TryGet(first, out var p) && p.Coins == 421 && p.SaleCredits == 321;
        private static bool FocusReady(SharedDepartureState state)
        {
            bool found = false;
            foreach (var avatar in LobbyNetworkAvatar.All)
            {
                if (avatar.Runner != state.Runner || !state.Includes(avatar.Object.StateAuthority)) continue;
                if (avatar.LogicalPosition.z < 9999) return false;
                found = true;
            }
            return found;
        }
        private static async Task Start(NetworkRunner runner, string room)
        {
            var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
            config.PrefabTable = NetworkProjectConfig.Global.PrefabTable; config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            var result = await runner.StartGame(new StartGameArgs { GameMode = GameMode.Shared, Config = config, SessionName = room,
                CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"), AuthValues = new Photon.Realtime.AuthenticationValues(Guid.NewGuid().ToString("N")),
                CustomLobbyName = "deadboat-sales271-migration-v1", PlayerCount = 2, IsVisible = false,
                SceneManager = runner.GetComponent<LobbySceneManager>(), ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>() });
            Require(result.Ok, "Cloud connection failed: " + result.ShutdownReason);
        }
        private static async Task Budget(Task task, string label)
        {
            if (await Task.WhenAny(task, Task.Delay(Math.Max(1, 100000 - (int)clock.ElapsedMilliseconds))) != task)
            { _ = task.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); throw new TimeoutException(label); }
            await task;
        }
        private static async Task Until(Func<bool> condition, string label)
        { while (!condition()) { Require(clock.ElapsedMilliseconds < 100000, "Global work deadline: " + label); await Task.Delay(25); } }
        private static async Task Stop(NetworkRunner runner, Report report)
        {
            if (runner == null) return; var root = runner.gameObject;
            try
            {
                var stop = runner.Shutdown();
                if (await Task.WhenAny(stop, Task.Delay(Math.Max(1, Math.Min(8000, 119000 - (int)clock.ElapsedMilliseconds)))) == stop) await stop;
                else { _ = stop.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); report.errors.Add("Transport shutdown deadline."); }
            }
            catch (Exception error) { report.errors.Add("Transport cleanup: " + error); }
            finally { if (root != null) Object.Destroy(root); }
        }
        private static SharedDepartureState Replica(NetworkRunner runner) => Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
