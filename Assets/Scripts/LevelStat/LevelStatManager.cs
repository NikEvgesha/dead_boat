using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct ItemWithRare
{
    public RareType Rare;
    public BoostItem Item;
    public bool IsAds;
}
[Serializable]
public struct RareChance
{
    public RareType Rare;
    public float Chance;
}
[Serializable]
public struct Stats
{
    public bool Save;
    // One persisted record owns both bonuses and all unspent choices (including an open offer).
    public bool HasUpgradeCount;
    public int UnspentUpgrades;
    public int InventorySlots;
    public int BloodPactRank, BattleRhythmRank, OnTheEdgeRank;
    public int LastRoundRank, HeavyCoatRank;
    public int SteadyAimRank, PatientReloadRank;
    [Header("Игрок")]
    public float HP;
    public float MultExp;
    public float MoveSpeedMult;
    public float MoneyMultSale;

    [Header("Лодка")]
    public float MaxFuel;
    public float ConsumptionFuel;
    public float AddMultFuel;
    public float MaxSpeedBoard;

    [Header("Ближний бой")]
    public int MeleDamage;
    public float MeleAttackSpeed;

    [Header("Дальний бой")]
    public float RangeDamage;
    public float RangeAttackSpeed;
    public float RangeReloadSpeed;
}
public class LevelStatManager : MonoBehaviour
{
    public static LevelStatManager Instance;
    public Stats StatsDefault = new Stats()
    {
        Save = true,
        HP = 0,
        MultExp = 1,
        MoveSpeedMult = 1,
        MoneyMultSale = 1,

        MaxFuel = 0,
        ConsumptionFuel = 1,
        AddMultFuel = 1,
        MaxSpeedBoard = 0,

        MeleDamage = 0,
        MeleAttackSpeed = 1,

        RangeDamage = 0,
        RangeAttackSpeed = 1,
        RangeReloadSpeed = 1
    };
    public Stats Stats = new Stats()
    {
        Save = true,
        HP = 0,
        MultExp = 1,
        MoveSpeedMult = 1,
        MoneyMultSale = 1,

        MaxFuel = 0,
        ConsumptionFuel = 1,
        AddMultFuel = 1,
        MaxSpeedBoard = 0,

        MeleDamage = 0,
        MeleAttackSpeed = 1,

        RangeDamage = 0,
        RangeAttackSpeed = 1,
        RangeReloadSpeed = 1
};
    public List<BoostItem> _boostItems = new List<BoostItem>();
    public List<RareChance> _rarityChances;
    public bool ScriptedOffersOnly { get; set; }
    private int _countLevelUp;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } 
        else
        {
            Destroy(gameObject);
            return;
        }
        CombatCards.Reset();
        LoadStats();
        Subscriptions();
    }
    private void OnDestroy()
    {
        foreach (var item in _boostItems) if (item != null) item.Change -= Change;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        PlayerStatsManager.Instance.Experience().NewLevel.AddListener(AddNewLevelNum);
        PlayerInput.Instance.ALevelUp += ButtonLevelUse;
        CombatCardHud.Attach(LevelUpUi.Instance);

        if (_countLevelUp > 0)
        {
            LevelUpUi.Instance.AddNewLevelCount(_countLevelUp);
        }
    }

    private void NewLevel()
    {
        LevelUpUi.Instance.NewLevel(GetRandomBoostsWithRarity()); 
    }
    private void AddNewLevelNum()
    {
        if (ScriptedOffersOnly) return;
        _countLevelUp++;
        SaveManager.Instance.SaveCardProgress(Stats, _countLevelUp);
        LevelUpUi.Instance.AddNewLevelCount(_countLevelUp);
    }
    private void ButtonLevelUse()
    {
        CheckNewLevel();
    }
    public bool CheckNewLevel()
    {
        if (ScriptedOffersOnly || _countLevelUp <= 0 || LevelUpUi.Instance == null || LevelUpUi.Instance.IsChoiceOpen) return false;
        NewLevel();
        return true;
    }
    public int PendingUpgrades => _countLevelUp;
    public bool HaveLevelUp()
    {
        // Preserve the previous interstitial rule: only the final normal choice shows it.
        return _countLevelUp > (LevelUpUi.Instance != null && LevelUpUi.Instance.IsChoiceOpen ? 1 : 0);
    }
    public bool ApplyEarnedChoice(BoostItem item, RareType rarity)
    {
        if (ScriptedOffersOnly || _countLevelUp <= 0 || item == null ||
            !item.RareRewards.Exists(x => x.Rare == rarity)) return false;
        _countLevelUp--;
        Change(item.BoostType, item.RareRewards.Find(x => x.Rare == rarity).Reward);
        return true;
    }
    public void Refresh()
    {
        LevelUpUi.Instance.RefreshLevel(GetRandomBoostsWithRarity());
    }
    private void Subscriptions()
    {
        var slots = Resources.Load<BoostItem>("BoostItem_InventorySlots");
        if (slots != null && !_boostItems.Contains(slots)) _boostItems.Add(slots);
        foreach (string name in new[] { "BloodPact", "BattleRhythm", "OnTheEdge", "LastRound", "HeavyCoat", "SteadyAim", "PatientReload" })
        {
            var card = Resources.Load<BoostItem>("BoostItem_" + name);
            if (card != null && !_boostItems.Contains(card)) _boostItems.Add(card);
        }
        foreach (var item in _boostItems)
        {
            item.Change += Change;
        }
    }
    private void SaveStats()
    {

    }
    private void LoadStats() 
    {
        Stats save = SaveManager.Instance.LoadLevelUpdate();
        _countLevelUp = SaveManager.Instance.LoadLevelUp();
        if (save.Save) Stats = save;
    }
    private void Change(BoostType boost, float count)
    {
        switch (boost)
        {
            case BoostType.BloodPact:
                Stats.BloodPactRank = Mathf.Clamp(Stats.BloodPactRank + 1, 0, 3);
                PlayerStatsManager.Instance?.ClampCardHealth();
                break;
            case BoostType.BattleRhythm:
                Stats.BattleRhythmRank = Mathf.Clamp(Stats.BattleRhythmRank + 1, 0, 3);
                break;
            case BoostType.OnTheEdge:
                Stats.OnTheEdgeRank = Mathf.Clamp(Stats.OnTheEdgeRank + 1, 0, 3);
                break;
            case BoostType.SteadyAim:
                Stats.SteadyAimRank = Mathf.Clamp(Stats.SteadyAimRank + 1, 0, 3);
                break;
            case BoostType.PatientReload:
                Stats.PatientReloadRank = Mathf.Clamp(Stats.PatientReloadRank + 1, 0, 3);
                break;
            case BoostType.LastRound:
                Stats.LastRoundRank = Mathf.Clamp(Stats.LastRoundRank + 1, 0, 3);
                break;
            case BoostType.HeavyCoat:
                Stats.HeavyCoatRank = Mathf.Clamp(Stats.HeavyCoatRank + 1, 0, 3);
                PlayerStatsManager.Instance?.ClampCardHealth();
                break;
            case BoostType.InventorySlots:
                Stats.InventorySlots = Mathf.Clamp(Stats.InventorySlots + Mathf.Max(0, Mathf.RoundToInt(count)), 0, 27);
                Inventory.Instance?.RefreshCapacity();
                break;
            case BoostType.HP:
                Stats.HP += count;
                PlayerStatsManager.Instance.AddHealth((int)count);
                break;
            case BoostType.MultExp:
                Stats.MultExp += count;
                break;
            case BoostType.MoveSpeedMult:
                Stats.MoveSpeedMult += count;
                break;
            case BoostType.MoneyMultSale:
                Stats.MoneyMultSale += count;
                break;
            case BoostType.MaxFuel:
                Stats.MaxFuel += count;
                break;
            case BoostType.ConsumptionFuel:
                Stats.ConsumptionFuel += count;
                break;
            case BoostType.AddMultFuel:
                Stats.AddMultFuel += count;
                break;
            case BoostType.MaxSpeedBoard:
                Stats.MaxSpeedBoard += count;
                break;
            case BoostType.MeleDamage:
                Stats.MeleDamage += (int)count;
                break;
            case BoostType.MeleAttackSpeed:
                Stats.MeleAttackSpeed += count;
                break;
            case BoostType.RangeDamage:
                Stats.RangeDamage += count;
                break;
            case BoostType.RangeAttackSpeed:
                Stats.RangeAttackSpeed += count;
                break;
            case BoostType.RangeReloadSpeed:
                Stats.RangeReloadSpeed += count;
                break;
            default:
                Debug.LogWarning($"Неизвестный тип BoostType: {boost}");
                break;
        }
        SaveManager.Instance.SaveCardProgress(Stats, _countLevelUp);

    }

    public List<ItemWithRare> GetRandomBoostsWithRarity(int count = 3)
    {

        var available = _boostItems.Where(x => x != null && x.Chance > 0 &&
            (!CombatCardRules.IsSpecial(x.BoostType) || CombatCardRules.Rank(Stats, x.BoostType) < 3) &&
            (x.BoostType != BoostType.InventorySlots || Inventory.Instance == null || Inventory.Instance.BagCapacity < Inventory.MaximumBagCapacity))
            .GroupBy(x => x.BoostType).Select(x => x.First()).ToList();
        var selected = new List<BoostItem>();
        // Reserve a safe numeric alternative before drawing the remaining weighted choices.
        var safe = available.Where(x => !CombatCardRules.IsSpecial(x.BoostType)).ToList();
        if (count > 0 && safe.Count > 0) selected.Add(WeightedCard(safe));
        available.RemoveAll(x => selected.Contains(x));
        while (selected.Count < count && available.Count > 0)
        {
            var item = WeightedCard(available); selected.Add(item); available.Remove(item);
        }
        return selected.OrderBy(x => UnityEngine.Random.value).Select(item => new ItemWithRare
        {
            Item = item, Rare = CombatCardRules.IsSpecial(item.BoostType)
                ? (item.BoostType == BoostType.BattleRhythm ? RareType.Uncommon : RareType.Rare)
                : GetRandomRarityByChance()
        }).ToList();
    }
    private static BoostItem WeightedCard(List<BoostItem> cards)
    {
        float roll = UnityEngine.Random.value * cards.Sum(x => x.Chance);
        foreach (var card in cards) { roll -= card.Chance; if (roll <= 0) return card; }
        return cards[cards.Count - 1];
    }
    private RareType GetRandomRarityByChance()
    {
        float total = _rarityChances.Sum(rc => rc.Chance);
        float roll = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var rc in _rarityChances)
        {
            cumulative += rc.Chance;
            if (roll <= cumulative)
                return rc.Rare;
        }

        // fallback на всякий случай
        return _rarityChances.Last().Rare;
    }
    public void RestoreRecovery(Stats cards, int upgrades)
    {
        Stats = cards;
        CombatCards.Reset();
        _countLevelUp = Mathf.Max(0, upgrades);
        SaveManager.Instance.SaveCardProgress(Stats, _countLevelUp);
        if (LevelUpUi.Instance != null)
        {
            LevelUpUi.Instance.RestartAll();
            if (_countLevelUp > 0) LevelUpUi.Instance.AddNewLevelCount(_countLevelUp);
        }
    }

    public void DeleteProgress()
    {
        CombatCards.Reset();
        Stats = StatsDefault;
        _countLevelUp = 0;
        SaveManager.Instance.SaveCardProgress(Stats, 0);
        LevelUpUi.Instance.RestartAll();
    }
}
