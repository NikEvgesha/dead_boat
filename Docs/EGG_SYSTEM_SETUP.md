# Egg System Setup

Updated: 2026-05-05

This file tracks Unity setup for the egg feature. The current implementation has temporary UI fallbacks, but production should use explicit scene/prefab references.

## Scene Objects

Required lobby objects:

- `EggHatchingManager`
- One or more `EggNestPoint` objects with unique `Nest Id`
- One or more `AnimalPlacementPoint` objects on the boat with unique `Point Id`
- Future: one `AnimalMergePoint` or merge station object

Required UI:

- `EggNestSelectionPanel`
- `AnimalPlacementSelectionPanel`
- Future: `AnimalInventoryPanel`
- Future: `AnimalMergePanel`
- Optional: `EggStorageDisplay`
- Optional: `EggNestUI` per nest

## Catalog

Current asset:

- `Assets/Resources/Eggs/EggHatchingCatalog.asset`

Before production, extend the catalog so it contains:

- Egg definitions
- Hatch result pools
- Animal definitions
- Animal stages
- Stage prefabs/tints/VFX
- Buff values per stage
- Merge limits/costs

## Manager Settings

For target gameplay, use:

- `Auto Load On Start = true`
- `Auto Collect Finished Eggs = false`

Reason: ready animals should wait at the nest until the player manually collects them. Auto-collect is useful for old tests, but it does not match the desired loop.

Check:

- `Catalog` is assigned or `Catalog Resource Path = Eggs/EggHatchingCatalog`
- `Nests` includes all production nests or auto-search finds them
- `Animal Points` includes all boat placement points or auto-search finds them
- `Animals Root` is assigned when animals should not be parented directly to placement anchors
- `Parent Animals To Anchor = true` for boat-mounted points

## Egg Pickup Setup

Each egg pickup prefab needs:

- `PickableItem`
- `EggCollectibleItem`
- Valid `eggId` matching the catalog

Spawn setup:

- Add egg prefabs to `LocationItemSpawnCollection`.
- Tune spawn chance per location.
- Use `EggSpawnBalancer` to reduce effective chance after each egg pickup in the same run.

## Nest Setup

For each nest:

- Add `EggNestPoint`.
- Set unique `Nest Id`, for example `nest_01`.
- Assign `Egg Visual Anchor`.
- Connect `EggNestUI` if using designer UI.
- Connect `EggNestSelectionPanel` if this nest uses a local panel; otherwise global panel instance can be used.

Production behavior:

- Empty nest opens egg selection.
- Hatching nest shows timer and optional skip.
- Ready nest shows animal preview and collect action.

## Animal Placement Setup

For each boat placement point:

- Add `AnimalPlacementPoint`.
- Set unique `Point Id`, for example `boat_pet_01`.
- Assign `Spawn Anchor` on the boat hierarchy.
- Connect `AnimalPlacementSelectionPanel` if this point uses a local panel.

Production behavior:

- Empty point opens animal selection.
- Occupied point allows remove/replace.
- Placed animals are excluded from merge inventory.

## Merge Station Setup

Not implemented yet.

Target setup:

- Add lobby interaction point, likely `AnimalMergePoint`.
- Connect `AnimalMergePanel`.
- Panel should list only mergeable pairs:
  - same `animalId`;
  - same `stage`;
  - count at least 2;
  - stage below max.
- Confirm action consumes two and adds one upgraded animal.

## Manual Smoke

Minimum smoke after every egg-system change:

1. Start from `LoadingScene`.
2. Enter a run.
3. Pick up an egg.
4. Confirm ordinary inventory did not receive the egg.
5. Confirm egg storage count increased.
6. Win or lose and return to lobby.
7. Start incubation in a nest.
8. Reload scene or restart Play Mode and confirm timer persists.
9. Wait or skip until ready.
10. Confirm ready animal stays at nest until collected.
11. Collect animal.
12. Place animal on boat.
13. Start a new run and confirm the animal buff applies.
14. Return to lobby, remove animal from boat.
15. Hatch/obtain a duplicate animal.
16. Merge two identical stage-1 animals into one stage-2 animal.
17. Confirm max-stage animals cannot be merged.

## Release Risks

- Do not change the storage key lightly. Use additive fields and migration.
- Do not delete unknown animal ids during normalization.
- Avoid auto-collect for production if the nest is meant to be an interaction point.
- Test egg/pet buffs together with profession buffs before release.
- Keep temporary UI replaceable; production UI should be prefab-driven.
