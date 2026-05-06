# Egg System Spec

Updated: 2026-05-06
Status: core implementation exists; needs scene hookup, final UI, and balance smoke before production.

## Goal

Eggs are a meta-progression feature that starts during a run and resolves in the lobby.

Target player loop:

1. Player starts a normal run.
2. Locations spawn eggs alongside ordinary pickup objects.
3. Picking up an egg does not put it into the run inventory. It goes into separate egg storage.
4. The UI shows how many eggs of each type the player owns.
5. After win/lose, the player returns to the lobby.
6. In the lobby, the player approaches nest points and starts hatching an owned egg.
7. Each egg type can hatch into several possible animal variants.
8. When the timer finishes, the animal appears at the nest and waits for manual collection.
9. Collected animals go into a separate animal inventory.
10. The player approaches animal placement points on the boat and chooses which owned animal to place.
11. Placed animals apply buffs during the next run.
12. A merge station lets the player merge two identical animals of the same stage into one stronger animal.
13. Merged animals change visuals slightly, for example tint/material variation and later particles.
14. Animals have a max stage, currently planned as 3-4 stages. Max-stage animals cannot be merged further.

## Current Implementation

Implemented foundation:

- Separate storage key: `EggFeatureState_v1`.
- Save scope is meta-progress, not run-progress. Eggs, nests, animal inventory, and placed animals must survive `GameManager.EndGame()` and must not be stored in `LevelStatManager.Stats`.
- Egg pickup bypasses ordinary inventory and uses `EggFeatureStorage.AddEgg`.
- `EggHatchingCatalog` exists in `Resources/Eggs/EggHatchingCatalog`.
- Egg and animal configs are separate ScriptableObject assets referenced by the catalog.
- `EggHatchingManager` supports:
  - starting incubation in `EggNestPoint`;
  - skip by gems;
  - ready nest detection;
  - collecting ready animals;
  - animal placement in `AnimalPlacementPoint`;
  - restoring placed animals after load;
  - applying placed animal buffs to run stats;
  - merging identical unplaced animals into the next stage.
- Temporary UI exists for nest selection and animal placement.
- Spawn chance degradation exists through `EggSpawnRuntimeState`.

Remaining gaps versus target loop:

- Final production UI is still needed; current panels can create temporary UI for testing.
- Scene needs final nest, boat placement, and merge station positions.
- Balance values, hatch weights, and spawn decay need real tuning.
- Final animal visuals/VFX are placeholder-friendly but not art-final.

## Data Model

Keep `EggFeatureState_v1` for save compatibility, but extend it additively. Do not rename the storage key unless a migration layer is added.

Recommended model:

```csharp
public class EggInventoryEntry
{
    public string eggId;
    public int amount;
}

public class AnimalInventoryEntry
{
    public string animalId;
    public int stage;
    public int amount;
}

public class EggNestState
{
    public string nestId;
    public string eggId;
    public string hatchedAnimalId;
    public int hatchedStage;
    public long finishAtUnix;
    public int durationSeconds;
    public bool isReady;
}

public class PlacedAnimalState
{
    public string pointId;
    public string animalId;
    public int stage;
}
```

Compatibility rule:

- The egg feature was not released to production before this model, so no legacy animal fields are kept.
- Unknown ids must be preserved when possible, not deleted, so temporary catalog mistakes do not wipe player progress.

## Catalog

`EggHatchingCatalog` should describe eggs, possible hatch results, animal stages, buffs, visuals, and merge limits.

Recommended data:

```csharp
public class EggDefinition : ScriptableObject
{
    public string eggId; // generated from asset name
    public string title;
    public int incubationSeconds;
    public int skipCostGems;
    public GameObject eggPreviewPrefab;
    public List<EggHatchResult> hatchResults;
}

public class EggHatchResult
{
    public AnimalDefinition animal;
    public int weight;
}

public class AnimalDefinition : ScriptableObject
{
    public string animalId; // generated from asset name
    public string title;
    public int maxStage;
    public List<AnimalStageDefinition> stages;
}

public class AnimalStageDefinition
{
    public int stage;
    public GameObject animalPrefab;
    public Color tint;
    public GameObject mergeParticlesPrefab;
    public AnimalRunBuffs buffs;
}
```

Hatch selection:

- Use weighted random among `hatchResults`.
- Egg configs reference animal assets directly; ids are derived from the referenced animal assets.
- Store the chosen `hatchedAnimalId` in the nest when incubation starts or when it finishes.
- Prefer choosing at incubation start if we want the result to be stable even if balance changes before collection.

## Buffs

Animals should affect the next run only when placed on the boat.

Recommended buff categories:

- Player: max health, move speed, damage resistance.
- Boat: max fuel, fuel consumption multiplier, boat speed.
- Combat: melee damage, melee attack speed, ranged damage, reload speed.
- Economy: coin reward multiplier, pickup value multiplier.
- Utility: egg spawn chance modifier, rare hatch chance modifier, extra inventory capacity.

Rules:

- Buffs from multiple placed animals stack through a deterministic aggregator.
- Multipliers must be clamped to avoid runaway combinations.
- Merge stages should improve buffs by table values, not a hardcoded formula, so balance can be controlled from catalog/Google Sheets.
- Profession and animal buffs must be tested together; neither should create an early-game auto-win.

## Merge Rules

Target merge behavior:

- Merge station exists in the lobby.
- Player selects two owned animals.
- Merge is allowed only when:
  - same `animalId`;
  - same `stage`;
  - `stage < maxStage`;
  - both animals are in inventory, not placed on the boat.
- Result:
  - consume 2 animals of `(animalId, stage)`;
  - add 1 animal of `(animalId, stage + 1)`;
  - show VFX/SFX feedback;
  - refresh animal inventory UI.

Future optional rules:

- Merge cost in coins/gems.
- Chance-based merge for higher stages.
- Duplicate protection or pity logic for rare hatch results.

## UI Requirements

Run UI:

- Egg pickup feedback.
- Egg storage counter by egg type.
- No egg item should appear in ordinary run inventory.

Lobby nest UI:

- Shows owned eggs by type.
- Shows empty nest, hatching timer, ready state, and collect action.
- Ready nest should show animal preview, not silently move the animal to inventory.
- Skip button shows gem cost and disabled state when the player lacks gems.

Animal inventory UI:

- Shows animal type, stage, count, and current buff summary.
- Supports selecting an animal for boat placement.
- Clearly marks placed animals as unavailable for merge.

Boat placement UI:

- Shows available animals.
- Allows replace/remove behavior.
- Shows currently active buffs for the run.

Merge UI:

- Shows mergeable pairs.
- Blocks invalid combinations with clear disabled states.
- Shows result stage and upgraded buff preview before confirming.

## Balance Requirements

Balance tables should eventually live in Google Sheets and import into local ScriptableObject assets.

Egg balance:

- Spawn chance per egg type.
- Per-run chance decay after collecting eggs.
- Max useful eggs per run if needed.
- Incubation duration.
- Skip cost.
- Hatch result weights.

Animal balance:

- Stage count per animal.
- Buff values per stage.
- Merge cost if used.
- Visual/VFX references per stage.

Progression targets to tune:

- Time to first egg.
- Time to first hatched animal.
- Time to first placed animal buff.
- Time to first merge.
- Expected number of runs to reach stage 2/3/4.

## Acceptance Checklist

- Eggs spawn in a normal run.
- Egg pickup updates egg storage and does not add to run inventory.
- After win/lose, lobby state still contains collected eggs.
- Player can start hatching an egg in a nest.
- Timer survives scene reload/restart.
- Ready nest waits for manual collection.
- Collected animal appears in animal inventory.
- Player can place an animal on the boat.
- Placed animal buff affects the next run.
- Player can remove/replace a placed animal.
- Two identical unplaced animals can merge into the next stage.
- Max-stage animals cannot merge.
- Old saves without egg data still load safely.
- Saves without egg data still load safely.

## Related Architecture

See `Docs/SAVE_SCOPE_AND_STAT_MODIFIERS.md` for the save-scope rules and the planned unified stat modifier layer. The current implementation applies animal buffs after run cards and professions; before adding the future class mechanic, stat application should be centralized so cards, professions, animals, and classes share one deterministic calculation path.
