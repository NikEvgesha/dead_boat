using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProfessionStarterItem
{
    public PickableItem itemPrefab;
    [Min(1)] public int amount = 1;
}

[Serializable]
public class ProfessionPassiveBonuses
{
    [Header("Player")]
    public float maxHealthFlat;
    public float maxStaminaFlat;
    public float moveSpeedFlat;
    public float moveSpeedMultiplier = 1f;
    public float staminaConsumptionMultiplier = 1f;
    public float staminaRestoreMultiplier = 1f;
    public float experienceMultiplier = 1f;
    public float incomingDamageMultiplier = 1f;
    [Range(0f, 1f)] public float dodgeChance;
    public float healthDrainPerSecond;
    public float healOnKillFlat;

    [Header("Economy")]
    public float saleRewardMultiplier = 1f;

    [Header("Boat")]
    public float maxFuelFlat;
    public float fuelConsumptionMultiplier = 1f;
    public float fuelFillMultiplier = 1f;
    public float boatSpeedFlat;

    [Header("Combat")]
    public float meleeDamageFlat;
    public float meleeAttackSpeedMultiplier = 1f;
    public float rangedDamageFlat;
    public float rangedAttackSpeedMultiplier = 1f;
    public float rangedReloadSpeedMultiplier = 1f;
    public float outgoingDamageMultiplier = 1f;
    public float lowHealthDamageMultiplier = 1f;
    [Range(0f, 1f)] public float criticalChance;
    public float criticalDamageMultiplier = 1f;
    public float randomDamageMinMultiplier = 1f;
    public float randomDamageMaxMultiplier = 1f;
    [Range(0f, 1f)] public float burnChance;
    public float burnDamagePerSecond;
    public float burnDuration;

    [Header("Loot")]
    public float rareLootChanceMultiplier = 1f;

    public float SafeMoveSpeedMultiplier => moveSpeedMultiplier > 0f ? moveSpeedMultiplier : 1f;
    public float SafeStaminaConsumptionMultiplier => staminaConsumptionMultiplier > 0f ? staminaConsumptionMultiplier : 1f;
    public float SafeStaminaRestoreMultiplier => staminaRestoreMultiplier > 0f ? staminaRestoreMultiplier : 1f;
    public float SafeExperienceMultiplier => experienceMultiplier > 0f ? experienceMultiplier : 1f;
    public float SafeIncomingDamageMultiplier => incomingDamageMultiplier > 0f ? incomingDamageMultiplier : 1f;
    public float SafeSaleRewardMultiplier => saleRewardMultiplier > 0f ? saleRewardMultiplier : 1f;
    public float SafeFuelConsumptionMultiplier => fuelConsumptionMultiplier > 0f ? fuelConsumptionMultiplier : 1f;
    public float SafeFuelFillMultiplier => fuelFillMultiplier > 0f ? fuelFillMultiplier : 1f;
    public float SafeMeleeAttackSpeedMultiplier => meleeAttackSpeedMultiplier > 0f ? meleeAttackSpeedMultiplier : 1f;
    public float SafeRangedAttackSpeedMultiplier => rangedAttackSpeedMultiplier > 0f ? rangedAttackSpeedMultiplier : 1f;
    public float SafeRangedReloadSpeedMultiplier => rangedReloadSpeedMultiplier > 0f ? rangedReloadSpeedMultiplier : 1f;
    public float SafeOutgoingDamageMultiplier => outgoingDamageMultiplier > 0f ? outgoingDamageMultiplier : 1f;
    public float SafeLowHealthDamageMultiplier => lowHealthDamageMultiplier > 0f ? lowHealthDamageMultiplier : 1f;
    public float SafeCriticalDamageMultiplier => criticalDamageMultiplier > 0f ? criticalDamageMultiplier : 1f;
    public float SafeRandomDamageMinMultiplier => randomDamageMinMultiplier > 0f ? randomDamageMinMultiplier : 1f;
    public float SafeRandomDamageMaxMultiplier => randomDamageMaxMultiplier > 0f ? randomDamageMaxMultiplier : 1f;
    public float SafeRareLootChanceMultiplier => rareLootChanceMultiplier > 0f ? rareLootChanceMultiplier : 1f;
}

[Serializable]
public class ProfessionDefinition
{
    public string professionId;
    public string title;
    [TextArea(2, 8)] public string description;
    public Sprite icon;

    [Header("Unlock")]
    public bool availableInRandomUnlockPool = true;
    [Min(0)] public int randomUnlockWeight = 1;
    [Min(0)] public int directSoftCurrencyCost = 500;
    public CurrencyType directSoftCurrencyType = CurrencyType.Coins;
    public string purchaseProductId;
    public bool allowSoftCurrencyFallbackWhenPurchasesUnavailable = true;

    [Header("Start Bonuses")]
    [Min(0)] public int startCoinsBonus;
    [Min(0)] public int startGemsBonus;
    public bool defaultUnlocked;
    public List<ProfessionStarterItem> starterItems = new();
    public List<string> perkLines = new();
    public ProfessionPassiveBonuses passiveBonuses = new();
}

[CreateAssetMenu(fileName = "ProfessionCatalog", menuName = "ScriptableObject/Professions/ProfessionCatalog")]
public class ProfessionCatalog : ScriptableObject
{
    [SerializeField] private List<ProfessionDefinition> _definitions = new();

    private Dictionary<string, ProfessionDefinition> _cache;

    public IReadOnlyList<ProfessionDefinition> Definitions => _definitions;

    public bool TryGet(string professionId, out ProfessionDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(professionId))
            return false;

        BuildCacheIfNeeded();
        return _cache.TryGetValue(professionId, out definition);
    }

    public string GetDefaultProfessionId()
    {
        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            if (definition.defaultUnlocked)
                return definition.professionId;
        }

        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            return definition.professionId;
        }

        return string.Empty;
    }

    public List<ProfessionDefinition> GetValidDefinitions()
    {
        List<ProfessionDefinition> result = new();

        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            result.Add(definition);
        }

        return result;
    }

    private void BuildCacheIfNeeded()
    {
        if (_cache != null)
            return;

        _cache = new Dictionary<string, ProfessionDefinition>();

        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                continue;

            _cache[definition.professionId] = definition;
        }
    }

    private void OnValidate()
    {
        _cache = null;
    }
}
