using System.Globalization;

public static class CombatCardStatusText
{
    public static string Status(BoostType type, int rank, int state, double value)
    {
        if (type == BoostType.SteadyAim || type == BoostType.PatientReload)
            return state == 1 ? EggFeatureLocalization.Text("Cards/Combat/RangedReady", "Выстрел готов: +{bonus}%", "Shot ready: +{bonus}%")
                .Replace("{bonus}", ((type == BoostType.SteadyAim ? CombatCardRules.AimBonus(rank) : CombatCardRules.PatientBonus(rank)) * 100).ToString("0", CultureInfo.InvariantCulture)) :
                type == BoostType.SteadyAim ? EggFeatureLocalization.Text("Cards/Combat/AimWaiting", "Стойте 0.75 с", "Stand still for 0.75 s") :
                EggFeatureLocalization.Text("Cards/Combat/ReloadWaiting", "Перезарядка с нуля", "Reload from empty");
        if (type == BoostType.BattleRhythm)
            return state == 1 ? EggFeatureLocalization.Text("Cards/Combat/Third", "Усиленный удар!", "Empowered hit!") :
                EggFeatureLocalization.Text("Cards/Combat/Combo", "Серия: {count}/3", "Combo: {count}/3")
                    .Replace("{count}", ((int)value).ToString(CultureInfo.InvariantCulture));
        if (type == BoostType.BloodPact)
        {
            if (state == 1 && value > 0 && value < .01)
                return EggFeatureLocalization.Text("Cards/Combat/SmallHeal", "Лечение: <0.01 HP", "Healed: <0.01 HP");
            return state == 1 ? EggFeatureLocalization.Text("Cards/Combat/Healed", "+{amount} HP", "+{amount} HP")
                .Replace("{amount}", value.ToString("0.##", CultureInfo.InvariantCulture)) :
                EggFeatureLocalization.Text("Cards/Combat/Lifesteal", "Вампиризм: {percent}%", "Lifesteal: {percent}%")
                    .Replace("{percent}", (CombatCardRules.Lifesteal(rank) * 100).ToString("0", CultureInfo.InvariantCulture));
        }
        return state == 1 ? EggFeatureLocalization.Text("Cards/Combat/EdgeActive", "Активно: +{percent}% урона", "Active: +{percent}% damage")
            .Replace("{percent}", (CombatCardRules.EdgeBonus(rank) * 100).ToString("0", CultureInfo.InvariantCulture)) :
            EggFeatureLocalization.Text("Cards/Combat/EdgeWaiting", "Нужно ≤40% HP", "Requires ≤40% HP");
    }
    public static string Label(BoostType type, int rank, int state, double value) =>
        EggFeatureLocalization.Text("Cards/Combat/Label", "{card} · {rank}\n{status}", "{card} · {rank}\n{status}")
            .Replace("{card}", CombatCardText.Title(type)).Replace("{rank}", rank == 1 ? "I" : rank == 2 ? "II" : "III")
            .Replace("{status}", Status(type, rank, state, value));
}
