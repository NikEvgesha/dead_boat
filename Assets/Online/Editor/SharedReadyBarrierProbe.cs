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
                    await BoatCase(first, second, authority, remote);
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
                Debug.Log("[Ready probe] " + name + " PASS");
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

        private static async Task BoatCase(NetworkRunner first, NetworkRunner second,
            SharedDepartureState authority, SharedDepartureState remote)
        {
            var go = new GameObject("Probe boat");
            try
            {
                var board = go.AddComponent<BoardController>();
                board.enabled = false; // Do not run scene/save initialization in this empty test.
                board.StartGame = true;
                var seat = go.AddComponent<DriverSeatTrigger>();
                seat.enabled = false;
                seat.trainController = board;
                seat.driverSeatTransform = go.transform;
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<NetworkObject>(
                    "Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
                first.Spawn(prefab, Vector3.zero);
                second.Spawn(prefab, Vector3.zero);
                Set(authority, "BoatInitialized", (NetworkBool)true);
                Set(authority, "BoatMaxFuel", 100f);
                Set(authority, "BoatMaxSpeed", 10f);
                Set(authority, "BoatFuel", 10f);
                Set(authority, "BoatAcceleration", 5f);
                Set(authority, "BoatConsumption", 1f);
                Set(authority, "BoatCoast", 2f);
                Set(authority, "BoatBrake", 10f);
                Set(authority, "BoatEndDistance", 100f);
                await Task.Delay(500);
                authority.RPC_Driver(true);
                await Until(() => authority.Driver == first.LocalPlayer, "driver claim");
                remote.RPC_Driver(true);
                remote.RPC_DriverInput(1);
                await Task.Delay(300);
                Require(authority.Driver == first.LocalPlayer && authority.BoatSpeed == 0,
                    "second driver/input rejected");
                authority.RPC_DriverInput(1);
                await Until(() => remote.BoatDistance > 0 && remote.BoatFuel < 10, "boat replication");
                await Task.Delay(3000);
                Require(authority.BoatSpeed == 0, "stale input coasts to stop");
                float fuel = authority.BoatFuel;
                remote.RPC_AddFuel(2);
                await Until(() => authority.BoatFuel > fuel + 1.9f, "shared refuel");
                authority.RPC_Driver(false);
                await Until(() => remote.Driver == PlayerRef.None, "driver release");
                remote.RPC_Driver(true);
                await Until(() => authority.Driver == second.LocalPlayer, "driver handover");
                float distance = authority.BoatDistance;
                await ItemCase(first, second, authority, remote);
                await FuelCase(authority, remote);
                await WeaponCase(authority, remote);
                await PageCase(authority, remote);
                await EnemyCase(first, second, authority, remote);
                await BonusCase(first, second, authority, remote);
                await first.Shutdown();
                await Until(() => remote.Object.HasStateAuthority &&
                    SharedWorldPage.All(second).All(p => p.Object.HasStateAuthority), "master/page transfer");
                Require(remote.Items.ContainsKey(9000000000) &&
                    remote.TryEnemy(SharedEnemyTestId(), out var defeated) && defeated.HP == 0,
                    "item and enemy snapshots survive master transfer");
                Require(remote.Driver == second.LocalPlayer && remote.BoatDistance >= distance,
                    "boat state survives master transfer");
                Debug.Log("[Boat probe] PASS: exclusive driver, rejected input, distance/fuel replication, stale input, refuel, master transfer.");
            }
            finally { UnityEngine.Object.Destroy(go); }
        }

        private static ulong SharedEnemyTestId()
        {
            ulong hash = 14695981039346656037UL;
            foreach (byte b in System.Text.Encoding.UTF8.GetBytes("test:enemy")) hash = (hash ^ b) * 1099511628211UL;
            return hash;
        }

        private static void Set(SharedDepartureState state, string property, object value) =>
            typeof(SharedDepartureState).GetProperty(property,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).SetValue(state, value);

        private static async Task ItemCase(NetworkRunner first, NetworkRunner second,
            SharedDepartureState authority, SharedDepartureState remote)
        {
            var go = new GameObject("Atomic item probe");
            try
            {
                var identity = go.AddComponent<WorldSpawnIdentity>();
                identity.Key = "test:atomic-item";
                authority.RegisterItem(identity);
                await Until(() => remote.Items.ContainsKey(identity.Id), "item registration");
                authority.RPC_ClaimItem(identity.Id);
                remote.RPC_ClaimItem(identity.Id);
                await Until(() => authority.Items[identity.Id].Owner != PlayerRef.None &&
                    remote.Items[identity.Id].Owner == authority.Items[identity.Id].Owner, "atomic claim");
                bool firstWon = authority.Items[identity.Id].Owner == first.LocalPlayer;
                var loser = firstWon ? remote : authority;
                var winner = firstWon ? authority : remote;
                loser.RPC_ItemState(identity.Id, 4, Vector3.zero, Quaternion.identity);
                await Task.Delay(300);
                Require(authority.Items[identity.Id].Mode == 1, "loser cannot consume item");
                winner.RPC_ItemState(identity.Id, 3, Vector3.zero, Quaternion.identity);
                await Until(() => remote.Items[identity.Id].Mode == 3 &&
                    remote.Items[identity.Id].Owner == PlayerRef.None, "attached item");
                loser.RPC_ClaimItem(identity.Id);
                await Until(() => authority.Items[identity.Id].Owner == loser.Runner.LocalPlayer, "reclaim attached item");
                loser.RPC_ItemState(identity.Id, 4, Vector3.zero, Quaternion.identity);
                await Until(() => authority.Items[identity.Id].Mode == 4 && remote.Items[identity.Id].Mode == 4,
                    "consumption replication");
                Debug.Log("[Item probe] PASS: one owner from simultaneous claims; loser rejected; attachment/reclaim and consumption replicated.");
            }
            finally { UnityEngine.Object.Destroy(go); }
        }

        private static async Task FuelCase(SharedDepartureState authority, SharedDepartureState remote)
        {
            var boat = BoardController.Instance;
            var deposit = boat.gameObject.AddComponent<FuelDeposit>();
            var go = new GameObject("Shared fuel probe");
            try
            {
                go.transform.position = boat.transform.position;
                go.AddComponent<FuelItem>().fuelValue = 5;
                var identity = go.AddComponent<WorldSpawnIdentity>(); identity.Key = "test:fuel";
                authority.RegisterItem(identity);
                Set(authority, "BoatFillMultiplier", 1f);
                float before = authority.BoatFuel;
                authority.RPC_BurnItem(identity.Id);
                await Until(() => remote.Items.TryGet(identity.Id, out var r) && r.Mode == 4 &&
                    remote.BoatFuel > before + 4.9f, "shared fuel consumption");
                authority.RPC_BurnItem(identity.Id);
                await Task.Delay(200);
                Require(Mathf.Abs(authority.BoatFuel - before - 5) < 0.01f, "fuel consumed once");
                Debug.Log("[Fuel probe] PASS: shared fuel/item consumption and duplicate burn rejection.");
            }
            finally { UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(deposit); }
        }

        private static async Task WeaponCase(SharedDepartureState authority, SharedDepartureState remote)
        {
            var entry = SharedItemCatalog.Load().entries.First(e =>
                e.prefab.GetComponentInChildren<RangedWeaponController>(true) != null);
            const ulong id = 8000000000;
            remote.RPC_DropPersonalItem(id, entry.id, Vector3.zero, Quaternion.identity, 3);
            await Until(() => authority.Items.TryGet(id, out var r) && r.WeaponAmmo == 3, "personal weapon drop");
            remote.RPC_DropPersonalItem(id, entry.id, Vector3.zero, Quaternion.identity, 10);
            await Task.Delay(200);
            Require(authority.Items[id].WeaponAmmo == 3, "drop replay cannot refill magazine");
            remote.RPC_ClaimItem(id);
            await Until(() => remote.Items[id].Owner == remote.Runner.LocalPlayer, "dropped weapon claim");
            remote.RPC_ItemState(id, 2, Vector3.zero, Quaternion.identity, 1);
            await Until(() => authority.Items[id].Mode == 2 && authority.Items[id].WeaponAmmo == 1, "inventory weapon ammo");
            remote.RPC_ItemState(id, 0, Vector3.zero, Quaternion.identity, 1);
            await Until(() => authority.Items[id].Mode == 0 && authority.Items[id].Owner == PlayerRef.None, "weapon redrop");
            Require(authority.Items[id].Dropped && authority.Items[id].Template == entry.id, "weapon template persists");
            Debug.Log("[Weapon probe] PASS: personal drop, replay rejection, inventory ammo, redrop preserves template/magazine.");
        }

        private static async Task PageCase(SharedDepartureState authority, SharedDepartureState remote)
        {
            const ulong start = 9000000000;
            for (ulong i = 0; i < 180; i++)
                authority.Items.Add(start + i, new SharedItemRecord {
                    Position = Vector3.zero, Rotation = Quaternion.identity, Prunable = true,
                    HomeZ = 0, Owner = i == 0 ? authority.Runner.LocalPlayer : PlayerRef.None,
                    Mode = i == 0 ? 2 : i == 1 ? 3 : 0
                });
            await Until(() => remote.Items.ContainsKey(start + 179), "overflow page replication");
            Require(remote.Items.Count >= 180, "growing snapshots preserve all records");
            Set(authority, "WorldRear", 10f);
            await Until(() => !remote.Items.ContainsKey(start + 179), "old world snapshot cleanup");
            Require(remote.Items.ContainsKey(start) && remote.Items.ContainsKey(start + 1),
                "inventory and attached items survive pruning");
            Set(authority, "WorldRear", 0f);
            Debug.Log("[World probe] PASS: overflow pages, remote records, cleanup preserving inventory/attached items.");
        }

        private static async Task EnemyCase(NetworkRunner first, NetworkRunner second,
            SharedDepartureState authority, SharedDepartureState remote)
        {
            var go = new GameObject("Enemy damage probe");
            try
            {
                var enemy = go.AddComponent<SharedProbeEnemy>();
                enemy.SetHP(100);
                var identity = go.AddComponent<WorldSpawnIdentity>();
                identity.Key = "test:enemy";
                authority.SampleEnemy(identity, enemy);
                await Until(() => remote.TryEnemy(identity.Id, out var e) && e.HP == 100, "enemy initial HP");
                authority.RPC_EnemyDamage(identity.Id, 15);
                remote.RPC_EnemyDamage(identity.Id, 20);
                await Until(() => remote.TryEnemy(identity.Id, out var e) && e.HP == 65, "shared damage");
                Require(enemy.SharedHP == 65, "damage once on authority");
                remote.RPC_EnemyBurn(identity.Id, 0.5f, 20);
                await Until(() => remote.TryEnemy(identity.Id, out var e) && e.HP <= 56, "shared burn");
                await Task.Delay(300);
                Require(enemy.SharedHP >= 55 && enemy.SharedHP <= 56, "burn damage applied once");
                remote.RPC_EnemyDamage(identity.Id, 5000);
                await Until(() => remote.TryEnemy(identity.Id, out var e) && e.HP == 0, "shared death");
                authority.MarkWon();
                await Until(() => remote.RunWon, "shared victory");
                Debug.Log("[Enemy probe] PASS: initial HP, two attackers, no duplicate damage, burn, death and shared victory.");
            }
            finally { UnityEngine.Object.Destroy(go); }
        }

        private static async Task BonusCase(NetworkRunner first, NetworkRunner second,
            SharedDepartureState authority, SharedDepartureState remote)
        {
            var a = new SharedBoatProfile
            {
                CardFuel = 100, CardSpeed = 5, CardConsumption = 0.8f, CardFill = 1.2f,
                ProfessionFuel = 10, ProfessionFuelMult = 2, ProfessionSpeedMult = 1.5f,
                ProfessionConsumption = 0.8f, ProfessionFill = 1.4f,
                AnimalFuel = 30, AnimalSpeed = 1, AnimalConsumption = 0.9f, AnimalFill = 1.1f
            };
            var b = new SharedBoatProfile
            {
                CardFuel = 50, CardSpeed = 10, CardConsumption = 0.9f, CardFill = 1.1f,
                ProfessionFuelMult = 1, ProfessionSpeed = 20, ProfessionSpeedMult = 1,
                ProfessionConsumption = 0.7f, ProfessionFill = 1.2f,
                AnimalFuel = 40, AnimalSpeed = 5, AnimalConsumption = 0.8f, AnimalFill = 1.2f
            };
            authority.RPC_BoatProfile(a);
            remote.RPC_BoatProfile(b);
            await Until(() => Mathf.Abs(remote.BoatMaxFuel - 2360) < 0.01f, "crew bonuses");
            Require(Mathf.Abs(remote.BoatMaxSpeed - 155) < 0.01f &&
                Mathf.Abs(remote.BoatFillMultiplier - 2.184f) < 0.001f, "best per stat / summed cards");
            remote.RPC_BoatProfile(b);
            await Task.Delay(250);
            Require(authority.BoatProfiles.Count == 2 && Mathf.Abs(authority.BoatMaxFuel - 2360) < 0.01f,
                "profile update does not duplicate bonuses");
            Debug.Log("[Bonus probe] PASS: best profession/animal per stat, additive cards, idempotent profile reports.");
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
