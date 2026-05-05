# Save Scope and Stat Modifiers

Updated: 2026-05-05

## Save Scopes

The project has two different progress scopes. New features must choose one explicitly.

### Run Save

Run save is temporary progress for the current run. It is restored if the player resumes an unfinished run and is reset when the run ends.

Current run-save data:

- `SaveKey.Distance`
- `SaveKey.LevelId`
- `SaveKey.InventoryList`
- `SaveKey.Boardlist`
- `SaveKey.Fuel`
- `SaveKey.Health`
- `SaveKey.Coins`
- `SaveKey.Exp`
- `SaveKey.Level`
- `SaveKey.Ammo_*`
- `SaveKey.PlayerFixPos`
- `SaveKey.BoardFixPos`
- card/level-up stats: `SaveKey.BoostType`
- unspent level-up choices: `SaveKey.LevelUp`

Reset path:

- `GameManager.EndGame()` calls `SaveManager.SaveGameProgress(-1, ...)`, clears inventory/board/fuel/positions/ammo, resets player exp, and calls `LevelStatManager.DeleteProgress()`.
- `LevelStatManager.DeleteProgress()` resets card/level-up `Stats` and `LevelUp`.

Rule: anything that should disappear after win/lose belongs here.

### Meta Save

Meta save is permanent player progress. It must survive win/lose, scene reloads, browser refreshes, and new runs.

Current meta-save data:

- gems and long-term currency where applicable;
- level/tutorial/quest/achievement progress;
- lobby items;
- roulette date;
- wins/scores;
- professions: `ProfessionState_v1`;
- eggs, nests, animals, placed animals: `EggFeatureState_v1`.

Rule: eggs, animals, purchased/unlocked professions, selected profession or explicit no-profession state, future classes, and bought/unlocked class content belong here.

## Existing Card System

The existing "skill cards" are implemented as `LevelStat` boosters:

- `BoostItem` describes a card reward by `BoostType` and rarity values.
- `LevelStatManager` rolls three boosters on level-up.
- Choosing a booster mutates `LevelStatManager.Stats`.
- `Stats` is saved through `SaveManager.SaveLevelUpdate`.
- These stats are run-save and are reset by `LevelStatManager.DeleteProgress()`.

This system is useful as a reference for:

- stat names already used by gameplay;
- rarity-driven reward values;
- temporary run-only upgrades;
- UI flow for choosing one of several options.

It should not be reused directly for permanent eggs/professions/classes because it is intentionally reset at run end.

## Current Stat Application

Gameplay stat reads currently apply modifiers directly at usage points:

1. Run cards from `LevelStatManager.Instance.Stats`.
2. Selected profession via `ProfessionService.Apply*`.
3. Placed animals via `EggAnimalBuffService.Apply*`.

Examples:

- movement speed in `PlayerMovement`;
- max health and experience in `PlayerStatsManager`;
- fuel/boat speed in `BoardController`;
- weapon damage/speed in weapon scripts;
- sale reward in `SellableItem`.

This is acceptable for the current egg/profession implementation, but it will become fragile when future classes also affect stats.

## Recommended Next Architecture

Before adding the class mechanic, add a unified run stat calculation layer.

Suggested shape:

```csharp
public enum StatModifierSource
{
    RunCard,
    Profession,
    Animal,
    Class
}

public sealed class RunStatSnapshot
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
}
```

Target flow:

1. `RunStatService` builds one snapshot for the current run.
2. It reads run cards from `LevelStatManager.Stats`.
3. It reads meta choices from `ProfessionService`, `EggAnimalBuffService`, and future `ClassService`. If no profession is selected, the profession source contributes no modifiers.
4. Gameplay scripts call only `RunStatService.Apply*`.
5. Save ownership stays separate:
   - `LevelStatManager` remains run-save;
   - professions/animals/classes remain meta-save.

Benefits:

- one place for modifier order;
- easier balancing and logging;
- less duplicated `Apply*` code;
- safer class implementation later;
- easier Google Sheets export/import because all stat names converge.

## Rules for New Permanent Features

- Do not store permanent progression in `Stats`, `SaveKey.BoostType`, or `SaveKey.LevelUp`.
- Do not call permanent feature resets from `GameManager.EndGame()`.
- Keep storage keys additive and version-tolerant, for example `ClassState_v1`.
- Normalize loaded state instead of deleting unknown ids.
- Unknown ids should be preserved when possible so catalog mistakes do not wipe player progress.
- If a permanent feature affects a run, it should affect the next run through the stat calculation layer, not by becoming run-save data.
