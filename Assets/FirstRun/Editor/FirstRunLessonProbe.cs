using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace DeadBoat.FirstRun.Editor
{
    // Isolated real-scene test. Distances and lethal damage are accelerated by the harness;
    // inventory, egg receipt, card buttons, weapon shot, boss death and loader are real.
    public static class FirstRunLessonProbe
    {
        public static string Result { get; private set; } = "not run";
        private static bool running;
        private static bool abortAtChoice;
        public static void RunAbort() { abortAtChoice = true; Run(); }
        public static async void Run()
        {
            if (running) return;
            running = true; Result = "running";
            bool abort = abortAtChoice; abortAtChoice = false;
            try
            {
                Require(SaveManager.Instance?.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave, "isolated save");
                var route = UnityEngine.Object.FindAnyObjectByType<FirstRunAuthoredRoute>();
                Require(route != null && route.supplyPrefabs.Length == 6, "lesson installed");
                int gems = SaveManager.Instance.GetGems();
                int wins = SaveManager.Instance.PersistentProvider.LoadWins();
                float baseMove = EggAnimalBuffService.ApplyMoveSpeed(10);
                // Exercise real burning and recovery from an out-of-world drop, without deleting scene objects.
                var steps = (System.Collections.Generic.List<TutorialStep>)typeof(TutorialManager)
                    .GetField("_tutorialSteps", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(route.tutorial);
                var deposit = (FuelDeposit)typeof(TutorialStep11).GetField("_item", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(steps[5]);
                var burnedEgg = route.supplies[2];
                typeof(FuelDeposit).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(deposit, new object[] {burnedEgg.GetComponent<Collider>()});
                var droppedGun = route.supplies[3];
                var lostPosition = droppedGun.transform.position; lostPosition.y = -30; droppedGun.transform.position = lostPosition;
                await Until(() => route.supplies[2] != null && route.supplies[2] != burnedEgg && droppedGun.transform.position.y > -15, "required egg replacement and pistol recovery");
                Require(!route.EggCollected && !EggFeatureStorage.IsOneShotEggCollected(FirstRunAuthoredRoute.EggReceipt), "burning does not collect egg");
                FirstRunAuthoredTools.SmokeTutorial();
                await Until(() => FirstRunAuthoredTools.Result.StartsWith("PASS") || FirstRunAuthoredTools.Result.StartsWith("FAIL"), "dock smoke");
                Require(FirstRunAuthoredTools.Result.StartsWith("PASS"), FirstRunAuthoredTools.Result);
                await Until(() => route.Lesson == FirstRunLesson.SailFirst, "first sail");
                var boat = BoardController.Instance;
                float originZ = boat.transform.position.z - boat.TotalDistanceTraveled;
                boat.TotalDistanceTraveled = 299.99f;
                SetSpeed(boat, 8);
                Call(boat, "FixedUpdate");
                Require(boat.TotalDistanceTraveled <= 300, "boat does not overshoot required stop");
                boat.TotalDistanceTraveled = 300;
                SetSpeed(boat, 8);
                float fuel = boat.currentFuel;
                Call(boat, "FixedUpdate");
                Require((float)typeof(BoardController).GetField("currentSpeed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boat) == 0 && boat.currentFuel == fuel, "waiting boat does not consume fuel");
                Place(boat, originZ, 300);
                PlayerMovement.Instance.Teleport(route.guards[0].transform.position + Vector3.up * 2 + Vector3.right * 4);
                await Until(() => route.Lesson == FirstRunLesson.FirstFight, "first fight");
                CombatCards.Hit(CombatCards.Begin(true), route.guards[0], 999);
                await Until(() => route.Lesson == FirstRunLesson.Egg, "egg objective");
                var egg = route.supplies[2].GetComponent<EggCollectibleItem>();
                Require(Inventory.Instance.AddItem(route.supplies[2]), "real egg pickup");
                Require(!egg.TryCollect(), "one-shot receipt rejects duplicate");
                await Until(() => route.Lesson == FirstRunLesson.FirstCard, "first offer ready");
                Require(route.Companion != null && EggAnimalBuffService.ApplyMoveSpeed(10) > baseMove, "real temporary companion buff");
                Require(route.OpenCards(), "first scripted offer");
                Require(LevelUpUi.Instance.IsScriptedChoice, "scripted window");
                LevelUpUi.Instance._RefreshClickGems(); LevelUpUi.Instance._RefreshClickAds();
                Require(SaveManager.Instance.GetGems() == gems && !DeadBoat.Online.SharedAdProtection.IsShowing, "no fees or ads in lesson");
                await Task.Delay(1300);
                Require(((System.Collections.IDictionary)typeof(LevelUpUi).GetField("_scriptedHidden", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(LevelUpUi.Instance)).Count > 0, "monetization controls hidden");
                if (abort)
                {
                    GameManager.Instance.EndGame(true, false);
                    await Until(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lobby", "abort to Lobby");
                    Require(!LevelUpUi.Instance.IsScriptedChoice && EggAnimalBuffService.ApplyMoveSpeed(10) == baseMove, "aborted choice and companion cleaned");
                    Result = "PASS: leaving during a scripted choice clears modal state and temporary companion on real Lobby return";
                    return;
                }
                Capture("first-run-card-one.png");
                await Task.Delay(200);
                Choose(2); // inventory expansion, safe numeric alternative
                await Until(() => route.Lesson == FirstRunLesson.SailSecond, "first choice releases boat");
                Require(route.ChoicesMade == 1 && LevelStatManager.Instance.Stats.InventorySlots == 1 && boat.ScenarioDistanceLimit == 600, "card applied once");
                Place(boat, originZ, 600);
                PlayerMovement.Instance.Teleport(route.guards[1].transform.position + Vector3.up * 2 + Vector3.right * 4);
                await Until(() => route.Lesson == FirstRunLesson.Supplies, "second stop");
                Require(Inventory.Instance.AddItem(route.supplies[3]), "pistol in real inventory");
                Inventory.Instance.SetActiveItem(route.supplies[3]);
                Require(Inventory.Instance.AddItem(route.supplies[4]), "real ammo collection");
                await Task.Delay(300);
                var weapon = route.supplies[3].GetComponentInChildren<RangedWeaponController>(true);
                Require(weapon.gameObject.activeInHierarchy, "equipped weapon survives the bag animation");
                int ammo = weapon.CurrentAmmo;
                Call(weapon, "UseUpdate");
                Require(route.Fired && weapon.CurrentAmmo == ammo - 1 && route.AmmunitionCollected, "real firing and ammo consumption");
                await Until(() => route.Lesson == FirstRunLesson.SecondFight, "ranged lesson");
                CombatCards.Hit(CombatCards.Begin(false), route.guards[1], 999);
                await Until(() => route.Lesson == FirstRunLesson.SecondCard, "second offer ready");
                Require(route.OpenCards(), "second offer");
                await Task.Delay(1300); Capture("first-run-card-two.png"); await Task.Delay(200); Choose(0); // Steady Aim
                await Until(() => route.Lesson == FirstRunLesson.SailBoss, "boss approach");
                Require(route.ChoicesMade == 2 && LevelStatManager.Instance.Stats.SteadyAimRank == 1 &&
                    float.IsPositiveInfinity(boat.ScenarioDistanceLimit), "second card and final gate");
                Place(boat, originZ, 900);
                PlayerMovement.Instance.Teleport(boat.transform.position + Vector3.up * 3);
                await Until(() => route.Dragon != null && route.Dragon.SharedHP > 0, "real mini dragon start");
                Require(route.Dragon.SharedHP == 120 && route.Dragon.transform.localScale.x == 2.5f, "HP survives dragon Start");
                weapon.ApplySharedAmmo(0);
                PlayerAmmoManager.Instance.UseAmmo(WeaponType.Pistol, PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol));
                await Until(() => route.EmergencyAmmunition != null, "emergency ammo crate");
                await Task.Delay(500);
                Require(UnityEngine.Object.FindObjectsByType<AmmoItem>(FindObjectsSortMode.None).Count(a => a.name == "AmmoMusket(Clone)") == 1, "no duplicate resupply");
                Require(Inventory.Instance.AddItem(route.EmergencyAmmunition), "resupply pickup");
                Call(weapon, "UseUpdate");
                await Until(() => weapon.CurrentAmmo > 0, "real reload from emergency resupply");
                Require(Vector3.Distance(route.Companion.transform.position, PlayerMovement.Instance.transform.position) < 6, "companion follows player");
                route.Dragon.TakeDamage(9999);
                await Until(() => route.Lesson == FirstRunLesson.Completed, "actual dragon death callback");
                await Task.Delay(400); Capture("first-run-completed.png");
                await Task.Delay(200);
                Require(SaveManager.Instance.GetGems() == gems && SaveManager.Instance.PersistentProvider.LoadWins() == wins, "no ordinary victory payout");
                Require(EggFeatureStorage.Load().GetEggAmount("egg_chicken") == 1 && EggFeatureStorage.Load().ownedAnimals.Count == 0, "one egg, no permanent companion grant");
                route.ReturnToLobby();
                await Until(() => LoadingManager.Instance.CurrentLocation == Location.Lobby && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lobby", "real lobby return");
                Require(UnityEngine.Object.FindAnyObjectByType<FirstRunAuthoredRoute>() == null, "lesson destroyed on exit");
                Require(EggAnimalBuffService.ApplyMoveSpeed(10) == baseMove, "temporary buffs removed");
                Require(UnityEngine.Object.FindObjectsByType<Fusion.NetworkRunner>(FindObjectsSortMode.None).Length == 0, "no Photon connections");
                Result = "PASS: burnt egg and dropped pistol recover; dock gameplay, boat gates/fuel, both fights, one-shot egg, temporary pet/buff/follow, scripted cards without ads/fees, pistol/ammo/shot/resupply/reload, 120HP dragon death, no normal victory, Lobby return and pet cleanup; no Photon";
            }
            catch (Exception e) { Result = "FAIL: " + e; Debug.LogError(Result); }
            finally { running = false; }
        }
        private static void Place(BoardController boat, float origin, float distance)
        {
            boat.TotalDistanceTraveled = distance;
            var p = boat.transform.position; p.z = origin + distance; boat.transform.position = p;
            SetSpeed(boat, 0);
        }
        private static void SetSpeed(BoardController boat, float speed) => typeof(BoardController).GetField("currentSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boat, speed);
        private static void Capture(string name)
        {
            System.IO.Directory.CreateDirectory(".ai-game-dev/captures");
            ScreenCapture.CaptureScreenshot(".ai-game-dev/captures/" + name);
        }
        private static void Choose(int index)
        {
            var buttons = (System.Collections.Generic.List<LevelUpButtomUI>)typeof(LevelUpUi)
                .GetField("_buttons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(LevelUpUi.Instance);
            Require(!((bool)typeof(LevelUpButtomUI).GetField("_isAds", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(buttons[index])), "free choice");
            Call(buttons[index], "UseButton");
        }
        private static object Call(object o, string method) => o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, null);
        private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> predicate, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + 20;
            while (!predicate()) { if (Time.realtimeSinceStartupAsDouble >= end) throw new Exception("Timeout: " + label); await Task.Delay(100); }
        }
    }
}
