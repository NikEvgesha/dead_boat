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
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Dedicated batch probe after the network fixture disposes its runners,
    // managers and world. Never prepares an owner's persistent save provider.
    public static class WorldActions266SoloProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private const string SourcePath = "Assets/Prefabs/Locations/Enemy/ruins_1.prefab";
        private const int Region = 26603;
        private static readonly MethodInfo Create = typeof(SoloRunBranch).GetMethod("Create", StaticPrivate);
        private static readonly MethodInfo SampleItem = typeof(SoloRunBranch).GetMethod("SampleWorldItem", Private);
        private static readonly FieldInfo BranchInstance = typeof(SoloRunBranch).GetField("instance", StaticPrivate);
        private static readonly FieldInfo RuntimeInstance = typeof(SharedWorldActionsRuntime).GetField("instance", StaticPrivate);
        private static readonly PropertyInfo Removing = typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", Private);
        private static readonly string[] ReadOnlyKeys = { "DeadBoat.SoloBranch.v1", "DeadBoat.SharedReturn.v1",
            "DeadBoat.FusionIdentity.v1", "DeadBoat.RewardWallet.v1", "EggFeatureState_v1" };

        [Serializable] private sealed class Report
        {
            public string result, utc, unityVersion, error;
            public string scope = "Dedicated empty native Play fixture after network cleanup. Reflection invokes production RAM-only SoloRunBranch.Create; Suspend disposes it. Actual ruins source clone, stable generation key, production action Update/ambush factory/podium callbacks, real DTO JSON roundtrip and canonical item-marker repair are exercised. One production SampleWorldItem call is explicitly invoked against a live actual Skull after consumption and deferred Destroy in the same synchronous continuation, then the cold factory is checked before and after visual destruction. Automatic MonoBehaviour Start/Update, guard AI/navigation, physics and normal SoloRunBranch.Update are suppressed. No Begin/Store/Clear/RestoreStartup, SaveManager, persistent provider, LocalId generation, Photon runners, owner save writes, full GameLoader/ContinueSolo UI, actual streaming transaction, real combat, WebGL or PlayerBuild are claimed.";
            public int configuredPodiums, decorativePodiums, guardRecords, releasedActors;
            public float initialRemaining, restoredRemaining;
            public string trapId, podiumId;
            public bool beforeDue, remainingReleased, deadNotResurrected, repeatNoWave,
                jsonRoundtrip, cursorRestore, markerRepair, sameFrameCapture, headerRepair, legacyDefaults,
                ownerKeysUnchanged, cleanup;
            public double elapsedSeconds;
            public List<string> passed = new List<string>();
        }
        private sealed class KeyState { public bool Exists; public string Value; }
        private sealed class Location
        {
            public GameObject Root;
            public DungeonTrap Trap;
            public CheckItemOnLocation Podium;
            public Portal[] Portals;
            public ulong TrapId, PodiumId;
        }
        public static string Result { get; private set; } = "not run";
        private static bool running;
        private static Stopwatch clock;

        public static async Task RunAsync()
        {
            Require(!running && EditorApplication.isPlaying && SaveManager.Instance == null &&
                PlayerMovement.Instance == null && GameManager.Instance == null && !SharedRunContext.Active &&
                !SoloRunBranch.Active && Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length == 0,
                "Solo probe requires the network fixture to finish all cleanup in an empty Play scene.");
            Require(Create != null && SampleItem != null && BranchInstance != null && RuntimeInstance != null && Removing != null,
                "Production RAM-only reflection seams must exist.");
            running = true; Result = "running"; clock = Stopwatch.StartNew();
            var report = new Report { utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
            var keys = ReadOnlyKeys.ToDictionary(k => k, k => new KeyState { Exists = PlayerPrefs.HasKey(k), Value = PlayerPrefs.GetString(k, "") });
            float oldScale = Time.timeScale; var oldPhysics = Physics.simulationMode;
            IDisposable eggs = null; GameObject owner = null; Location live = null; Exception failure = null;
            try
            {
                Time.timeScale = 1; Physics.simulationMode = SimulationMode.Script;
                eggs = EggFeatureStorage.UseTemporaryStorageForTests(); EggAnimalBuffService.MarkDirty();
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
                Require(source != null, "Actual ruins source exists.");
                owner = new GameObject("World266 solo transient fixture");
                owner.AddComponent<GameManager>().enabled = false;
                var playerRoot = new GameObject("World266 solo transient player"); playerRoot.transform.SetParent(owner.transform);
                var player = playerRoot.AddComponent<PlayerMovement>(); player.enabled = false; playerRoot.tag = "Player";
                var character = playerRoot.GetComponent<CharacterController>(); if (character != null) character.enabled = false;
                var loc = owner.AddComponent<LocalizationManager>(); loc.enabled = false;
                var managers = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers.prefab");
                Set(loc, "localizationData", managers.GetComponentInChildren<LocalizationManager>(true).LocalizationData);
                Set(loc, "currentLanguage", "Ru");

                // The first RAM-only branch binds the real authored paths; IDs are not fabricated.
                CreateRam(Empty()); live = Clone(source, owner);
                ulong trapId = live.TrapId, podiumId = live.PodiumId;
                var template = live.Trap.ScriptedEnemyPrefab;
                ulong templateId = SharedAmbushCatalog.TemplateId(template);
                Vector3 spawn = live.Trap.ScriptedSpawnPosition.position + LobbyNetworkAvatar.Origin;
                Quaternion rotation = template.transform.rotation;
                Require(live.Trap.ScriptedSpawnCount == 20, "Actual ruins authored wave must contain20 guards.");
                report.trapId = trapId.ToString(); report.podiumId = podiumId.ToString();
                report.configuredPodiums = live.Root.GetComponentsInChildren<CheckItemOnLocation>(true).Count(p => p.RequiredItems.Count > 0);
                report.decorativePodiums = live.Root.GetComponentsInChildren<CheckItemOnLocation>(true).Count(p => p.RequiredItems.Count == 0);
                Require(report.configuredPodiums == 1 && report.decorativePodiums == 3, "One configured and three decorative podiums.");
                await Retire(live); live = null;

                var partial = Empty();
                partial.WorldActions = new[] { new SoloWorldAction { Id = trapId, Kind = (int)WorldActionKind.Trap,
                    Flags = 2, Spawned = 1, Total = 20, Remaining = .2f, Interval = .03f,
                    Region = Region, Position = spawn } };
                partial.Enemies = Enumerable.Range(0, 20).Select(i => new SoloWorldEnemy { Id = SharedAmbushCatalog.GuardId(trapId, i),
                    ActionId = trapId, PendingSpawn = i != 0, HP = 0, MaxHP = SharedAmbushCatalog.InitialHealth(template, 5),
                    Template = templateId, Level = 5, Position = spawn, Rotation = rotation, Prunable = true }).ToArray();
                string json = JsonUtility.ToJson(partial);
                var decoded = JsonUtility.FromJson<SharedSoloSnapshot>(json);
                Require(decoded.WorldActions.Length == 1 && decoded.WorldActions[0].Id == trapId &&
                    decoded.WorldActions[0].Flags == 2 && decoded.WorldActions[0].Spawned == 1 &&
                    Mathf.Abs(decoded.WorldActions[0].Remaining - .2f) < .0001f &&
                    decoded.Enemies.Length == 20 && decoded.Enemies[0].ActionId == trapId && !decoded.Enemies[0].PendingSpawn &&
                    decoded.Enemies.Skip(1).All(e => e.PendingSpawn && e.Level == 5 && e.MaxHP == SharedAmbushCatalog.InitialHealth(template, 5)),
                    "Real DTO JSON preserves ulong IDs, pending flags, level/HP and remaining timer.");
                report.jsonRoundtrip = true;
                CreateRam(decoded); live = Clone(source, owner); SameIds(live, trapId, podiumId);
                var action = Action(trapId); report.initialRemaining = action.Remaining;
                Require(action.Remaining > .15f && action.Remaining <= .201f, "Restored remaining is a relative .2s timer.");
                float notBefore = Time.time + action.Remaining;
                while (Time.time < notBefore)
                {
                    Pump();
                    if (Time.time < notBefore) Require(Action(trapId).Spawned == 1 && ActorCount(trapId) == 0, "Pending guards must not spawn before saved due.");
                    await Delay(15);
                }
                report.beforeDue = true;
                await Until(() => Action(trapId).Spawned == 20, "remaining19 guards released");
                Require(ActorCount(trapId) == 19 && EnemyRows().Count == 20 &&
                    EnemyRows().Skip(0).All(e => !e.PendingSpawn), "Exactly19 living actors,20 canonical records, no pending remainder.");
                Require(SoloRunBranch.TryActionEnemy(SharedAmbushCatalog.GuardId(trapId, 0), out var dead) &&
                    dead.HP == 0 && !dead.PendingSpawn && !SharedItemsRuntime.TryFind(dead.Id, out _), "Previously dead ordinal0 must remain dead without actor.");
                report.remainingReleased = report.deadNotResurrected = true; report.guardRecords = 20; report.releasedActors = 19;
                live.Trap._InTrap(); live.Trap._InTrap();
                await Settle();
                Require(Action(trapId).Spawned == 20 && ActorCount(trapId) == 19 && EnemyRows().Count == 20, "Repeated adapter trigger does not create a second wave.");
                report.repeatNoWave = true;
                report.passed.Add("Actual ruins generation26603: relative .2s saved delay,19 pending releases at recorded level5, dead ordinal0 skipped, total/cursor20 and duplicate trigger idempotency.");

                var captured = Empty(); captured.WorldActions = SharedWorldActionsRuntime.CaptureSolo(); captured.Enemies = EnemyRows().ToArray();
                captured.Items = ItemRows().ToArray();
                var capturedJson = JsonUtility.ToJson(captured);
                await Retire(live); live = null;
                CreateRam(JsonUtility.FromJson<SharedSoloSnapshot>(capturedJson)); live = Clone(source, owner); SameIds(live, trapId, podiumId);
                Invoke(Branch(), "ApplyWorldInternal"); FreezeActors(); await Settle();
                Require(Action(trapId).Flags == 2 && Action(trapId).Spawned == 20 && Action(trapId).Total == 20 &&
                    Action(trapId).Remaining == -1 && ActorCount(trapId) == 19 && !SharedItemsRuntime.TryFind(dead.Id, out _),
                    "RAM destroy/recreate plus actual cold world factory retains completed cursor and does not resurrect dead guard.");
                report.restoredRemaining = Action(trapId).Remaining; report.cursorRestore = true;
                report.passed.Add("CaptureSolo and canonical enemy DTO→JSON→Suspend/destroy→Create/rebind→ApplyWorldInternal restore19 living actors, one dead record and completed trap flags/cursor without retrigger.");
                await Retire(live); live = null;

                ulong itemId = WorldSpawnIdentity.IdForKey("test:world266-solo-offering");
                var marker = Empty();
                marker.Items = new[] { new SoloWorldItem { Id = itemId, Mode = 4, ConsumedByAction = podiumId, ActionSlot = 0,
                    Position = spawn, Rotation = Quaternion.identity, WorldScale = Vector3.one } };
                CreateRam(JsonUtility.FromJson<SharedSoloSnapshot>(JsonUtility.ToJson(marker))); live = Clone(source, owner);
                Require(Action(podiumId).Flags == 1 && Action(podiumId).ConsumedItem == itemId, "Orphan consumed item marker reconstructs missing podium header during Begin.");
                int calls = 0; live.Podium.PutItem.AddListener(_ => calls++);
                await Until(() => Unlocked(live), "orphan marker repairs portal chain"); await Settle();
                Require(calls == 1 && ItemRows().Single().Mode == 4, "Repaired marker fires actual configured podium callbacks once.");
                report.markerRepair = true;

                // Exercise the periodic capture's exact production helper while Destroy
                // is deferred. A full Solo.Update would require and write owner saves.
                var catalog = SharedItemCatalog.Load();
                var skullEntry = catalog.entries.First(e => e.prefab != null && e.prefab.Data != null && e.prefab.Data.Name == "Skull");
                var raceSkull = SharedItemScale.CreateReplica(skullEntry.prefab, live.Podium.transform.position,
                    Quaternion.identity, Vector3.zero);
                DisableTree(raceSkull.gameObject);
                var raceIdentity = raceSkull.gameObject.AddComponent<WorldSpawnIdentity>();
                ulong raceId = WorldSpawnIdentity.IdForKey("test:world266-solo-offering-capture-race");
                raceIdentity.ItemTemplate = skullEntry.id; raceIdentity.InitializeRemote(raceId, 0);
                Require(raceSkull.Status == ItemStatus.Free && !raceSkull.Grabbed && !raceSkull.Attached,
                    "Race fixture uses a real free Skull visual, not an inventory/attached state that would hide the regression.");
                SampleItem.Invoke(Branch(), new object[] { raceIdentity });
                Require(ItemRows().Single(i => i.Id == raceId).Mode == 0, "Baseline production sampler captures the free Skull as Mode0.");
                SoloRunBranch.ConsumeWorldActionItem(raceIdentity, podiumId, 0);
                Removing.SetValue(raceIdentity, true); Object.Destroy(raceSkull.gameObject);
                Require(raceIdentity != null && raceSkull != null, "Skull visual must still be alive before deferred Destroy completes.");
                SampleItem.Invoke(Branch(), new object[] { raceIdentity });
                var sampled = ItemRows().Single(i => i.Id == raceId);
                Require(sampled.Mode == 4 && sampled.ConsumedByAction == podiumId && sampled.ActionSlot == 0 &&
                    sampled.Template == skullEntry.id, "Same-frame periodic sampler preserves accepted consumption marker/slot and Mode4.");
                Invoke(Branch(), "ApplyWorldInternal");
                Require(Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(i => i.Id == raceId) == 1, "Cold factory creates no duplicate while consumed Skull visual is still pending destruction.");
                await Until(() => raceIdentity == null && !SharedItemsRuntime.TryFind(raceId, out _), "consumed Skull deferred visual destruction");
                Invoke(Branch(), "ApplyWorldInternal");
                Require(!SharedItemsRuntime.TryFind(raceId, out _) &&
                    ItemRows().Single(i => i.Id == raceId).Mode == 4 &&
                    Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(i => i.Id != raceId),
                    "After deferred destruction the real cold factory does not resurrect the sampled offering.");
                report.sameFrameCapture = true;
                report.passed.Add("Actual free Skull: production ConsumeWorldActionItem→deferred Destroy→production SampleWorldItem in the same synchronous continuation retains Mode4/podiumId/slot0; ApplyWorldInternal duplicates neither the pending visual nor the destroyed offering. No full save or persistent provider was invoked.");
                await Retire(live); live = null;

                var header = Empty();
                header.Items = new[] { new SoloWorldItem { Id = itemId, Mode = 0, Position = spawn, Rotation = Quaternion.identity } };
                header.WorldActions = new[] { new SoloWorldAction { Id = podiumId, Kind = (int)WorldActionKind.Podium,
                    Flags = 1, ConsumedItem = itemId, Region = Region, Position = spawn, Remaining = -1 } };
                CreateRam(JsonUtility.FromJson<SharedSoloSnapshot>(JsonUtility.ToJson(header)));
                Require(ItemRows().Single().Mode == 4, "Accepted header consumes matching Mode0 item in the production RAM dictionary.");
                live = Clone(source, owner); calls = 0; live.Podium.PutItem.AddListener(_ => calls++);
                await Until(() => Unlocked(live), "accepted header restores portal chain"); await Settle();
                Require(calls == 1 && ItemRows().Single().Mode == 4, "Reverse partial header repair remains consumed and once-only.");
                report.headerRepair = true;
                report.passed.Add("Real configured Skull podium only: orphan consumed marker reconstructs mask1/header and both portals; reverse accepted header changes Mode0 canonical item to4; actual callbacks fire once after each cold bind.");
                await Retire(live); live = null;

                var legacy = JsonUtility.FromJson<SharedSoloSnapshot>("{\"Seed\":26603,\"Level\":5,\"Items\":[],\"Enemies\":[{\"Id\":1,\"HP\":0,\"MaxHP\":10,\"Level\":5,\"Template\":0}]}");
                Require(legacy.WorldActions == null && legacy.Enemies.Length == 1 && !legacy.Enemies[0].PendingSpawn && legacy.Enemies[0].ActionId == 0,
                    "Legacy JSON omitted fields receive compatible null/false/0 defaults.");
                CreateRam(legacy); live = Clone(source, owner); Pump(); Invoke(Branch(), "ApplyWorldInternal"); FreezeActors();
                Require(SharedWorldActionsRuntime.CaptureSolo().Length == 0 && ActorCount(trapId) == 0,
                    "Legacy JSON accepted by actual RAM Create/rebind with no fabricated wave or dead actor.");
                report.legacyDefaults = true;
                report.passed.Add("Legacy JSON missing WorldActions/PendingSpawn/ActionId parses with compatible defaults and production RAM Create tolerates it; startup save/UI restore is outside scope.");
            }
            catch (Exception error) { failure = error; report.error = error.ToString(); }
            finally
            {
                try
                {
                    await Retire(live);
                    if (owner != null) { Object.Destroy(owner); await Until(() => owner == null, "fixture managers destroyed", false); }
                    eggs?.Dispose(); eggs = null; EggAnimalBuffService.MarkDirty();
                    report.cleanup = !SoloRunBranch.Active && !SharedRunContext.Active && RuntimeInstance.GetValue(null) == null &&
                        SaveManager.Instance == null && PlayerMovement.Instance == null && GameManager.Instance == null &&
                        Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length == 0;
                    Require(report.cleanup, "All transient solo managers/runtime/actors cleaned without runners or owner SaveManager.");
                    report.ownerKeysUnchanged = keys.All(p => PlayerPrefs.HasKey(p.Key) == p.Value.Exists && PlayerPrefs.GetString(p.Key, "") == p.Value.Value);
                    Require(report.ownerKeysUnchanged, "Known owner solo/reconnect/identity/reward/egg keys unchanged (read only).");
                }
                catch (Exception cleanup) { failure ??= cleanup; report.error = (report.error ?? "") + "\nCleanup: " + cleanup; }
                finally
                {
                    eggs?.Dispose(); Time.timeScale = oldScale; Physics.simulationMode = oldPhysics;
                    running = false; report.elapsedSeconds = clock.Elapsed.TotalSeconds;
                    report.result = failure == null ? "PASS" : "FAIL";
                    Result = report.result + ": native RAM-only solo actions; " + report.elapsedSeconds.ToString("F2") + "s";
                    Directory.CreateDirectory("Logs/WorldActions266-20261114");
                    File.WriteAllText("Logs/WorldActions266-20261114/solo.json", JsonUtility.ToJson(report, true));
                }
            }
            if (failure != null) throw new InvalidOperationException(Result, failure);
            UnityEngine.Debug.Log("[World266 solo] " + Result);
        }

        private static SharedSoloSnapshot Empty() => new SharedSoloSnapshot { Seed = 26603, Level = 5, Rear = -100000,
            SourceRun = "transient-solo266", PlayerId = 0, Items = Array.Empty<SoloWorldItem>(), Enemies = Array.Empty<SoloWorldEnemy>(),
            WorldActions = Array.Empty<SoloWorldAction>(), Sales = Array.Empty<SoloSaleState>(), Bag = Array.Empty<SharedRecoveryItem>() };
        private static SoloRunBranch Branch() => (SoloRunBranch)BranchInstance.GetValue(null);
        private static SharedWorldActionsRuntime Runtime() => (SharedWorldActionsRuntime)RuntimeInstance.GetValue(null);
        private static void CreateRam(SharedSoloSnapshot value)
        {
            Require(!SoloRunBranch.Active && Runtime() == null, "Previous branch must finish teardown before RAM Create.");
            Create.Invoke(null, new object[] { value });
            Branch().enabled = false; Runtime().enabled = false;
            Require(SoloRunBranch.ReadyForWorldActions && SaveManager.Instance == null && !SharedRunContext.Active,
                "RAM-only Create is ready without owner save or Shared context.");
        }
        private static Location Clone(GameObject source, GameObject owner)
        {
            var stage = new GameObject("World266 solo inactive staging"); stage.transform.SetParent(owner.transform); stage.SetActive(false);
            var root = Object.Instantiate(source, stage.transform); root.name = source.name;
            root.transform.position = Vector3.zero; DisableTree(root);
            var content = root.GetComponentsInChildren<LocationContentSpawner>(true).Single();
            content.InitializeGeneration(Region); content.SetLevel(5);
            root.transform.SetParent(owner.transform, true); Object.Destroy(stage); root.SetActive(true);
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component is ActivateZone || component is DungeonTrap || component is ActivateObject ||
                    component is Lever || component is Portal || component is CheckItemOnLocation) SharedWorldActionsRuntime.Register(component);
            var trap = root.GetComponentsInChildren<DungeonTrap>(true).Single();
            var podium = root.GetComponentsInChildren<CheckItemOnLocation>(true).Single(p => p.RequiredItems.Count > 0);
            return new Location { Root = root, Trap = trap, Podium = podium, Portals = root.GetComponentsInChildren<Portal>(true),
                TrapId = SharedWorldActionsRuntime.FindNode(trap).Id, PodiumId = SharedWorldActionsRuntime.FindNode(podium).Id };
        }
        private static void SameIds(Location value, ulong trap, ulong podium) => Require(value.TrapId == trap && value.PodiumId == podium,
            "Actual procedural stable IDs persist across independently cloned source roots.");
        private static List<SoloWorldEnemy> EnemyRows() => ((Dictionary<ulong, SoloWorldEnemy>)typeof(SoloRunBranch).GetField("enemies", Private).GetValue(Branch())).Values.ToList();
        private static List<SoloWorldItem> ItemRows() => ((Dictionary<ulong, SoloWorldItem>)typeof(SoloRunBranch).GetField("items", Private).GetValue(Branch())).Values.ToList();
        private static SoloWorldAction Action(ulong id) => SharedWorldActionsRuntime.CaptureSolo().Single(a => a.Id == id);
        private static int ActorCount(ulong trap) => Enumerable.Range(0, 20).Count(i => SharedItemsRuntime.TryFind(SharedAmbushCatalog.GuardId(trap, i), out var actor) && actor != null && actor.Enemy != null);
        private static bool Unlocked(Location value) => value.Portals.Length == 2 && value.Portals.All(p =>
            SharedWorldActionsRuntime.CaptureSolo().Any(a => a.Id == SharedWorldActionsRuntime.FindNode(p).Id && (a.Flags & 1) != 0) &&
            (bool)typeof(Portal).GetField("_isActivate", Private).GetValue(p));
        private static void Pump()
        {
            Require(clock.Elapsed.TotalSeconds < 60, "Solo probe global60s deadline.");
            if (Runtime() != null) Invoke(Runtime(), "Update");
            FreezeActors();
        }
        private static void FreezeActors()
        {
            foreach (var identity in Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (identity.Enemy != null) DisableTree(identity.gameObject);
        }
        private static void DisableTree(GameObject root)
        {
            foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) { script.StopAllCoroutines(); script.enabled = false; }
            foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>(true)) agent.enabled = false;
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) { body.useGravity = false; body.isKinematic = true; body.constraints = RigidbodyConstraints.FreezeAll; }
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }
        private static async Task Retire(Location location)
        {
            GameObject branch = Branch() != null ? Branch().gameObject : null;
            SoloRunBranch.Suspend(); // RAM-only teardown: never Clear/DeleteKey.
            foreach (var identity in Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { Removing.SetValue(identity, true); Object.Destroy(identity.gameObject); }
            if (location?.Root != null) Object.Destroy(location.Root);
            await Until(() => branch == null && (location == null || location.Root == null) &&
                Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "RAM branch/location/actors destroyed", false);
            SharedItemsRuntime.Clear(); SharedEnemiesRuntime.Clear();
            Require(Runtime() == null && !SoloRunBranch.Active, "Old owner OnDestroy finished before next Create.");
        }
        private static async Task Until(Func<bool> condition, string label, bool pump = true)
        {
            double deadline = Math.Min(60, clock.Elapsed.TotalSeconds + 8);
            do { if (pump) Pump(); if (condition()) return; await Task.Delay(20); }
            while (clock.Elapsed.TotalSeconds < deadline);
            throw new TimeoutException("Solo probe8s/global60s: " + label);
        }
        private static async Task Settle() { for (int i = 0; i < 8; i++) { Pump(); await Delay(20); } }
        private static async Task Delay(int milliseconds) { Require(clock.Elapsed.TotalSeconds < 60, "Solo probe global60s deadline."); await Task.Delay(milliseconds); }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    }
}
