using UnityEngine;

public static class EggAnimalBuffService
{
    private const string DefaultCatalogResourcePath = "Eggs/EggHatchingCatalog";

    private static EggHatchingCatalog _catalog;
    private static AnimalRunBuffs _cachedBuffs;
    private static bool _dirty = true;
    private static Object _temporaryOwner;
    private static AnimalRunBuffs _temporaryBuffs;

    // A lesson companion participates in the normal stat formulas without changing
    // the player's permanent animal collection or equipped loadout.
    public static void SetTemporaryCompanion(Object owner, AnimalRunBuffs buffs)
    {
        if (owner == null) throw new System.ArgumentNullException(nameof(owner));
        if (_temporaryOwner != null && _temporaryOwner != owner)
            throw new System.InvalidOperationException("A temporary companion is already active");
        _temporaryOwner = owner;
        _temporaryBuffs = new AnimalRunBuffs();
        _temporaryBuffs.Add(buffs);
        MarkDirty();
    }
    public static void ClearTemporaryCompanion(Object owner)
    {
        if (_temporaryOwner != owner) return;
        _temporaryOwner = null;
        _temporaryBuffs = null;
        MarkDirty();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTemporaryCompanion()
    {
        _temporaryOwner = null;
        _temporaryBuffs = null;
        MarkDirty();
    }

    public static void MarkDirty()
    {
        _dirty = true;
    }

    public static int InventorySlots => Mathf.Max(0, GetCurrentBuffs().inventorySlots);

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

    public static AnimalRunBuffs GetCurrentBuffsSnapshot()
    {
        AnimalRunBuffs snapshot = new();
        snapshot.Add(GetCurrentBuffs());
        return snapshot;
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

                AnimalRunBuffs buffs = _catalog.GetAnimalBuffs(placed.animalId, Mathf.Max(1, placed.stage));
                result.Add(buffs);
            }
        }

        if (_temporaryOwner != null) result.Add(_temporaryBuffs);
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
