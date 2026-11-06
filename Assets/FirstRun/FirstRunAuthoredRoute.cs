using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.FirstRun
{
    public enum FirstRunLesson { Intro, SailFirst, FirstFight, Egg, FirstCard, SailSecond, Supplies, SecondFight, SecondCard, SailBoss, Boss, Completed }

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
            PauseManager.Instance.SetPause(true, false);
            ControlManager.Instance.CursorActive = true;
        }
        public void ReturnToLobby()
        {
            if (Lesson != FirstRunLesson.Completed || GameManager.Instance.isEndGame) return;
            // The lesson has its own result, not a normal boss achievement / victory payout.
            GameManager.Instance.EndGame(true, false);
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
