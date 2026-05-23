using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class ProfessionService
{
    private const string DefaultCatalogResourcePath = "Professions/ProfessionCatalog";
    private const string FallbackProfessionId = "yunga";

    private static ProfessionCatalog _configuredCatalog;
    private static readonly List<ProfessionDefinition> _definitions = new();
    private static readonly Dictionary<string, ProfessionDefinition> _definitionsById = new();
    private static readonly ProfessionPassiveBonuses _defaultPassiveBonuses = new();
    private static ProfessionState _state;
    private static bool _initialized;
    private static float _healthDrainAccumulator;

    public static event Action StateChanged;

    public static string CurrentProfessionId
    {
        get
        {
            EnsureInitialized();
            return _state.selectedProfessionId;
        }
    }

    public static void ConfigureCatalog(ProfessionCatalog catalog)
    {
        if (_configuredCatalog == catalog && _initialized)
            return;

        _configuredCatalog = catalog;
        _initialized = false;
        _state = null;
    }

    public static IReadOnlyList<ProfessionDefinition> GetDefinitions()
    {
        EnsureInitialized();
        return _definitions;
    }

    public static ProfessionDefinition GetCurrentProfession()
    {
        EnsureInitialized();
        if (!_state.hasExplicitProfessionChoice)
            return null;

        return TryGetDefinition(_state.selectedProfessionId, out ProfessionDefinition definition)
            ? definition
            : null;
    }

    public static bool TryGetDefinition(string professionId, out ProfessionDefinition definition)
    {
        EnsureInitialized();
        return _definitionsById.TryGetValue(professionId, out definition);
    }

    public static bool IsUnlocked(string professionId)
    {
        EnsureInitialized();
        return _state.unlockedProfessionIds.Contains(professionId);
    }

    public static bool HasLockedProfessions()
    {
        EnsureInitialized();

        for (int i = 0; i < _definitions.Count; i++)
        {
            if (!IsUnlocked(_definitions[i].professionId))
                return true;
        }

        return false;
    }

    public static bool HasRandomLockedProfessions()
    {
        EnsureInitialized();

        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition == null)
                continue;

            if (IsRandomUnlockCandidate(definition) && !IsUnlocked(definition.professionId))
                return true;
        }

        return false;
    }

    public static int GetUnlockedCount()
    {
        EnsureInitialized();
        return _state.unlockedProfessionIds.Count;
    }

    public static int GetLockedCount()
    {
        EnsureInitialized();
        return Mathf.Max(0, _definitions.Count - _state.unlockedProfessionIds.Count);
    }

    public static bool HasExplicitProfessionChoice()
    {
        EnsureInitialized();
        return _state.hasExplicitProfessionChoice;
    }

    public static bool IsNoProfessionSelected()
    {
        EnsureInitialized();
        return !_state.hasExplicitProfessionChoice;
    }

    public static int GetTotalProfessionCount()
    {
        EnsureInitialized();
        return _definitions.Count;
    }

    public static bool TrySelectProfession(string professionId)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(professionId))
            return false;

        if (!IsUnlocked(professionId))
            return false;

        if (_state.selectedProfessionId == professionId && _state.hasExplicitProfessionChoice)
            return true;

        _state.selectedProfessionId = professionId;
        _state.hasExplicitProfessionChoice = true;
        SaveState();
        return true;
    }

    public static bool TrySelectNoProfession()
    {
        EnsureInitialized();

        if (!_state.hasExplicitProfessionChoice && string.IsNullOrWhiteSpace(_state.selectedProfessionId))
            return true;

        _state.selectedProfessionId = string.Empty;
        _state.hasExplicitProfessionChoice = false;
        SaveState();
        return true;
    }

    public static bool TryUnlockRandomLockedProfession(int unlockPrice, CurrencyType currencyType, out ProfessionDefinition unlockedDefinition)
    {
        EnsureInitialized();
        unlockedDefinition = null;

        List<ProfessionDefinition> lockedDefinitions = new();
        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (IsRandomUnlockCandidate(definition) && !IsUnlocked(definition.professionId))
                lockedDefinitions.Add(definition);
        }

        if (lockedDefinitions.Count == 0)
            return false;

        int clampedPrice = Mathf.Max(0, unlockPrice);
        if (clampedPrice > 0)
        {
            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager == null)
                return false;

            if (!currencyManager.CheckEnoughCurrency(currencyType, clampedPrice))
                return false;

            if (!currencyManager.RemoveCurrency(currencyType, clampedPrice))
                return false;
        }

        unlockedDefinition = RollRandomProfession(lockedDefinitions);
        UnlockProfessionInternal(unlockedDefinition.professionId, false);
        SaveState();
        return true;
    }

    public static bool TryUnlockRandomLockedProfession(int unlockPriceGems, out ProfessionDefinition unlockedDefinition)
    {
        return TryUnlockRandomLockedProfession(unlockPriceGems, CurrencyType.Gems, out unlockedDefinition);
    }

    public static bool TryUnlockProfessionWithSoftCurrency(string professionId, int price, CurrencyType currencyType)
    {
        EnsureInitialized();

        if (currencyType == CurrencyType.Real)
            return false;

        if (!TryGetDefinition(professionId, out ProfessionDefinition definition))
            return false;

        if (IsUnlocked(definition.professionId))
            return true;

        int clampedPrice = Mathf.Max(0, price);
        if (clampedPrice > 0)
        {
            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager == null)
                return false;

            if (!currencyManager.CheckEnoughCurrency(currencyType, clampedPrice))
                return false;

            if (!currencyManager.RemoveCurrency(currencyType, clampedPrice))
                return false;
        }

        UnlockProfessionInternal(definition.professionId, true);
        SaveState();
        return true;
    }

    public static bool UnlockProfessionFromPurchase(string professionId)
    {
        EnsureInitialized();

        if (!TryGetDefinition(professionId, out ProfessionDefinition definition))
            return false;

        UnlockProfessionInternal(definition.professionId, true);
        SaveState();
        return true;
    }

    public static ReadOnlyCollection<PickableItem> BuildStarterPack(IReadOnlyList<PickableItem> baseStarterItems, bool includeProfessionStarterItems)
    {
        EnsureInitialized();

        List<PickableItem> result = new();
        if (baseStarterItems != null)
        {
            for (int i = 0; i < baseStarterItems.Count; i++)
            {
                PickableItem item = baseStarterItems[i];
                if (item != null)
                    result.Add(item);
            }
        }

        if (includeProfessionStarterItems && _state.hasExplicitProfessionChoice)
        {
            ProfessionDefinition profession = GetCurrentProfession();
            AppendProfessionStarterItems(result, profession);
        }

        return result.AsReadOnly();
    }

    public static bool ApplyCurrentProfessionStartBonuses()
    {
        EnsureInitialized();

        if (!_state.hasExplicitProfessionChoice)
            return false;

        ProfessionDefinition profession = GetCurrentProfession();
        if (profession == null)
            return false;

        CurrencyManager currencyManager = CurrencyManager.Instance;
        if (currencyManager == null)
            return false;

        bool changed = false;

        if (profession.startCoinsBonus > 0)
        {
            currencyManager.AddCurrency(CurrencyType.Coins, profession.startCoinsBonus);
            changed = true;
        }

        if (profession.startGemsBonus > 0)
        {
            currencyManager.AddCurrency(CurrencyType.Gems, profession.startGemsBonus);
            changed = true;
        }

        return changed;
    }

    public static ProfessionPassiveBonuses GetCurrentPassiveBonuses()
    {
        EnsureInitialized();

        if (!_state.hasExplicitProfessionChoice)
            return _defaultPassiveBonuses;

        ProfessionDefinition profession = GetCurrentProfession();
        if (profession == null || profession.passiveBonuses == null)
            return _defaultPassiveBonuses;

        return profession.passiveBonuses;
    }

    public static float ApplyMoveSpeed(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return (baseValue + bonuses.moveSpeedFlat) * bonuses.SafeMoveSpeedMultiplier;
    }

    public static float ApplyMaxHealth(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue + bonuses.maxHealthFlat;
    }

    public static float ApplyMaxStamina(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue + bonuses.maxStaminaFlat;
    }

    public static float ApplyStaminaConsumption(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue * bonuses.SafeStaminaConsumptionMultiplier;
    }

    public static float ApplyStaminaRestore(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue * bonuses.SafeStaminaRestoreMultiplier;
    }

    public static int ApplyExperienceGain(int baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * bonuses.SafeExperienceMultiplier));
    }

    public static int ApplySaleReward(int baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * bonuses.SafeSaleRewardMultiplier));
    }

    public static float ApplyMaxFuel(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue + bonuses.maxFuelFlat;
    }

    public static float ApplyFuelConsumption(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue * bonuses.SafeFuelConsumptionMultiplier;
    }

    public static float ApplyFuelFill(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue * bonuses.SafeFuelFillMultiplier;
    }

    public static float ApplyBoatMaxSpeed(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue + bonuses.boatSpeedFlat;
    }

    public static float ApplyMeleeDamage(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return ApplyOutgoingDamageModifiers(baseValue + bonuses.meleeDamageFlat, bonuses);
    }

    public static float ApplyMeleeAttackSpeed(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue * bonuses.SafeMeleeAttackSpeedMultiplier;
    }

    public static float ApplyRangedDamage(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return ApplyOutgoingDamageModifiers(baseValue + bonuses.rangedDamageFlat, bonuses);
    }

    public static float ApplyRangedAttackSpeed(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue * bonuses.SafeRangedAttackSpeedMultiplier;
    }

    public static float ApplyRangedReloadSpeed(float baseValue)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        return baseValue * bonuses.SafeRangedReloadSpeedMultiplier;
    }

    public static int ApplyIncomingDamage(int baseDamage)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        int damage = Mathf.Max(0, baseDamage);
        if (damage <= 0)
            return 0;

        if (bonuses.dodgeChance > 0f && UnityEngine.Random.value < Mathf.Clamp01(bonuses.dodgeChance))
            return 0;

        damage = Mathf.RoundToInt(damage * bonuses.SafeIncomingDamageMultiplier);
        return Mathf.Max(0, damage);
    }

    public static void TickPlayerEffects(PlayerStatsManager player, float deltaTime)
    {
        if (player == null || deltaTime <= 0f)
            return;

        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        if (bonuses.healthDrainPerSecond <= 0f)
        {
            _healthDrainAccumulator = 0f;
            return;
        }

        _healthDrainAccumulator += bonuses.healthDrainPerSecond * deltaTime;
        int damage = Mathf.FloorToInt(_healthDrainAccumulator);
        if (damage <= 0)
            return;

        _healthDrainAccumulator -= damage;
        player.TakeProfessionDrainDamage(damage);
    }

    public static void HandleEnemyKilled(EnemyCore enemy)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        if (bonuses.healOnKillFlat <= 0f)
            return;

        PlayerStatsManager player = PlayerStatsManager.Instance;
        if (player == null)
            return;

        player.AddHealth(Mathf.RoundToInt(bonuses.healOnKillFlat));
    }

    public static void TryApplyOnHitEffect(EnemyCore enemy)
    {
        if (enemy == null || enemy.IsDead)
            return;

        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        if (bonuses.burnDamagePerSecond <= 0f || bonuses.burnDuration <= 0f)
            return;

        float chance = bonuses.burnChance <= 0f ? 1f : Mathf.Clamp01(bonuses.burnChance);
        if (UnityEngine.Random.value > chance)
            return;

        enemy.ApplyBurn(bonuses.burnDuration, bonuses.burnDamagePerSecond);
    }

    public static float ApplyLocationItemSpawnChance(float baseChance)
    {
        ProfessionPassiveBonuses bonuses = GetCurrentPassiveBonuses();
        float chance = Mathf.Max(0f, baseChance);
        float multiplier = bonuses.SafeRareLootChanceMultiplier;
        if (Mathf.Approximately(multiplier, 1f))
            return chance;

        if (chance <= 10f)
            return chance * multiplier;

        if (chance <= 30f)
            return chance * Mathf.Lerp(1f, multiplier, 0.5f);

        return chance;
    }

    private static float ApplyOutgoingDamageModifiers(float damage, ProfessionPassiveBonuses bonuses)
    {
        if (bonuses == null)
            return damage;

        damage *= bonuses.SafeOutgoingDamageMultiplier;

        if (!Mathf.Approximately(bonuses.SafeLowHealthDamageMultiplier, 1f))
        {
            PlayerStatsManager player = PlayerStatsManager.Instance;
            if (player != null && player.MaxHealth > 0f)
            {
                float missingHealthPercent = 1f - Mathf.Clamp01(player.Health / player.MaxHealth);
                damage *= Mathf.Lerp(1f, bonuses.SafeLowHealthDamageMultiplier, missingHealthPercent);
            }
        }

        if (!Mathf.Approximately(bonuses.SafeRandomDamageMinMultiplier, 1f) ||
            !Mathf.Approximately(bonuses.SafeRandomDamageMaxMultiplier, 1f))
        {
            float min = Mathf.Min(bonuses.SafeRandomDamageMinMultiplier, bonuses.SafeRandomDamageMaxMultiplier);
            float max = Mathf.Max(bonuses.SafeRandomDamageMinMultiplier, bonuses.SafeRandomDamageMaxMultiplier);
            damage *= UnityEngine.Random.Range(min, max);
        }

        if (bonuses.criticalChance > 0f && UnityEngine.Random.value < Mathf.Clamp01(bonuses.criticalChance))
            damage *= bonuses.SafeCriticalDamageMultiplier;

        return Mathf.Max(0f, damage);
    }

    public static string BuildStarterItemsSummary(ProfessionDefinition definition)
    {
        if (definition == null)
            return ProfessionLocalization.NoData;

        StringBuilder builder = new();
        bool hasAnyLine = false;

        for (int i = 0; i < definition.starterItems.Count; i++)
        {
            ProfessionStarterItem starterItem = definition.starterItems[i];
            if (starterItem == null || starterItem.itemPrefab == null)
                continue;

            int amount = Mathf.Max(1, starterItem.amount);
            string itemName = ResolveItemDisplayName(starterItem.itemPrefab);
            builder.AppendLine($"{itemName} x{amount}");
            hasAnyLine = true;
        }

        if (definition.startCoinsBonus > 0)
        {
            builder.AppendLine(ProfessionLocalization.FormatCoinsBonus(definition.startCoinsBonus));
            hasAnyLine = true;
        }

        if (definition.startGemsBonus > 0)
        {
            builder.AppendLine(ProfessionLocalization.FormatGemsBonus(definition.startGemsBonus));
            hasAnyLine = true;
        }

        if (!hasAnyLine)
            return ProfessionLocalization.NoStarterItems;

        return builder.ToString().TrimEnd();
    }

    public static string BuildPerksSummary(ProfessionDefinition definition)
    {
        if (definition == null)
            return ProfessionLocalization.NoSpecialAbilities;

        List<string> positiveLines = new();
        List<string> negativeLines = new();
        BuildPassivePerkLines(definition.passiveBonuses, positiveLines, negativeLines);

        if (positiveLines.Count == 0 && negativeLines.Count == 0 && definition.perkLines != null)
        {
            for (int i = 0; i < definition.perkLines.Count; i++)
            {
                string perkLine = definition.perkLines[i];
                if (string.IsNullOrWhiteSpace(perkLine))
                    continue;

                positiveLines.Add(ProfessionLocalization.DefinitionPerk(definition, i, perkLine.Trim()));
            }
        }

        if (positiveLines.Count == 0 && negativeLines.Count == 0)
            return ProfessionLocalization.NoSpecialAbilities;

        return BuildColoredPerkSummary(positiveLines, negativeLines);
    }

    private static void BuildPassivePerkLines(ProfessionPassiveBonuses bonuses, List<string> positiveLines, List<string> negativeLines)
    {
        if (bonuses == null || positiveLines == null || negativeLines == null)
            return;

        AppendFlatLine(positiveLines, negativeLines, ProfessionLocalization.PassiveMoveSpeedFlatLabel, bonuses.moveSpeedFlat);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveMoveSpeedMultLabel, bonuses.SafeMoveSpeedMultiplier);
        AppendFlatLine(positiveLines, negativeLines, ProfessionLocalization.PassiveMaxHealthFlatLabel, bonuses.maxHealthFlat);
        AppendFlatLine(positiveLines, negativeLines, ProfessionLocalization.PassiveMaxStaminaFlatLabel, bonuses.maxStaminaFlat);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveStaminaConsumptionMultLabel, bonuses.SafeStaminaConsumptionMultiplier, false);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveStaminaRestoreMultLabel, bonuses.SafeStaminaRestoreMultiplier);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveIncomingDamageMultLabel, bonuses.SafeIncomingDamageMultiplier, false);
        AppendPercentLine(positiveLines, ProfessionLocalization.PassiveDodgeChanceLabel, bonuses.dodgeChance);
        AppendFlatLine(positiveLines, negativeLines, ProfessionLocalization.PassiveHealOnKillFlatLabel, bonuses.healOnKillFlat);
        AppendHealthDrainLine(negativeLines, ProfessionLocalization.PassiveHealthDrainPerSecondLabel, bonuses.healthDrainPerSecond);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveExperienceMultLabel, bonuses.SafeExperienceMultiplier);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveSaleRewardMultLabel, bonuses.SafeSaleRewardMultiplier);
        AppendFlatLine(positiveLines, negativeLines, ProfessionLocalization.PassiveMaxFuelFlatLabel, bonuses.maxFuelFlat);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveFuelConsumptionMultLabel, bonuses.SafeFuelConsumptionMultiplier, false);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveFuelFillMultLabel, bonuses.SafeFuelFillMultiplier);
        AppendFlatLine(positiveLines, negativeLines, ProfessionLocalization.PassiveBoatSpeedFlatLabel, bonuses.boatSpeedFlat);
        AppendFlatLine(positiveLines, negativeLines, ProfessionLocalization.PassiveMeleeDamageFlatLabel, bonuses.meleeDamageFlat);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveMeleeAttackSpeedMultLabel, bonuses.SafeMeleeAttackSpeedMultiplier);
        AppendFlatLine(positiveLines, negativeLines, ProfessionLocalization.PassiveRangedDamageFlatLabel, bonuses.rangedDamageFlat);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveRangedAttackSpeedMultLabel, bonuses.SafeRangedAttackSpeedMultiplier);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveRangedReloadMultLabel, bonuses.SafeRangedReloadSpeedMultiplier);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveOutgoingDamageMultLabel, bonuses.SafeOutgoingDamageMultiplier);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveLowHealthDamageMultLabel, bonuses.SafeLowHealthDamageMultiplier);
        AppendPercentLine(positiveLines, ProfessionLocalization.PassiveCriticalChanceLabel, bonuses.criticalChance);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveCriticalDamageMultLabel, bonuses.SafeCriticalDamageMultiplier);
        AppendDamageRangeLine(positiveLines, negativeLines, ProfessionLocalization.PassiveRandomDamageRangeLabel, bonuses.SafeRandomDamageMinMultiplier, bonuses.SafeRandomDamageMaxMultiplier);
        AppendBurnLine(positiveLines, bonuses);
        AppendMultiplierLine(positiveLines, negativeLines, ProfessionLocalization.PassiveRareLootChanceMultLabel, bonuses.SafeRareLootChanceMultiplier);
    }

    private static string BuildColoredPerkSummary(List<string> positiveLines, List<string> negativeLines)
    {
        StringBuilder builder = new();
        AppendColoredLines(builder, positiveLines, "#5DFF83");

        if (positiveLines.Count > 0 && negativeLines.Count > 0)
        {
            if (builder.Length > 0)
                builder.AppendLine();
            builder.Append("<color=#89909C>----------</color>");
        }

        AppendColoredLines(builder, negativeLines, "#FF6767");
        return builder.ToString();
    }

    private static void AppendColoredLines(StringBuilder builder, List<string> lines, string color)
    {
        if (lines == null)
            return;

        for (int i = 0; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            if (builder.Length > 0)
                builder.AppendLine();

            builder.Append("<color=");
            builder.Append(color);
            builder.Append(">- ");
            builder.Append(lines[i]);
            builder.Append("</color>");
        }
    }

    private static void AppendFlatLine(List<string> positiveLines, List<string> negativeLines, string label, float value, bool higherIsPositive = true)
    {
        if (Mathf.Approximately(value, 0f))
            return;

        List<string> target = IsPositiveDelta(value, higherIsPositive) ? positiveLines : negativeLines;
        string sign = value > 0f ? "+" : string.Empty;
        target.Add($"{label}: {sign}{Mathf.RoundToInt(value)}");
    }

    private static void AppendMultiplierLine(List<string> positiveLines, List<string> negativeLines, string label, float multiplier, bool higherIsPositive = true)
    {
        int deltaPercent = Mathf.RoundToInt((multiplier - 1f) * 100f);
        if (deltaPercent == 0)
            return;

        List<string> target = IsPositiveDelta(deltaPercent, higherIsPositive) ? positiveLines : negativeLines;
        string sign = deltaPercent > 0 ? "+" : string.Empty;
        target.Add($"{label}: {sign}{deltaPercent}%");
    }

    private static void AppendPercentLine(List<string> positiveLines, string label, float value)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        if (percent == 0)
            return;

        positiveLines.Add($"{label}: +{percent}%");
    }

    private static void AppendHealthDrainLine(List<string> negativeLines, string label, float value)
    {
        if (value <= 0f)
            return;

        string formatted = value.ToString("0.#", CultureInfo.InvariantCulture);
        negativeLines.Add($"{label}: -{formatted}/s");
    }

    private static void AppendDamageRangeLine(List<string> positiveLines, List<string> negativeLines, string label, float minMultiplier, float maxMultiplier)
    {
        if (Mathf.Approximately(minMultiplier, 1f) && Mathf.Approximately(maxMultiplier, 1f))
            return;

        int min = Mathf.RoundToInt(Mathf.Min(minMultiplier, maxMultiplier) * 100f);
        int max = Mathf.RoundToInt(Mathf.Max(minMultiplier, maxMultiplier) * 100f);
        List<string> target = minMultiplier >= 1f && maxMultiplier >= 1f ? positiveLines : negativeLines;
        target.Add($"{label}: {min}-{max}%");
    }

    private static void AppendBurnLine(List<string> positiveLines, ProfessionPassiveBonuses bonuses)
    {
        if (bonuses == null || bonuses.burnDamagePerSecond <= 0f || bonuses.burnDuration <= 0f)
            return;

        string damage = bonuses.burnDamagePerSecond.ToString("0.#", CultureInfo.InvariantCulture);
        string duration = bonuses.burnDuration.ToString("0.#", CultureInfo.InvariantCulture);
        int chance = Mathf.RoundToInt((bonuses.burnChance <= 0f ? 1f : Mathf.Clamp01(bonuses.burnChance)) * 100f);
        positiveLines.Add($"{ProfessionLocalization.PassiveBurnLabel}: {damage}/s, {duration}s, {chance}%");
    }

    private static bool IsPositiveDelta(float delta, bool higherIsPositive)
    {
        return higherIsPositive ? delta > 0f : delta < 0f;
    }

    private static string ResolveItemDisplayName(PickableItem itemPrefab)
    {
        if (itemPrefab == null)
            return ProfessionLocalization.UnknownItem;

        string fallbackName = itemPrefab.Data != null
            ? itemPrefab.Data.Name
            : itemPrefab.name;

        LocalizationManager localizationManager = LocalizationManager.Instance;
        if (localizationManager == null || localizationManager.LocalizationData == null)
            return fallbackName;

        return localizationManager.LocalizationData.GetTranslation(
            fallbackName,
            localizationManager.CurrentLanguage,
            LocalizationKeyType.Item.ToString());
    }

    private static void AppendProfessionStarterItems(List<PickableItem> result, ProfessionDefinition profession)
    {
        if (profession == null || profession.starterItems == null)
            return;

        for (int i = 0; i < profession.starterItems.Count; i++)
        {
            ProfessionStarterItem starterItem = profession.starterItems[i];
            if (starterItem == null || starterItem.itemPrefab == null)
                continue;

            int amount = Mathf.Max(1, starterItem.amount);
            for (int j = 0; j < amount; j++)
                result.Add(starterItem.itemPrefab);
        }
    }

    private static bool IsRandomUnlockCandidate(ProfessionDefinition definition)
    {
        return definition != null &&
               definition.availableInRandomUnlockPool &&
               definition.randomUnlockWeight > 0;
    }

    private static ProfessionDefinition RollRandomProfession(List<ProfessionDefinition> definitions)
    {
        int totalWeight = 0;
        for (int i = 0; i < definitions.Count; i++)
            totalWeight += Mathf.Max(1, definitions[i].randomUnlockWeight);

        int roll = UnityEngine.Random.Range(0, Mathf.Max(1, totalWeight));
        int cumulative = 0;
        for (int i = 0; i < definitions.Count; i++)
        {
            cumulative += Mathf.Max(1, definitions[i].randomUnlockWeight);
            if (roll < cumulative)
                return definitions[i];
        }

        return definitions[UnityEngine.Random.Range(0, definitions.Count)];
    }

    private static void UnlockProfessionInternal(string professionId, bool purchased)
    {
        if (string.IsNullOrWhiteSpace(professionId))
            return;

        if (!_state.unlockedProfessionIds.Contains(professionId))
            _state.unlockedProfessionIds.Add(professionId);

        if (purchased && !_state.purchasedProfessionIds.Contains(professionId))
            _state.purchasedProfessionIds.Add(professionId);
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        BuildDefinitions();
        string fallbackDefaultProfessionId = ResolveDefaultProfessionId();
        _state = ProfessionStorage.Load(_definitions, fallbackDefaultProfessionId);
        _initialized = true;
    }

    private static void BuildDefinitions()
    {
        _definitions.Clear();
        _definitionsById.Clear();

        ProfessionCatalog activeCatalog = _configuredCatalog != null
            ? _configuredCatalog
            : Resources.Load<ProfessionCatalog>(DefaultCatalogResourcePath);

        if (activeCatalog != null)
        {
            List<ProfessionDefinition> validDefinitions = activeCatalog.GetValidDefinitions();
            for (int i = 0; i < validDefinitions.Count; i++)
            {
                ProfessionDefinition definition = validDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.professionId))
                    continue;

                if (_definitionsById.ContainsKey(definition.professionId))
                    continue;

                _definitionsById.Add(definition.professionId, definition);
                _definitions.Add(definition);
            }
        }

        if (_definitions.Count > 0)
            return;

        ProfessionDefinition fallback = CreateFallbackProfession();
        _definitionsById.Add(fallback.professionId, fallback);
        _definitions.Add(fallback);
    }

    private static ProfessionDefinition CreateFallbackProfession()
    {
        return new ProfessionDefinition
        {
            professionId = FallbackProfessionId,
            title = ProfessionLocalization.DefaultProfessionTitle,
            description = ProfessionLocalization.DefaultProfessionDescription,
            defaultUnlocked = true,
            availableInRandomUnlockPool = false,
            starterItems = new List<ProfessionStarterItem>(),
            perkLines = new List<string>(),
            passiveBonuses = new ProfessionPassiveBonuses()
        };
    }

    private static string ResolveDefaultProfessionId()
    {
        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (definition.defaultUnlocked)
                return definition.professionId;
        }

        return _definitions.Count > 0
            ? _definitions[0].professionId
            : FallbackProfessionId;
    }

    private static void SaveState()
    {
        string fallbackDefaultProfessionId = ResolveDefaultProfessionId();
        ProfessionStorage.Save(_state, _definitions, fallbackDefaultProfessionId);
        StateChanged?.Invoke();
    }
}
