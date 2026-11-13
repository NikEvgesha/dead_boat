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
    // Called inside the isolated Loot265 fixture, never in an owner's gameplay scene.
    public static class SharedSales271Probe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly MethodInfo ApplyCredits = typeof(SharedSalesRuntime).GetMethod("ApplyCredits", StaticPrivate);
        private static readonly PropertyInfo RemovingReplica = typeof(WorldSpawnIdentity).GetProperty("RemovingReplica", Private);
        [Serializable] private sealed class Report
        {
            public string result, error;
            public string scope = "Two real Cloud peers in the caller's empty Play fixture and one local world/wallet. Production SellPoint/FuelDeposit trigger methods are invoked explicitly with real prefab colliders, not physical trigger motion. Actual MoneyBag collection coroutine uses controlled PlayerInput hold. Missing-authority-binding retry temporarily selects the second peer context only for its synchronous request wrapper; no independent second world is claimed. Delayed feedback is a deterministic replay of an old collection revision. Wallet/save components are RAM-only and have automatic Start/saving disabled. No owner PlayerPrefs, SDK persistence, full game/recovery UI, independent rendered clients or Player Build. Authority takeover is not performed here; the separate264 fixture covers it. Ordinary offline sale/collection is left to a separate fixture.";
            public int treasures, fuels, checks, totalSaleValue;
            public bool cleanupComplete;
            public List<string> passed = new List<string>();
        }
        public static string Result { get; private set; } = "not run";

        public static async Task CheckAsync(SharedDepartureState state, SharedDepartureState remote, Action tick)
        {
            var report = new Report();
            var clock = Stopwatch.StartNew();
            GameObject root = null;
            SellPoint point = null;
            ulong pointId = 0;
            var ownedIds = new HashSet<ulong>();
            var objects = new List<GameObject>();
            var input = PlayerInput.Instance;
            var player = PlayerMovement.Instance;
            bool previousHold = input != null && (bool)Get(input, "_interactionHold");
            Vector3 previousPlayerPosition = player != null ? player.transform.position : Vector3.zero;
            float previousFuel = 0, previousMaxFuel = 0, previousFill = 0;
            int previousApplied = SharedSalesRuntime.AppliedCredits;
            int localSlot = -1, remoteSlot = -1, oldLocalCredits = 0, oldRemoteCredits = 0;
            bool hadLocalData = false, hadRemoteData = false;
            SharedPersonalData oldLocalData = default, oldRemoteData = default;
            var preview = SharedRunContext.Save;
            int previousPreviewCoins = preview != null ? preview.LoadGameCoin() : 0;
            bool initialized = false;
            bool suppressSaleTick = false;
            Exception failure = null;
            Action pump = () =>
            {
                tick?.Invoke();
                if (point != null)
                {
                    if (!suppressSaleTick) SharedSalesRuntime.Tick(point);
                    var bag = point.CheckpointBag;
                    if (bag != null) DisableAutomaticBehaviours(bag.gameObject);
                }
                if (state != null && state.Object != null && state.Object.IsValid) ApplyCredits?.Invoke(null, new object[] { state });
            };
            void Check(bool condition, string label)
            {
                report.checks++;
                if (!condition) throw new InvalidOperationException("Sales271: " + label);
            }
            async Task Until(Func<bool> condition, string label)
            {
                double until = clock.Elapsed.TotalSeconds + 8;
                while (!condition())
                {
                    if (clock.Elapsed.TotalSeconds >= until) throw new TimeoutException("Sales271: " + label);
                    pump(); await Task.Delay(20);
                }
                pump();
            }
            async Task Settle()
            {
                for (int i = 0; i < 10; i++) { pump(); await Task.Delay(20); }
            }
            async Task<PickableItem> Spawn(SharedItemCatalog.Entry entry)
            {
                var item = SharedItemScale.CreateReplica(entry.prefab, point.transform.position, Quaternion.identity, Vector3.zero);
                objects.Add(item.gameObject); DisableAutomaticBehaviours(item.gameObject);
                item.CheckComponents();
                foreach (var body in item.GetComponentsInChildren<Rigidbody>(true))
                { body.constraints = RigidbodyConstraints.FreezeAll; body.useGravity = false; body.isKinematic = true; }
                LocationContentSpawner.PlaceItemAtSpawnPoint(item.gameObject, new LocationSpawnPoint
                { spawnTransform = point.transform, alignVisualBase = true, maxVisualFootprint = .72f });
                SharedItemsRuntime.PublishDrop(item);
                var id = item.GetComponent<WorldSpawnIdentity>();
                Check(id != null, entry.prefab.name + " actual personal-drop identity");
                ownedIds.Add(id.Id);
                await Until(() => state.Items.TryGet(id.Id, out var a) && remote.Items.TryGet(id.Id, out var b) &&
                    a.Dropped && a.Mode == 0 && b.Mode == 0 && a.Owner == Fusion.PlayerRef.None,
                    entry.prefab.name + " dropped free publication");
                return item;
            }

            try
            {
                Result = "running";
                Check(SharedRunContext.Playing && state == SharedRunContext.State && state.Object.HasStateAuthority &&
                    remote != null && input != null && player != null && Inventory.Instance != null &&
                    BoardController.Instance != null && SaveManager.Instance == null && CurrencyManager.Instance == null,
                    "requires clean caller fixture, live peers and no owner save/wallet");
                Check(ApplyCredits != null && RemovingReplica != null, "production audit methods");
                localSlot = state.CrewSlot(state.Runner.LocalPlayer); remoteSlot = state.CrewSlot(remote.Runner.LocalPlayer);
                Check(localSlot >= 0 && remoteSlot >= 0 && localSlot != remoteSlot, "two sealed crew slots");
                oldLocalCredits = state.SaleCredits[localSlot]; oldRemoteCredits = state.SaleCredits[remoteSlot];
                hadLocalData = state.Personal.TryGet(state.Runner.LocalPlayer, out oldLocalData);
                hadRemoteData = state.Personal.TryGet(remote.Runner.LocalPlayer, out oldRemoteData);
                previousFuel = state.BoatFuel; previousMaxFuel = state.BoatMaxFuel; previousFill = state.BoatFillMultiplier;
                initialized = true;

                // Both concrete providers here keep coins in fields. Neither points at an owner provider.
                root = new GameObject("Sales271 RAM-only fixture"); root.SetActive(false);
                var ram = root.AddComponent<SharedRunPreviewSave>(); ram.enabled = false;
                var save = root.AddComponent<SaveManager>(); save.enabled = false; Set(save, "saveProvider", ram);
                var wallet = root.AddComponent<CurrencyManager>(); wallet.enabled = false;
                root.SetActive(true); save.StopAllCoroutines();
                Check(ram.Persistent == null && save.PersistentProvider == ram && SaveManager.Instance == save &&
                    CurrencyManager.Instance == wallet, "RAM SaveManager/wallet initialized without persistent backend");
                wallet.AddCurrency(CurrencyType.Coins, 100);
                SharedSalesRuntime.RestoreCredits(oldLocalCredits);
                state.RPC_RecoveryProgress(new SharedPersonalData { Coins = 100, Health = 100, SaleCredits = oldLocalCredits });
                remote.RPC_RecoveryProgress(new SharedPersonalData { Coins = 50, Health = 100, SaleCredits = oldRemoteCredits });
                await Until(() => state.Personal.TryGet(state.Runner.LocalPlayer, out var a) && a.Coins == 100 &&
                    state.Personal.TryGet(remote.Runner.LocalPlayer, out var b) && b.Coins == 50, "initial RAM progress");
                var staleLocal = state.Personal[state.Runner.LocalPlayer];
                var staleRemote = state.Personal[remote.Runner.LocalPlayer];

                var pointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SellPoint.prefab");
                Check(pointPrefab != null && pointPrefab.GetComponentInChildren<SellPoint>(true) != null, "production SellPoint prefab");
                var saleRoot = Object.Instantiate(pointPrefab, root.transform); objects.Add(saleRoot);
                DisableAutomaticBehaviours(saleRoot);
                foreach (var collider in saleRoot.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                point = saleRoot.GetComponentInChildren<SellPoint>(true);
                point.transform.position = BoardController.Instance.transform.position + Vector3.right * 2;
                Set(point, "_type", StoreType.Items);
                pointId = SharedSalesRuntime.Key(point);
                Check(Get(point, "_moneyBagPrefab") is MoneyBag, "actual MoneyBag prefab binding");

                var boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoardController.prefab");
                var depositSource = boardPrefab != null ? boardPrefab.GetComponentInChildren<FuelDeposit>(true) : null;
                Check(depositSource != null, "production FuelDeposit source");
                var deposit = Object.Instantiate(depositSource, BoardController.Instance.transform);
                objects.Add(deposit.gameObject); deposit.transform.localPosition = Vector3.zero;
                DisableAutomaticBehaviours(deposit.gameObject);
                foreach (var collider in deposit.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                deposit.trainController = BoardController.Instance;
                SetProperty(state, "BoatMaxFuel", 10000f); SetProperty(state, "BoatFuel", 50f); SetProperty(state, "BoatFillMultiplier", 1f);
                var entries = SharedItemCatalog.Load().entries.Where(e => e.prefab != null &&
                    e.prefab.name.StartsWith("Loot203_", StringComparison.Ordinal)).OrderBy(e => e.prefab.name).ToArray();
                Check(entries.Length == 20, "all20 production loot entries");
                int saleTotal = 0;
                foreach (var entry in entries)
                {
                    var item = await Spawn(entry);
                    ulong id = item.GetComponent<WorldSpawnIdentity>().Id;
                    var collider = item.GetComponent<BoxCollider>(); Check(collider != null, "production root BoxCollider");
                    var fuel = item.GetComponent<FuelItem>();
                    if (fuel != null)
                    {
                        Invoke(point, "OnTriggerEnter", collider); await Settle();
                        Check(state.Items[id].Mode == 0 && (!state.Sales.TryGet(pointId, out var rejected) || rejected.Coins == saleTotal),
                            entry.prefab.name + " fuel cannot be sold");
                        float before = state.BoatFuel, amount = fuel.fuelValue;
                        item.transform.position = deposit.transform.position; Physics.SyncTransforms();
                        Invoke(deposit, "OnTriggerEnter", collider); Invoke(deposit, "OnTriggerEnter", collider);
                        await Until(() => state.Items[id].Mode == 4 && remote.Items[id].Mode == 4 &&
                            Mathf.Abs(state.BoatFuel - before - amount) < .001f, entry.prefab.name + " actual burn once");
                        state.RPC_BurnItem(id); await Settle();
                        Check(Mathf.Abs(state.BoatFuel - before - amount) < .001f, entry.prefab.name + " duplicate burn ignored");
                        report.fuels++;
                    }
                    else
                    {
                        Check(item.GetComponent<SellableItem>() != null, "treasure class");
                        float beforeFuel = state.BoatFuel;
                        Invoke(deposit, "OnTriggerEnter", collider); state.RPC_BurnItem(id); await Settle();
                        Check(state.Items[id].Mode == 0 && Mathf.Abs(state.BoatFuel - beforeFuel) < .001f,
                            entry.prefab.name + " treasure cannot be burned");
                        int price = item.GetComponent<SellableItem>().GetReward(); saleTotal += price;
                        if (report.treasures == 0)
                        {
                            Check(state.Items[id].SalePrice == price + 1, "drop publishes seller's encoded canonical price");
                            var localPrice = item.GetComponent<SellableItem>();
                            Set(localPrice, "_cost", (int)Get(localPrice, "_cost") + 17);
                            Check(localPrice.GetReward() != price, "local authority price differs from the stored seller price");
                        }
                        Invoke(point, "OnTriggerEnter", collider); Invoke(point, "OnTriggerEnter", collider);
                        await Until(() => state.Items[id].Mode == 4 && remote.Items[id].Mode == 4 &&
                            state.Sales.TryGet(pointId, out var sale) && sale.Coins == saleTotal,
                            entry.prefab.name + " dropped free sale once");
                        state.RPC_SellItem(id, pointId, price); remote.RPC_SellItem(id, pointId, price); await Settle();
                        Check(state.Sales[pointId].Coins == saleTotal && remote.Sales[pointId].Coins == saleTotal,
                            entry.prefab.name + " duplicate/non-owner sale ignored");
                        Check(point.CheckpointBag != null && point.CheckpointBag.Money == saleTotal &&
                            point.CheckpointBag.SalePoint == point, "one actual canonical MoneyBag balance");
                        report.treasures++;
                    }
                    await Until(() => !SharedItemsRuntime.TryFind(id, out _), entry.prefab.name + " consumed replica removed");
                    await Settle();
                    Check(!SharedItemsRuntime.TryFind(id, out _), "consumed dropped item is not rediscovered");
                }
                Check(report.fuels == 10 && report.treasures == 10 && saleTotal > 0, "10 burns/10 sales");
                report.totalSaleValue = saleTotal;
                report.passed.Add("all10 actual FuelDeposit burns and10 dropped-free SellPoint sales; opposite-type/duplicate transactions rejected and canonical seller price wins over changed authority price");

                var offered = state.Sales[pointId];
                remote.RPC_CollectSale(pointId, offered.Revision - 1); await Settle();
                Check(state.Sales[pointId].Coins == saleTotal && state.SaleCredits[remoteSlot] == oldRemoteCredits,
                    "wrong collection revision rejected");
                player.transform.position = point.transform.position + Vector3.right * 100;
                await Until(() => Far(state, remote.Runner.LocalPlayer, offered.Position), "remote avatar far from bag");
                remote.RPC_CollectSale(pointId, offered.Revision); await Settle();
                Check(state.Sales[pointId].Coins == saleTotal, "distant collection rejected");
                player.transform.position = previousPlayerPosition;
                await Until(() => Near(state, state.Runner.LocalPlayer, offered.Position) && Near(state, remote.Runner.LocalPlayer, offered.Position),
                    "both avatars back near bag");

                var actualBag = point.CheckpointBag; Check(actualBag != null, "actual balance visual before collection");
                var playerCollider = new GameObject("Sales271 player trigger", typeof(BoxCollider)); objects.Add(playerCollider);
                playerCollider.transform.SetParent(root.transform, false); playerCollider.tag = "Player";
                Set(input, "_interactionHold", true);
                Invoke(actualBag, "OnTriggerEnter", playerCollider.GetComponent<BoxCollider>());
                Invoke(actualBag, "TryCollect"); var routine = Get(actualBag, "_collectionRoutine");
                Invoke(actualBag, "TryCollect");
                Check(routine != null && ReferenceEquals(routine, Get(actualBag, "_collectionRoutine")), "duplicate collect does not start another coroutine");
                await Task.Delay(70); actualBag.CancelCollection();
                Check(!(bool)Get(actualBag, "_collectInProgress") && (float)Get(actualBag, "_progress") == 0,
                    "explicit cancellation clears progress/guard");
                Invoke(actualBag, "TryCollect"); await Task.Delay(70);
                Invoke(actualBag, "OnTriggerExit", playerCollider.GetComponent<BoxCollider>());
                Check(!(bool)Get(actualBag, "_collectInProgress") && Get(actualBag, "_collectionRoutine") == null &&
                    state.Sales[pointId].Coins == saleTotal, "exit cancels without spending balance");
                Invoke(actualBag, "OnTriggerEnter", playerCollider.GetComponent<BoxCollider>());
                Invoke(actualBag, "TryCollect");
                await Until(() => state.Sales[pointId].Coins == 0 && state.SaleCredits[localSlot] == oldLocalCredits + saleTotal &&
                    wallet.Coins == 100 + saleTotal && point.CheckpointBag == null, "real coroutine reentry collection and one local credit");
                Set(input, "_interactionHold", false);
                state.RPC_CollectSale(pointId, offered.Revision); remote.RPC_CollectSale(pointId, offered.Revision); await Settle();
                Check(wallet.Coins == 100 + saleTotal && state.SaleCredits[remoteSlot] == oldRemoteCredits,
                    "duplicate collect cannot award either peer twice");
                for (int i = 0; i < 2; i++) { state.RPC_RecoveryProgress(staleLocal); await Settle(); }
                Check(state.Personal[state.Runner.LocalPlayer].Coins == 100 + saleTotal &&
                    state.Personal[state.Runner.LocalPlayer].SaleCredits == oldLocalCredits + saleTotal && wallet.Coins == 100 + saleTotal,
                    "stale progress reconciles cumulative credits without duplicate wallet grant");
                report.passed.Add("wrong revision/range rejected; actual MoneyBag duplicate guard, Cancel/exit/reentry coroutine and exactly-once local credits/stale progress");

                var extra = entries.First(e => e.prefab.GetComponent<SellableItem>() != null);
                var extraItem = await Spawn(extra); ulong extraId = extraItem.GetComponent<WorldSpawnIdentity>().Id;
                int extraPrice = extraItem.GetComponent<SellableItem>().GetReward();
                Invoke(point, "OnTriggerEnter", extraItem.GetComponent<BoxCollider>());
                await Until(() => state.Items[extraId].Mode == 4 && remote.Sales.TryGet(pointId, out var current) && current.Coins == extraPrice,
                    "fresh bag for remote collector");
                int nextRevision = remote.Sales[pointId].Revision;
                remote.RPC_CollectSale(pointId, nextRevision); remote.RPC_CollectSale(pointId, nextRevision);
                await Until(() => state.SaleCredits[remoteSlot] == oldRemoteCredits + extraPrice && state.Sales[pointId].Coins == 0,
                    "remote collect credited once");
                remote.RPC_RecoveryProgress(staleRemote); await Settle();
                Check(state.Personal[remote.Runner.LocalPlayer].Coins == 50 + extraPrice && wallet.Coins == 100 + saleTotal,
                    "remote credit survives stale progress and never awards the other local wallet");
                report.passed.Add("remote duplicate collection/stale progress preserves remote personal ledger; local wallet unchanged");

                // A missing authority binding must leave the item live. Do not call Tick while missing:
                // Tick intentionally reconstructs the local binding. The second peer's RPC is queued,
                // then the shared test world's authority lookup is removed before the next await.
                var retryItem = await Spawn(extra); ulong retryId = retryItem.GetComponent<WorldSpawnIdentity>().Id;
                remote.RPC_ClaimItem(retryId);
                await Until(() => state.Items[retryId].Owner == remote.Runner.LocalPlayer, "remote retry seller claim");
                int retryPrice = retryItem.GetComponent<SellableItem>().GetReward();
                suppressSaleTick = true;
                AsPeer(remote, () =>
                {
                    Invoke(point, "OnTriggerEnter", retryItem.GetComponent<BoxCollider>());
                    SharedSalesRuntime.Remove(point);
                });
                Check(!SharedSalesRuntime.TryPoint(pointId, out _), "authority binding deliberately unavailable");
                for (int i = 0; i < 33; i++) { pump(); await Task.Delay(20); }
                Check(state.Items[retryId].Mode != 4 && state.Sales[pointId].Coins == 0,
                    "rejected sale without authority binding does not consume or award");
                Check(SharedSalesRuntime.Key(point) == pointId, "same stable binding restored");
                AsPeer(remote, () =>
                {
                    var retryCollider = retryItem.GetComponent<BoxCollider>();
                    Invoke(point, "OnTriggerStay", retryCollider); Invoke(point, "OnTriggerStay", retryCollider);
                });
                await Until(() => state.Items[retryId].Mode == 4 && state.Sales[pointId].Coins == retryPrice,
                    "actual shared OnTriggerStay retries after binding returns");
                await Settle();
                Check(state.Sales[pointId].Coins == retryPrice, "stay retry pays once");
                suppressSaleTick = false; pump();
                int retryRevision = state.Sales[pointId].Revision;
                remote.RPC_CollectSale(pointId, retryRevision);
                await Until(() => state.Sales[pointId].Coins == 0, "clear retry balance before revision race");
                report.passed.Add("remote production TriggerEnter rejected while authority binding absent; stable Key plus TriggerStay retries once after throttle interval");

                var raceFirst = await Spawn(extra); ulong raceFirstId = raceFirst.GetComponent<WorldSpawnIdentity>().Id;
                var raceSecond = await Spawn(extra); ulong raceSecondId = raceSecond.GetComponent<WorldSpawnIdentity>().Id;
                int racePrice = raceFirst.GetComponent<SellableItem>().GetReward();
                int walletBeforeRace = wallet.Coins, creditsBeforeRace = state.SaleCredits[localSlot];
                Invoke(point, "OnTriggerEnter", raceFirst.GetComponent<BoxCollider>());
                await Until(() => state.Items[raceFirstId].Mode == 4 && state.Sales[pointId].Coins == racePrice &&
                    point.CheckpointBag != null, "first balance for revision race");
                var oldBag = point.CheckpointBag;
                int oldRevision = oldBag.SharedRevision;
                Check(oldRevision == state.Sales[pointId].Revision, "visual is bound to offered revision");
                suppressSaleTick = true;
                state.RPC_CollectSale(pointId, oldRevision);
                await Until(() => state.Sales[pointId].Coins == 0 && state.SaleCredits[localSlot] == creditsBeforeRace + racePrice,
                    "first collection before any point Tick");
                Check(point.CheckpointBag == oldBag, "no intervening zero-balance visual Tick");
                Invoke(point, "OnTriggerEnter", raceSecond.GetComponent<BoxCollider>());
                await Until(() => state.Items[raceSecondId].Mode == 4 && state.Sales[pointId].Coins == racePrice,
                    "new sale commits before zero-balance Tick");
                suppressSaleTick = false; pump();
                var newBag = point.CheckpointBag;
                Check(newBag != null && newBag.SharedRevision == state.Sales[pointId].Revision &&
                    newBag.SharedRevision != oldRevision && !newBag.Collected,
                    "new nonzero revision rebinds a collectable bag even when old bag was collected");
                int newConfirmations = 0;
                newBag.TakeBag += () => newConfirmations++;
                newBag.ConfirmSharedCollection(oldRevision); newBag.ConfirmSharedCollection(oldRevision);
                Check(!newBag.Collected && newConfirmations == 0, "delayed old revision feedback cannot collect the new balance");
                Set(input, "_interactionHold", true);
                Invoke(newBag, "OnTriggerEnter", playerCollider.GetComponent<BoxCollider>());
                Invoke(newBag, "TryCollect");
                await Until(() => state.Sales[pointId].Coins == 0 && state.SaleCredits[localSlot] == creditsBeforeRace + racePrice * 2 &&
                    wallet.Coins == walletBeforeRace + racePrice * 2 && point.CheckpointBag == null,
                    "new revision bag still collects through actual coroutine");
                Set(input, "_interactionHold", false);
                Check(newConfirmations == 1, "only matching new collection feedback fires TakeBag once");
                report.passed.Add("collect then new sale before point Tick; replayed old-revision feedback ignored; new revision bag collects through production coroutine");
                report.result = "PASS"; Result = "PASS:20 production sale/burn type transactions, consume/no respawn, canonical bag, revision/range, coroutine cancellation/reentry and two-peer cumulative credits.";
            }
            catch (Exception error) { failure = error; report.result = "FAIL"; report.error = error.ToString(); Result = "FAIL: " + error; }
            finally
            {
                if (input != null) Set(input, "_interactionHold", previousHold);
                if (player != null) player.transform.position = previousPlayerPosition;
                foreach (ulong id in ownedIds)
                {
                    if (SharedItemsRuntime.TryFind(id, out var identity) && identity != null)
                    { RemovingReplica.SetValue(identity, true); Object.Destroy(identity.gameObject); }
                    if (state != null && state.Object != null && state.Object.IsValid && state.Object.HasStateAuthority)
                        foreach (var page in SharedWorldPage.All(state.Runner))
                            if (page.Object.HasStateAuthority) page.Items.Remove(id);
                }
                if (initialized && state != null && state.Object != null && state.Object.IsValid && state.Object.HasStateAuthority)
                {
                    if (point != null) state.Sales.Remove(pointId);
                    state.SaleCredits.Set(localSlot, oldLocalCredits); state.SaleCredits.Set(remoteSlot, oldRemoteCredits);
                    if (hadLocalData) state.Personal.Set(state.Runner.LocalPlayer, oldLocalData); else state.Personal.Remove(state.Runner.LocalPlayer);
                    if (hadRemoteData) state.Personal.Set(remote.Runner.LocalPlayer, oldRemoteData); else state.Personal.Remove(remote.Runner.LocalPlayer);
                    SetProperty(state, "BoatFuel", previousFuel); SetProperty(state, "BoatMaxFuel", previousMaxFuel);
                    SetProperty(state, "BoatFillMultiplier", previousFill);
                }
                SharedSalesRuntime.RestoreCredits(previousApplied);
                if (preview != null) preview.SaveGameCoin(previousPreviewCoins);
                foreach (var value in objects) if (value != null) Object.Destroy(value);
                if (root != null) Object.Destroy(root);
                await Task.Delay(40);
                report.cleanupComplete = SaveManager.Instance == null && CurrencyManager.Instance == null &&
                    ownedIds.All(id => !SharedItemsRuntime.TryFind(id, out _));
                Directory.CreateDirectory("Logs/Loot265-20261114");
                File.WriteAllText("Logs/Loot265-20261114/sales271.json", JsonUtility.ToJson(report, true));
            }
            if (failure != null) throw new InvalidOperationException(Result, failure);
            if (!report.cleanupComplete) throw new InvalidOperationException("Sales271 cleanup did not finish.");
            UnityEngine.Debug.Log("[Sales271 gameplay] " + Result);
        }

        private static bool Near(SharedDepartureState state, Fusion.PlayerRef player, Vector3 position)
        {
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == state.Runner && avatar.Object.StateAuthority == player &&
                    Vector3.Distance(avatar.LogicalPosition, position) <= 6) return true;
            return false;
        }
        private static bool Far(SharedDepartureState state, Fusion.PlayerRef player, Vector3 position)
        {
            foreach (var avatar in LobbyNetworkAvatar.All)
                if (avatar.Runner == state.Runner && avatar.Object.StateAuthority == player &&
                    Vector3.Distance(avatar.LogicalPosition, position) > 20) return true;
            return false;
        }
        private static void DisableAutomaticBehaviours(GameObject root)
        { foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false; }
        private static void AsPeer(SharedDepartureState peer, Action action)
        {
            var property = typeof(SharedRunContext).GetProperty("State", BindingFlags.Static | BindingFlags.Public);
            var previous = SharedRunContext.State;
            property.SetValue(null, peer);
            try { action(); }
            finally { property.SetValue(null, previous); }
        }
        private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void SetProperty(object target, string property, object value)
            => target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static void Invoke(object target, string method, params object[] values)
            => target.GetType().GetMethod(method, Private).Invoke(target, values.Length == 0 ? null : values);
    }
}
