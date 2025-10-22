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
    private int _countLevelUp;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } 
        else
        {
            Destroy(Instance.gameObject);
            return;
        }
        LoadStats();
        Subscriptions();
    }
    private void Start()
    {
        PlayerStatsManager.Instance.Experience().NewLevel.AddListener(AddNewLevelNum);
        PlayerInput.Instance.ALevelUp += ButtonLevelUse;

    }

    private void NewLevel()
    {
        LevelUpUi.Instance.NewLevel(GetRandomBoostsWithRarity()); 
    }
    private void AddNewLevelNum()
    {
        _countLevelUp++;
        LevelUpUi.Instance.AddNewLevelCount(_countLevelUp);
    }
    private void ButtonLevelUse()
    {
        CheckNewLevel();
    }
    public bool CheckNewLevel()
    {
        if (_countLevelUp == 0) return false;
        _countLevelUp--;
        NewLevel();
        return true;
    }
    public void Refresh()
    {
        LevelUpUi.Instance.RefreshLevel(GetRandomBoostsWithRarity());
    }
    private void Subscriptions()
    {
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
        if (!save.Save) { return; }
        Stats = save;
    }
    private void Change(BoostType boost, float count)
    {
        switch (boost)
        {
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
        SaveManager.Instance.SaveLevelUpdate(Stats);

    }

    public List<ItemWithRare> GetRandomBoostsWithRarity(int count = 3)
    {

        List<ItemWithRare> result = new List<ItemWithRare>();

        // Убедись, что у нас достаточно разных бустов
        if (_boostItems.Count < count)
        {
            Debug.LogWarning("Недостаточно BoostItem для выбора.");
            return result;
        }

        // Получаем 3 случайных и уникальных бустера
        List<BoostItem> selectedItems = _boostItems
            .OrderBy(x => UnityEngine.Random.value)
            .Distinct()
            .Take(count)
            .ToList();

        foreach (BoostItem item in selectedItems)
        {
            RareType selectedRarity = GetRandomRarityByChance();

            // Проверим, есть ли такая рарность у этого бустера
            if (item.RareRewards.Any(r => r.Rare == selectedRarity))
            {
                ItemWithRare itemR = new ItemWithRare() { Rare = selectedRarity , Item = item}; 
                result.Add(itemR) ;
            }
            else
            {
                // Если нет нужной рарности — выбираем первую доступную
                var fallbackRarity = item.RareRewards[0].Rare;
                ItemWithRare itemR = new ItemWithRare() { Rare = selectedRarity, Item = item };
                result.Add(itemR);
            }
        }

        return result;
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
    public void DeleteProgress()
    {
        Stats = StatsDefault;
        SaveManager.Instance.SaveLevelUpdate(Stats);
    }
}
