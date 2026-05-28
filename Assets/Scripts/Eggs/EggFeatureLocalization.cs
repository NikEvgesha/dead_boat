using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class EggFeatureLocalization
{
    public static string Text(string key, string fallbackRu, string fallbackEn)
    {
        string language = GetCurrentLanguage();
        string fallback = IsEnglish(language) ? fallbackEn : fallbackRu;

        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null || manager.LocalizationData == null)
            return fallback;

        return manager.LocalizationData.TryGetTranslation(key, language, out string translated) &&
               !string.IsNullOrWhiteSpace(translated) &&
               !string.Equals(translated, key, StringComparison.Ordinal)
            ? translated
            : fallback;
    }

    public static string Format(string key, string fallbackRu, string fallbackEn, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, Text(key, fallbackRu, fallbackEn), args);
    }

    public static string AnimalTitle(AnimalDefinition definition)
    {
        if (definition == null)
            return string.Empty;

        return AnimalTitle(definition.animalId, string.IsNullOrWhiteSpace(definition.title) ? definition.animalId : definition.title);
    }

    public static string EggTitle(EggDefinition definition)
    {
        if (definition == null)
            return string.Empty;

        string fallback = string.IsNullOrWhiteSpace(definition.title) ? definition.eggId : definition.title;
        return Text(fallback, fallback, fallback);
    }

    public static string AnimalTitle(string animalId, string fallback)
    {
        if (string.IsNullOrWhiteSpace(animalId))
            return fallback ?? string.Empty;

        return Text($"Animals/{animalId}", fallback, fallback);
    }

    public static string StageShort(int stage)
    {
        return $"{Text("Eggs/AnimalStageShort", "Уровень ", "Level ")}{Mathf.Max(1, stage)}";
    }

    public static string StageLong(int stage)
    {
        return $"{Text("Eggs/AnimalStage", "Уровень", "Level")} {Mathf.Max(1, stage)}";
    }

    public static string FormatAnimalBuffSummary(AnimalRunBuffs buffs)
    {
        if (buffs == null)
            return Text("UI/AnimalBuff/None", "Нет бонусов", "No buffs");

        List<string> parts = new();
        AppendFlat(parts, "UI/AnimalBuff/HP", "Здоровье", "HP", buffs.maxHealthFlat);
        AppendFlat(parts, "UI/AnimalBuff/Speed", "Скорость", "Speed", buffs.moveSpeedFlat);
        AppendMultiplier(parts, "UI/AnimalBuff/Move", "Движение", "Move", buffs.SafeMoveSpeedMultiplier);
        AppendMultiplier(parts, "UI/AnimalBuff/XP", "Опыт", "XP", buffs.SafeExperienceMultiplier);
        AppendMultiplier(parts, "UI/AnimalBuff/Sale", "Продажа", "Sale", buffs.SafeSaleRewardMultiplier);
        AppendFlat(parts, "UI/AnimalBuff/Fuel", "Топливо", "Fuel", buffs.maxFuelFlat);
        AppendMultiplier(parts, "UI/AnimalBuff/FuelSave", "Экономия топлива", "Fuel save", buffs.SafeFuelConsumptionMultiplier, invertSign: true);
        AppendMultiplier(parts, "UI/AnimalBuff/FuelFill", "Заправка", "Fuel fill", buffs.SafeFuelFillMultiplier);
        AppendFlat(parts, "UI/AnimalBuff/Boat", "Корабль", "Boat", buffs.boatSpeedFlat);
        AppendFlat(parts, "UI/AnimalBuff/Melee", "Ближний бой", "Melee", buffs.meleeDamageFlat);
        AppendMultiplier(parts, "UI/AnimalBuff/MeleeSpeed", "Скор. ближ. боя", "Melee spd", buffs.SafeMeleeAttackSpeedMultiplier);
        AppendFlat(parts, "UI/AnimalBuff/Ranged", "Дальний бой", "Ranged", buffs.rangedDamageFlat);
        AppendMultiplier(parts, "UI/AnimalBuff/RangedSpeed", "Скорострельность", "Range spd", buffs.SafeRangedAttackSpeedMultiplier);
        AppendMultiplier(parts, "UI/AnimalBuff/ReloadSpeed", "Перезарядка", "Reload spd", buffs.SafeRangedReloadSpeedMultiplier);

        return parts.Count == 0 ? Text("UI/AnimalBuff/None", "Нет бонусов", "No buffs") : string.Join(", ", parts);
    }

    private static void AppendFlat(List<string> parts, string key, string ru, string en, float value)
    {
        if (Mathf.Approximately(value, 0f))
            return;

        string sign = value > 0f ? "+" : string.Empty;
        float absolute = Mathf.Abs(value);
        string formatted = absolute >= 10f || Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.#", CultureInfo.InvariantCulture);
        parts.Add($"{Text(key, ru, en)} {sign}{formatted}");
    }

    private static void AppendMultiplier(List<string> parts, string key, string ru, string en, float multiplier, bool invertSign = false)
    {
        int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
        if (percent == 0)
            return;

        if (invertSign)
            percent *= -1;

        string sign = percent > 0 ? "+" : string.Empty;
        parts.Add($"{Text(key, ru, en)} {sign}{percent}%");
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
