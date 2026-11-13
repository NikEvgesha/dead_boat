using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    // Separate from the two-peer fixture: ordinary offline branches, RAM wallet only.
    public static class SharedSalesOfflineProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] private sealed class Report
        {
            public string result, error, unityVersion;
            public string scope = "Empty native Play scene after network-fixture cleanup. Production SellPoint/MoneyBag and two distinct Treasure prefabs; real sale handler and MoneyBag collection coroutine with controlled PlayerInput hold. Trigger methods are invoked, not physical trigger motion. SaveManager/SharedRunPreviewSave/CurrencyManager are transient and RAM-only; no loader, PlayerPrefs writes, SDK persistence, network, full recovery or Player Build. Legacy Sales checks cover JSON DTO compatibility only, not disk restore.";
            public int initialCoins, firstPrice, secondPrice, checks, payouts;
            public bool cleanupComplete;
            public double elapsedSeconds;
            public List<string> passed = new List<string>();
        }
        public static string Result { get; private set; } = "not run";
        private static bool running;

        public static async Task RunAsync()
        {
            Require(!running && EditorApplication.isPlaying && !SharedRunContext.Active && !SoloRunBranch.Active &&
                SaveManager.Instance == null && CurrencyManager.Instance == null && PlayerInput.Instance == null &&
                Inventory.Instance == null && LoadingManager.Instance == null &&
                Object.FindObjectsByType<Fusion.NetworkRunner>(FindObjectsSortMode.None).Length == 0 &&
                !DeadBoat.FirstRun.FirstRunSession.Active, "Requires complete network/manager cleanup in an empty Play scene.");
            running = true; Result = "running";
            var report = new Report { unityVersion = Application.unityVersion, initialCoins = 37 };
            var clock = Stopwatch.StartNew();
            float previousScale = Time.timeScale;
            GameObject root = null;
            IDisposable eggs = null;
            var spawned = new List<GameObject>();
            Exception failure = null;
            void Check(bool value, string label)
            { Require(value, label); report.checks++; report.passed.Add(label); }
            async Task Until(Func<bool> condition, string label)
            {
                double end = Math.Min(20000, clock.ElapsedMilliseconds + 5000);
                while (!condition())
                { Require(clock.ElapsedMilliseconds < end, "timeout: " + label); await Task.Delay(20); }
                Check(true, label);
            }
            try
            {
                Time.timeScale = 1;
                // Avoid the migration path in ordinary EggFeatureStorage.Load used by sale bonuses.
                eggs = EggFeatureStorage.UseTemporaryStorageForTests(); EggAnimalBuffService.MarkDirty();
                root = new GameObject("Sales271 ordinary offline RAM fixture"); root.SetActive(false);
                var ram = root.AddComponent<SharedRunPreviewSave>(); ram.enabled = false;
                var save = root.AddComponent<SaveManager>(); save.enabled = false; Set(save, "saveProvider", ram);
                var wallet = root.AddComponent<CurrencyManager>(); wallet.enabled = false;
                var input = root.AddComponent<PlayerInput>(); input.enabled = false;
                var localization = root.AddComponent<LocalizationManager>(); localization.enabled = false;
                var managers = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers.prefab");
                Set(localization, "localizationData", managers.GetComponentInChildren<LocalizationManager>(true).LocalizationData);
                Set(localization, "currentLanguage", "Ru");
                root.SetActive(true); save.StopAllCoroutines();
                Check(SaveManager.Instance == save && save.PersistentProvider == ram && ram.Persistent == null &&
                    PlayerInput.Instance == input && CurrencyManager.Instance == wallet, "RAM-only providers installed before Awake");
                wallet.AddCurrency(CurrencyType.Coins, report.initialCoins);
                Check(ram.LoadGameCoin() == report.initialCoins, "initial coins stored only in RAM provider");

                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SellPoint.prefab");
                Check(source != null && source.GetComponentInChildren<SellPoint>(true) != null, "production SellPoint source");
                var pointRoot = Object.Instantiate(source, root.transform); DisableAutomatic(pointRoot);
                var point = pointRoot.GetComponentInChildren<SellPoint>(true); Set(point, "_type", StoreType.Items);
                Check(Get(point, "_moneyBagPrefab") is MoneyBag, "production MoneyBag binding");
                var entries = SharedItemCatalog.Load().entries.Where(e => e.prefab != null && e.prefab.name.StartsWith("Loot203_", StringComparison.Ordinal)).ToArray();
                var treasures = entries.Where(e => e.prefab.GetComponent<SellableItem>() != null && e.prefab.GetComponent<FuelItem>() == null).Take(2).ToArray();
                var fuelEntry = entries.First(e => e.prefab.GetComponent<FuelItem>() != null && e.prefab.GetComponent<SellableItem>() == null);
                Check(treasures.Length == 2 && treasures[0].id != treasures[1].id, "two distinct production Treasure entries and non-sale Fuel entry");
                PickableItem Spawn(SharedItemCatalog.Entry entry)
                {
                    var item = SharedItemScale.CreateReplica(entry.prefab, point.transform.position, Quaternion.identity, Vector3.zero);
                    spawned.Add(item.gameObject); DisableAutomatic(item.gameObject); item.CheckComponents();
                    foreach (var body in item.GetComponentsInChildren<Rigidbody>(true)) { body.useGravity = false; body.isKinematic = true; }
                    Require(item.GetComponent<BoxCollider>() != null, "production item collider"); return item;
                }
                var first = Spawn(treasures[0]); report.firstPrice = first.GetComponent<SellableItem>().GetReward();
                Check(report.firstPrice > 0, "first production sale price positive");
                Invoke(point, "OnTriggerEnter", first.GetComponent<BoxCollider>());
                var bag = point.CheckpointBag;
                Check(bag != null && bag.Money == report.firstPrice && bag.SalePoint == point, "first offline sale creates and binds actual MoneyBag");
                spawned.Add(bag.gameObject); DisableAutomatic(bag.gameObject);
                Invoke(point, "OnTriggerEnter", first.GetComponent<BoxCollider>());
                Check(point.CheckpointBag == bag && bag.Money == report.firstPrice, "duplicate first enter cannot sell twice before deferred destruction");
                await Until(() => first == null, "first sold Treasure destroyed");
                var fuel = Spawn(fuelEntry);
                Invoke(point, "OnTriggerEnter", fuel.GetComponent<BoxCollider>());
                await Task.Delay(80);
                Check(fuel != null && point.CheckpointBag == bag && bag.Money == report.firstPrice && wallet.Coins == report.initialCoins,
                    "offline item sale rejects Fuel and leaves wallet/bag unchanged");
                var second = Spawn(treasures[1]); report.secondPrice = second.GetComponent<SellableItem>().GetReward();
                Invoke(point, "OnTriggerEnter", second.GetComponent<BoxCollider>());
                int expected = report.firstPrice + report.secondPrice;
                Check(report.secondPrice > 0 && point.CheckpointBag == bag && bag.Money == expected && bag.SalePoint == point,
                    "second offline sale accumulates in the same bag");
                await Until(() => second == null, "second sold Treasure destroyed");
                Check(wallet.Coins == report.initialCoins && ram.LoadGameCoin() == report.initialCoins,
                    "selling alone does not credit wallet");

                var player = new GameObject("Offline collection trigger", typeof(BoxCollider)); player.transform.SetParent(root.transform, false);
                player.tag = "Player"; var playerCollider = player.GetComponent<BoxCollider>(); playerCollider.enabled = false;
                bag.TakeBag += () => report.payouts++;
                Set(input, "_interactionHold", true);
                Invoke(bag, "OnTriggerEnter", playerCollider); Invoke(bag, "TryCollect");
                var routine = Get(bag, "_collectionRoutine"); Invoke(bag, "TryCollect");
                Check(routine != null && ReferenceEquals(routine, Get(bag, "_collectionRoutine")), "duplicate collect keeps one actual coroutine");
                await Task.Delay(70); bag.CancelCollection();
                Check(!(bool)Get(bag, "_collectInProgress") && (float)Get(bag, "_progress") == 0 && Get(bag, "_collectionRoutine") == null &&
                    report.payouts == 0 && wallet.Coins == report.initialCoins, "Cancel clears guard/progress without payout");
                Invoke(bag, "TryCollect"); await Task.Delay(70); Invoke(bag, "OnTriggerExit", playerCollider);
                Check(!(bool)Get(bag, "_collectInProgress") && Get(bag, "_collectionRoutine") == null && bag.Money == expected &&
                    wallet.Coins == report.initialCoins, "trigger exit cancels partial collection without spending bag");
                Set(input, "_interactionHold", false); Invoke(bag, "TryCollect");
                await Task.Delay(30);
                Check(!(bool)Get(bag, "_collectInProgress") && report.payouts == 0, "released hold cannot award coins");
                Set(input, "_interactionHold", true); Invoke(bag, "OnTriggerEnter", playerCollider); Invoke(bag, "TryCollect");
                await Until(() => wallet.Coins == report.initialCoins + expected && point.CheckpointBag == null && report.payouts == 1,
                    "actual coroutine reentry pays accumulated value once and removes bag");
                Set(input, "_interactionHold", false); await Task.Delay(150);
                Check(wallet.Coins == report.initialCoins + expected && ram.LoadGameCoin() == wallet.Coins && report.payouts == 1,
                    "no delayed second wallet/save payout");

                var legacy = JsonUtility.FromJson<SharedSoloSnapshot>("{\"Seed\":271,\"Level\":0,\"Items\":[],\"Enemies\":[],\"Bag\":[]}");
                Check(legacy != null && legacy.Sales == null && (legacy.Sales ?? Array.Empty<SoloSaleState>()).Length == 0,
                    "legacy JSON without Sales deserializes to null and empty-compatible collection");
                legacy.Sales = new[] { new SoloSaleState { Id = 271, Coins = expected } };
                var roundTrip = JsonUtility.FromJson<SharedSoloSnapshot>(JsonUtility.ToJson(legacy));
                Check(roundTrip.Sales.Length == 1 && roundTrip.Sales[0].Id == 271 && roundTrip.Sales[0].Coins == expected,
                    "real SharedSoloSnapshot sales DTO round-trip");
                report.result = "PASS";
                Result = "PASS: ordinary offline first bag creation, second Treasure accumulation, Fuel rejection, actual collect coroutine duplicate/cancel/exit/reentry, one RAM wallet payout, legacy Sales JSON DTO compatibility. No owner-save writes, network or WebGL.";
            }
            catch (Exception error) { failure = error; report.result = "FAIL"; report.error = error.ToString(); Result = "FAIL: " + error; }
            finally
            {
                try
                {
                    if (PlayerInput.Instance != null && root != null && PlayerInput.Instance.transform.IsChildOf(root.transform))
                        Set(PlayerInput.Instance, "_interactionHold", false);
                    if (SaveManager.Instance != null && root != null && SaveManager.Instance.transform.IsChildOf(root.transform))
                        SaveManager.Instance.StopAllCoroutines();
                    foreach (var obj in spawned) if (obj != null) Object.Destroy(obj);
                    if (root != null) Object.Destroy(root);
                    await Task.Delay(80);
                    report.cleanupComplete = SaveManager.Instance == null && CurrencyManager.Instance == null && PlayerInput.Instance == null &&
                        !SharedRunContext.Active && !SoloRunBranch.Active;
                    Require(report.cleanupComplete, "offline fixture manager cleanup");
                }
                catch (Exception error)
                { failure = failure ?? error; report.result = "FAIL"; report.error = (report.error ?? "") + "\nCleanup: " + error; Result = "FAIL: " + report.error; }
                finally
                {
                    eggs?.Dispose(); EggAnimalBuffService.MarkDirty(); Time.timeScale = previousScale; running = false;
                    report.elapsedSeconds = clock.Elapsed.TotalSeconds;
                    Directory.CreateDirectory("Logs/Loot265-20261114"); File.WriteAllText("Logs/Loot265-20261114/offline271.json", JsonUtility.ToJson(report, true));
                }
            }
            if (failure != null) throw new InvalidOperationException(Result, failure);
            UnityEngine.Debug.Log("[Sales271 offline] " + Result);
        }

        private static void DisableAutomatic(GameObject root)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }
        private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
