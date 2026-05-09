# GPT Journal (dead_boat)

Дата старта: 2026-03-05  
Проект: `E:\GitFork\dead_boat`

## Цель журнала

- Держать единый рабочий контекст между сессиями.
- Фиксировать не только "что сделали", но и "что осталось".
- Избежать повторного аудита на старте каждой новой задачи.

## Текущий статус (снимок)

- Основной backlog: `Assets/Scripts/TODO.md`.
- Основная механика в коде: `Assets/Scripts/Eggs/*`.
- Техническая документация: `Docs/EGG_SYSTEM_SPEC.md`, `Docs/EGG_SYSTEM_SETUP.md`.
- Главный риск на сейчас: не код ядра, а незавершенная ручная настройка Unity-сцены и smoke по полному циклу.

## Зафиксированные наблюдения

1. Реализовано отдельное состояние механики (`EggFeatureState_v1`) вне обычного `Inventory`.
2. Подбор яйца идет через `EggCollectibleItem` и не добавляет предмет в стандартный инвентарь.
3. `EggHatchingManager` покрывает полный runtime-цикл: инкубация, skip, автосбор готовых, размещение животных и начисление дохода.
4. Шанс спавна яиц адаптируется по ходу текущего забега через `EggSpawnRuntimeState`.
5. В коде есть готовые UI-панели выбора, но требуется финальная проводка и проверка в сцене.
6. Пользовательские строки механики пока частично hardcoded (нужна локализация/полировка).

## Рекомендуемый порядок работ

1. Закрыть блокеры Unity wiring (`EggNestSelectionPanel`, `AnimalPlacementSelectionPanel`).
2. Пройти сквозной smoke-сценарий от подбора яйца до пассивного дохода от установленного животного.
3. Закрыть проверку миграции сохранений и edge-case по перезаходам.
4. После стабилизации добить UX (фидбек, локализация, баланс).

## Definition of Done для задач по механике

- Есть воспроизводимый сценарий "до/после".
- Обновлен статус в `Assets/Scripts/TODO.md`.
- Добавлена запись в этот журнал.
- Если менялась конфигурация механики, обновлены соответствующие документы в `Docs/`.

## Лог сессий

### 2026-03-05

- Проведен аудит `dead_boat` и сверка с документационным подходом `steal_brainrot`.
- Зафиксированы текущие наработки по новой механике (яйца, гнезда, размещение животных, доход, баланс спавна).
- Переведен `Assets/Scripts/TODO.md` в формат структурированного backlog со статусами, приоритетами и регресс-чеклистом.
- Добавлены стартовый контекст и документы по механике для дальнейшего масштабирования на другие проекты.

### 2026-03-05 (профессии: первый этап реализации)

- Добавлена новая система профессий:
  - `Assets/Scripts/Professions/ProfessionCatalog.cs`
  - `Assets/Scripts/Professions/ProfessionState.cs`
  - `Assets/Scripts/Professions/ProfessionStorage.cs`
  - `Assets/Scripts/Professions/ProfessionService.cs`
  - `Assets/Scripts/Professions/ProfessionSelectionPanel.cs`
  - `Assets/Scripts/Professions/ProfessionNpcPoint.cs`
- Реализованы ключевые сценарии:
  - дефолтно открытая базовая профессия,
  - выбор только открытых профессий,
  - случайное открытие закрытой профессии за гемы,
  - сохранение текущей профессии и списка открытых,
  - интеграция бонусов профессии в старт рана (стартовые предметы + валютные бонусы).
- Интеграция в существующий проект:
  - `StarterPackManager` теперь может добавлять стартовые предметы выбранной профессии,
  - `GameManager` при старте нового рана применяет валютные бонусы текущей профессии.
- Добавлена документация:
  - `Docs/PROFESSION_SYSTEM_SPEC.md`
  - `Docs/PROFESSION_SYSTEM_SETUP.md`
- Обновлены `TODO.md`, `Docs/README.md` и `START_PROMPT.md` под новую механику.

### 2026-03-27 (prod-срез + roadmap на 3 направления)

- Проведена ревизия проекта как прод-потока (сцена + префабы + runtime, без внедрения новых фич).
- Зафиксировано:
  - egg-механика подключена в `Lobby` сцене,
  - `SaveManager` в основном потоке использует `MirraSDKSaveProvider`,
  - профессии есть в коде, но не подключены в сценовой проводке (нет активной интеграции UI/NPC и каталога).
- Актуализирован backlog в `Assets/Scripts/TODO.md`:
  - добавлен единый roadmap по 3 направлениям (яйца, профессии, баланс),
  - добавлен отдельный блок задач по балансу.
- Добавлен документ `Docs/BALANCE_ROADMAP.md` с планом итераций:
  - baseline метрики,
  - core tuning,
  - synergy tuning,
  - final regression pass.
- Обновлен `Docs/README.md` (индекс документации расширен баланс-документом).

### 2026-03-28 (eggs + professions passives + balance pass)

- Eggs/pets:
  - EggHatchingManager now supports parenting spawned pets to placement anchors (_parentAnimalsToAnchor), so pets follow boat movement when anchors are on the boat hierarchy.
  - Added optional local pose reset for anchored pets (_resetAnimalLocalPoseWhenParented).
  - Added passive income balancing controls:
    - _fullIncomeAnimalCount
    - _extraAnimalIncomeMultiplier
    - _maxAccumulatedIncomeSeconds (offline accumulation cap)
- Professions:
  - Extended ProfessionDefinition with structured runtime passive bonuses (ProfessionPassiveBonuses) in ProfessionCatalog.cs.
  - Added runtime passive calculation API in ProfessionService (ApplyMoveSpeed, ApplyMaxHealth, ApplyExperienceGain, etc.).
  - BuildPerksSummary now includes configured numeric passive lines, not only text perkLines.
  - ProfessionSelectionPanel now supports dynamic random-unlock gem pricing (base + per-opened step + max cap).
- Runtime stat integration:
  - Applied profession passive modifiers in:
    - PlayerMovement (move speed)
    - PlayerStatsManager (max health, exp gain)
    - BoardController (max fuel, fuel consumption, fuel refill, max speed)
    - MeleWeapon (damage, attack speed)
    - RangedWeaponController (damage, attack speed, reload speed)
    - SellableItem (sale reward)
- Localization:
  - Added passive label keys/fallbacks in ProfessionLocalization for passive summary output.

Open follow-up:
- Create/fill ProfessionCatalog.asset content with final class roster and passive values in production scene setup.
- Smoke test in Unity: new save + resumed save + pet placement on moving boat + unlock price progression.

### 2026-05-04 (Unity 6000.3.9f1 migration cleanup)

- Project opened and smoke-tested on Unity `6000.3.9f1`.
- Fixed profession localization fallback lookup so missing optional profession keys no longer emit warning spam during Play Mode.
- Hardened MirraSDK leaderboard provider to skip score calls on Editor/local/unknown/fallback-like platforms, avoiding fallback achievement warnings in local Play Mode.
- Converted corrupted diagnostic text in `LocalizationData` to ASCII-safe messages and added `TryGetTranslation`.
- Verified script compilation succeeds after refresh; remaining Unity 6 warnings are obsolete API warnings and package/ProBuilder warnings, not blocking errors.

### 2026-05-04 (Unity 6000.3.9f1 startup cleanup follow-up)

- Fixed MirraSDK gameplay analytics fallback warnings in Editor/unsupported deployments by guarding `GameIsReady`, `GameplayStart`, and `GameplayStop` calls behind deployment/platform checks.
- Fixed Play Mode shutdown `NullReferenceException` noise from singleton teardown in UI/item/location components by adding null-safe unsubscribe/cleanup guards.
- Tested `com.unity.probuilder` 6.0.9 during package audit, but rolled back to 6.0.8 because 6.0.9 caused AssetImportWorker/asmdef import crashes on this project.
- Verified script compilation: Tundra build success on Unity 6000.3.9f1. Fresh Play Mode start/stop tail is clean for `NullReferenceException` and `FallbackGameplayReporter` not-implemented warnings.
- Remaining migration warnings are ProBuilder package/editor warnings, invalid editor font reference reload warnings, and scene geometry warnings for large ProBuilder mesh triangles (`pb_Mesh-166884`, `pb_Mesh-166284`, `pb_Mesh-1173094`).

### 2026-05-04 (large ProBuilder collider cleanup)

- Fixed PhysX large triangle warnings for land ProBuilder meshes in `LevelForest`, `LevelDesert`, `LevelMerged`, and `LevelWinter`.
- Visual ProBuilder meshes were left intact; unstable large `MeshCollider` components were removed from `GameCollection/Spawners/LendController/Land*` objects.
- Added child-only `Generated Subdivided MeshCollider` objects that use generated collider meshes under `Assets/Generated/CollisionMeshes`.
- Verified Play Mode from `LoadingScene`: fresh log tail loads `LevelForest` without `distance between any 2 vertices`, `NullReferenceException`, or MirraSDK not-implemented warnings.

### 2026-05-04 (second smoke pass)

- Re-ran Unity smoke after collider cleanup and editor API cleanup.
- Removed Unity 6 obsolete API warnings from `LanguageButtonEditor`, `TCKInitialize`, and `TCKPrefabCreator`.
- Verified latest compile succeeds and the fresh Play Mode pass has no runtime exceptions, no large-triangle physics warnings, and no MirraSDK not-implemented warnings.
- Remaining non-runtime Unity/package noise: ProBuilder editor `ExtensionOfNativeClass`, ProBuilder `[SerializeReference]` shape serialization warnings, and Unity editor font reference reload warning.

### 2026-05-04 (egg temporary UI fallback)

- Reviewed the egg feature wiring: `Lobby` has egg nests and animal placement points, and `GameCanvas` already contains selection panels.
- Added `EggTemporaryUIFactory` as a runtime fallback for egg/animal selection panels when designer UI references are missing.
- `EggNestSelectionPanel` and `AnimalPlacementSelectionPanel` now auto-create a temporary header, grid, slot template, empty state, and close button only when the serialized references are not assigned.
- `EggNestSelectionSlot` and `AnimalPlacementSelectionSlot` now expose temporary reference binding so generated slots reuse the same selection logic as prefab slots.
- `EggStorageDisplay` now auto-loads `Resources/Eggs/EggHatchingCatalog` if the catalog is not assigned in the inspector.
- Explicit passive income values were added to `EggHatchingCatalog.asset` for chicken and condor eggs.
- Verified Unity compile: Tundra build success. Ran temporary UI smoke via Unity Bridge: `Egg temporary UI smoke OK`.

### 2026-05-05 (profession temporary UI + catalog)

- Reviewed profession feature wiring: code exists, but scenes/prefabs still have no `ProfessionSelectionPanel` or `ProfessionNpcPoint` references.
- Added `Assets/Resources/Professions/ProfessionCatalog.asset` with 4 testable professions: `yunga`, `mechanic`, `hunter`, `trader`.
- Added `ProfessionTemporaryUIFactory` so `ProfessionSelectionPanel` can auto-create a temporary replaceable UI when inspector references are missing.
- Added `ProfessionTemporaryUIBootstrap`: in `Lobby`, if no real profession panel exists, it creates a temporary panel and floating `Profession` button.
- Existing designer-driven panel flow remains supported: assigned inspector references take priority over generated temporary UI.
- Verified Unity compile: Tundra build success. Ran temporary UI/catalog smoke via Unity Bridge: `Profession temporary UI smoke OK: count=4, current=yunga`.

### 2026-05-05 (balance Google Sheets sync)

- Added editor-only balance sync window at `Tools/Balance/Google Sheets Sync`.
- Export/import covers:
  - `EggHatchingCatalog.asset` -> `Eggs` sheet.
  - `ProfessionCatalog.asset` -> `Professions` sheet.
  - `BoostItem` assets under `Assets/Scripts/LevelStat/Boosters` -> `Boosters` sheet.
- Runtime balance source remains local ScriptableObject assets; Google Sheets is only an editor workflow for designers/tuning.
- Import updates text/numeric/bool balance fields and intentionally leaves object references such as prefabs, icons, and starter item prefabs local to Unity assets.
- Default sheet id and credentials path are reused from the existing localization integration for now. Follow-up: move Google service account credentials out of `Assets/Resources`.
- Verified Unity compile: Tundra build success.

### 2026-05-05 (prod save compatibility guard)

- Treated commit `97063ad` as the last production save baseline.
- Added explicit profession choice tracking to `ProfessionState`.
- Profession passives, starter items, and start currency bonuses now apply only after the player explicitly selects a profession.
- This keeps old production saves from receiving hidden profession balance changes when the profession feature is introduced or retuned later.

### 2026-05-05 (Unity MCP cleanup, historical)

- Compared the project-local `com.unity-bridge` package with Unity's official MCP direction in Unity AI.
- Removed the custom `Tools/unity-bridge` package from the project and from package manifest/lock.
- Official Unity MCP should be enabled through Unity AI/AI Assistant flow in Unity 6.3+, not pinned manually in `manifest.json`; direct `com.unity.ai.assistant@2.7.0` manifest pin was rejected by Unity Package Manager.
- Follow-up check: official Unity MCP configured correctly for Codex, but Unity rejects the direct Codex connection with `Your Unity plan doesn't include MCP connections. Upgrade your Unity plan to add more.` Current direct connection limit is 0, so official MCP is not usable from Codex on this Unity account until the plan/entitlement allows MCP direct connections.
- Historical decision at that time: restored the old project-local `com.unity-bridge` package and switched Codex back to the old `unity-mcp-advanced` server targeting `http://localhost:7778`. Superseded on 2026-05-09 by Ivan Murzak Unity-MCP on `http://localhost:22348`.
- Bridge improvement backlog:
  - Add read-only project/package diagnostics: Unity version, active scene, package list, compile state, console errors/warnings.
  - Add asset search/read helpers for ScriptableObjects, prefabs, scenes, materials, and addressable-like references.
  - Add safer editor actions: load/save scene, refresh asset database, ping/select object, capture game/scene view, play-mode smoke test.
  - Add guarded write actions later: create/update assets and prefabs through explicit endpoints with dry-run output first.
- Historical old-bridge expansion:
  - `/api/project_status`
  - `/api/console`
  - `/api/assets_find`
  - `/api/asset_read`
  - Updated `Tools/unity-bridge/mcp-wrapper.ps1` and smoke/docs for these checks.
- Added matching direct MCP tools to `E:/GitFork/unity-mcp-advanced/unity-mcp/tools/unity.js`:
  - `unity_project_status`
  - `unity_console`
  - `unity_assets_find`
  - `unity_asset_read`
  - Requires restarting Codex after the Node MCP server file changes.
- Recommended official MCP setup for Codex:
  - Keep Validation Level at `standard`.
  - Keep Show Debug Logs off unless diagnosing MCP itself.
  - Configure the `Codex` integration from the Unity MCP Server window when Codex needs direct Unity Editor access.
  - Enable only the minimal tool set by default: RunCommand, GetConsoleLogs, Camera/SceneView captures, FindInFile, ReadResource, ManageScene, ManageGameObject, ManageAsset, ValidateScript. Turn on destructive/editing tools such as ApplyTextEdits/DeleteScript only when needed for a specific task.

### 2026-05-05 (Unity 6.3 regression + credentials cleanup)

- Ran a short Unity regression on `6000.3.9f1`:
  - `AssetDatabase.Refresh` completed after fixing the bridge executor references.
  - Play Mode from `LoadingScene` loaded `LevelForest`.
  - Play Mode stop returned to `LoadingScene`.
  - Unity Console stayed clean: 0 errors, 0 warnings.
- Fixed the project-local Unity Bridge dynamic executor for Unity 6.3 by adding `netstandard.dll` to generated-code compiler references.
- Verified save-safe defaults for new feature storage:
  - Empty/old egg state loads as empty inventory/nests.
  - Empty/old profession state normalizes to default `yunga` with one unlocked profession.
- Moved Google service account credentials out of `Assets/Resources` to ignored local path `UserSettings/Google/credentials.json`.
- Updated balance and localization Google tools to use `UserSettings/Google/credentials.json` by default.
- Google Sheets export/import was not run automatically because it writes external spreadsheet data; run `Tools/Balance/Google Sheets Sync` explicitly when ready.

### 2026-05-05 (Unity 6.3 material shader repair)

- After Unity 6.3 migration, many scene renderers appeared magenta because 86 material assets referenced missing shader GUID `1ccecd9b89b8a4f14bfb64f29ddfcc81`.
- Project is currently Built-in Render Pipeline (`m_CustomRenderPipeline: 0`) and has no URP package installed, so the affected materials were repaired by assigning supported built-in `Standard` shader.
- Verified in Play Mode from `LoadingScene` into `LevelForest`:
  - Scene material scan: `badRefs=0`, `uniqueBad=0`.
  - Unity Console: 0 errors, 0 warnings.
  - Game screenshot no longer shows magenta world materials.

### 2026-05-05 (egg loop v2 design sync)

- Re-synced egg documentation with the intended gameplay loop:
  - eggs are run pickups stored outside ordinary inventory;
  - lobby nests hatch eggs after win/lose return;
  - each egg can roll several animal variants;
  - ready animals should wait at nests for manual collection;
  - animals go to separate animal inventory;
  - boat placement gives run buffs;
  - merge station combines 2 identical unplaced animals of the same stage into 1 higher-stage animal;
  - animals should support 3-4 stages and max-stage merge lockout.
- Replaced the mojibake egg docs with readable UTF-8 docs:
  - `Docs/EGG_SYSTEM_SPEC.md`
  - `Docs/EGG_SYSTEM_SETUP.md`
- Key implementation gaps now documented:
  - migrate animal storage from `eggId` to `animalId + stage`;
  - add hatch result pools;
  - disable/replace production auto-collect for ready nests;
  - add run buff aggregation;
  - add merge service/UI;
  - extend balance pipeline for hatch weights, stages, merge, and buffs.

### 2026-05-05 (egg loop v2 implementation pass 1)

- Implemented additive egg save model v2 while keeping storage key `EggFeatureState_v1`:
  - `AnimalInventoryEntry` now supports `animalId + stage + amount`.
  - `PlacedAnimalState` now supports `animalId + stage`.
  - Old `eggId` animal saves migrate in memory to stage 1 without deleting unknown ids.
- Extended `EggHatchingCatalog`:
  - egg hatch result pools;
  - `AnimalDefinition`;
  - stage definitions;
  - per-stage tint, prefab, merge VFX reference, and run buff data.
- Updated `EggHatchingManager`:
  - incubation stores a stable rolled `hatchedAnimalId`;
  - ready nests mark `isReady` and wait for manual collection;
  - collect adds `(animalId, stage)` to animal inventory;
  - boat placement consumes/returns staged animals;
  - merge API combines two identical unplaced animals into one higher-stage animal;
  - spawned animals can receive stage tint.
- Added `EggAnimalBuffService` and wired animal buffs into the same gameplay stat points as professions: movement, max health, experience, sale reward, fuel, boat speed, melee, ranged, and reload.
- Added temporary merge interaction:
  - `AnimalMergePoint`;
  - `AnimalMergeSelectionPanel`;
  - `AnimalMergeTemporaryUIBootstrap`, which creates a replaceable `Merge Pets` button/panel in `Lobby` when no designer merge UI exists.
- Seeded `EggHatchingCatalog.asset` with initial `chicken` and `condor` animals, 3 stages each, and conservative test buffs.
- Verification:
  - Unity compile clean.
  - Model smoke: roll/migration/merge passed.
  - Play Mode from `LoadingScene` loads `LevelForest`, stop returns to `LoadingScene`.
  - Unity Console: 0 errors, 0 warnings.

### 2026-05-05 (save scope and stat modifier audit)

- Audited the existing skill-card system:
  - cards are implemented as `LevelStat` boosters (`BoostItem`, `LevelStatManager`, `Stats`);
  - selected card bonuses are run-save through `SaveKey.BoostType`;
  - pending level-up choices are run-save through `SaveKey.LevelUp`;
  - both are reset at run end by `GameManager.EndGame()` -> `LevelStatManager.DeleteProgress()`.
- Confirmed current feature save scopes:
  - eggs/animals/nests/placed animals use meta-save key `EggFeatureState_v1`;
  - professions use meta-save key `ProfessionState_v1`;
  - neither is cleared by the normal win/lose run reset.
- Added `Docs/SAVE_SCOPE_AND_STAT_MODIFIERS.md`:
  - documents run-save vs meta-save ownership;
  - records that eggs, animals, professions, and future classes must be permanent meta-save;
  - recommends a unified `RunStatService` before adding class mechanics, so run cards, professions, animals, and classes share one deterministic stat pipeline.

### 2026-05-06 (profession loop v2 design sync)

- Rewrote profession docs as readable UTF-8:
  - `Docs/PROFESSION_SYSTEM_SPEC.md`
  - `Docs/PROFESSION_SYSTEM_SETUP.md`
- Updated target profession loop:
  - new players start with no active profession;
  - player can start a run without a profession;
  - random profession unlock spends soft currency, currently expected as coins;
  - any specific profession can be permanently bought directly;
  - direct buy uses real money on purchase-capable platforms;
  - direct buy falls back to soft currency on platforms without purchases when allowed;
  - professions are configured through ScriptableObjects;
  - selected profession gives starter items and passive run stats.
- Documented current implementation gaps:
  - current code auto-unlocks a default profession via `defaultUnlocked`;
  - current random unlock is gem-priced;
  - direct specific-profession purchase is not implemented yet;
  - no-profession state needs to be preserved as valid meta-save state.

### 2026-05-06 (profession loop v2 implementation)

- Implemented no-profession as a valid meta-save state:
  - `ProfessionState.Normalize` now keeps `hasExplicitProfessionChoice == false` as no selected profession;
  - old explicit selected professions still normalize and survive.
- Extended `ProfessionDefinition` with v2 unlock/purchase data:
  - random unlock pool flag and weight;
  - direct soft-currency cost/type;
  - real-money `purchaseProductId`;
  - soft-currency fallback flag for platforms without purchases.
- Updated `ProfessionService`:
  - selectable no-profession state;
  - random unlock with configurable soft currency instead of hardcoded gems;
  - weighted random unlock pool;
  - direct soft-currency unlock;
  - purchase-complete unlock path.
- Updated `ProfessionSelectionPanel` and temporary UI:
  - first carousel entry is "No profession";
  - random unlock button is separate from direct buy;
  - direct buy uses real purchase when available, otherwise soft-currency fallback when allowed.
- Updated `Tools/Balance/Google Sheets Sync` profession export/import columns for the new unlock and direct-buy fields.
- Verification:
  - Unity compile clean after `AssetDatabase.Refresh`;
  - Unity Console: 0 errors, 0 warnings;
  - model smoke: catalog loads, no-profession state is valid, random pool is available.

### 2026-05-06 (manual Unity integration checklist)

- Added and then rewrote `Docs/UNITY_FEATURE_INTEGRATION_CHECKLIST.md` as a Russian step-by-step Unity instruction for:
  - baseline regression;
  - save-scope checks;
  - egg scene wiring and smoke;
  - profession scene wiring, economy setup, and smoke;
  - run card reset testing;
  - Google Sheets balance sync;
  - Unity Bridge/MCP checks;
  - material regression after Unity 6 migration;
  - release readiness.

### 2026-05-06 (egg catalog inspector refactor)

- Refactored egg configuration away from one large inline catalog:
  - `EggHatchingCatalog` is now only the top-level registry;
  - each egg is a separate `EggDefinition` asset;
  - each animal is a separate `AnimalDefinition` asset;
  - egg hatch results reference animal assets directly instead of typed animal ids.
- Ids are generated from asset names:
  - `egg_chicken.asset` -> `egg_chicken`;
  - `chicken.asset` -> `chicken`.
- Removed unreleased legacy animal fields and old V2 naming from the egg runtime model.
- Rebuilt current egg resources:
  - `Assets/Resources/Eggs/Definitions/Eggs/egg_chicken.asset`
  - `Assets/Resources/Eggs/Definitions/Eggs/egg_condor.asset`
  - `Assets/Resources/Eggs/Definitions/Animals/chicken.asset`
  - `Assets/Resources/Eggs/Definitions/Animals/condor.asset`
- Updated egg balance sync:
  - exports egg asset path, id, title, incubation, skip cost, and hatch result weights;
  - imports editable egg fields back into existing egg assets.
- Verification:
  - Unity compile clean after refresh;
  - Unity Console: 0 errors, 0 warnings;
  - catalog smoke: 2 eggs, 2 animals, `egg_chicken` and `chicken` resolve, roll returns `chicken`.

### 2026-05-06 (inline egg catalog editor)

- Added custom editor for `EggHatchingCatalog`.
- The catalog inspector now lets egg and animal references expand inline, so referenced `EggDefinition` and `AnimalDefinition` assets can be edited directly from the catalog.
- Inline editor shows generated id as read-only and keeps the real data inside separate asset files.
- Added a create button for empty catalog slots so new egg/animal config assets can be created and assigned from the same inspector flow.
- Verification:
  - Unity compile clean after refresh;
  - Unity Console: 0 errors, 0 warnings.

### 2026-05-06 (zoo prefab egg/animal configs)

- User added animal prefabs under `Assets/_models/ZOO/PrefabZoo` and egg prefabs under `Assets/_models/ZOO/PrefabEgg`.
- Generated separate `AnimalDefinition` assets for the zoo prefabs in `Assets/Resources/Eggs/Definitions/Animals`.
- Generated separate `EggDefinition` assets for egg prefabs in `Assets/Resources/Eggs/Definitions/Eggs`.
- Kept `egg_chicken`, `egg_condor`, and `condor` ids for compatibility with existing test content/save ids.
- Added `Tools/Eggs/Sync Zoo Prefabs To Catalog` and a matching button in the `EggHatchingCatalog` inspector so future prefab additions can be synced from Unity.
- Local file verification:
  - catalog references 13 eggs and 19 animals;
  - all GUIDs referenced by `EggHatchingCatalog.asset` exist in generated asset `.meta` files.
- Unity refresh was not verified in this pass because the local Unity Bridge stopped responding on port `7778`.
### 2026-05-09 (IvanMurzak Unity-MCP adopted)

- Switched the project Codex/Unity workflow to Ivan Murzak Unity-MCP:
  - Unity package: `com.ivanmurzak.unity.mcp` pinned to `0.71.0` from GitHub;
  - Codex config: `.codex/config.toml`;
  - generated skills: `.agents/skills`;
  - local MCP URL: `http://localhost:22348`.
- Retired the old project-local `com.unity-bridge` package and `Tools/unity-bridge` wrapper. New work should use `npx unity-mcp-cli` and the generated MCP skills, not port `7778`.
- Added `Docs/UNITY_MCP_WORKFLOW.md` with the current quick checks.

### 2026-05-09 (Burst resolver aliases for Unity-MCP NuGet DLLs)

- Fixed a Burst `Failed to find entry-points` error caused by Cecil resolving NuGet dependencies by assembly name.
- IvanMurzak Unity-MCP installs versioned DLL filenames under `Assets/Plugins/NuGet`, for example `McpPlugin.Common.6.2.1.dll`, while Burst searches for the manifest assembly name, for example `McpPlugin.Common.dll`.
- Added disabled Unity import aliases named by assembly name so Burst can resolve dependencies without Unity importing duplicate assemblies.
- Verification:
  - `AssetDatabase.Refresh` completed;
  - Unity state reports `IsCompiling=false`;
  - Unity Console is empty after clearing and refreshing.
