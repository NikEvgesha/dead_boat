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
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // One real local Inventory/world and two Cloud peers in an empty Play scene.
    // Calls production item/UI methods; does not emulate mouse input or run a game loader.
    public static class SharedLoot265GameplayProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] private sealed class ItemResult
        {
            public string prefab, kind;
            public int catalogId;
            public Vector3 fittedScale;
            public float fittedWidth, fittedBoxSides;
            public List<string> passed = new List<string>();
        }
        [Serializable] private sealed class Report
        {
            public string unityVersion, result, error;
            public string scope = "Empty native Play fixture, two real Photon Cloud peers, one local world/Inventory, production fit/claim/PickUp/Drop/Inventory.AddItem/DropOutItem/ItemAttacher trigger/TrySetAttach and cargo application. Presentation shell is transient; InventorySlot templates and localization come from production assets. Sale/burn is delegated to SharedSales271Probe and has a separate report at Logs/Loot265-20261114/sales271.json. Normal item Start, hardware input, independent browser worlds, physics motion, full game/recovery coordinator and WebGL are not tested here.";
            public string saleBurnReport = "Logs/Loot265-20261114/sales271.json";
            public string saleBurnResult = "not run";
            public double elapsedSeconds;
            public bool cleanupComplete;
            public List<ItemResult> items = new List<ItemResult>();
            public List<string> weaponChecks = new List<string>();
        }

        public static string Result { get; private set; } = "not run";
        private static bool running;
        private static Stopwatch clock;
        private static readonly MethodInfo Audit = typeof(SharedItemsRuntime).GetMethod("LateUpdate", Private);
        private static readonly PropertyInfo HeldId = typeof(LobbyNetworkAvatar).GetProperty("HeldItemId", Private);
        private static readonly PropertyInfo RemovingReplica = typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", Private);

        public static async Task RunAsync()
        {
            Require(!running && EditorApplication.isPlaying && SaveManager.Instance == null && LoadingManager.Instance == null &&
                Inventory.Instance == null && PlayerInput.Instance == null && GameManager.Instance == null && !SharedRunContext.Active &&
                !SoloRunBranch.Active && Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length == 0,
                "Requires a clean empty Play scene without owner managers/runners.");
            running = true; Result = "running"; clock = Stopwatch.StartNew();
            var report = new Report { unityVersion = Application.unityVersion };
            var previousMode = NetworkProjectConfig.Global.PeerMode;
            float previousScale = Time.timeScale;
            var previousPhysics = Physics.simulationMode;
            var previousSocial = Object.FindAnyObjectByType<MirraSocialService>();
            NetworkRunner first = null, second = null;
            Fixture fixture = null;
            GameObject fixtureRoot = null;
            IDisposable eggs = null;
            var ids = new HashSet<ulong>();
            Exception failure = null;
            try
            {
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
                Time.timeScale = 1; // The real stow coroutine needs normal frames; bodies are position-locked.
                // Test item transactions/geometry at a fixed orientation, not
                // collision-driven AABB changes while the real stow coroutine runs.
                Physics.simulationMode = SimulationMode.Script;
                eggs = EggFeatureStorage.UseTemporaryStorageForTests(); EggAnimalBuffService.MarkDirty();
                var catalog = SharedItemCatalog.Load();
                var entries = catalog.entries.Where(e => e.prefab != null && e.prefab.name.StartsWith("Loot203_", StringComparison.Ordinal))
                    .OrderBy(e => e.id).ToArray();
                Require(entries.Length == 20 && entries.Select(e => e.id).Distinct().Count() == 20, "20 unique production loot entries");
                var avatarPrefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/Online/Avatars/LobbyNetworkAvatar.prefab");
                Require(avatarPrefab != null && Audit != null && HeldId != null, "production avatar and probe methods");
                // Construct/read all presentation references before Cloud connections.
                fixtureRoot = new GameObject("Loot265 transient gameplay fixture");
                fixture = new Fixture(fixtureRoot);
                var heldCatalog = (LobbyHeldItemCatalog)Get(avatarPrefab.GetComponent<LobbyNetworkAvatar>(), "itemCatalog");
                Require(heldCatalog != null, "production held catalog");
                string room = "loot265-" + Guid.NewGuid().ToString("N");
                first = SharedReadyBarrierProbe.NewRunner(); await Budget(Start(first, room), "first Cloud peer");
                var state = first.Spawn(Resources.Load<NetworkObject>("Online/SharedDepartureState"),
                    onBeforeSpawned: (_, obj) => obj.GetComponent<SharedDepartureState>().Initialize(0, 2)).GetComponent<SharedDepartureState>();
                second = SharedReadyBarrierProbe.NewRunner(); await Budget(Start(second, room), "second Cloud peer");
                await Until(() => state.Phase == 2 && Replica(second) != null && Replica(second).Phase == 2, "sealed crew");
                var remote = Replica(second);
                first.SetPlayerObject(first.LocalPlayer, first.Spawn(avatarPrefab));
                second.SetPlayerObject(second.LocalPlayer, second.Spawn(avatarPrefab));
                await Until(() => first.ActivePlayers.All(p => first.TryGetPlayerObject(p, out var obj) && obj != null) &&
                    second.ActivePlayers.All(p => second.TryGetPlayerObject(p, out var obj) && obj != null), "both peer avatars");
                foreach (var companions in Object.FindObjectsByType<AvatarCompanions>(FindObjectsSortMode.None)) companions.enabled = false;
                state.RPC_SceneReady(); remote.RPC_SceneReady();
                await Until(() => state.Phase == 3 && remote.Phase == 3, "ready barrier");
                SharedRunContext.Begin(state.Seed, state.LevelId, fixture.Root, null, state);
                SharedRunContext.Independent.enabled = false;
                foreach (var entry in entries)
                    await CheckLoot(fixture, state, remote, entry, heldCatalog, ids, report);
                Require(report.items.Count(r => r.kind == "fuel") == 10 && report.items.Count(r => r.kind == "treasure") == 10,
                    "10 fuel / 10 treasure production classes");
                await CheckQuickHand(fixture, state, remote, catalog, heldCatalog, ids, report);
                report.saleBurnResult = "running";
                await SharedSales271Probe.CheckAsync(state, remote, fixture.Tick);
                report.saleBurnResult = SharedSales271Probe.Result;
                Require(report.saleBurnResult.StartsWith("PASS", StringComparison.Ordinal), "separate sale/burn probe: " + report.saleBurnResult);
                report.result = "PASS";
                Result = "PASS: all20 production loot, actual carry/drop/bag/idempotent add/bag dropout/attach/detach, canonical mode/scale on two Cloud peers; existing sword/pistol quick-hand checks; separate sale/burn probe PASS (sales271.json). Physical trigger motion, full recovery/rejoin and WebGL remain untested.";
            }
            catch (Exception error)
            {
                failure = error; report.result = "FAIL"; report.error = error.ToString(); Result = "FAIL: " + error;
            }
            finally
            {
                try
                {
                    SharedRunContext.End();
                    foreach (var identity in Object.FindObjectsByType<WorldSpawnIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (ids.Contains(identity.Id)) { MarkRemoval(identity); Object.Destroy(identity.gameObject); }
                    // UI OnDestroy unsubscribes from the input singleton; dispose it before its host.
                    if (fixture != null && fixture.Ui != null) Object.Destroy(fixture.Ui.gameObject);
                    await Task.Delay(30);
                    if (fixtureRoot != null) Object.Destroy(fixtureRoot);
                    try { await Stop(second); }
                    finally { await Stop(first); }
                    if (previousSocial == null)
                    {
                        var social = Object.FindAnyObjectByType<MirraSocialService>();
                        if (social != null) Object.Destroy(social.gameObject);
                    }
                    await Task.Delay(50);
                    report.cleanupComplete = !SharedRunContext.Active && !SoloRunBranch.Active &&
                        Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length == 0;
                    Require(report.cleanupComplete, "run/runner cleanup");
                }
                catch (Exception error)
                {
                    failure = failure ?? error; report.result = "FAIL"; report.error = (report.error ?? "") + "\nCleanup: " + error;
                    Result = "FAIL: " + report.error;
                }
                finally
                {
                    NetworkProjectConfig.Global.PeerMode = previousMode; Time.timeScale = previousScale; running = false;
                    Physics.simulationMode = previousPhysics;
                    eggs?.Dispose(); EggAnimalBuffService.MarkDirty();
                    report.elapsedSeconds = clock.Elapsed.TotalSeconds;
                    Directory.CreateDirectory("Logs/Loot265-20261114");
                    File.WriteAllText("Logs/Loot265-20261114/gameplay.json", JsonUtility.ToJson(report, true));
                }
            }
            if (failure != null) throw new InvalidOperationException(Result, failure);
            UnityEngine.Debug.Log("[Loot265 gameplay] " + Result);
        }

        private static async Task CheckLoot(Fixture f, SharedDepartureState state, SharedDepartureState remote,
            SharedItemCatalog.Entry entry, LobbyHeldItemCatalog heldCatalog, HashSet<ulong> ids, Report report)
        {
            var item = MakeItem(entry.prefab, f.Fit);
            var row = new ItemResult { prefab = entry.prefab.name, catalogId = entry.id,
                kind = item.GetComponent<FuelItem>() != null ? "fuel" : "treasure" };
            report.items.Add(row);
            Require(!item.Usable && item.GetComponent<ActivateItem>() == null && heldCatalog.FindId(item) == 0,
                row.prefab + " remains non-equippable");
            LocationContentSpawner.PlaceItemAtSpawnPoint(item.gameObject, new LocationSpawnPoint
            { spawnTransform = f.Fit, alignVisualBase = true, maxVisualFootprint = .72f });
            Physics.SyncTransforms();
            row.fittedScale = SharedItemScale.Capture(item.transform); row.fittedWidth = Width(item); row.fittedBoxSides = BoxSides(item);
            Require(row.fittedWidth <= .721f && row.fittedWidth > 0 && row.fittedBoxSides > 0, row.prefab + " fitted bounds");
            var identity = item.gameObject.AddComponent<WorldSpawnIdentity>(); identity.ItemTemplate = entry.id;
            identity.Key = "test:loot265-" + Guid.NewGuid().ToString("N"); ulong sourceId = identity.Id; ids.Add(sourceId);
            state.RegisterItem(identity);
            await Until(() => remote.Items.TryGet(identity.Id, out var r) && Same(r.WorldScale, row.fittedScale), row.prefab + " register", f);
            row.passed.Add("production fit/register/remote scale");
            Require(SharedItemsRuntime.Request(item, claimed => claimed.PickUp(f.Hand)), "claim defers pickup");
            await Until(() => item.Grabbed && item.Status == ItemStatus.Grabbed && remote.Items[identity.Id].Mode == 1 &&
                remote.Items[identity.Id].Owner == state.Runner.LocalPlayer, row.prefab + " actual pickup", f);
            Require(RemoteHeld(remote.Runner, state.Runner.LocalPlayer) == 0, "world carry does not add an equipment visual");
            CheckShape(item, row); row.passed.Add("actual Request/PickUp and remote world carry/no held ID");
            item.Drop(); LockBody(item);
            await Until(() => remote.Items[identity.Id].Mode == 0 && remote.Items[identity.Id].Owner == PlayerRef.None,
                row.prefab + " actual drop", f);
            Require(!item.Grabbed && item.Status == ItemStatus.Free, "drop is free before bag"); row.passed.Add("actual Drop");
            f.Bag.AddItem(item); // First call may return false while the real claim/action is pending.
            await Until(() => f.Bag.GetInventoryList().Contains(item) && remote.Items[identity.Id].Mode == 2,
                row.prefab + " actual bag add", f);
            Require(item.Status == ItemStatus.InInventory, "bag item status");
            var slot = f.Slots().Single(s => s.CurrentItem == item);
            Require(!slot.QuickSlot && f.Active.Active == null, "loot uses bag, not quick/hand");
            int count = f.Bag.GetInventoryList().Count;
            Require(f.Bag.AddItem(item) && f.Bag.GetInventoryList().Count == count, "duplicate bag insertion is idempotent");
            await Until(() => !item.gameObject.activeSelf, row.prefab + " real stow animation", f);
            row.passed.Add("actual Inventory.AddItem/bag slot/no quick/idempotent add/stow");
            f.Bag.DropOutItem(item, slot, f.Drop); LockBody(item);
            await Until(() => item.gameObject.activeSelf && item.Status == ItemStatus.Free &&
                remote.Items[identity.Id].Mode == 0 && remote.Items[identity.Id].Owner == PlayerRef.None,
                row.prefab + " real slot dropout", f);
            Require(!f.Bag.GetInventoryList().Contains(item) && slot.CurrentItem == null, "dropout clears bag and actual slot");
            CheckShape(item, row); row.passed.Add("actual DropOutItem/slot clear/visible fitted replica");

            var attacher = item.GetComponent<ItemAttacher>(); Require(attacher != null, row.prefab + " ItemAttacher");
            int mask = ((LayerMask)Get(attacher, "_attachLayer")).value;
            Require(mask != 0, "production attach mask");
            f.CargoZone.gameObject.layer = Enumerable.Range(0, 32).First(n => (mask & (1 << n)) != 0);
            typeof(ItemAttacher).GetMethod("OnTriggerEnter", Private).Invoke(attacher, new object[] { f.CargoZone });
            Require(attacher.InAttachZone, "actual attach-zone trigger");
            SharedItemsRuntime.Request(item, claimed => claimed.TrySetAttach(true));
            await Until(() => remote.Items[identity.Id].Mode == 3 && remote.Items[identity.Id].Owner == PlayerRef.None &&
                item.Attached && item.transform.parent == f.Boat.transform, row.prefab + " cargo attachment", f);
            var local = remote.Items[identity.Id].BoatLocalPosition;
            Require((item.transform.localPosition - local).sqrMagnitude < .00001f, "canonical cargo local position");
            f.Boat.transform.position += Vector3.forward * .25f; f.Tick();
            Require((item.transform.position - f.Boat.transform.TransformPoint(local)).sqrMagnitude < .00001f, "cargo follows the actual boat parent");
            CheckShape(item, row); row.passed.Add("actual ItemAttacher trigger/TrySetAttach and canonical cargo application");
            SharedItemsRuntime.Request(item, claimed => claimed.TrySetAttach(false));
            await Until(() => !item.Attached && item.Status == ItemStatus.Free && item.transform.parent == null &&
                remote.Items[identity.Id].Mode == 0 && remote.Items[identity.Id].Owner == PlayerRef.None,
                row.prefab + " detach", f);
            typeof(ItemAttacher).GetMethod("OnTriggerExit", Private).Invoke(attacher, new object[] { f.CargoZone });
            Require(!attacher.InAttachZone, "zone cleanup"); CheckShape(item, row); row.passed.Add("actual detach/zone exit/scale retained");
            // Cleanup only: no sale/burn is simulated or claimed by this probe.
            MarkRemoval(identity); Object.Destroy(item.gameObject);
            await Until(() => item == null, row.prefab + " replica cleanup");
            RemoveRecord(state, sourceId);
            f.Boat.transform.position = Vector3.zero;
        }

        private static async Task CheckQuickHand(Fixture f, SharedDepartureState state, SharedDepartureState remote,
            SharedItemCatalog catalog, LobbyHeldItemCatalog heldCatalog, HashSet<ulong> ids, Report report)
        {
            var sword = catalog.entries.First(e => e.prefab != null && e.prefab.GetComponentInChildren<MeleWeapon>(true) != null && heldCatalog.FindId(e.prefab) != 0);
            var pistol = catalog.entries.First(e => e.prefab != null && e.prefab.GetComponentInChildren<RangedWeaponController>(true)?.GetWeaponType() == WeaponType.Pistol && heldCatalog.FindId(e.prefab) != 0);
            foreach (var entry in new[] { sword, pistol })
            {
                var item = MakeItem(entry.prefab, f.Fit);
                Require(item.Usable, "existing weapon remains usable");
                var gun = item.GetComponentInChildren<RangedWeaponController>(true); if (gun != null) gun.ApplySharedAmmo(3);
                var identity = item.gameObject.AddComponent<WorldSpawnIdentity>(); identity.ItemTemplate = entry.id;
                identity.Key = "test:loot265-weapon-" + Guid.NewGuid().ToString("N"); ulong id = identity.Id; ids.Add(id);
                state.RegisterItem(identity);
                await Until(() => remote.Items.ContainsKey(id), "weapon register", f);
                f.Bag.AddItem(item);
                await Until(() => f.Bag.GetInventoryList().Contains(item) && remote.Items[id].Mode == 2, "weapon quick insert", f);
                var slot = f.Slots().Single(s => s.CurrentItem == item);
                Require(slot.QuickSlot && f.Active.Active == item && item.transform.parent == f.Hand && item.GetComponent<ActivateItem>().IsActive,
                    "real quick slot and active hand");
                await Until(() => RemoteHeld(remote.Runner, state.Runner.LocalPlayer) == heldCatalog.FindId(item), "replicated existing held ID", f);
                if (gun != null) Require(gun.CurrentAmmo == 3 && remote.Items[id].WeaponAmmo == 3, "pistol magazine preserved");
                report.weaponChecks.Add(entry.prefab.name + ": actual Inventory quick slot/ActiveItemManager hand/replicated held ID" + (gun != null ? "/magazine3" : ""));
                f.Bag.DropOutItem(item, slot, f.Drop); LockBody(item);
                await Until(() => f.Active.Active == null && remote.Items[id].Mode == 0 && RemoteHeld(remote.Runner, state.Runner.LocalPlayer) == 0,
                    "weapon dropout clears hand", f);
                MarkRemoval(identity); Object.Destroy(item.gameObject);
                await Until(() => item == null, "weapon replica cleanup"); RemoveRecord(state, id);
            }
        }

        private sealed class Fixture
        {
            public readonly GameObject Root;
            public readonly Inventory Bag;
            public readonly InventoryUI Ui;
            public readonly ActiveItemManager Active;
            public readonly BoardController Boat;
            public readonly Transform Hand, Drop, Fit;
            public readonly BoxCollider CargoZone;
            private readonly SharedItemsRuntime runtime;
            private double nextTick;
            public Fixture(GameObject root)
            {
                Root = root;
                Root.AddComponent<GameManager>().enabled = false;
                var loading = Root.AddComponent<LoadingManager>(); loading.enabled = false; loading.CurrentLocation = Location.Game;
                Root.AddComponent<PlayerInput>().enabled = false;
                var player = Child("Player", Root.transform); player.AddComponent<PlayerMovement>().enabled = false;
                // Awake initializes the ammo dictionary; no Start/Save/RestoreRecovery is called.
                player.AddComponent<PlayerAmmoManager>().enabled = false;
                var loc = Root.AddComponent<LocalizationManager>(); loc.enabled = false;
                var managers = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers.prefab");
                Set(loc, "localizationData", managers.GetComponentInChildren<LocalizationManager>(true).LocalizationData);
                Set(loc, "currentLanguage", "Ru");
                BuildControlUI();
                Hand = Child("Hand", player.transform).transform; Drop = Child("Drop", player.transform).transform;
                Drop.position = Vector3.right; Fit = Child("Fit", Root.transform).transform;
                var bagHost = Child("Inventory", player.transform);
                Active = bagHost.AddComponent<ActiveItemManager>(); Active.enabled = false; Set(Active, "_handPoint", Hand);
                Bag = bagHost.AddComponent<Inventory>(); Bag.enabled = false; Set(Bag, "_dropOutPoint", Drop);
                Ui = BuildInventoryUI();
                Invoke(Ui, "Start"); Invoke(Bag, "Start");
                var board = Child("Boat", Root.transform); Boat = board.AddComponent<BoardController>(); Boat.enabled = false;
                var zone = Child("Cargo zone", board.transform); CargoZone = zone.AddComponent<BoxCollider>(); CargoZone.isTrigger = true;
                runtime = Root.AddComponent<SharedItemsRuntime>(); runtime.enabled = false;
            }
            public InventorySlot[] Slots() => Ui.GetComponentsInChildren<InventorySlot>(true);
            public void Tick()
            {
                if (!SharedRunContext.Playing || Time.realtimeSinceStartupAsDouble < nextTick) return;
                nextTick = Time.realtimeSinceStartupAsDouble + .1;
                Audit.Invoke(runtime, null);
            }
            private void BuildControlUI()
            {
                var go = Child("ControlUI", Root.transform); go.SetActive(false);
                var control = go.AddComponent<ControlUI>(); control.enabled = false;
                Set(control, "_descktopHints", new DesktopHints
                { common = Child("Common", go.transform), pickUp = Child("Pickup", go.transform), putToInventory = Child("Bag hint", go.transform),
                    attach = Child("Attach", go.transform), rotate = Child("Rotate", go.transform), healing = Child("Use", go.transform) });
                go.SetActive(true);
            }
            private InventoryUI BuildInventoryUI()
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameCanvas.prefab").GetComponentInChildren<InventoryUI>(true);
                Require(source != null, "production InventoryUI source");
                var go = Child("Inventory UI", Root.transform, typeof(RectTransform), typeof(Canvas)); go.SetActive(false);
                var ui = go.AddComponent<InventoryUI>(); ui.enabled = false;
                Set(ui, "_InventorySlotPrefab", Get(source, "_InventorySlotPrefab")); Set(ui, "_QuickSlotPrefab", Get(source, "_QuickSlotPrefab"));
                Set(ui, "_inventorySlotsParent", Child("Bag slots", go.transform, typeof(RectTransform)).transform);
                Set(ui, "_quickSlotsParent", Child("Quick slots", go.transform, typeof(RectTransform)).transform);
                Set(ui, "_panel", Child("Panel", go.transform)); Set(ui, "_dropOutPanel", Child("Drop panel", go.transform));
                Set(ui, "_capacityText", Child("Capacity", go.transform, typeof(RectTransform), typeof(Text)).GetComponent<Text>());
                Set(ui, "_capacityButtonText", Child("Capacity HUD", go.transform, typeof(RectTransform), typeof(Text)).GetComponent<Text>());
                Set(ui, "_buttonAnimation", go.AddComponent<Animator>());
                Set(ui, "_inventoryImagePrefab", Child("Inventory icon", go.transform, typeof(RectTransform), typeof(Image)).GetComponent<Image>());
                go.SetActive(true); return ui;
            }
        }

        private static PickableItem MakeItem(PickableItem prefab, Transform point)
        {
            var item = SharedItemScale.CreateReplica(prefab, point.position, Quaternion.identity, Vector3.zero);
            // Preserve production components and handlers, but suppress their unrelated automatic Start/Update.
            // MonoBehaviour methods/coroutines still run on an active GameObject.
            foreach (var behaviour in item.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            item.CheckComponents(); LockBody(item); return item;
        }
        private static void LockBody(PickableItem item)
        { foreach (var body in item.GetComponentsInChildren<Rigidbody>(true)) { body.constraints = RigidbodyConstraints.FreezeAll; body.useGravity = false; body.isKinematic = true; } }
        private static void CheckShape(PickableItem item, ItemResult row)
        {
            Physics.SyncTransforms();
            Require(Same(SharedItemScale.Capture(item.transform), row.fittedScale) && Mathf.Abs(Width(item) - row.fittedWidth) < .001f &&
                Mathf.Abs(BoxSides(item) - row.fittedBoxSides) < .001f, row.prefab + " instance shape/size retained; scale=" +
                SharedItemScale.Capture(item.transform) + "/" + row.fittedScale + "; width=" + Width(item) + "/" + row.fittedWidth +
                "; box=" + BoxSides(item) + "/" + row.fittedBoxSides + "; rotation=" + item.transform.eulerAngles);
        }
        private static float Width(PickableItem item)
        {
            var renderers = item.GetComponentsInChildren<Renderer>(true); Require(renderers.Length > 0, "native renderer bounds");
            var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return Mathf.Max(bounds.size.x, bounds.size.z);
        }
        private static float BoxSides(PickableItem item)
        {
            var box = item.GetComponent<BoxCollider>(); Require(box != null, "production BoxCollider");
            var sides = Vector3.Scale(box.size, box.transform.lossyScale); return Mathf.Max(Mathf.Abs(sides.x), Mathf.Abs(sides.z));
        }
        private static byte RemoteHeld(NetworkRunner runner, PlayerRef owner)
        {
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == runner && avatar.Object.StateAuthority == owner) return (byte)HeldId.GetValue(avatar);
            return byte.MaxValue;
        }
        private static GameObject Child(string name, Transform parent, params Type[] components)
        { var go = new GameObject(name, components); go.transform.SetParent(parent, false); return go; }
        private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
        private static void MarkRemoval(WorldSpawnIdentity identity)
        { Require(RemovingReplica != null, "replica removal fixture property"); RemovingReplica.SetValue(identity, true); }
        private static void RemoveRecord(SharedDepartureState state, ulong id)
        { foreach (var page in SharedWorldPage.All(state.Runner)) if (page.Object.HasStateAuthority) page.Items.Remove(id); }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
        private static bool Same(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < .000001f;
        private static SharedDepartureState Replica(NetworkRunner runner) => Object.FindObjectsByType<SharedDepartureState>(FindObjectsSortMode.None).FirstOrDefault(s => s.Runner == runner);
        private static async Task Start(NetworkRunner runner, string room)
        {
            var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
            config.PrefabTable = NetworkProjectConfig.Global.PrefabTable; config.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            var result = await runner.StartGame(new StartGameArgs { GameMode = GameMode.Shared, Config = config, SessionName = room,
                CustomPhotonAppSettings = SharedRunRecovery.Settings("eu"), AuthValues = new Photon.Realtime.AuthenticationValues(Guid.NewGuid().ToString("N")),
                CustomLobbyName = "deadboat-loot265-probe-v1", PlayerCount = 2, IsVisible = false,
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
        private static async Task Until(Func<bool> condition, string label, Fixture fixture = null)
        {
            double end = Math.Min(180000, clock.ElapsedMilliseconds + 8000);
            while (!condition())
            { Require(clock.ElapsedMilliseconds < end, "timeout: " + label); fixture?.Tick(); await Task.Delay(20); }
        }
        private static async Task Stop(NetworkRunner runner)
        {
            if (runner == null) return;
            var root = runner.gameObject;
            try
            {
                var shutdown = runner.Shutdown(); int left = Math.Max(1, Math.Min(8000, 199000 - (int)clock.ElapsedMilliseconds));
                if (await Task.WhenAny(shutdown, Task.Delay(left)) == shutdown) await shutdown;
                else { _ = shutdown.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted); throw new TimeoutException("transport cleanup"); }
            }
            finally { if (root != null) Object.Destroy(root); }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
