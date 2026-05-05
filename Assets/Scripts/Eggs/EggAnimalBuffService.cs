using UnityEngine;

public static class EggAnimalBuffService
{
    private const string DefaultCatalogResourcePath = "Eggs/EggHatchingCatalog";

    private static EggHatchingCatalog _catalog;
    private static AnimalRunBuffs _cachedBuffs;
    private static bool _dirty = true;

    public static void MarkDirty()
    {
        _dirty = true;
    }

    public static float ApplyMoveSpeed(float baseValue)
    {
        AnimalRunBuffs buffs = GetCurrentBuffs();
        return (baseValue + buffs.moveSpeedFlat) * buffs.SafeMoveSpeedMultiplier;
    }

    public static float ApplyMaxHealth(float baseValue)
    {
        return baseValue + GetCurrentBuffs().maxHealthFlat;
    }

    public static int ApplyExperienceGain(int baseValue)
    {
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * GetCurrentBuffs().SafeExperienceMultiplier));
    }

    public static int ApplySaleReward(int baseValue)
    {
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * GetCurrentBuffs().SafeSaleRewardMultiplier));
    }

    public static float ApplyMaxFuel(float baseValue)
    {
        return baseValue + GetCurrentBuffs().maxFuelFlat;
    }

    public static float ApplyFuelConsumption(float baseValue)
    {
        return baseValue * GetCurrentBuffs().SafeFuelConsumptionMultiplier;
    }

    public static float ApplyFuelFill(float baseValue)
    {
        return baseValue * GetCurrentBuffs().SafeFuelFillMultiplier;
    }

    public static float ApplyBoatMaxSpeed(float baseValue)
    {
        return baseValue + GetCurrentBuffs().boatSpeedFlat;
    }

    public static float ApplyMeleeDamage(float baseValue)
    {
        return baseValue + GetCurrentBuffs().meleeDamageFlat;
    }

    public static float ApplyMeleeAttackSpeed(float baseValue)
    {
        return baseValue * GetCurrentBuffs().SafeMeleeAttackSpeedMultiplier;
    }

    public static float ApplyRangedDamage(float baseValue)
    {
        return baseValue + GetCurrentBuffs().rangedDamageFlat;
    }

    public static float ApplyRangedAttackSpeed(float baseValue)
    {
        return baseValue * GetCurrentBuffs().SafeRangedAttackSpeedMultiplier;
    }

    public static float ApplyRangedReloadSpeed(float baseValue)
    {
        return baseValue * GetCurrentBuffs().SafeRangedReloadSpeedMultiplier;
    }

    private static AnimalRunBuffs GetCurrentBuffs()
    {
        if (!_dirty && _cachedBuffs != null)
            return _cachedBuffs;

        EnsureCatalog();
        EggFeatureState state = EggFeatureStorage.Load();

        AnimalRunBuffs result = new();
        if (state?.placedAnimals != null && _catalog != null)
        {
            for (int i = 0; i < state.placedAnimals.Count; i++)
            {
                PlacedAnimalState placed = state.placedAnimals[i];
                if (placed == null)
                    continue;

                AnimalRunBuffs buffs = _catalog.GetAnimalBuffs(placed.EffectiveAnimalId, placed.EffectiveStage);
                result.Add(buffs);
            }
        }

        _cachedBuffs = result;
        _dirty = false;
        return _cachedBuffs;
    }

    private static void EnsureCatalog()
    {
        if (_catalog != null)
            return;

        _catalog = Resources.Load<EggHatchingCatalog>(DefaultCatalogResourcePath);
    }
}
