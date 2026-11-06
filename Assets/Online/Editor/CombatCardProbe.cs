using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadBoat.Online.Editor
{
    public static class CombatCardProbe
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        private static void Near(float actual, float expected, string label) => Require(Mathf.Abs(actual - expected) < .002f, label + ": " + actual + " != " + expected);
        public static string RunMath()
        {
            for (int rank = 1; rank <= 3; rank++)
            {
                Near(CombatCardRules.MaximumHealth(100, rank), 80, "one HP price");
                Near(CombatCardRules.Lifesteal(rank), .02f + .02f * rank, "lifesteal rank");
                var budget = new CombatCardRules.HealingBudget();
                Near(budget.Take(0, rank, 5, 80, 80), 5 * CombatCardRules.Lifesteal(rank), "actual removed HP");
                float total = 5 * CombatCardRules.Lifesteal(rank);
                for (int i = 0; i < 100; i++) total += budget.Take(.1, rank, 10000, 80, 80);
                Near(total, 80 * CombatCardRules.HealLimit(rank), "shared AoE/pellet cap");
                Near(budget.Take(.999, rank, 10000, 80, 80), 0, "sliding window not fixed bucket");
                Require(budget.Take(1.01, rank, 10000, 80, 80) > 0, "window expires");
                budget.Lock(2);
                Near(budget.Take(2.99, rank, 10000, 80, 80), 0, "recovery lock");
                Require(budget.Take(3, rank, 10000, 80, 80) > 0, "recovery lock ends");
                Near(new CombatCardRules.HealingBudget().Take(0, rank, 100, 80, .1f), .1f, "no overheal");
            }
            Require(CombatCardRules.EdgeActive(88f * .4f, 88f) && !CombatCardRules.EdgeActive(88f * .401f, 88f) && !CombatCardRules.EdgeActive(0, 88), "inclusive float HP threshold");
            var combo = new CombatCardRules.Combo();
            Require(!combo.Hit(0) && !combo.Hit(1) && combo.Hit(2) && !combo.Hit(2.1), "third hit and reset");
            combo.Miss(); Require(!combo.Hit(3) && !combo.Hit(6) && !combo.Hit(7) && combo.Hit(8), "miss/gap reset");
            Require(CombatCardRules.Damage(100, 1.15f) == 200, "additive bonus cap");
            Near(CombatCardRules.MaximumHealth(.5f, 3), 1, "minimum HP");
            var snapshot = new SharedPersonalData { BloodPactRank = 1, BattleRhythmRank = 2, OnTheEdgeRank = 3 };
            var restored = JsonUtility.FromJson<Stats>(JsonUtility.ToJson(snapshot.Cards));
            Require(restored.BloodPactRank == 1 && restored.BattleRhythmRank == 2 && restored.OnTheEdgeRank == 3, "personal/JSON rank mapping");
            Require(JsonUtility.FromJson<Stats>("{\"Save\":true}").BloodPactRank == 0 && (int)BoostType.InventorySlots == 14, "legacy compatibility");
            foreach (var type in new[] { BoostType.BloodPact, BoostType.BattleRhythm, BoostType.OnTheEdge })
            {
                var card = Resources.Load<BoostItem>("BoostItem_" + type);
                Require(card != null && card.BoostType == type && card.RareRewards.Count == 1 && card.RareRewards[0].Reward == 1, "rank card asset");
                var offers = new List<ItemWithRare> { new ItemWithRare { Item = card, Rare = RareType.Rare } };
                Require(!LevelUpUi.TryUpgradeHighestRarity(offers, out _) && offers[0].Rare == RareType.Rare, "no advertising rank bypass");
                Require(!CombatCardText.Description(type, 3, 100, 80).Contains("{"), "named text parameters");
            }
            var numeric = AssetDatabase.LoadAssetAtPath<BoostItem>("Assets/Scripts/LevelStat/Boosters/BoostItem_HP.asset");
            var mixed = new List<ItemWithRare>
            {
                new ItemWithRare { Item = numeric, Rare = RareType.Common },
                new ItemWithRare { Item = Resources.Load<BoostItem>("BoostItem_BloodPact"), Rare = RareType.Rare }
            };
            Require(!LevelUpUi.TryUpgradeHighestRarity(mixed, out _) && !mixed[0].IsAds, "safe numeric choice remains free");
            return "[Combat cards] PASS: ranks, HP price, actual damage, sliding cap, recovery lock, combo resets, additive ceiling, legacy/JSON mapping, assets and ad exclusion";
        }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly LevelStatManager Cards;
            public readonly PlayerStatsManager Player;
            public readonly SharedProbeEnemy Enemy;
            public readonly SaveManager Save;
            public Fixture()
            {
                Require(EditorApplication.isPlaying && LevelStatManager.Instance == null && PlayerStatsManager.Instance == null && SaveManager.Instance == null, "isolated Play scene required");
                Root = new GameObject("Temporary combat card fixture"); Root.SetActive(false);
                Save = Root.AddComponent<SaveManager>();
                var memory = Root.AddComponent<SharedRunPreviewSave>();
                typeof(SaveManager).GetField("saveProvider", Private).SetValue(Save, memory);
                typeof(SaveManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, Save);
                Cards = Root.AddComponent<LevelStatManager>(); LevelStatManager.Instance = Cards; Cards.Stats = Cards.StatsDefault;
                Cards._rarityChances = new List<RareChance> { new RareChance { Rare = RareType.Common, Chance = 1 } };
                Player = Root.AddComponent<PlayerStatsManager>(); PlayerStatsManager.Instance = Player;
                Enemy = Root.AddComponent<SharedProbeEnemy>(); Enemy.SetHP(10000);
                CombatCards.Reset();
            }
            public void Health(float value) => typeof(PlayerStatsManager).GetField("_health", Private).SetValue(Player, value);
            public void EnemyHP(int value) { Enemy.SetHP(value); typeof(EnemyCore).GetField("isDead", Private).SetValue(Enemy, false); }
            public void UnlockedBudget() => typeof(CombatCards).GetField("healing", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new CombatCardRules.HealingBudget());
            public void Choose(BoostType type) => typeof(LevelStatManager).GetMethod("Change", Private).Invoke(Cards, new object[] { type, 1f });
            public void Dispose()
            {
                CombatCards.Reset();
                LevelStatManager.Instance = null; PlayerStatsManager.Instance = null;
                typeof(SaveManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
                Object.Destroy(Root);
            }
        }
        public static string RunWeaponExpansion()
        {
            using var fixture = new Fixture();
            GameObject target = null;
            try
            {
                fixture.Cards.Stats.LastRoundRank = 3;
                target = new GameObject("Temporary raycast victim"); target.transform.position = new Vector3(0, 0, 10);
                target.AddComponent<BoxCollider>().size = new Vector3(5, 5, 1);
                var victim = target.AddComponent<SharedProbeEnemy>(); victim.SetHP(10000);
                var camera = fixture.Root.AddComponent<Camera>(); camera.enabled = false;
                var gun = fixture.Root.AddComponent<RangedWeaponController>();
                var ammo = fixture.Root.AddComponent<PlayerAmmoManager>(); PlayerAmmoManager.Instance = ammo;
                typeof(PlayerAmmoManager).GetField("_ammo", Private).SetValue(ammo, new Dictionary<WeaponType, int> { [WeaponType.Pistol] = 50, [WeaponType.Shotgun] = 50 });
                typeof(RangedWeaponController).GetField("fpsCam", Private).SetValue(gun, camera);
                typeof(RangedWeaponController).GetField("muzzleTransform", Private).SetValue(gun, camera.transform);
                typeof(RangedWeaponController).GetField("hitLayers", Private).SetValue(gun, (LayerMask)(~0));
                typeof(RangedWeaponController).GetField("spreadAngle", Private).SetValue(gun, 0f);
                Physics.SyncTransforms();
                var fire = typeof(RangedWeaponController).GetMethod("Fire", Private);
                foreach (var type in new[] { WeaponType.Pistol, WeaponType.Shotgun })
                {
                    typeof(RangedWeaponController).GetField("weaponType", Private).SetValue(gun, type);
                    int basic = type == WeaponType.Shotgun ? 100 : 10;
                    gun.ApplySharedAmmo(2); int before = victim.SharedHP; fire.Invoke(gun, null);
                    Require(before - victim.SharedHP == basic, "real ordinary raycast damage");
                    gun.ApplySharedAmmo(1); before = victim.SharedHP; fire.Invoke(gun, null);
                    Require(before - victim.SharedHP == CombatCardRules.Damage(basic, .75f), "real last-round raycast/pellets");
                    gun.ApplySharedAmmo(0);
                    var reload = (System.Collections.IEnumerator)typeof(RangedWeaponController).GetMethod("Reload", Private).Invoke(gun, null);
                    Require(reload.MoveNext() && reload.Current is WaitForSeconds, "real reload did not wait");
                    float seconds = (float)typeof(WaitForSeconds).GetField("m_Seconds", Private).GetValue(reload.Current);
                    Near(seconds, 1.15f, "actual coroutine reload duration");
                    Require(gun.CurrentAmmo == 0 && ammo.GetAmmo(type) == 50, "cancelled reload created ammo");
                    Require(!reload.MoveNext() && gun.CurrentAmmo == 10 && ammo.GetAmmo(type) == 40, "completed reload ammo balance");
                }
                camera.transform.rotation = Quaternion.Euler(0, 180, 0); gun.ApplySharedAmmo(1);
                int unchanged = victim.SharedHP; fire.Invoke(gun, null); Require(victim.SharedHP == unchanged, "miss damages enemy");
                Debug.Log("[Card weapons] PASS: real Fire raycasts, pistol/shotgun whole volley, real reload yield +15%, cancel before completion has no ammo, exact transfer after completion, miss. Editor fixture, no timed browser animation test.");
                return "PASS";
            }
            finally { PlayerAmmoManager.Instance = null; if (target != null) Object.Destroy(target); }
        }
        public static string RunExpansion()
        {
            using var fixture = new Fixture();
            var cards = fixture.Cards; var p = fixture.Player; float original = p.MaxHealth;
            fixture.Health(10);
            for (int rank = 1; rank <= 3; rank++)
            {
                fixture.Choose(BoostType.HeavyCoat);
                Near(p.MaxHealth, original * (1 + CombatCardRules.CoatHealth(rank)), "coat maximum from original");
                Near(p.Health, 10, "coat does not heal");
                var movement = fixture.Root.GetComponent<PlayerMovement>() ?? fixture.Root.AddComponent<PlayerMovement>();
                Near(movement.MoveSpeed, EggAnimalBuffService.ApplyMoveSpeed(ProfessionService.ApplyMoveSpeed(6)) * .92f, "actual movement getter/one speed price");
                fixture.Choose(BoostType.LastRound);
                int before = fixture.Enemy.SharedHP;
                var ordinary = CombatCards.Begin(false, 2); CombatCards.Hit(ordinary, fixture.Enemy, 100);
                Require(before - fixture.Enemy.SharedHP == 100, "ordinary round boosted");
                before = fixture.Enemy.SharedHP;
                var shot = CombatCards.Begin(false, 1); CombatCards.Hit(shot, fixture.Enemy, 100);
                Require(before - fixture.Enemy.SharedHP == CombatCardRules.Damage(100, CombatCardRules.LastRoundBonus(rank)), "last round live damage");
                CombatCards.Hit(shot, fixture.Enemy, 100);
                Require(before - fixture.Enemy.SharedHP == CombatCardRules.Damage(100, CombatCardRules.LastRoundBonus(rank)), "same target repeated damage");
                before = fixture.Enemy.SharedHP;
                CombatCards.Hit(CombatCards.Begin(true, 1), fixture.Enemy, 100);
                Require(before - fixture.Enemy.SharedHP == 100, "sword got magazine bonus");
            }
            fixture.Choose(BoostType.HeavyCoat); fixture.Choose(BoostType.LastRound);
            Require(cards.Stats.HeavyCoatRank == 3 && cards.Stats.LastRoundRank == 3, "rank cap");
            float preview = CombatCardRules.PreviewHealth(p.MaxHealth, 0, 3, BoostType.BloodPact);
            fixture.Choose(BoostType.BloodPact); Near(p.MaxHealth, original, "coat III plus pact"); Near(p.MaxHealth, preview, "HP preview");
            Near(p.Health, 10, "combined acquisition heals");
            var saved = JsonUtility.FromJson<Stats>(JsonUtility.ToJson(cards.Stats));
            for (int i = 0; i < 3; i++) cards.RestoreRecovery(saved, 0);
            Near(p.MaxHealth, original, "repeat recovery compounds HP");
            Require(saved.LastRoundRank == 3 && saved.HeavyCoatRank == 3, "solo JSON mapping");
            var snapshot = new SharedPersonalData { LastRoundRank = 2, HeavyCoatRank = 1 };
            Require(snapshot.Cards.LastRoundRank == 2 && snapshot.Cards.HeavyCoatRank == 1, "Shared mapping");
            Require(JsonUtility.FromJson<Stats>("{}").HeavyCoatRank == 0 && (int)BoostType.OnTheEdge == 17 && (int)BoostType.LastRound == 18 && (int)BoostType.HeavyCoat == 19, "legacy/enum compatibility");
            typeof(LevelStatManager).GetMethod("Subscriptions", Private).Invoke(cards, null);
            cards._boostItems.Add(AssetDatabase.LoadAssetAtPath<BoostItem>("Assets/Scripts/LevelStat/Boosters/BoostItem_HP.asset"));
            for (int i = 0; i < 40; i++) Require(cards.GetRandomBoostsWithRarity().TrueForAll(o => o.Item.BoostType != BoostType.HeavyCoat && o.Item.BoostType != BoostType.LastRound), "capped offer returned");
            foreach (var type in new[] { BoostType.LastRound, BoostType.HeavyCoat })
            {
                var asset = Resources.Load<BoostItem>("BoostItem_" + type);
                Require(asset != null && asset.BoostType == type && asset.RareRewards.Count == 1, "new card asset");
                var offer = new List<ItemWithRare> { new ItemWithRare { Item = asset, Rare = RareType.Rare } };
                Require(!LevelUpUi.TryUpgradeHighestRarity(offer, out _), "advertising upgraded rank");
                Require(!CombatCardText.Description(type, 3, 100, 125).Contains("{"), "unresolved translation");
            }
            Near(CombatCardRules.ReloadDurationMultiplier(1), 1.15f, "reload price");
            Near(CombatCardRules.ReloadDurationMultiplier(3), 1.15f, "reload price compounds");
            Debug.Log("[Card expansion] PASS: actual damage/movement/maxHP, ranks/cap, recovery/solo JSON, offers/ad guard/assets/text. Editor fixture.");
            return "PASS";
        }
        public static string RunLocal()
        {
            using var fixture = new Fixture();
            var player = fixture.Player; var stats = fixture.Cards; var enemy = fixture.Enemy;
            float original = player.MaxHealth;
            fixture.Health(original); int deaths = 0; player.NoHealth += () => deaths++;
            fixture.Choose(BoostType.BloodPact); Near(player.MaxHealth, original * .8f, "live maxHP cost");
            Near(player.Health, player.MaxHealth, "live clamp"); fixture.Health(10);
            for (int i = 0; i < 5; i++) fixture.Choose(BoostType.BloodPact);
            Near(player.Health, 10, "rank upgrade does not heal"); Near(player.MaxHealth, original * .8f, "rank price not compounded");
            Require(stats.Stats.BloodPactRank == 3 && deaths == 0, "rank cap/no death event");
            fixture.UnlockedBudget(); fixture.EnemyHP(5);
            var overkill = CombatCards.Begin(false); CombatCards.Hit(overkill, enemy, 100); CombatCards.Hit(overkill, enemy, 100);
            Near(player.Health, 10.4f, "overkill/duplicate corpse heal");
            Near((float)CombatCards.HealingTotal, .4f, "actual healing presentation once");
            fixture.Health(0); fixture.EnemyHP(100); CombatCards.Hit(CombatCards.Begin(false), enemy, 100);
            Near(player.Health, 0, "cannot resurrect");
            stats.Stats.BloodPactRank = 0;
            fixture.Health(player.MaxHealth); fixture.EnemyHP(10000);
            for (int i = 0; i < 2; i++) { var attack = CombatCards.Begin(true); CombatCards.Hit(attack, enemy, 100); CombatCards.End(attack); }
            stats.Stats.BattleRhythmRank = 3; stats.Stats.OnTheEdgeRank = 3;
            fixture.Health(player.MaxHealth * .4f); fixture.EnemyHP(10000);
            CombatCards.Attack finalSwing = null;
            for (int i = 0; i < 3; i++) { var attack = CombatCards.Begin(true); CombatCards.Hit(attack, enemy, 100); CombatCards.Hit(attack, enemy, 100); CombatCards.End(attack); finalSwing = attack; }
            Require(CombatCards.ThirdHitRevision == 1 && CombatCards.ComboHits == 0, "one confirmed third hit cue");
            var secondTarget = new GameObject("Temporary second sword victim"); secondTarget.SetActive(false); secondTarget.transform.SetParent(fixture.Root.transform);
            var other = secondTarget.AddComponent<SharedProbeEnemy>(); other.SetHP(1000);
            CombatCards.Hit(finalSwing, other, 100);
            Require(CombatCards.ThirdHitRevision == 1, "multi-target swing repeats cue");
            Require(enemy.SharedHP == 9510, "one hit per swing and third hit bonus ceiling"); //145 +145 +200
            typeof(LevelStatManager).GetMethod("Subscriptions", Private).Invoke(stats, null);
            stats._boostItems.Add(AssetDatabase.LoadAssetAtPath<BoostItem>("Assets/Scripts/LevelStat/Boosters/BoostItem_HP.asset"));
            var random = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(127);
                for (int i = 0; i < 100; i++)
                {
                    var offers = stats.GetRandomBoostsWithRarity();
                    Require(offers.Exists(x => !CombatCardRules.IsSpecial(x.Item.BoostType)), "safe numeric alternative");
                    Require(!offers.Exists(x => x.Item.BoostType == BoostType.BattleRhythm || x.Item.BoostType == BoostType.OnTheEdge), "max rank excluded");
                }
            }
            finally { UnityEngine.Random.state = random; }
            return "[Combat cards] PASS: live acquisition/clamp, rank ceiling, no repeated price/refill/death, actual overkill heal, duplicate targets, dead player, melee combo and safe offers";
        }

        public static async Task RunShared(SharedDepartureState authority, SharedDepartureState remote)
        {
            using var fixture = new Fixture();
            var identity = fixture.Root.AddComponent<WorldSpawnIdentity>();
            SharedRunContext.Begin(authority.Seed, authority.LevelId, fixture.Root, fixture.Save.PersistentProvider, remote);
            identity.Key = "test:combat-cards";
            try
            {
                fixture.Cards.Stats.BloodPactRank = 3; fixture.Health(10); fixture.UnlockedBudget();
                foreach (var avatar in LobbyNetworkAvatar.All) typeof(LobbyNetworkAvatar).GetProperty("Health").SetValue(avatar, 100f);
                fixture.EnemyHP(5); authority.SampleEnemy(identity, fixture.Enemy);
                await Until(() => remote.TryEnemy(identity.Id, out var record) && record.HP == 5);
                CombatCards.Hit(CombatCards.Begin(false), fixture.Enemy, 100);
                await Until(() => fixture.Player.Health > 10);
                Near(fixture.Player.Health, 10.4f, "remote shooter owns actual-damage healing");
                int slot = authority.CrewSlot(remote.Runner.LocalPlayer);
                int sequence = authority.WeaponSequences[slot];
                fixture.EnemyHP(100); authority.SampleEnemy(identity, fixture.Enemy);
                remote.RPC_WeaponDamage(identity.Id, 100, sequence); // Replay must not damage or heal.
                await Task.Delay(300);
                Require(fixture.Enemy.SharedHP == 100, "duplicate damage rejected");
                Near(fixture.Player.Health, 10.4f, "duplicate heal rejected");
                Near((float)CombatCards.HealingTotal, .4f, "confirmed heal cue once");
                authority.RPC_EnemyDamage(identity.Id, 10); // Environmental/legacy route has no lifesteal.
                await Until(() => fixture.Enemy.SharedHP == 90);
                Near(fixture.Player.Health, 10.4f, "another player's damage cannot heal local owner");
                fixture.Cards.Stats.BloodPactRank = 0; fixture.Cards.Stats.BattleRhythmRank = 3;
                fixture.EnemyHP(1000); authority.SampleEnemy(identity, fixture.Enemy);
                await Until(() => remote.TryEnemy(identity.Id, out var resetEnemy) && resetEnemy.HP == 1000);
                for (int i = 0; i < 3; i++) { var attack = CombatCards.Begin(true); CombatCards.Hit(attack, fixture.Enemy, 100); CombatCards.End(attack); }
                Require(CombatCards.ThirdHitRevision == 0, "unconfirmed third hit flashes");
                await Until(() => CombatCards.ThirdHitRevision == 1 && fixture.Enemy.SharedHP == 630);
                int thirdSequence = authority.WeaponSequences[slot];
                remote.RPC_WeaponDamage(identity.Id, 170, thirdSequence);
                CombatCards.Confirm(thirdSequence, 170);
                await Task.Delay(200);
                Require(CombatCards.ThirdHitRevision == 1 && fixture.Enemy.SharedHP == 630, "third hit replay repeats cue/damage");
                authority.RPC_RecoveryProgress(new SharedPersonalData { BloodPactRank = 1, BattleRhythmRank = 2, OnTheEdgeRank = 3, LastRoundRank = 2, HeavyCoatRank = 1 });
                remote.RPC_RecoveryProgress(new SharedPersonalData { BloodPactRank = 3, BattleRhythmRank = 1, OnTheEdgeRank = 2, LastRoundRank = 1, HeavyCoatRank = 3 });
                await Until(() => remote.Personal.TryGet(authority.Runner.LocalPlayer, out var a) && a.Cards.BloodPactRank == 1 &&
                    remote.Personal.TryGet(remote.Runner.LocalPlayer, out var b) && b.Cards.BloodPactRank == 3 && b.Cards.OnTheEdgeRank == 2 && a.Cards.LastRoundRank == 2 && a.Cards.HeavyCoatRank == 1 && b.Cards.LastRoundRank == 1 && b.Cards.HeavyCoatRank == 3);
                Debug.Log("[Combat cards] PASS: two Cloud peers; actual HP confirmation to attacker, overkill, replay rejection, other attacker isolation and separate rank snapshots");
            }
            finally { SharedRunContext.End(); }
        }
        private static async Task Until(Func<bool> check)
        {
            double end = Time.realtimeSinceStartupAsDouble + 8;
            while (!check()) { if (Time.realtimeSinceStartupAsDouble > end) throw new TimeoutException("combat card protocol"); await Task.Delay(25); }
        }
    }
}
