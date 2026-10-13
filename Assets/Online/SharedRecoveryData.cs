using System;
using Fusion;
using UnityEngine;

namespace DeadBoat.Online
{
    [Serializable]
    public struct SharedRecoveryItem : INetworkStruct
    {
        public ulong WorldId;
        public int Template, Ammo;
        public int Equipped;
    }

    [Serializable]
    public struct SharedPersonalData : INetworkStruct
    {
        public int Revision, ItemCount, Coins, Experience, Level, Upgrades;
        public int Pistol, Rifle, Shotgun, MeleeDamage;
        public float Health, CardHP, ExpMultiplier, MoveMultiplier, SaleMultiplier;
        public float Fuel, Consumption, Fill, Speed, MeleeSpeed, RangeDamage, RangeSpeed, ReloadSpeed;
        public Stats Cards => new Stats
        {
            Save = true, HP = CardHP, MultExp = ExpMultiplier, MoveSpeedMult = MoveMultiplier,
            MoneyMultSale = SaleMultiplier, MaxFuel = Fuel, ConsumptionFuel = Consumption,
            AddMultFuel = Fill, MaxSpeedBoard = Speed, MeleDamage = MeleeDamage,
            MeleAttackSpeed = MeleeSpeed, RangeDamage = RangeDamage, RangeAttackSpeed = RangeSpeed,
            RangeReloadSpeed = ReloadSpeed
        };

        public static SharedPersonalData Capture()
        {
            var save = SharedRunContext.Save != null ? SharedRunContext.Save : SaveManager.Instance.PersistentProvider;
            var cards = LevelStatManager.Instance != null ? LevelStatManager.Instance.Stats : default;
            var exp = save.LoadPlayerExperience();
            var ammo = PlayerAmmoManager.Instance;
            return new SharedPersonalData
            {
                Health = PlayerStatsManager.Instance.Health, Coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.Coins : save.LoadGameCoin(),
                Experience = exp.Item1, Level = exp.Item2, Upgrades = save.LoadLevelUp(),
                Pistol = ammo.GetAmmo(WeaponType.Pistol), Rifle = ammo.GetAmmo(WeaponType.Rifle),
                Shotgun = ammo.GetAmmo(WeaponType.Shotgun),
                CardHP = cards.HP, ExpMultiplier = cards.MultExp, MoveMultiplier = cards.MoveSpeedMult,
                SaleMultiplier = cards.MoneyMultSale, Fuel = cards.MaxFuel, Consumption = cards.ConsumptionFuel,
                Fill = cards.AddMultFuel, Speed = cards.MaxSpeedBoard, MeleeDamage = cards.MeleDamage,
                MeleeSpeed = cards.MeleAttackSpeed, RangeDamage = cards.RangeDamage,
                RangeSpeed = cards.RangeAttackSpeed, ReloadSpeed = cards.RangeReloadSpeed
            };
        }

        public void SeedSave(SaveProvider save, int level, float distance, float origin, float boatFuel)
        {
            save.SaveDistance(Mathf.Max(0, Mathf.RoundToInt(distance)));
            save.SaveLevelId(level);
            save.SaveBoardFixPos(origin);
            save.SavePlayerFixPos(origin);
            save.SaveFuel(Mathf.RoundToInt(boatFuel));
            save.SavePlayerStats(Coins, Health);
            save.SavePlayerExperience(Experience, Level);
            save.SaveLevelUpdate(Cards);
            save.SaveLevelUp(Upgrades);
            save.SaveAmmo(WeaponType.Pistol, Pistol);
            save.SaveAmmo(WeaponType.Rifle, Rifle);
            save.SaveAmmo(WeaponType.Shotgun, Shotgun);
        }
    }
}
