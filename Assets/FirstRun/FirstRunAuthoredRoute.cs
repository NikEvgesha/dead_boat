using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.FirstRun
{
    public enum FirstRunLesson { Intro, SailFirst, FirstFight, Egg, FirstCard, SailSecond, Supplies, SecondFight, SecondCard, SailBoss, Boss, Completed }

    [DefaultExecutionOrder(-2)]
    public sealed class FirstRunAuthoredRoute : MonoBehaviour
    {
        public const int Seed = 14052026;
        public const int ContentVersion = 2;
        public const string EggReceipt = "deadboat-first-run-egg-v2";
        public TutorialManager tutorial;
        public Transform[] stops;
        public Transform arena;
        public PickableItem[] supplies;
        public PickableItem[] supplyPrefabs;
        public ZombieController[] guards;
        public DragonBossController miniDragonPrefab;
        public ArrowLine guideArrow;
        public FirstRunLesson Lesson { get; private set; }
        public DragonBossController Dragon { get; private set; }
        public GameObject Companion { get; private set; }
        public int ChoicesMade { get; private set; }
        public bool EggCollected { get; private set; }
        public bool AmmunitionCollected { get; private set; }
        public bool Fired { get; private set; }
        private EggCollectibleItem egg;
        private AmmoItem ammunition;
        private RangedWeaponController gun;
        private BoardController boat;
        private FirstRunGuide guide;
        private float nextTick, enteredAt;
        private bool choicePending, initialized;
        private bool previousScriptedOnly;
        private LevelStatManager stats;
        private readonly Vector3[] supplyPositions = new Vector3[6];
        private PickableItem reserveAmmo;
        public PickableItem EmergencyAmmunition => reserveAmmo;
        private GameObject questPanel;
        private bool questPanelWasActive;
        private FirstRunCheckpointWorld checkpointWorld;
        private FirstRunCheckpoint pendingCheckpoint;
        private bool profileReady;
        private float nextCheckpoint;
        private int restoreBossHealth = -1;
        private void Awake()
        {
            if (!FirstRunSession.Active) return;
            checkpointWorld = gameObject.AddComponent<FirstRunCheckpointWorld>();
            checkpointWorld.Initialize(this);
            pendingCheckpoint = FirstRunSession.Checkpoint;
        }

        private void Start()
        {
            boat = BoardController.Instance;
            stats = LevelStatManager.Instance;
            previousScriptedOnly = stats.ScriptedOffersOnly;
            stats.ScriptedOffersOnly = true;
            boat.ScenarioDistanceLimit = 300;
            egg = supplies[2].GetComponent<EggCollectibleItem>();
            ammunition = supplies[4].GetComponent<AmmoItem>();
            gun = supplies[3].GetComponentInChildren<RangedWeaponController>(true);
            egg.Collected += OnEgg;
            ammunition.Collected += OnAmmo;
            gun.ShotFired += OnShot;
            for (int i = 0; i < supplies.Length; i++) supplyPositions[i] = supplies[i].transform.position;
            EggCollected = EggFeatureStorage.IsOneShotEggCollected(EggReceipt);
            guide = FirstRunGuide.Create(transform);
            guide.ReturnToLobby += ReturnToLobby;
            if (QuestManager.Instance != null && QuestManager.Instance.questsUIContainer != null)
            {
                questPanel = QuestManager.Instance.questsUIContainer.gameObject;
                questPanelWasActive = questPanel.activeSelf;
                questPanel.SetActive(false);
            }
            initialized = true;
        }
        private void OnEgg() => EggCollected = true;
        private void OnAmmo() => AmmunitionCollected = true;
        private void OnShot() => Fired = true;

        private void Update()
        {
            if (!initialized || !boat.StartGame || Time.unscaledTime < nextTick) return;
            if (GameManager.Instance.isEndGame) return;
            if (FirstRunSession.Active && !profileReady)
            {
                if (pendingCheckpoint != null)
                {
                    try { checkpointWorld.Restore(pendingCheckpoint); }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("Lesson checkpoint could not be restored; retrying the lesson: " + e.Message);
                        FirstRunSession.Restart();
                        LoadingManager.Instance.LoadLocation(Location.Game, FirstRunSession.SceneName, false);
                        return;
                    }
                    pendingCheckpoint = null;
                }
                else
                {
                    stats.RestoreRecovery(stats.StatsDefault, 0);
                    PlayerStatsManager.Instance.RestoreRecoveryHealth(PlayerStatsManager.Instance.MaxHealth);
                    PlayerStatsManager.Instance.ResetExp();
                    int delta = FirstRunSession.Save.LoadGameCoin() - CurrencyManager.Instance.Coins;
                    if (delta != 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, delta);
                }
                FirstRunSession.Restoring = false;
                profileReady = true;
                CaptureCheckpoint();
            }
            nextTick = Time.unscaledTime + .2f;
            guide.Show(Lesson, Dragon != null ? Dragon.SharedHP : 0, LevelUpUi.Instance.IsChoiceOpen, tutorial.CurrentStepIndex);
            if (Time.timeScale <= 0 || LevelUpUi.Instance.IsChoiceOpen) return;
            MaintainSupplies();
            switch (Lesson)
            {
                case FirstRunLesson.Intro:
                    if (tutorial.IntroComplete) Enter(FirstRunLesson.SailFirst);
                    break;
                case FirstRunLesson.SailFirst:
                    if (boat.TotalDistanceTraveled >= 290) Enter(FirstRunLesson.FirstFight);
                    break;
                case FirstRunLesson.FirstFight:
                    if (guards[0].IsDead) Enter(FirstRunLesson.Egg);
                    break;
                case FirstRunLesson.Egg:
                    if (EggCollected) { SpawnCompanion(); Enter(FirstRunLesson.FirstCard); }
                    break;
                case FirstRunLesson.FirstCard:
                case FirstRunLesson.SecondCard:
                    if (!choicePending && Time.unscaledTime - enteredAt >= 3) OpenCards();
                    break;
                case FirstRunLesson.SailSecond:
                    if (boat.TotalDistanceTraveled >= 590) Enter(FirstRunLesson.Supplies);
                    break;
                case FirstRunLesson.Supplies:
                    bool hasGun = Inventory.Instance.GetItems().Contains(supplies[3]);
                    if (hasGun && AmmunitionCollected && Fired) Enter(FirstRunLesson.SecondFight);
                    break;
                case FirstRunLesson.SecondFight:
                    if (guards[1].IsDead) Enter(FirstRunLesson.SecondCard);
                    break;
                case FirstRunLesson.SailBoss:
                    if (boat.TotalDistanceTraveled >= 899) BeginBoss();
                    break;
            }
        }
        private void Enter(FirstRunLesson lesson)
        {
            Lesson = lesson;
            enteredAt = Time.unscaledTime;
            Transform goal = null;
            if (lesson == FirstRunLesson.SailFirst) goal = stops[0];
            if (lesson == FirstRunLesson.FirstFight) goal = guards[0].transform;
            if (lesson == FirstRunLesson.Egg && supplies[2]) goal = supplies[2].transform;
            if (lesson == FirstRunLesson.SailSecond) { boat.ScenarioDistanceLimit = 600; goal = stops[1]; }
            if (lesson == FirstRunLesson.Supplies) goal = supplies[3].transform;
            if (lesson == FirstRunLesson.SecondFight) goal = guards[1].transform;
            if (lesson == FirstRunLesson.SailBoss) { boat.ScenarioDistanceLimit = float.PositiveInfinity; goal = arena; }
            guideArrow.ActiveArrowLine(false);
            if (goal != null) guideArrow.StartArrowLine(PlayerMovement.Instance.transform, goal);
            guide.Show(Lesson, Dragon != null ? Dragon.SharedHP : 0, false, tutorial.CurrentStepIndex);
        }
        public bool OpenCards()
        {
            if (choicePending || (Lesson != FirstRunLesson.FirstCard && Lesson != FirstRunLesson.SecondCard)) return false;
            bool first = Lesson == FirstRunLesson.FirstCard;
            var names = first ? new[] { "BattleRhythm", "BloodPact", "InventorySlots" }
                : new[] { "SteadyAim", "PatientReload", "LastRound" };
            var cards = new List<ItemWithRare>();
            foreach (var name in names)
            {
                var item = Resources.Load<BoostItem>("BoostItem_" + name);
                cards.Add(new ItemWithRare { Item = item, Rare = item.RareRewards[0].Rare, IsAds = false });
            }
            var expected = Lesson;
            choicePending = LevelUpUi.Instance.OpenScriptedChoice(cards, type => {
                if (this == null || Lesson != expected) return;
                choicePending = false;
                ChoicesMade++;
                Enter(first ? FirstRunLesson.SailSecond : FirstRunLesson.SailBoss);
            });
            return choicePending;
        }
        private void SpawnCompanion()
        {
            if (Companion != null) return;
            var catalog = Resources.Load<EggHatchingCatalog>("Eggs/EggHatchingCatalog");
            var prefab = catalog.ResolveAnimalPrefab("chicken", 1);
            Companion = Instantiate(prefab, PlayerMovement.Instance.transform.position + Vector3.right * 2, Quaternion.identity, transform);
            Companion.name = "Temporary lesson companion";
            foreach (var collider in Companion.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in Companion.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            EggAnimalBuffService.SetTemporaryCompanion(this, catalog.GetAnimalBuffs("chicken", 1));
        }
        private void MaintainSupplies()
        {
            if (Lesson == FirstRunLesson.Completed) return;
            // A required object accidentally sold or burned must not strand the lesson.
            RestoreSupply(2, !EggCollected);
            RestoreSupply(3, true);
            RestoreSupply(4, !AmmunitionCollected);
            if (reserveAmmo != null && reserveAmmo.Status == ItemStatus.Free && reserveAmmo.transform.position.y < -15)
                RecoverPosition(reserveAmmo, boat.transform.position + Vector3.up * 2 + Vector3.right * 2);
            if ((Lesson == FirstRunLesson.Boss || Lesson == FirstRunLesson.SailBoss) &&
                gun.CurrentAmmo == 0 && PlayerAmmoManager.Instance.GetAmmo(WeaponType.Pistol) == 0 && reserveAmmo == null)
            {
                reserveAmmo = Instantiate(supplyPrefabs[4], boat.transform.position + Vector3.up * 2 + Vector3.right * 2, Quaternion.identity, transform);
            }
        }
        private void RestoreSupply(int index, bool needed)
        {
            if (!needed) return;
            if (supplies[index] != null)
            {
                if (supplies[index].Status == ItemStatus.Free && supplies[index].transform.position.y < -15)
                    RecoverPosition(supplies[index], supplyPositions[index]);
                return;
            }
            supplies[index] = Instantiate(supplyPrefabs[index], supplyPositions[index], Quaternion.identity, transform);
            if (index == 2) { egg = supplies[index].GetComponent<EggCollectibleItem>(); egg.ConfigureOneShot(EggReceipt); egg.Collected += OnEgg; }
            if (index == 3) { gun = supplies[index].GetComponentInChildren<RangedWeaponController>(true); gun.ShotFired += OnShot; }
            if (index == 4) { ammunition = supplies[index].GetComponent<AmmoItem>(); ammunition.Collected += OnAmmo; }
            if (Lesson == FirstRunLesson.Egg && index == 2) guideArrow.StartArrowLine(PlayerMovement.Instance.transform, supplies[index].transform);
            if (Lesson == FirstRunLesson.Supplies && index == 3) guideArrow.StartArrowLine(PlayerMovement.Instance.transform, supplies[index].transform);
        }
        private static void RecoverPosition(PickableItem item, Vector3 position)
        {
            item.transform.position = position;
            var body = item.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        }
        private void LateUpdate()
        {
            if (profileReady && Time.unscaledTime >= nextCheckpoint) CaptureCheckpoint();
            if (restoreBossHealth > 0 && Dragon != null && Dragon.SharedHP > 0)
            {
                Dragon.RestoreCheckpointHealth(restoreBossHealth, (int)Dragon.GetStartHP());
                restoreBossHealth = -1;
            }
            if (Companion == null || PlayerMovement.Instance == null) return;
            var player = PlayerMovement.Instance.transform;
            var target = player.position - player.forward * 1.5f + player.right * 1.25f + Vector3.up * (.55f + Mathf.Sin(Time.time * 2) * .1f);
            Companion.transform.position = Vector3.Lerp(Companion.transform.position, target, 1 - Mathf.Exp(-4 * Time.deltaTime));
            Companion.transform.rotation = Quaternion.Slerp(Companion.transform.rotation, player.rotation, 1 - Mathf.Exp(-4 * Time.deltaTime));
        }
        private void BeginBoss()
        {
            if (Dragon != null || Lesson != FirstRunLesson.SailBoss) return;
            Dragon = Instantiate(miniDragonPrefab, boat.transform.position + Vector3.up * 5 + Vector3.forward * 12, Quaternion.identity, transform);
            Dragon.Dead += Complete;
            Enter(FirstRunLesson.Boss);
        }
        private void Complete()
        {
            if (Lesson != FirstRunLesson.Boss) return;
            Enter(FirstRunLesson.Completed);
            FirstRunSession.Close(true);
            PauseManager.Instance.SetPause(true, false);
            ControlManager.Instance.CursorActive = true;
        }
        public void ReturnToLobby()
        {
            if (Lesson != FirstRunLesson.Completed || GameManager.Instance.isEndGame) return;
            // The lesson has its own result, not a normal boss achievement / victory payout.
            GameManager.Instance.EndGame(true, false);
        }
        public void CaptureCheckpoint()
        {
            if (!profileReady || !FirstRunSession.CanCapture || checkpointWorld == null || !boat.StartGame || GameManager.Instance.isEndGame) return;
            if (restoreBossHealth > 0) return;
            if (Lesson == FirstRunLesson.Boss && Dragon != null && Dragon.IsDead) { FirstRunSession.Close(true); return; }
            if (Lesson == FirstRunLesson.Boss && (Dragon == null || Dragon.SharedHP <= 0)) return;
            nextCheckpoint = Time.unscaledTime + 2;
            FirstRunSession.Persist(checkpointWorld.Capture());
        }
        private void OnApplicationPause(bool pause) { if (pause) CaptureCheckpoint(); }
        private void OnApplicationFocus(bool focus) { if (!focus) CaptureCheckpoint(); }
        private void OnApplicationQuit() => CaptureCheckpoint();
        public void RestoreCheckpointState(FirstRunCheckpoint c)
        {
            if (c.egg && !EggFeatureStorage.IsOneShotEggCollected(EggReceipt))
                EggHatchingManager.RegisterOneShotEggPickup(EggReceipt, "egg_chicken", 1, false);
            ChoicesMade = c.choices; EggCollected = c.egg || EggFeatureStorage.IsOneShotEggCollected(EggReceipt);
            AmmunitionCollected = c.ammo; Fired = c.shot;
            boat.TotalDistanceTraveled = c.distance;
            var position = boat.transform.position; position.z = c.distance; boat.transform.position = position;
            boat.currentFuel = c.fuel; boat.SwitchFuel?.Invoke(boat.currentFuel, boat.MaxFuel);
            if (EggCollected) { supplies[2].gameObject.SetActive(false); SpawnCompanion(); }
            boat.ScenarioDistanceLimit = c.lesson <= FirstRunLesson.FirstCard ? 300 : c.lesson <= FirstRunLesson.SecondCard ? 600 : float.PositiveInfinity;
            Enter(c.lesson == FirstRunLesson.Boss ? FirstRunLesson.SailBoss : c.lesson);
            if (c.lesson == FirstRunLesson.Boss)
            {
                BeginBoss(); restoreBossHealth = Mathf.Max(1, c.bossHP);
            }
        }
        public void BindCheckpointSupply(int index, PickableItem item)
        {
            if (index == 6) { reserveAmmo = item; return; }
            if (index < 0 || index >= supplies.Length || supplies[index] == item) return;
            if (index == 2 && egg) egg.Collected -= OnEgg;
            if (index == 3 && gun) gun.ShotFired -= OnShot;
            if (index == 4 && ammunition) ammunition.Collected -= OnAmmo;
            supplies[index] = item;
            if (index == 2) { egg = item.GetComponent<EggCollectibleItem>(); egg.Collected += OnEgg; }
            if (index == 3) { gun = item.GetComponentInChildren<RangedWeaponController>(true); gun.ShotFired += OnShot; }
            if (index == 4) { ammunition = item.GetComponent<AmmoItem>(); ammunition.Collected += OnAmmo; }
        }
        private void OnDestroy()
        {
            if (LevelUpUi.Instance != null) LevelUpUi.Instance.CancelScriptedChoice();
            if (egg) egg.Collected -= OnEgg;
            if (ammunition) ammunition.Collected -= OnAmmo;
            if (gun) gun.ShotFired -= OnShot;
            if (Dragon) Dragon.Dead -= Complete;
            if (guideArrow) guideArrow.ActiveArrowLine(false);
            if (boat) boat.ScenarioDistanceLimit = float.PositiveInfinity;
            if (stats) stats.ScriptedOffersOnly = previousScriptedOnly;
            if (questPanel) questPanel.SetActive(questPanelWasActive);
            EggAnimalBuffService.ClearTemporaryCompanion(this);
        }
    }
}
