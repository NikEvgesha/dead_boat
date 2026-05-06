using System;

[Serializable]
public class AnimalRunBuffs
{
    public float maxHealthFlat;
    public float moveSpeedFlat;
    public float moveSpeedMultiplier = 1f;
    public float experienceMultiplier = 1f;
    public float saleRewardMultiplier = 1f;
    public float maxFuelFlat;
    public float fuelConsumptionMultiplier = 1f;
    public float fuelFillMultiplier = 1f;
    public float boatSpeedFlat;
    public float meleeDamageFlat;
    public float meleeAttackSpeedMultiplier = 1f;
    public float rangedDamageFlat;
    public float rangedAttackSpeedMultiplier = 1f;
    public float rangedReloadSpeedMultiplier = 1f;

    public float SafeMoveSpeedMultiplier => moveSpeedMultiplier > 0f ? moveSpeedMultiplier : 1f;
    public float SafeExperienceMultiplier => experienceMultiplier > 0f ? experienceMultiplier : 1f;
    public float SafeSaleRewardMultiplier => saleRewardMultiplier > 0f ? saleRewardMultiplier : 1f;
    public float SafeFuelConsumptionMultiplier => fuelConsumptionMultiplier > 0f ? fuelConsumptionMultiplier : 1f;
    public float SafeFuelFillMultiplier => fuelFillMultiplier > 0f ? fuelFillMultiplier : 1f;
    public float SafeMeleeAttackSpeedMultiplier => meleeAttackSpeedMultiplier > 0f ? meleeAttackSpeedMultiplier : 1f;
    public float SafeRangedAttackSpeedMultiplier => rangedAttackSpeedMultiplier > 0f ? rangedAttackSpeedMultiplier : 1f;
    public float SafeRangedReloadSpeedMultiplier => rangedReloadSpeedMultiplier > 0f ? rangedReloadSpeedMultiplier : 1f;

    public void Add(AnimalRunBuffs other)
    {
        if (other == null)
            return;

        maxHealthFlat += other.maxHealthFlat;
        moveSpeedFlat += other.moveSpeedFlat;
        moveSpeedMultiplier *= other.SafeMoveSpeedMultiplier;
        experienceMultiplier *= other.SafeExperienceMultiplier;
        saleRewardMultiplier *= other.SafeSaleRewardMultiplier;
        maxFuelFlat += other.maxFuelFlat;
        fuelConsumptionMultiplier *= other.SafeFuelConsumptionMultiplier;
        fuelFillMultiplier *= other.SafeFuelFillMultiplier;
        boatSpeedFlat += other.boatSpeedFlat;
        meleeDamageFlat += other.meleeDamageFlat;
        meleeAttackSpeedMultiplier *= other.SafeMeleeAttackSpeedMultiplier;
        rangedDamageFlat += other.rangedDamageFlat;
        rangedAttackSpeedMultiplier *= other.SafeRangedAttackSpeedMultiplier;
        rangedReloadSpeedMultiplier *= other.SafeRangedReloadSpeedMultiplier;
    }
}
