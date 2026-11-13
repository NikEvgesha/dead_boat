using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DeadBoat.Online;
using DeadBoat.Online.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RunWinter175
{
    const string Key = "RunWinter175";
    const string Report = "Docs/locations/previews/event-castle175-play.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static RunWinter175() => EditorApplication.playModeStateChanged += OnPlayMode;
    public static void Main()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LoadingScene.unity");
        SharedPerformanceTestSave.Prepare();
        File.WriteAllText(Report, "");
        SessionState.SetInt(Key, 1);
        EditorApplication.EnterPlaymode();
    }

    static async void OnPlayMode(PlayModeStateChange state)
    {
        int phase = SessionState.GetInt(Key, 0);
        if (state == PlayModeStateChange.EnteredEditMode && phase >= 2)
        {
            SessionState.EraseInt(Key);
            EditorApplication.Exit(phase == 2 ? 0 : 1);
        }
        if (state != PlayModeStateChange.EnteredPlayMode || phase != 1) return;
        EditorApplication.CallbackFunction clock = () => Time.timeScale = 1f;
        EditorApplication.update += clock;
        try
        {
            await Verify();
            Log("PASS");
            SessionState.SetInt(Key, 2);
        }
        catch (Exception e)
        {
            Log("FAIL " + e);
            SessionState.SetInt(Key, 3);
        }
        EditorApplication.update -= clock;
        EditorApplication.ExitPlaymode();
    }

    static async Task Verify()
    {
        await Until(() => LoadingManager.Instance && LoadingManager.Instance.CurrentLocation == Location.Lobby, "Lobby");
        var manager = SaveManager.Instance;
        var preview = manager.PersistentProvider as SharedRunPreviewSave;
        Require(preview && preview.Persistent, "Isolated save initialized");
        var memory = manager.gameObject.AddComponent<Winter175MemorySave>();
        memory.Persistent = preview.Persistent;
        typeof(SaveManager).GetField("saveProvider", Private).SetValue(manager, memory);
        LoadingManager.Instance.LoadLocation(Location.Game, "LevelEvent", false);
        await Until(() => SceneManager.GetActiveScene().name == "LevelEvent" &&
            LoadingManager.Instance.CurrentLocation == Location.Game && BoardController.Instance &&
            BoardController.Instance.StartGame, "LevelEvent loaded through LoadingManager");
        await Task.Yield();
        Require(manager.PersistentProvider == memory, "Isolated save active in LevelEvent");
        var art = GameObject.Find("Winter_175");
        Require(art, "Winter decor active at start");
        var castle = art.transform.parent;
        VerifyCastle(castle);

        var tutorial = TutorialManager.Instance;
        Require(tutorial && tutorial.CurrentStepIndex == 0 && !memory.GetTutorialProgress(), "Tutorial started on a fresh save");
        var steps = Get<List<TutorialStep>>(tutorial, "_tutorialSteps");
        Require(steps.Count > 0 && steps[0].StepEnd != null, "First tutorial step active");
        Log("Tutorial sequence: " + string.Join(", ", steps.Select(s => s.GetType().Name)));
        var player = PlayerStatsManager.Instance.transform;
        var playerCollider = player.GetComponentsInChildren<Collider>().First(c => c.CompareTag("Player"));
        StorePoint tutorialShop = null;
        foreach (var step in steps)
        {
            int index = tutorial.CurrentStepIndex;
            switch (step)
            {
                case TutorialStep1 pick:
                    Get<PickableItem>(pick, "_item").PickUp(player);
                    break;
                case TutorialStep2 sale:
                    var item = Get<PickableItem>(steps.OfType<TutorialStep1>().Single(), "_item");
                    item.Drop();
                    var seller = Get<SellPoint>(sale, "_sellPoint");
                    Trigger(seller, item.GetComponent<Collider>());
                    Require(seller.CheckpointBag && seller.CheckpointBag.Money > 0, "Sale produced a money bag");
                    break;
                case TutorialStep3 collection:
                    var bag = Get<MoneyBag>(collection, "_moneyBag");
                    int coinsBefore = CurrencyManager.Instance.Coins;
                    int reward = bag.Money;
                    Trigger(bag, playerCollider);
                    var collectPanel = Get<BuyTouchHandler>(bag, "_collectTouchPanel");
                    Require(collectPanel.gameObject.activeInHierarchy && collectPanel.PointerDown != null, "Sale reward panel active");
                    collectPanel.OnPointerDown(null);
                    await Until(() => tutorial.CurrentStepIndex > index, "Collect sale reward");
                    Require(CurrencyManager.Instance.Coins == coinsBefore + reward, "Sale credited " + reward + " coins");
                    break;
                case TutorialStep4 pick:
                    Get<PickableItem>(pick, "_item").PickUp(player);
                    break;
                case TutorialStep7 buy:
                    tutorialShop = Get<StorePoint>(buy, "_storePoint");
                    await Buy(tutorialShop, playerCollider);
                    break;
                case TutorialStep8 pickup:
                    var purchased = Get<PickableItem>(pickup, "_item");
                    Require(purchased && purchased == tutorialShop.LastPurchasedItem, "Tutorial tracks issued item");
                    await Task.Yield();
                    purchased.PickUp(player);
                    break;
                case TutorialStep11 fuel:
                    var coal = tutorialShop.LastPurchasedItem;
                    coal.Drop();
                    var deposit = Get<FuelDeposit>(fuel, "_item");
                    var fuelBoard = deposit.trainController;
                    float previousFuel = fuelBoard.currentFuel;
                    Trigger(deposit, coal.GetComponent<Collider>());
                    Require(fuelBoard.currentFuel > previousFuel, "Purchased coal added fuel");
                    break;
                case TutorialStep12 departure:
                    Require(Get<GameObject>(departure, "_canvas").activeInHierarchy, "Departure hint visible");
                    var boardAtStart = BoardController.Instance;
                    boardAtStart.TotalDistanceTraveled = Get<float>(boardAtStart, "_endTutorialDistance") + 1f;
                    await Until(() => tutorial.IntroComplete, "Departure distance completed tutorial");
                    break;
                default:
                    throw new InvalidOperationException("Unsupported tutorial step " + step.GetType().Name);
            }
            Require(tutorial.CurrentStepIndex == index + 1, "Tutorial completed step " + (index + 1));
        }
        Require(tutorial.IntroComplete && memory.GetTutorialProgress(), "Tutorial completion recorded");

        var spawner = Object.FindAnyObjectByType<CitySpawner>();
        Require(spawner && spawner.cityPrefab && spawner.Cities > 1, "CitySpawner configured for multiple castles");
        var board = spawner.boardController;
        float first = spawner.baseSpawnDistance;
        float threshold = first - spawner.spawnBuffer;
        var cities = Get<List<GameObject>>(spawner, "spawnedCities");
        board.TotalDistanceTraveled = threshold - 20;
        await Task.Delay(150);
        Require(cities.Count == 0, "No next castle below the spawn threshold");
        board.TotalDistanceTraveled = threshold + 1;
        await Until(() => cities.Count == 1 && cities[0], "First decorated castle spawned through Update");
        var next = cities[0];
        await Until(() => next.GetComponentsInChildren<StorePoint>().All(s => Get<GameObject>(s, "_displayModel")), "Next castle shops stocked");
        VerifyCastle(next.transform);
        var startMesh = art.GetComponentInChildren<MeshFilter>().sharedMesh;
        Require(next.transform.Find("Winter_175").GetComponentInChildren<MeshFilter>().sharedMesh == startMesh,
            "Both castles use the same winter meshes");
        Require(Mathf.Abs(next.transform.position.z + FixCoordinate.Instance.BoardAddPos - first) < .05f &&
            Mathf.Abs(next.transform.position.x) < .01f, "Next castle centered on river at " + first + " m");
        await Buy(next.GetComponentsInChildren<StorePoint>().First(s => s.StaticItem), playerCollider);
    }

    static void VerifyCastle(Transform castle)
    {
        var buys = castle.GetComponentsInChildren<StorePoint>();
        var sells = castle.GetComponentsInChildren<SellPoint>();
        Require(buys.Length == 9 && sells.Length == 2 && buys.All(s => Get<GameObject>(s, "_displayModel")), castle.name + ": 9 stocked shops and 2 sale points");
        Require(castle.Find("Winter_175/Festive_Firs").gameObject.activeInHierarchy &&
            castle.Find("Winter_174/Roof_Garlands").gameObject.activeInHierarchy &&
            castle.Find("Winter_174/Presents").gameObject.activeInHierarchy &&
            castle.Find("Winter_174/Snowman").gameObject.activeInHierarchy, castle.name + ": full winter decoration");
        Require(!castle.GetComponentsInChildren<Transform>().Any(t => t.name == "Shade_Tree" || t.name == "Tree1"), castle.name + ": no ordinary trees");
    }

    static async Task Buy(StorePoint store, Collider player)
    {
        Require(store && store.isActiveAndEnabled, "Purchase point active");
        var currency = CurrencyManager.Instance;
        currency.AddCurrency(CurrencyType.Coins, 1000);
        int before = currency.Coins;
        int price = Get<int>(store, "price");
        Trigger(store, player);
        var panel = Get<BuyTouchHandler>(store, "_buyTouchPanel");
        Require(panel && panel.gameObject.activeInHierarchy, "Purchase panel opened at " + store.name);
        panel.OnPointerDown(null);
        await Until(() => store.LastPurchasedItem, "Held purchase at " + store.name);
        panel.OnPointerUp(null);
        Require(price > 0 && currency.Coins == before - price, "Purchase issued item and deducted " + price + " coins");
    }

    static void Trigger(MonoBehaviour target, Collider collider) =>
        target.GetType().GetMethod("OnTriggerEnter", Private).Invoke(target, new object[] { collider });
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);

    static async Task Until(Func<bool> condition, string label)
    {
        var limit = DateTime.UtcNow.AddSeconds(35);
        while (!condition())
        {
            if (DateTime.UtcNow > limit) throw new TimeoutException(label);
            await Task.Delay(30);
        }
        Log(label);
    }

    static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        Log(label);
    }

    static void Log(string value)
    {
        File.AppendAllText(Report, value + Environment.NewLine);
        Debug.Log("[Winter175] " + value);
    }
}


