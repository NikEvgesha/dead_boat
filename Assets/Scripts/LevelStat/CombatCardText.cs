using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class CombatCardText
{
    private static string Text(BoostType type, string part, string ru, string en) =>
        EggFeatureLocalization.Text("Cards/" + type + "/" + part, ru, en);
    public static string Title(BoostType type)
    {
        if (type == BoostType.BloodPact) return Text(type, "Title", "Кровавый договор", "Blood Pact");
        if (type == BoostType.BattleRhythm) return Text(type, "Title", "Ритм боя", "Battle Rhythm");
        if (type == BoostType.LastRound) return Text(type, "Title", "Последний довод", "Last Round");
        if (type == BoostType.HeavyCoat) return Text(type, "Title", "Тяжёлый бушлат", "Heavy Coat");
        if (type == BoostType.SteadyAim) return Text(type, "Title", "Верная рука", "Steady Aim");
        if (type == BoostType.PatientReload) return Text(type, "Title", "Новый заход", "Fresh Start");
        return Text(type, "Title", "На грани", "On the Edge");
    }
    public static string RankPreview(int current) => EggFeatureLocalization.Text("Cards/RankPreview",
        "Ранг {current} → {next} / 3", "Rank {current} → {next} / 3")
        .Replace("{current}", current.ToString()).Replace("{next}", Mathf.Min(3, current + 1).ToString());

    public static string Description(BoostType type, int nextRank, float beforeHP, float afterHP)
    {
        string template;
        if (type == BoostType.BloodPact)
            template = Text(type, "Description", "Максимальное HP −{loss}%: {before} → {after}. Прямой урон оружием по живым врагам лечит на {steal}% снятого HP, до {cap}% макс. HP за секунду. Цена применяется один раз; огонь не лечит.",
                "Maximum HP −{loss}%: {before} → {after}. Direct weapon damage to living enemies heals for {steal}% of HP removed, up to {cap}% of max HP per second. Cost applies once; burn does not heal.");
        else if (type == BoostType.BattleRhythm)
            template = Text(type, "Description", "Третье попадание мечом в серии наносит +{bonus}% урона. Между попаданиями не более {window} с. Промах сбрасывает серию; один взмах считается один раз.",
                "The third sword hit in a combo deals +{bonus}% damage. Hits must be within {window} s. A missed swing resets the combo; each swing counts once.");
        else if (type == BoostType.SteadyAim)
            template = Text(type, "Description", "Стойте неподвижно {prepare} с: следующий выстрел +{aim}% урона. Откат {cooldown} с; промах тоже тратит усиление. Движение и смена оружия сбрасывают подготовку. Общий предел +{limit}%.",
                "Stand still for {prepare} s: the next shot deals +{aim}% damage. Cooldown: {cooldown} s; misses consume the boost. Moving or switching weapons resets preparation. Combined bonus cap: +{limit}%.");
        else if (type == BoostType.PatientReload)
            template = Text(type, "Description", "Завершите перезарядку пустого магазина: первый выстрел в течение {cooldown} с наносит +{patient}% урона. Промах тратит усиление; смена оружия его снимает. Частичная и отменённая перезарядка не дают бонус. Общий предел +{limit}%.",
                "Finish reloading an empty magazine: the first shot within {cooldown} s deals +{patient}% damage. A miss consumes the boost; switching weapons removes it. Partial or cancelled reloads grant no boost. Combined bonus cap: +{limit}%.");
        else if (type == BoostType.LastRound)
            template = Text(type, "Description", "Последний патрон магазина наносит +{last}% прямого урона. Любая перезарядка на {reload}% дольше. Дробь усиливается целиком; промах расходует патрон. Общий предел бонусов +{limit}%.",
                "The last round in the magazine deals +{last}% direct damage. Every reload takes {reload}% longer. All pellets are boosted; a miss still consumes ammo. Combined bonus cap: +{limit}%.");
        else if (type == BoostType.HeavyCoat)
            template = Text(type, "Description", "Максимальное HP +{coat}%: {before} → {after}; скорость передвижения −{slow}%. Штраф скорости применяется один раз. Получение карты не лечит; действует вместе с Кровавым договором.",
                "Maximum HP +{coat}%: {before} → {after}; movement speed −{slow}%. Speed cost applies once. Taking this card does not heal; combines with Blood Pact.");
        else
            template = Text(type, "Description", "При HP не выше {threshold}% перед атакой прямой урон оружием +{bonus}%. Бонусы особых карт суммируются до +{limit}%.",
                "At or below {threshold}% HP before an attack, direct weapon damage gains +{bonus}%. Special card damage bonuses add up to +{limit}%.");
        var parameters = new Dictionary<string, float>
        {
            ["prepare"] = .75f, ["cooldown"] = 3, ["aim"] = CombatCardRules.AimBonus(nextRank) * 100,
            ["patient"] = CombatCardRules.PatientBonus(nextRank) * 100,
            ["last"] = CombatCardRules.LastRoundBonus(nextRank) * 100, ["reload"] = 15,
            ["coat"] = CombatCardRules.CoatHealth(nextRank) * 100, ["slow"] = 8,
            ["loss"] = 20, ["before"] = beforeHP, ["after"] = afterHP,
            ["steal"] = CombatCardRules.Lifesteal(nextRank) * 100, ["cap"] = CombatCardRules.HealLimit(nextRank) * 100,
            ["bonus"] = (type == BoostType.BattleRhythm ? CombatCardRules.RhythmBonus(nextRank) : CombatCardRules.EdgeBonus(nextRank)) * 100,
            ["window"] = 2, ["threshold"] = 40, ["limit"] = 100
        };
        foreach (var pair in parameters) template = template.Replace("{" + pair.Key + "}", pair.Value.ToString("0.##", CultureInfo.InvariantCulture));
        return template;
    }
}
