using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;

public static class ProfessionService
{
    private const string DefaultCatalogResourcePath = "Professions/ProfessionCatalog";
    private const string FallbackProfessionId = "yunga";

    private static ProfessionCatalog _configuredCatalog;
    private static readonly List<ProfessionDefinition> _definitions = new();
    private static readonly Dictionary<string, ProfessionDefinition> _definitionsById = new();
    private static ProfessionState _state;
    private static bool _initialized;

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

    public static bool TrySelectProfession(string professionId)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(professionId))
            return false;

        if (!IsUnlocked(professionId))
            return false;

        if (_state.selectedProfessionId == professionId)
            return true;

        _state.selectedProfessionId = professionId;
        SaveState();
        return true;
    }

    public static bool TryUnlockRandomLockedProfession(int unlockPriceGems, out ProfessionDefinition unlockedDefinition)
    {
        EnsureInitialized();
        unlockedDefinition = null;

        List<ProfessionDefinition> lockedDefinitions = new();
        for (int i = 0; i < _definitions.Count; i++)
        {
            ProfessionDefinition definition = _definitions[i];
            if (!IsUnlocked(definition.professionId))
                lockedDefinitions.Add(definition);
        }

        if (lockedDefinitions.Count == 0)
            return false;

        int clampedPrice = Mathf.Max(0, unlockPriceGems);
        if (clampedPrice > 0)
        {
            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager == null)
                return false;

            if (!currencyManager.CheckEnoughCurrency(CurrencyType.Gems, clampedPrice))
                return false;

            if (!currencyManager.RemoveCurrency(CurrencyType.Gems, clampedPrice))
                return false;
        }

        int randomIndex = UnityEngine.Random.Range(0, lockedDefinitions.Count);
        unlockedDefinition = lockedDefinitions[randomIndex];
        _state.unlockedProfessionIds.Add(unlockedDefinition.professionId);
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

        if (includeProfessionStarterItems)
        {
            ProfessionDefinition profession = GetCurrentProfession();
            AppendProfessionStarterItems(result, profession);
        }

        return result.AsReadOnly();
    }

    public static bool ApplyCurrentProfessionStartBonuses()
    {
        EnsureInitialized();

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

    public static string BuildStarterItemsSummary(ProfessionDefinition definition)
    {
        if (definition == null)
            return "Нет данных.";

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
            builder.AppendLine($"Монеты +{definition.startCoinsBonus}");
            hasAnyLine = true;
        }

        if (definition.startGemsBonus > 0)
        {
            builder.AppendLine($"Гемы +{definition.startGemsBonus}");
            hasAnyLine = true;
        }

        if (!hasAnyLine)
            return "Нет стартовых предметов или бонусов.";

        return builder.ToString().TrimEnd();
    }

    public static string BuildPerksSummary(ProfessionDefinition definition)
    {
        if (definition == null || definition.perkLines == null || definition.perkLines.Count == 0)
            return "Нет специальных способностей.";

        StringBuilder builder = new();
        for (int i = 0; i < definition.perkLines.Count; i++)
        {
            string perkLine = definition.perkLines[i];
            if (string.IsNullOrWhiteSpace(perkLine))
                continue;

            if (builder.Length > 0)
                builder.AppendLine();

            builder.Append("• ");
            builder.Append(perkLine.Trim());
        }

        if (builder.Length == 0)
            return "Нет специальных способностей.";

        return builder.ToString();
    }

    private static string ResolveItemDisplayName(PickableItem itemPrefab)
    {
        if (itemPrefab == null)
            return "Неизвестный предмет";

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
            title = "Юнга",
            description = "Базовая профессия без дополнительных бонусов.",
            defaultUnlocked = true,
            starterItems = new List<ProfessionStarterItem>(),
            perkLines = new List<string>()
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
