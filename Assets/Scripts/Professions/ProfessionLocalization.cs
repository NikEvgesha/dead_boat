using System;

public static class ProfessionLocalization
{
    public static string NoData => Translate("UI/Profession/NoData", "Нет данных.", "No data.");
    public static string NoDescription => Translate("UI/Profession/NoDescription", "Нет описания.", "No description.");
    public static string StatusEquipped => Translate("UI/Profession/StatusEquipped", "Экипировано", "Equipped");
    public static string StatusOpened => Translate("UI/Profession/StatusOpened", "Открыто", "Unlocked");
    public static string StatusLocked => Translate("UI/Profession/StatusLocked", "Заблокировано", "Locked");
    public static string MessageAllUnlocked => Translate("UI/Profession/MessageAllUnlocked", "Все профессии уже открыты.", "All professions are already unlocked.");
    public static string MessageUnlockFailed => Translate("UI/Profession/MessageUnlockFailed", "Не удалось открыть профессию.", "Failed to unlock profession.");
    public static string NoStarterItems => Translate("UI/Profession/NoStarterItems", "Нет стартовых предметов или бонусов.", "No starter items or bonuses.");
    public static string NoSpecialAbilities => Translate("UI/Profession/NoSpecialAbilities", "Нет специальных способностей.", "No special abilities.");
    public static string UnknownItem => Translate("UI/Profession/UnknownItem", "Неизвестный предмет", "Unknown item");
    public static string DefaultProfessionTitle => Translate("UI/Profession/DefaultTitle", "Юнга", "Deckhand");
    public static string DefaultProfessionDescription => Translate("UI/Profession/DefaultDescription", "Базовая профессия без дополнительных бонусов.", "Base profession without extra bonuses.");

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

    private static string Translate(string key, string fallbackRu, string fallbackEn)
    {
        string language = GetCurrentLanguage();
        bool isEnglish = IsEnglish(language);
        string fallback = isEnglish ? fallbackEn : fallbackRu;

        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null || manager.LocalizationData == null)
            return fallback;

        string translated = manager.LocalizationData.GetTranslation(key, language);
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
