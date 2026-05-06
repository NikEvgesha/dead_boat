# Profession System Spec

Updated: 2026-05-06
Status: v2 implementation pass completed; needs manual UI/economy smoke in Lobby.

## Goal

Professions are a lobby meta-feature that lets the player choose a run style before starting a run.

Target player loop:

1. Player enters the lobby.
2. Player approaches the profession selection point.
3. By default the player has no active profession.
4. Player can start a run with no profession.
5. Player can randomly unlock a profession for soft currency.
6. Player can permanently buy any specific profession:
   - for real money on platforms with purchases;
   - for soft currency on platforms without real-money purchases.
7. Player selects one unlocked profession for the next run.
8. Selected profession gives starting items and stat bonuses for that run.
9. Profession unlocks and selected profession are permanent meta-progress and survive run reset.

## Current Implementation

Existing code foundation:

- `ProfessionCatalog` ScriptableObject stores profession definitions.
- `ProfessionDefinition` already supports `professionId`, `title`, `description`, `icon`, start currency bonuses, `defaultUnlocked`, `starterItems`, `perkLines`, and `passiveBonuses`.
- `ProfessionPassiveBonuses` already covers player health/movement, experience, sale reward, fuel, boat speed, melee, and ranged combat.
- `ProfessionState_v1` stores selected profession and unlocked professions.
- `ProfessionService` can select, unlock random locked profession, apply start currency bonuses, build starter packs, and apply passive stat modifiers.
- `ProfessionSelectionPanel` and temporary UI exist.
- `ProfessionNpcPoint` exists for lobby interaction.

Implemented v2 additions:

- No-profession state is valid and selected by default when the player has no explicit profession choice.
- Random unlock supports configurable soft currency, currently coins in `ProfessionSelectionPanel`.
- Profession definitions include random pool, weight, direct soft-currency cost, purchase product id, and soft fallback flags.
- Direct buy supports real-money purchase when `PurchasesManager.PurchasesAvailable()` is true and `purchaseProductId` is set.
- Direct buy falls back to soft currency when purchases are unavailable and the profession allows fallback.
- Temporary UI now includes a separate direct buy button.

Remaining implementation notes:

- Purchase product ids and final direct-buy prices still need real content values in `ProfessionCatalog.asset`.
- Current stat application is direct through `ProfessionService.Apply*`; before adding class mechanics, this should move behind the planned unified run stat layer.

## Save Scope

Profession data is meta-save, not run-save.

Storage key:

- `ProfessionState_v1`

State:

```csharp
public class ProfessionState
{
    public int version;
    public string selectedProfessionId;
    public bool hasExplicitProfessionChoice;
    public List<string> unlockedProfessionIds;
}
```

Current v2 rules:

- no profession is a valid state through `hasExplicitProfessionChoice == false`;
- unknown ids should not wipe the whole state;
- permanent unlocks must survive win/lose, scene reload, and browser refresh;
- profession state must not be stored in `LevelStatManager.Stats`, `SaveKey.BoostType`, or `SaveKey.LevelUp`.

Compatibility:

- Old saves with a selected default profession keep working.
- If an old save has `hasExplicitProfessionChoice == false`, it behaves as no profession selected.
- If an old save has selected profession unlocked explicitly, it keeps that selected profession.

## Data Model

Professions should remain data-driven through ScriptableObjects.

Recommended `ProfessionDefinition` v2:

```csharp
public class ProfessionDefinition
{
    public string professionId;
    public string title;
    public string description;
    public Sprite icon;

    public bool availableInRandomUnlockPool;
    public int randomUnlockWeight;

    public int randomUnlockCoinCostOverride;
    public int directSoftCurrencyCost;
    public CurrencyType directSoftCurrencyType;

    public string purchaseProductId;
    public bool allowSoftCurrencyFallbackWhenPurchasesUnavailable;

    public List<ProfessionStarterItem> starterItems;
    public int startCoinsBonus;
    public int startGemsBonus;
    public ProfessionPassiveBonuses passiveBonuses;
    public List<string> perkLines;
}
```

Notes:

- `defaultUnlocked` should be replaced or reinterpreted. We need no-profession as the default, not a free hidden profession.
- Random unlock should have a pool flag so premium-only professions can be excluded if needed.
- Direct buy should use stable `purchaseProductId` for real-money platforms.
- Soft-currency fallback should be explicit per profession, not automatic for every product.
- Balance fields should later be exportable/importable through Google Sheets.

## Unlock Rules

### No Profession

- Always available.
- Costs nothing.
- Gives no starter items and no stat bonuses.
- Should be shown in UI as a selectable option or a clear "None" state.
- This is the default for new players.

### Random Soft-Currency Unlock

Allowed when at least one profession in the random pool is still locked.

Rules:

- costs coins or another configured soft currency;
- chooses one locked profession from `availableInRandomUnlockPool`;
- supports equal weights at first, weighted unlock later;
- unlocks the profession permanently;
- does not have to auto-select the profession, but the UI should focus it and offer selection.

Open design choice:

- Whether random unlock cost is global or per-profession weighted by rarity. Recommended first pass: global scalable cost in the panel/service, then move to catalog/Sheets when balance needs it.

### Direct Permanent Buy

Allowed on any locked profession that has a direct buy option.

Purchase modes:

- Real-money mode:
  - enabled only when `PurchasesManager.Instance.PurchasesAvailable()` is true;
  - uses `purchaseProductId`;
  - unlocks the chosen profession permanently after successful purchase.
- Soft-currency fallback:
  - used on platforms without purchases;
  - allowed only when `allowSoftCurrencyFallbackWhenPurchasesUnavailable` is true;
  - costs `directSoftCurrencyCost` in configured currency;
  - unlocks the chosen profession permanently.

If neither mode is available, the UI must show the profession as locked without a direct buy button.

## Run Effects

A selected profession affects a run in two ways.

### Starter Items

At new run start:

- add configured `starterItems` to the starting inventory or starter pack;
- do not add them when resuming an already saved run;
- do not write starter items into meta-save;
- save the current run normally after the run starts.

### Stat Bonuses

Profession passive bonuses affect only active run calculations.

Currently they are applied directly in gameplay scripts through `ProfessionService.Apply*`.

Target architecture:

- use `RunStatService` described in `Docs/SAVE_SCOPE_AND_STAT_MODIFIERS.md`;
- combine run cards, profession, placed animals, and future classes in one deterministic snapshot;
- keep profession ownership/selection in meta-save while stat effects are recalculated for the run.

## UI Requirements

Profession selection point:

- Exists in the lobby.
- Opens the profession panel on interact.
- Should be replaceable by final art/UI later.

Panel:

- Shows "No profession" as default/selectable state.
- Shows profession icon, title, description, starter items, and stat bonuses.
- Shows locked/unlocked/selected status.
- For locked professions:
  - random unlock button is separate from direct buy;
  - direct buy button appears if a purchase or fallback is available.
- For unlocked professions:
  - select button chooses it for the next run.
- For selected profession:
  - button is disabled or says equipped.

Important UX points:

- Player should clearly understand whether they are buying a random profession or a specific profession.
- On platforms without purchases, the same direct-buy slot should switch to soft-currency cost.
- If no profession is selected, starting a run should feel intentional and not look like a broken state.

## Balance Requirements

Profession balance should eventually be synced with Google Sheets.

Tables needed:

- profession base data;
- random unlock pool and weights;
- random unlock cost;
- direct soft-currency cost;
- purchase product ids;
- starter items;
- passive stat bonuses.

Balance constraints:

- Profession bonuses should be meaningful but not mandatory for the first runs.
- Starter items should change play style more than raw power where possible.
- Direct buy professions should not create pay-to-win spikes that break early-game tuning.
- Random unlock should feel useful even if the player does not get the exact profession they wanted.
- Profession bonuses must be tested together with card upgrades and placed animal buffs.

## Acceptance Checklist

- New player starts with no active profession.
- Player can start a run without selecting a profession.
- Lobby profession point opens the selection UI.
- ScriptableObject catalog drives profession content.
- Random unlock spends soft currency and unlocks one locked profession permanently.
- Direct buy unlocks a chosen profession with real money where purchases are available.
- Direct buy falls back to soft currency on platforms without purchases when allowed.
- Unlocked profession can be selected.
- Selected profession persists between sessions.
- Selected profession gives starter items only on a new run, not on resumed run.
- Selected profession stat bonuses affect run stats.
- Win/lose run reset does not clear profession unlocks or selection.
- Old saves with no explicit profession choice behave as no profession selected.
- Unity Console stays clean after opening UI, buying/unlocking, selecting, and starting a run.
