# Profession System Setup

Updated: 2026-05-06

This setup doc describes the target profession feature. Some parts are not implemented yet and are marked as pending.

## 1. Catalog

Create or update the profession catalog:

- Asset menu: `Create -> ScriptableObject -> Professions -> ProfessionCatalog`.
- Recommended path: `Assets/Resources/Professions/ProfessionCatalog.asset`.

Each profession should have:

- stable `professionId`;
- `title`;
- `description`;
- `icon`;
- starter items;
- start currency bonuses if needed;
- passive stat bonuses;
- random unlock settings;
- direct purchase settings.

Current implementation fields:

- `professionId`
- `title`
- `description`
- `icon`
- `defaultUnlocked`
- `starterItems`
- `startCoinsBonus`
- `startGemsBonus`
- `perkLines`
- `passiveBonuses`

Pending v2 fields:

- random unlock pool flag;
- random unlock weight;
- direct coin/soft-currency cost;
- purchase product id;
- soft-currency fallback flag for platforms without purchases.

## 2. Default State

Target behavior:

- New players start with no active profession.
- "No profession" should be selectable and should give no bonuses.

Current implementation note:

- Existing code always unlocks a default profession when `defaultUnlocked` is set.
- During the v2 implementation pass, normalize logic should be changed so old saves still work, but new players can stay in no-profession state.

## 3. Lobby Interaction

Add a profession point in the lobby:

1. Create a lobby object for profession selection.
2. Add `ProfessionNpcPoint`.
3. Add trigger collider for interaction.
4. Assign:
   - info canvas or hint object;
   - `ProfessionSelectionPanel`;
   - optional touch open button.

The interaction should open the profession panel and not start a run by itself.

## 4. UI Panel

Required panel controls:

- close button;
- previous/next or list selection;
- select/equip button;
- random unlock button;
- direct buy button;
- title text;
- description text;
- starter item summary;
- stat/perk summary;
- status text;
- price text;
- icon;
- locked marker.

Target UI states:

- No profession selected.
- Profession locked.
- Profession unlocked but not selected.
- Profession selected.
- Random unlock available.
- Random unlock unavailable because all pool professions are unlocked.
- Direct real-money buy available.
- Direct soft-currency fallback available.
- Direct buy unavailable on this platform.

Temporary UI is acceptable during implementation, but it should be easy to replace with final UI.

## 5. Economy Wiring

Random unlock:

- Spend configured soft currency, initially coins.
- Unlock one random locked profession from the random pool.
- Persist the unlock to `ProfessionState_v1`.

Direct buy:

- If purchases are available, use the configured product id.
- If purchases are unavailable and fallback is allowed, spend configured soft currency.
- Unlock the chosen profession permanently after success.

Need implementation check:

- Confirm exact `PurchasesManager` product API before wiring direct real-money unlock.
- Confirm whether coins or gems should be the fallback currency per platform.

## 6. Run Integration

At new run start:

- If no profession is selected, do nothing.
- If a profession is selected:
  - add starter items;
  - apply start currency bonuses;
  - include passive stat bonuses in run stat calculation.

Rules:

- Do not apply starter items when resuming an unfinished saved run.
- Do not clear profession state at win/lose.
- Do not store profession bonuses in `LevelStatManager.Stats`.

## 7. Smoke Checklist

Run these checks after implementation:

1. Clean save opens profession UI with no profession selected.
2. Starting a run with no profession works and gives no extra items/stats.
3. Random unlock spends soft currency and unlocks one profession.
4. Random unlock cannot pick already unlocked professions.
5. Direct buy unlocks the selected profession on purchase-capable platform.
6. Direct buy switches to soft-currency fallback when purchases are unavailable and fallback is allowed.
7. Unlocked profession can be selected.
8. Selected profession persists after restart.
9. New run receives starter items and passive stat bonuses.
10. Resumed run does not duplicate starter items.
11. Win/lose reset does not clear unlocked professions or selected profession.
12. Unity Console stays free of errors and dangerous warnings.
