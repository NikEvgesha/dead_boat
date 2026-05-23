using System;

public static class ProfessionLocalization
{
    public static string NoProfessionTitle => Translate("UI/Profession/NoneTitle", "Без профессии", "No profession");
    public static string NoProfessionDescription => Translate("UI/Profession/NoneDescription", "Начать забег без стартовых бонусов профессии.", "Start the run without profession bonuses.");
    public static string NoData => Translate("UI/Profession/NoData", "Нет данных.", "No data.");
    public static string NoDescription => Translate("UI/Profession/NoDescription", "Нет описания.", "No description.");
    public static string StatusEquipped => Translate("UI/Profession/StatusEquipped", "Выбрано", "Equipped");
    public static string StatusOpened => Translate("UI/Profession/StatusOpened", "Открыто", "Unlocked");
    public static string StatusLocked => Translate("UI/Profession/StatusLocked", "Закрыто", "Locked");
    public static string StatusNoProfession => Translate("UI/Profession/StatusNoProfession", "Без профессии", "No profession");
    public static string MessageAllUnlocked => Translate("UI/Profession/MessageAllUnlocked", "Все профессии уже открыты.", "All professions are already unlocked.");
    public static string MessageUnlockFailed => Translate("UI/Profession/MessageUnlockFailed", "Не удалось открыть профессию.", "Failed to unlock profession.");
    public static string MessagePurchaseUnavailable => Translate("UI/Profession/MessagePurchaseUnavailable", "Покупка недоступна на этой платформе.", "Purchase is unavailable on this platform.");
    public static string NoStarterItems => Translate("UI/Profession/NoStarterItems", "Нет стартовых предметов или бонусов.", "No starter items or bonuses.");
    public static string NoSpecialAbilities => Translate("UI/Profession/NoSpecialAbilities", "Нет специальных способностей.", "No special abilities.");
    public static string UnknownItem => Translate("UI/Profession/UnknownItem", "Неизвестный предмет", "Unknown item");
    public static string DefaultProfessionTitle => Translate("UI/Profession/DefaultTitle", "Юнга", "Deckhand");
    public static string DefaultProfessionDescription => Translate("UI/Profession/DefaultDescription", "Базовая профессия без дополнительных бонусов.", "Base profession without extra bonuses.");
    public static string LockLabel => Translate("UI/Profession/LockLabel", "ЗАКРЫТО", "LOCKED");
    public static string ApplyButton => Translate("UI/Profession/Button/Apply", "Выбрать", "Apply");
    public static string RandomButton => Translate("UI/Profession/Button/Random", "\u0421\u043b\u0443\u0447\u0430\u0439\u043d\u0430\u044f", "Random");
    public static string BuyButton => Translate("UI/Profession/Button/Buy", "Купить", "Buy");
    public static string OpenButton => Translate("UI/Profession/Button/Open", "Профессии", "Professions");
    public static string ClassHeader => Translate("UI/Profession/Header/Class", "Класс", "Class");
    public static string AbilitiesHeader => Translate("UI/Profession/Header/Abilities", "Умения", "Abilities");
    public static string StarterItemsHeader => Translate("UI/Profession/Header/StarterItems", "Начальные предметы", "Starter items");

    public static string PassiveMoveSpeedFlatLabel => Translate("UI/Profession/Passive/MoveSpeedFlat", "Скорость", "Move speed");
    public static string PassiveMoveSpeedMultLabel => Translate("UI/Profession/Passive/MoveSpeedMult", "Множитель скорости", "Move speed multiplier");
    public static string PassiveMaxHealthFlatLabel => Translate("UI/Profession/Passive/MaxHealthFlat", "Макс. здоровье", "Max health");
    public static string PassiveMaxStaminaFlatLabel => Translate("UI/Profession/Passive/MaxStaminaFlat", "Макс. стамина", "Max stamina");
    public static string PassiveStaminaConsumptionMultLabel => Translate("UI/Profession/Passive/StaminaConsumptionMult", "Расход стамины", "Stamina use");
    public static string PassiveStaminaRestoreMultLabel => Translate("UI/Profession/Passive/StaminaRestoreMult", "Восстановление стамины", "Stamina restore");
    public static string PassiveIncomingDamageMultLabel => Translate("UI/Profession/Passive/IncomingDamageMult", "Входящий урон", "Incoming damage");
    public static string PassiveDodgeChanceLabel => Translate("UI/Profession/Passive/DodgeChance", "Уклонение", "Dodge chance");
    public static string PassiveHealOnKillFlatLabel => Translate("UI/Profession/Passive/HealOnKillFlat", "Лечение за убийство", "Heal on kill");
    public static string PassiveHealthDrainPerSecondLabel => Translate("UI/Profession/Passive/HealthDrainPerSecond", "Потеря здоровья", "Health drain");
    public static string PassiveExperienceMultLabel => Translate("UI/Profession/Passive/ExperienceMult", "Опыт", "Experience");
    public static string PassiveSaleRewardMultLabel => Translate("UI/Profession/Passive/SaleRewardMult", "Продажа", "Sell rewards");
    public static string PassiveMaxFuelFlatLabel => Translate("UI/Profession/Passive/MaxFuelFlat", "Макс. топливо", "Max fuel");
    public static string PassiveFuelConsumptionMultLabel => Translate("UI/Profession/Passive/FuelConsumptionMult", "Расход топлива", "Fuel consumption");
    public static string PassiveFuelFillMultLabel => Translate("UI/Profession/Passive/FuelFillMult", "Заправка", "Fuel fill efficiency");
    public static string PassiveBoatSpeedFlatLabel => Translate("UI/Profession/Passive/BoatSpeedFlat", "Скорость корабля", "Boat speed");
    public static string PassiveMeleeDamageFlatLabel => Translate("UI/Profession/Passive/MeleeDamageFlat", "Урон ближнего боя", "Melee damage");
    public static string PassiveMeleeAttackSpeedMultLabel => Translate("UI/Profession/Passive/MeleeAttackSpeedMult", "Скорость ближнего боя", "Melee attack speed");
    public static string PassiveRangedDamageFlatLabel => Translate("UI/Profession/Passive/RangedDamageFlat", "Дальний урон", "Ranged damage");
    public static string PassiveRangedAttackSpeedMultLabel => Translate("UI/Profession/Passive/RangedAttackSpeedMult", "Скорострельность", "Ranged attack speed");
    public static string PassiveRangedReloadMultLabel => Translate("UI/Profession/Passive/RangedReloadMult", "Перезарядка", "Reload speed");
    public static string PassiveOutgoingDamageMultLabel => Translate("UI/Profession/Passive/OutgoingDamageMult", "Урон", "Damage");
    public static string PassiveLowHealthDamageMultLabel => Translate("UI/Profession/Passive/LowHealthDamageMult", "Урон при низком HP", "Low HP damage");
    public static string PassiveCriticalChanceLabel => Translate("UI/Profession/Passive/CriticalChance", "Шанс крита", "Critical chance");
    public static string PassiveCriticalDamageMultLabel => Translate("UI/Profession/Passive/CriticalDamageMult", "Критический урон", "Critical damage");
    public static string PassiveRandomDamageRangeLabel => Translate("UI/Profession/Passive/RandomDamageRange", "Нестабильный урон", "Unstable damage");
    public static string PassiveBurnLabel => Translate("UI/Profession/Passive/Burn", "Поджог", "Ignite");
    public static string PassiveRareLootChanceMultLabel => Translate("UI/Profession/Passive/RareLootChanceMult", "Шанс редкого лута", "Rare loot chance");

    public static string DefinitionTitle(ProfessionDefinition definition)
    {
        if (definition == null)
            return NoProfessionTitle;

        string fallback = string.IsNullOrWhiteSpace(definition.title) ? definition.professionId : definition.title;
        return Translate($"UI/Profession/Definition/{definition.professionId}/Title", fallback, fallback);
    }

    public static string DefinitionDescription(ProfessionDefinition definition)
    {
        if (definition == null)
            return NoProfessionDescription;

        string fallback = string.IsNullOrWhiteSpace(definition.description) ? NoDescription : definition.description;
        return Translate($"UI/Profession/Definition/{definition.professionId}/Description", fallback, fallback);
    }

    public static string DefinitionPerk(ProfessionDefinition definition, int index, string fallback)
    {
        if (definition == null)
            return fallback;

        return Translate($"UI/Profession/Definition/{definition.professionId}/Perk{index + 1}", fallback, fallback);
    }

    public static string FormatSelectedProfession(string title)
    {
        string format = Translate("UI/Profession/SelectedFormat", "Выбрана профессия: {0}", "Selected profession: {0}");
        return string.Format(format, title ?? string.Empty);
    }

    public static string FormatUnlockedProfession(string title)
    {
        string format = Translate("UI/Profession/UnlockedFormat", "Открыта профессия: {0}", "Unlocked profession: {0}");
        return string.Format(format, title ?? string.Empty);
    }

    public static string FormatCoinsBonus(int amount)
    {
        string format = Translate("UI/Profession/CoinsBonusFormat", "Монеты +{0}", "Coins +{0}");
        return string.Format(format, amount);
    }

    public static string FormatGemsBonus(int amount)
    {
        string format = Translate("UI/Profession/GemsBonusFormat", "Гемы +{0}", "Gems +{0}");
        return string.Format(format, amount);
    }

    public static string FormatSoftPrice(int amount, CurrencyType currencyType)
    {
        string currency = currencyType == CurrencyType.Gems
            ? Translate("UI/Currency/Gems", "гемы", "gems")
            : Translate("UI/Currency/Coins", "монеты", "coins");
        string format = Translate("UI/Profession/SoftPriceFormat", "{0} {1}", "{0} {1}");
        return string.Format(format, amount, currency);
    }

    private static string Translate(string key, string fallbackRu, string fallbackEn)
    {
        string language = GetCurrentLanguage();
        bool isEnglish = IsEnglish(language);
        string fallback = isEnglish ? fallbackEn : fallbackRu;

        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null || manager.LocalizationData == null)
            return fallback;

        if (!manager.LocalizationData.TryGetTranslation(key, language, out string translated))
            return fallback;

        if (string.IsNullOrWhiteSpace(translated) || string.Equals(translated, key, StringComparison.Ordinal))
            return fallback;

        return translated;
    }

    private static string GetCurrentLanguage()
    {
        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null || string.IsNullOrWhiteSpace(manager.CurrentLanguage))
            return "Ru";

        return manager.CurrentLanguage;
    }

    private static bool IsEnglish(string language)
    {
        return !string.IsNullOrWhiteSpace(language) &&
               language.StartsWith("En", StringComparison.OrdinalIgnoreCase);
    }
}
