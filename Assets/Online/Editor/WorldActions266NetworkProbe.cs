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
using UnityEngine.AI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // DRAFT outside Assets: root copies this file into Editor only after the initial compile.
    // This is deliberately one controlled local world with four real Cloud transports.
    public static class WorldActions266NetworkProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string RuinsPath = "Assets/Prefabs/Locations/Enemy/ruins_1.prefab";
        private const string ShopPath = "Assets/Prefabs/Locations/Enemy/Sklep_1.prefab";
        [Serializable] private sealed class Wave
        {
            public string source;
            public int region, sourceTrapCount, boundTrapCount, expectedGuards, reserved, released;
            public List<string> actionIds = new List<string>();
            public List<string> checks = new List<string>();
        }
        [Serializable] private sealed class Report
        {
            public string utc, unityVersion, result, error, portalResult = "not run";
            public string scope = "Native empty Play fixture, four real hidden Photon Cloud peers in one local world. Real source-prefab Zone handlers and Lever.Use, authority RPC validation, duplicate requests, same-page guard reservations/cursor, pending/living/dead cold factory gates, stable IDs across controlled source-root destruction/reclone, and mid-wave Shared master/page takeover. Each instance cooldown is .03 seconds solely to shorten this fixture; authored balance is unchanged. Released AI, navigation, physics and normal prefab Start/Update are suppressed immediately. Canonical deaths are explicit MarkEnemyDead calls, not combat. No independent browser worlds, full residency transaction, owner SaveManager, persistent save/recovery UI, input, real combat, WebGL, PlayerBuild or SDK writes are claimed. Offering/portal assertions have a separate report produced by WorldActions266PortalProbe.";
            public int peers;
            public double elapsedSeconds;
            public bool cleanupComplete;
            public List<Wave> waves = new List<Wave>();
            public List<string> checks = new List<string>();
        }
        private sealed class Trigger
        {
            public ActivateZone Zone;
            public int Command;
            public UnityEvent Events;
            public List<DungeonTrap> Targets(string method) => Enumerable.Range(0, Events.GetPersistentEventCount())
                .Where(i => Events.GetPersistentMethodName(i) == method)
                .Select(i => Events.GetPersistentTarget(i) as DungeonTrap).Where(t => t != null).ToList();
        }
        private sealed class Location
        {
            public GameObject Root, Source;
            public int Region;
            public Vector3 Position;
            public DungeonTrap[] Traps;
            public readonly List<Trigger> Triggers = new List<Trigger>();
            public IEnumerable<DungeonTrap> Bound => Triggers.SelectMany(t => t.Targets("_InTrap")).Distinct();
        }
        private sealed class Fixture
        {
            public readonly GameObject Root;
            public readonly PlayerMovement Player;
            public readonly Collider ActorCollider;
            public SharedWorldActionsRuntime Runtime;
            public SharedDepartureState State;
            public bool Advance = true;
            public Fixture(GameObject root)
            {
                Root = root;
                root.AddComponent<GameManager>().enabled = false;
                var player = new GameObject("World266 transient player"); player.transform.SetParent(root.transform);
                Player = player.AddComponent<PlayerMovement>(); Player.enabled = false;
                ActorCollider = player.GetComponent<CharacterController>(); ActorCollider.enabled = false;
                player.tag = "Player";
                var loc = root.AddComponent<LocalizationManager>(); loc.enabled = false;
                var managers = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers.prefab");
                Set(loc, "localizationData", managers.GetComponentInChildren<LocalizationManager>(true).LocalizationData);
                Set(loc, "currentLanguage", "Ru");
            }
            public void Tick()
            {
                // Owned avatar health/position uses the production snapshot method; this
                // fixture has one PlayerMovement and therefore does not prove four worlds.
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (avatar.Object.HasStateAuthority) avatar.FixedUpdateNetwork();
                if (Advance && Runtime != null && SharedRunContext.Playing) Invoke(Runtime, "Update");
                FreezeActors();
            }
        }
        public static string Result { get; private set; } = "not run";
        private static bool running;
        private static Stopwatch clock;
        private static readonly PropertyInfo Removing = typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", Private);
        private static readonly MethodInfo Ready = typeof(SharedDepartureState).GetMethod("WorldActionsReady", Private);
        private static readonly PropertyInfo ContextState = typeof(SharedRunContext).GetProperty("State", BindingFlags.Public | BindingFlags.Static);

        public static async Task RunAsync()
        {
            Require(!running && EditorApplication.isPlaying && SaveManager.Instance == null && PlayerMovement.Instance == null &&
                GameManager.Instance == null && !SharedRunContext.Active && !SoloRunBranch.Active &&
                Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length == 0,
                "Requires an empty Play scene without owner managers or runners.");
            running = true; Result = "running"; clock = Stopwatch.StartNew();
            var report = new Report { utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
            var runners = new List<NetworkRunner>(); var locations = new List<Location>();
            var previousMode = NetworkProjectConfig.Global.PeerMode;
            float previousScale = Time.timeScale; var previousPhysics = Physics.simulationMode;
            var previousSocial = Object.FindAnyObjectByType<MirraSocialService>();
            IDisposable eggs = null; GameObject owner = null; Exception failure = null;
            try
            {
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                Time.timeScale = 1; Physics.simulationMode = SimulationMode.Script;
                eggs = EggFeatureStorage.UseTemporaryStorageForTests(); EggAnimalBuffService.MarkDirty();
                Require(Removing != null && Ready != null && ContextState?.GetSetMethod(true) != null, "probe reflection seams");
                var ruinsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuinsPath);
                var shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPath);
                var avatar = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
                Require(ruinsPrefab != null && shopPrefab != null && avatar != null, "production source assets");
                owner = new GameObject("World266 transient fixture"); var fixture = new Fixture(owner);
                string room = "world266-" + Guid.NewGuid().ToString("N");
                for (int i = 0; i < 4; i++)
                {
                    var runner = SharedReadyBarrierProbe.NewRunner(); runners.Add(runner);
                    await Budget(Start(runner, room), "Cloud peer " + (i + 1));
                    if (i == 0) fixture.State = runner.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                        onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(0, 4)).GetComponent<SharedDepartureState>();
                }
                report.peers = runners.Count;
                await Until(() => runners.All(r => Replica(r) != null && Replica(r).Phase == 2), "four sealed roster replicas");
                foreach (var runner in runners) runner.SetPlayerObject(runner.LocalPlayer, runner.Spawn(avatar));
                await Until(() => runners.All(r => r.ActivePlayers.All(p => r.TryGetPlayerObject(p, out var obj) && obj != null)), "four avatars on four peers");
                foreach (var companion in Object.FindObjectsByType<AvatarCompanions>(FindObjectsSortMode.None)) companion.enabled = false;
                foreach (var runner in runners) Replica(runner).RPC_SceneReady();
                await Until(() => runners.All(r => Replica(r).Phase == 3), "four-player ready barrier", fixture);
                SharedRunContext.Begin(fixture.State.Seed, fixture.State.LevelId, owner, null, fixture.State);
                SharedRunContext.Independent.enabled = false;
                fixture.Runtime = owner.GetComponent<SharedWorldActionsRuntime>(); fixture.Runtime.enabled = false;
                await Until(() => IsReady(fixture.State), "world pages/authority ready", fixture);
                report.checks.Add("four real hidden Cloud peers, canonical crew/ready barrier; no persistent provider");
                var ruins = Clone(ruinsPrefab, 26601, Vector3.zero, owner); locations.Add(ruins);
                var shop = Clone(shopPrefab, 26602, Vector3.right * 300, owner); locations.Add(shop);
                var remote = Replica(runners[1]);
                var ruinsRow = MakeRow(ruins, RuinsPath); report.waves.Add(ruinsRow);
                var shopRow = MakeRow(shop, ShopPath); report.waves.Add(shopRow);
                Require(ruins.Traps.Length == 1 && ruins.Bound.Count() == 1 && ruinsRow.expectedGuards == 20, "actual ruins one*twenty binding");
                Require(shop.Traps.Length == 5 && shop.Bound.Count() == 4 && shopRow.expectedGuards == 12, "actual Sklep four*three with fifth unbound");

                // The authored warning event reserves the whole wave before exit releases it.
                foreach (var trigger in ruins.Triggers.Where(t => t.Targets("_InDungeons").Count != 0))
                    await Fire(fixture, trigger, remote, runners);
                await Until(() => ruins.Traps.All(t => fixture.State.TryWorldAction(Node(t).Id, out _)), "ruins warning canonical trap", fixture);
                CheckReservations(fixture.State, ruins, ruinsRow, true);
                foreach (var trap in ruins.Bound)
                {
                    ulong id = SharedAmbushCatalog.GuardId(Node(trap).Id, 0);
                    Require(fixture.State.TryEnemy(id, out var pending) && pending.PendingSpawn && pending.HP == 0,
                        "pending canonical reservation has no living HP");
                    Require(!SharedEnemiesRuntime.TryCreateReplica(trap.ScriptedEnemyPrefab, id, pending, out _), "cold pending factory rejects");
                    fixture.State.MarkEnemyDead(id);
                    Require(fixture.State.TryEnemy(id, out pending) && pending.PendingSpawn, "pending cannot be killed before release");
                }
                ruinsRow.checks.Add("all20 same-page pending reservations before exit; pending cold factory/death rejected");
                foreach (var trigger in ruins.Triggers.Where(t => t.Targets("_InTrap").Count != 0)) await Fire(fixture, trigger, remote, runners);
                await Until(() => Complete(fixture.State, ruins), "ruins exactly20 release", fixture, 12000);
                await Wire(fixture.State, ruins, runners, fixture);
                CheckReservations(fixture.State, ruins, ruinsRow, false);
                await CheckDead(fixture, ruins, remote, ruinsRow);
                // Portal author requested this shared fixture contract; report is separate.
                await WorldActions266PortalProbe.CheckAsync(fixture.State, remote, ruins.Root, fixture.Tick);
                report.portalResult = "completed; see separate portal report";
                await CheckLever(fixture, ruins, remote, runners, report);

                var saved = Capture(fixture.State, ruins);
                RemoveSource(ruins); await Until(() => ruins.Root == null, "source-root local destruction", fixture);
                var recloned = Clone(ruinsPrefab, ruins.Region, ruins.Position, owner); locations.Add(recloned);
                Require(recloned.Traps.Select(t => Node(t).Id).OrderBy(v => v).SequenceEqual(saved.Keys.OrderBy(v => v)), "source re-clone stable action IDs");
                await Until(() => saved.All(pair => fixture.State.TryWorldAction(pair.Key, out var v) && v.Flags == pair.Value.Flags &&
                    v.Spawned == pair.Value.Spawned && v.Total == pair.Value.Total), "source re-clone canonical cursor/flags", fixture);
                await Task.Delay(250); fixture.Tick();
                Require(CountGuards(fixture.State, saved.Keys) == 20, "source re-clone does not replay consumed wave");
                ruinsRow.checks.Add("source root manually removed/re-cloned with same generation index; stable IDs/cursor and no extra wave (not full residency API)");

                // Begin Sklep with real zone events, then stop the old master while its wave is partial.
                foreach (var trigger in shop.Triggers.Where(t => t.Targets("_InTrap").Count != 0)) await Fire(fixture, trigger, remote, runners, false);
                await Until(() => shop.Bound.All(t => fixture.State.TryWorldAction(Node(t).Id, out var v) && (v.Flags & 2) != 0), "Sklep accepted wave headers", fixture);
                CheckReservations(fixture.State, shop, shopRow, false);
                await Until(() => shop.Bound.Any(t => fixture.State.TryWorldAction(Node(t).Id, out var v) && v.Spawned > 0 && v.Spawned < v.Total),
                    "partial Sklep wave before takeover", fixture);
                fixture.Advance = false;
                var before = Capture(fixture.State, shop);
                Require(before.Values.Any(v => v.Spawned > 0 && v.Spawned < v.Total), "pause retains a genuinely partial wave");
                await Budget(Stop(runners[0]), "first-master shutdown");
                ContextState.GetSetMethod(true).Invoke(null, new object[] { remote }); fixture.State = remote;
                // No world Tick is allowed between changing transport authority and switching context.
                await Until(() => remote.Object != null && remote.Object.IsValid && remote.Object.HasStateAuthority &&
                    SharedWorldPage.All(remote.Runner).All(p => p.Object.HasStateAuthority), "root and every page authority transfer", fixture, 15000);
                Require(before.All(pair => remote.TryWorldAction(pair.Key, out var v) && v.Spawned >= pair.Value.Spawned && v.Total == pair.Value.Total),
                    "transferred partial cursors preserved");
                fixture.Advance = true;
                await Until(() => IsReady(remote) && Complete(remote, shop), "new master finishes exact remaining guards", fixture, 12000);
                await Wire(remote, shop, runners.Skip(1), fixture);
                CheckReservations(remote, shop, shopRow, false);
                Require(CountGuards(remote, shop.Bound.Select(t => Node(t).Id)) == 12, "Sklep exactly12 guards, no fifth wave");
                var unbound = shop.Traps.Except(shop.Bound).Single();
                Require(!remote.TryWorldAction(Node(unbound).Id, out _) && CountGuards(remote, new[] { Node(unbound).Id }) == 0, "unbound fifth trap never starts");
                foreach (var trigger in shop.Triggers.Where(t => t.Targets("_InTrap").Count != 0)) await Fire(fixture, trigger, Replica(runners[2]), runners.Skip(1));
                Require(CountGuards(remote, before.Keys) == 12 && Complete(remote, shop), "repeated real/RPC requests after takeover add no guards");
                shopRow.checks.Add("mid-wave old master shutdown; context switched before pump; root/all page authority barrier; new master completes12 exactly; unbound fifth absent");
                report.checks.Add("one real Shared master departure with three peers left; live same-page state survives transfer");
                Require(SaveManager.Instance == null && SharedRunContext.Save is SharedRunPreviewSave, "fixture never creates owner SaveManager");
                report.result = "PASS"; Result = "PASS: four Cloud peers; actual ruins20/Sklep4x3 action callbacks, duplicate/pending/dead/cold guards, controlled source re-clone and mid-wave master/page takeover. One local world; no physics/combat/WebGL acceptance.";
            }
            catch (Exception error)
            { failure = error; report.result = "FAIL"; report.error = error.ToString(); Result = "FAIL: " + error; }
            finally
            {
                try
                {
                    SharedRunContext.End();
                    foreach (var identity in Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    { MarkRemoval(identity); Object.Destroy(identity.gameObject); }
                    foreach (var location in locations) if (location.Root != null) Object.Destroy(location.Root);
                    if (owner != null) Object.Destroy(owner);
                    var cleanup = Task.WhenAll(runners.Select(Stop));
                    if (await Task.WhenAny(cleanup, Task.Delay(12000)) == cleanup) await cleanup;
                    else throw new TimeoutException("bounded Cloud cleanup");
                    if (previousSocial == null)
                    { var social = Object.FindAnyObjectByType<MirraSocialService>(); if (social != null) Object.Destroy(social.gameObject); }
                    await Task.Delay(50);
                    report.cleanupComplete = !SharedRunContext.Active && !SoloRunBranch.Active &&
                        Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length == 0 && SaveManager.Instance == null;
                    Require(report.cleanupComplete, "transient fixture/runners cleanup");
                }
                catch (Exception error)
                { failure = failure ?? error; report.result = "FAIL"; report.error = (report.error ?? "") + "\nCleanup: " + error; Result = "FAIL: " + report.error; }
                finally
                {
                    NetworkProjectConfig.Global.PeerMode = previousMode; Time.timeScale = previousScale; Physics.simulationMode = previousPhysics;
                    eggs?.Dispose(); EggAnimalBuffService.MarkDirty(); running = false;
                    report.elapsedSeconds = clock.Elapsed.TotalSeconds;
                    Directory.CreateDirectory("Logs/WorldActions266-20261114");
                    File.WriteAllText("Logs/WorldActions266-20261114/network.json", JsonUtility.ToJson(report, true));
                }
            }
            if (failure != null) throw new InvalidOperationException(Result, failure);
            UnityEngine.Debug.Log("[World266 network] " + Result);
        }

        private static Location Clone(GameObject source, int region, Vector3 position, GameObject owner)
        {
            var staging = new GameObject("inactive world266 staging"); staging.transform.SetParent(owner.transform); staging.SetActive(false);
            var root = Object.Instantiate(source, staging.transform); root.name = source.name;
            root.transform.position = position;
            DisableTree(root);
            var location = new Location { Root = root, Source = source, Region = region, Position = position,
                Traps = root.GetComponentsInChildren<DungeonTrap>(true) };
            var spawners = root.GetComponentsInChildren<LocationContentSpawner>(true);
            Require(spawners.Length == 1, "fixture expects one actual generation root: " + source.name);
            spawners[0].InitializeGeneration(region); spawners[0].SetLevel(1);
            Require(SharedRunContext.Membership.Begin(region, position.z + LobbyNetworkAvatar.Origin.z), "begin generation membership");
            foreach (var trap in location.Traps) Set(trap, "_cooldown", .03f);
            root.transform.SetParent(owner.transform, true); Object.Destroy(staging);
            root.SetActive(true);
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component is ActivateZone || component is DungeonTrap || component is ActivateObject || component is Lever ||
                    component is Portal || component is CheckItemOnLocation) SharedWorldActionsRuntime.Register(component);
            foreach (var zone in root.GetComponentsInChildren<ActivateZone>(true))
            {
                location.Triggers.Add(new Trigger { Zone = zone, Command = 1, Events = zone.PlayerOnTriggerEnter });
                location.Triggers.Add(new Trigger { Zone = zone, Command = 2, Events = zone.PlayerOnTriggerExit });
            }
            SharedRunContext.Membership.Seal(region); return location;
        }
        private static Wave MakeRow(Location location, string source) => new Wave { source = source, region = location.Region,
            sourceTrapCount = location.Traps.Length, boundTrapCount = location.Bound.Count(), expectedGuards = location.Bound.Sum(t => t.ScriptedSpawnCount),
            actionIds = location.Bound.Select(t => Node(t).Id.ToString()).ToList() };
        private static async Task Fire(Fixture f, Trigger trigger, SharedDepartureState remote, IEnumerable<NetworkRunner> peers, bool waitWire = true)
        {
            await MoveNear(f, trigger.Zone.transform.position, peers);
            typeof(ActivateZone).GetMethod(trigger.Command == 1 ? "OnTriggerEnter" : "OnTriggerExit", Private)
                .Invoke(trigger.Zone, new object[] { f.ActorCollider });
            var node = Node(trigger.Zone);
            f.State.RPC_WorldAction(node.Id, trigger.Command); remote.RPC_WorldAction(node.Id, trigger.Command);
            await Until(() => f.State.TryWorldAction(node.Id, out var value) && (value.Flags & trigger.Command) != 0, "actual zone accepted", f);
            if (waitWire) await Until(() => peers.Where(r => r != null && r.IsRunning).All(r => Replica(r).TryWorldAction(node.Id, out var v) &&
                (v.Flags & trigger.Command) != 0), "zone header on all peers", f);
        }
        private static async Task MoveNear(Fixture f, Vector3 position, IEnumerable<NetworkRunner> peers)
        {
            f.Player.transform.position = position;
            await Until(() => peers.Where(r => r != null && r.IsRunning).All(r =>
            {
                foreach (var avatar in LobbyNetworkAvatar.All)
                    if (avatar.Runner == r && avatar.Object.HasStateAuthority)
                        return avatar.Health > 0 && Vector3.Distance(avatar.LogicalPosition - LobbyNetworkAvatar.Origin, position) < .1f;
                return false;
            }), "actor near actual action", f);
        }
        private static void CheckReservations(SharedDepartureState state, Location location, Wave row, bool allPending)
        {
            int reserved = 0, released = 0;
            foreach (var trap in location.Bound)
            {
                var id = Node(trap).Id; Require(state.TryWorldAction(id, out var action), "canonical trap action");
                var page = SharedWorldPage.All(state.Runner).Single(p => p.Actions.ContainsKey(id));
                Require(action.Total == trap.ScriptedSpawnCount && action.Region == location.Region && action.Spawned <= action.Total,
                    "authored total/region/cursor preserved");
                for (int ordinal = 0; ordinal < action.Total; ordinal++)
                {
                    ulong guardId = SharedAmbushCatalog.GuardId(id, ordinal);
                    Require(page.Enemies.TryGet(guardId, out var enemy) && enemy.ActionId == id && enemy.Template == SharedAmbushCatalog.TemplateId(trap.ScriptedEnemyPrefab) &&
                        enemy.Level == trap.ScriptedLevel && enemy.MaxHP == SharedAmbushCatalog.InitialHealth(trap.ScriptedEnemyPrefab, trap.ScriptedLevel), "all reservations on action page with stable ID/template/level/HP");
                    Require(SharedRunContext.Membership.BelongsTo(location.Region, guardId, true), "dynamic guard membership");
                    if (allPending) Require(enemy.PendingSpawn, "all pending before exit");
                    if (ordinal < action.Spawned) Require(!enemy.PendingSpawn, "cursor never precedes release publication");
                    if (!enemy.PendingSpawn) released++; reserved++;
                }
                Require(page.Enemies.Count(e => e.Value.ActionId == id) == action.Total, "no duplicate IDs outside reservation range");
            }
            Require(reserved == row.expectedGuards, "configured guard count is not capped"); row.reserved = reserved; row.released = released;
            row.checks.Add("same-page canonical total/cursor and every stable guard ID/template/level/HP/membership; reserved=" + reserved + ", released=" + released);
        }
        private static async Task CheckLever(Fixture f, Location location, SharedDepartureState remote, IEnumerable<NetworkRunner> peers, Report report)
        {
            var lever = location.Root.GetComponentsInChildren<Lever>(true).First(l => l._activeSubject != null && l.gameObject.activeInHierarchy);
            await MoveNear(f, lever.transform.position, peers); lever.Use();
            f.State.RPC_WorldAction(Node(lever).Id, 1); remote.RPC_WorldAction(Node(lever).Id, 1);
            await Until(() => peers.All(r => Replica(r).TryWorldAction(Node(lever).Id, out var a) && (a.Flags & 1) != 0 &&
                Replica(r).TryWorldAction(Node(lever._activeSubject).Id, out var b) && (b.Flags & 1) != 0), "real Lever.Use and child switch on four peers", f);
            report.checks.Add("actual Lever.Use plus first/second peer duplicate RPC; shared lever and child switch flags on all four peers (animation not visually accepted)");
        }
        private static async Task CheckDead(Fixture f, Location location, SharedDepartureState remote, Wave row)
        {
            var trap = location.Bound.First(); ulong id = SharedAmbushCatalog.GuardId(Node(trap).Id, 0);
            f.State.MarkEnemyDead(id);
            await Until(() => remote.TryEnemy(id, out var value) && value.HP == 0 && !value.PendingSpawn, "canonical explicit guard death remote", f);
            if (SharedItemsRuntime.TryFind(id, out var identity)) { MarkRemoval(identity); Object.Destroy(identity.gameObject); }
            await Until(() => !SharedItemsRuntime.TryFind(id, out _), "remove local dead guard", f);
            Require(f.State.TryEnemy(id, out var record) && record.HP == 0 &&
                !SharedEnemiesRuntime.TryCreateReplica(trap.ScriptedEnemyPrefab, id, record, out _), "cold canonical dead factory rejects respawn");
            row.checks.Add("explicit canonical MarkEnemyDead replicates; local actor removed; actual cold factory rejects HP0 (no combat assertion)");
            ulong livingId = SharedAmbushCatalog.GuardId(Node(trap).Id, 1);
            bool advance = f.Advance; f.Advance = false;
            try
            {
                Require(f.State.TryEnemy(livingId, out var living) && !living.PendingSpawn && living.HP > 0, "living canonical released guard");
                if (SharedItemsRuntime.TryFind(livingId, out var previous)) { MarkRemoval(previous); Object.Destroy(previous.gameObject); }
                await Until(() => !SharedItemsRuntime.TryFind(livingId, out _), "cold living local removal");
                Require(SharedEnemiesRuntime.TryCreateReplica(trap.ScriptedEnemyPrefab, livingId, living, out var restored) && restored != null,
                    "actual cold living factory recreates canonical guard");
                DisableTree(restored.gameObject);
                Require(restored.Id == livingId && restored.Enemy.SharedHP == living.HP && restored.Enemy.SharedMaxHP == living.MaxHP &&
                    !SharedEnemiesRuntime.TryCreateReplica(trap.ScriptedEnemyPrefab, livingId, living, out _), "cold HP/ID preserved and duplicate rejected");
                row.checks.Add("actual cold living TryCreateReplica after local removal preserves canonical ID/HP/maxHP; duplicate factory rejected");
            }
            finally { f.Advance = advance; }
        }
        private static Dictionary<ulong, SharedWorldActionRecord> Capture(SharedDepartureState state, Location location)
        {
            var result = new Dictionary<ulong, SharedWorldActionRecord>();
            foreach (var trap in location.Bound) { ulong id = Node(trap).Id; Require(state.TryWorldAction(id, out var value), "capture action"); result[id] = value; }
            return result;
        }
        private static async Task Wire(SharedDepartureState source, Location location, IEnumerable<NetworkRunner> peers, Fixture f)
        {
            await Until(() => peers.Where(r => r != null && r.IsRunning).All(r =>
            {
                var remote = Replica(r); if (remote == null) return false;
                foreach (var trap in location.Bound)
                {
                    ulong id = Node(trap).Id;
                    if (!source.TryWorldAction(id, out var a) || !remote.TryWorldAction(id, out var b) || a.Flags != b.Flags || a.Spawned != b.Spawned || a.Total != b.Total) return false;
                    var page = SharedWorldPage.All(remote.Runner).FirstOrDefault(p => p.Actions.ContainsKey(id)); if (page == null) return false;
                    for (int i = 0; i < a.Total; i++)
                        if (!page.Enemies.TryGet(SharedAmbushCatalog.GuardId(id, i), out var e) || e.PendingSpawn || e.ActionId != id || e.MaxHP <= 0) return false;
                }
                return true;
            }), "complete wave records on all live peers", f);
        }
        private static void RemoveSource(Location location)
        {
            foreach (var component in location.Root.GetComponentsInChildren<MonoBehaviour>(true)) SharedWorldActionsRuntime.Remove(component);
            foreach (var identity in location.Root.GetComponentsInChildren<WorldSpawnIdentity>(true)) MarkRemoval(identity);
            location.Root.SetActive(false); Object.Destroy(location.Root);
        }
        private static bool Complete(SharedDepartureState state, Location location) => location.Bound.All(t =>
            state.TryWorldAction(Node(t).Id, out var value) && value.Total == t.ScriptedSpawnCount && value.Spawned == value.Total);
        private static int CountGuards(SharedDepartureState state, IEnumerable<ulong> actions)
        { var ids = new HashSet<ulong>(actions); return SharedWorldPage.All(state.Runner).Sum(p => p.Enemies.Count(e => ids.Contains(e.Value.ActionId))); }
        private static SharedWorldActionsRuntime.Node Node(Component component)
        { var node = SharedWorldActionsRuntime.FindNode(component); Require(node != null && node.Id != 0, "actual scene action node"); return node; }
        private static bool IsReady(SharedDepartureState state) => state != null && state.Object != null && state.Object.IsValid && (bool)Ready.Invoke(state, null);
        private static void DisableTree(GameObject root)
        {
            foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) { script.StopAllCoroutines(); script.enabled = false; }
            foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>(true)) agent.enabled = false;
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) { body.useGravity = false; body.isKinematic = true; body.constraints = RigidbodyConstraints.FreezeAll; }
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }
        private static void FreezeActors()
        {
            foreach (var identity in Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (identity.Enemy != null) DisableTree(identity.gameObject);
        }
        private static SharedDepartureState Replica(NetworkRunner runner) => Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static async Task Start(NetworkRunner runner, string room)
        {
            var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
            config.PrefabTable = NetworkProjectConfig.Global.PrefabTable; config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            var result = await runner.StartGame(new StartGameArgs { GameMode = GameMode.Shared, Config = config, SessionName = room,
                CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"), AuthValues = new Photon.Realtime.AuthenticationValues(Guid.NewGuid().ToString("N")),
                CustomLobbyName = "deadboat-world266-probe-v1", PlayerCount = 4, IsVisible = false,
                SceneManager = runner.GetComponent<LobbySceneManager>(), ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>() });
            Require(result.Ok, "Cloud connection " + result.ShutdownReason);
        }
        private static async Task Budget(Task task, string label)
        {
            int left = Math.Max(1, 180000 - (int)clock.ElapsedMilliseconds);
            if (await Task.WhenAny(task, Task.Delay(left)) != task)
            { _ = task.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); throw new TimeoutException(label); }
            await task;
        }
        private static async Task Until(Func<bool> test, string label, Fixture fixture = null, int timeout = 10000)
        {
            long end = Math.Min(180000, clock.ElapsedMilliseconds + timeout);
            while (!test()) { Require(clock.ElapsedMilliseconds < end, "timeout: " + label); fixture?.Tick(); await Task.Delay(20); }
        }
        private static async Task Stop(NetworkRunner runner)
        {
            if (runner == null) return;
            var root = runner.gameObject;
            try
            {
                if (runner.IsRunning)
                {
                    var task = runner.Shutdown();
                    if (await Task.WhenAny(task, Task.Delay(8000)) != task) throw new TimeoutException("transport shutdown");
                    await task;
                }
            }
            finally { if (root != null) Object.Destroy(root); }
        }
        private static void MarkRemoval(WorldSpawnIdentity identity) { if (identity != null) Removing?.SetValue(identity, true); }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
