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

### 2026-05-12 (Unity-MCP workflow note)

- Current Unity MCP workflow is IvanMurzak Unity-MCP `com.ivanmurzak.unity.mcp@0.72.0` on `http://localhost:22348`.
- Use `npx.cmd unity-mcp-cli run-tool <tool> --input-file <utf8-no-bom-json>` from PowerShell when direct MCP tools are not exposed in Codex.
- The old Unity Bridge ports `7777`/`7778` are obsolete for current work.

### 2026-07-21 (Unity-MCP extensions update)

- Verified the project through the live Unity-MCP connection at `http://localhost:22348` on Unity `6000.3.9f1`.
- Confirmed the core package and CLI are on `com.ivanmurzak.unity.mcp@0.86.0` / `unity-mcp-cli@0.86.0`.
- Updated the installed MCP extensions to `1.2.28`:
  - `com.ivanmurzak.unity.mcp.animation`;
  - `com.ivanmurzak.unity.mcp.particlesystem`;
  - `com.ivanmurzak.unity.mcp.probuilder`.
- Animation and ParticleSystem resolve from OpenUPM. ProBuilder remains project-embedded and its checked-in package contents were updated from `1.0.72` to the published `1.2.28` package.
- Verification after the domain reload:
  - Unity reports `IsCompiling=false` and `IsUpdating=false`;
  - the MCP registry reports 94 tools;
  - the active scene is `Assets/Scenes/Lobby.unity` and is not dirty;
  - there are no extension compilation errors. The existing `com.mirrasdk.playgama.api/package.png` missing-meta error and obsolete Unity API warnings remain unrelated follow-ups.

### 2026-07-21 (GameDistribution WebGL startup fix)

- Diagnosed the GameDistribution `SyntaxError: Invalid or unexpected token` before Unity startup.
- The rejected Mirra-template build (`ObbyDeadRiver[139]-mirraSDK[5.1.20].zip`) contains Brotli payloads with raw `.br` extensions. Its loader requires the host to return `Content-Encoding: br`; without that header, the browser parses the compressed framework bytes as JavaScript.
- Restored `PlayerSettings.WebGL.decompressionFallback = true` (`webGLDecompressionFallback: 1`) through the live Unity-MCP connection.
- Future Brotli WebGL builds use `.unityweb` payloads with Unity's JavaScript decompressor and no longer depend on GameDistribution's compression headers. The current `PROJECT:Bridge` template and the other pending WebGL/Playgama changes were preserved.
- A control `BuildPipeline.BuildPlayer` did not produce an artifact because Unity's `Unity.ILPP.Runner` stalled during build-time processing. The stalled child process and orphaned MCP server were cleaned up, Unity was restarted, and the editor returned with `IsCompiling=false` / `IsUpdating=false`. No temporary verification scripts remain in `Assets`.


### 2026-10-05 — общий игровой мир кооператива

- В рабочей ветке добавлены общая лодка/водитель/топливо, бонусы экипажа, страницы снимков объектов, атомарное владение и кинематические прокси предметов, attached, личный выброс оружия с патронами, authority AI/HP/горение/ночные спавны и общий RunWon.
- Логические координаты отделены от визуальных аватаров; общий фронт геометрии учитывает всех участников. SDK не изменялись.
- Два Cloud peers подтвердили протокол, включая переход authority страниц. Настоящий Lobby→LevelForest с transport-only вторым peer проверил инвентарь, тестовую рекламу/возрождение и реального Forest-босса до общей победы. Это не ручная приёмка WebGL.
- Карточки и доказательства: Docs/COOP_EDITOR_CHECKS_20261005.md. Постоянные награды/reconnect остаются работой; SharedRunPreviewSave сохраняет изоляцию.
- По просьбе владельца новую сборку в черновик не загружали. Следующий шаг — ИИ: подготовка сборки после окончательной проверки; владелец: затем приёмка двух/четырёх устройств.

### 2026-10-12 — живой возврат и сохранённая соло-ветка (DB-ON-026)

- Реализованы стабильный Fusion PlayerUniqueId, локальная запись для входа в живой исходный забег, атомарный личный снимок на оставшихся клиентах, сохранение владения после обрыва и смены master. Проверка без аватара не поддерживает пустой забег и не создаёт исчезнувшую комнату.
- Потеря сети останавливает локальные действия и предлагает переподключение/явное отделение соло. Соло сначала записывает копию, закрывающую возврат, затем перезагружает уровень локально; JSON дополняет обычное сохранение сидом и изменёнными объектами. Онлайн-мир постоянно не сохраняется.
- Реальные Photon peers подтвердили протокол. Настоящий Forest с изолированным сохранением подтвердил возврат с предметом, магазином, запасом патронов и HP; вторую потерю сети и отделение соло. Отдельный холодный старт подтвердил восстановление сохранённой соло-ветки. Это один игровой мир плюс транспортный товарищ, не приёмка независимых WebGL-клиентов.
- Исправлены гонки уничтожения устаревшего предмета, сжигание инвентарного топлива при восстановлении, неназначенный Runner у аватара возврата и повторное прикрепление персонажа к уходящей лодке. Тестовые ключи и playModeStartScene владельца восстановлены. SDK не менялись.
- Доказательства и ограничения: Docs/COOP_RECOVERY_20261012.md. DB-ON-026 в in_review; постоянная однократная выдача наград ещё относится к DB-ON-021, полное промежуточное состояние атак босса в соло-копии не подтверждено.
- Дополнительно проверены опыт/уровень, стабильная идентичность нового трупа в соло и сохранённая отметка его уничтожения. Финальная WebGL-сборка Succeeded, 0 ошибок. Архив Builds/DeadBoatRecoveryDraft-20261012-f6ad544b.zip, 31,8 MiB; ZIP CRC и ссылки загрузчика проверены.
- Следующий шаг — владелец: загрузить архив в черновик 424778 и проверить два независимых клиента. ИИ: DB-ON-021 и исправления приёмки. Черновик этой правкой пока не обновлялся.


### 2026-10-12 — клиентский пилот постоянных наград (DB-ON-021)

- Добавлены GameManager.GrantVictoryReward и SharedVictoryRewards: общий RunWon, исходный run/player для соло-ветки, отдельный постоянный кошелёк кристаллов/побед/отметок. Обычный соло использует прежнее начисление. SDK не менялся; игровая обёртка MirraSDKSaveProvider мигрирует старые Gems/Wins.
- Клиентская выдача разрешена только в Editor/DEADBOAT_MIRRA_DIAGNOSTICS. Для публичной сборки нужен серверный claim; Photon master не считается доверенным сервером. SDK Save не подтверждает доставку в C#, конкурентные вкладки/устройства не защищены.
- Unity SharedRewardProbe PASS: JSON reload, повтор той же награды/соло, поздний баланс, разные участники/забеги, нулевая награда, ошибки и переполнение. Игровой прогон и WebGL-запись не проверены — главный поток Unity не отвечает. Карточка остаётся in_progress. Доказательства: Docs/COOP_REWARDS_20261012.md.
- Следующий шаг — ИИ: интеграционный прогон и новая тестовая сборка после доступности Unity. Владелец параллельно проверяет предыдущую сборку возврата. Черновик этим изменением не обновлялся; внешний backend/гарантии сохранения — отдельное незавершённое условие задачи.


### 2026-10-12 — игровая проверка наград (продолжение DB-ON-021)

- После восстановления Unity добавлен изолированный SharedRewardGameplayProbe. Итог PASS: Forest-босс, общий RunWon, локальный UI, одна награда/победа, повторы, сохранение комнаты после выхода победителя и повтор через SoloRunBranch с исходным ключом.
- Сохранение владельца не использовано для записи; тестовые лидерборды при SharedRunPreviewSave пропускаются. Проверка VerifyRestored подтверждает исходные ключи return/identity/solo/mode и playModeStartScene после остановки.
- Неуспешные запуски harness сохранены в Docs/COOP_REWARDS_20261012.md; исключены чрезмерный диапазон при искусственном прыжке, прямой вход в Lobby без boot и отсутствие BoatProfile у транспортного клиента. Это не полноценная приёмка двух WebGL и не проверка сохранения на Яндексе.
- DB-ON-021 in_review для клиентского пилота. Следующий шаг — ИИ: новая тестовая сборка; владелец: награды и независимый выход в двух браузерах. Серверный claim по-прежнему нужен для публичной выдачи.

- Дополнительный прогон PASS ordinary solo duplicate UI: повторная проверка награды ограничена коопом/его веткой. Промежуточная сборка намеренно остановлена; окончательная по 75e1e2ee — Succeeded, 0 ошибок. Архив Builds/DeadBoatRewardsDraft-20261012-75e1e2ee.zip: 33 456 064 байта, 28 файлов, CRC/ссылки проверены. SHA256 17f9a50792e81cc56ac32e7d8511b779532f371bd691429c6b656cb94dff4bf7. Черновик не обновляли автоматически во время тестов владельца.
- Следующий шаг — владелец: новый архив в черновик и приёмка наград; ИИ: ACK рекламы/возрождение, затем профиль чрезмерного разделения экипажа (добавленные факты DB-ON-027). Публичный серверный claim остаётся незавершённым.


### 2026-10-13 — DB-ON-026: зависшая проверка после обновления страницы

- По отчёту владельца исправлены неограниченный Shutdown probe, автоповтор с немедленным relock, недоступная отмена, изоляция поздних попыток. Добавлен realtime watchdog стадий и поддержка сетевых кадров при паузе; локальный игровой ввод остаётся блокированным. SDK не менялся.
- Fault-injection SharedRecoveryHangProbe PASS. RunColdRestart настоящего Forest PASS с новым recovery из билета, живым transport peer, сумкой/оружием/патронами/HP/опытом и дальнейшей соло-веткой. Это не браузерный refresh; проверка использует foreground-clock fixture. Причина WebGL без Console не установлена.
- VerifyRestored подтвердил исходные ключи и playModeStartScene. Владелец сообщил, что Unity снова отвечает. Доказательства: Docs/COOP_RECOVERY_20261012.md. DB-ON-026 снова in_review.
- Следующий шаг — ИИ: новый тестовый архив; владелец: повторная приёмка refresh/сеть/отмена/ручной повтор. После неё ИИ возвращается к DB-ON-023/022.


## Сборка исправления возврата — 2026-10-13

Исходники: `e8b0256b`, ветка `codex/online-photon-fusion`. Unity 6000.3.9f1, WebGL, тестовый `DEADBOAT_MIRRA_DIAGNOSTICS`. BuildReport: Succeeded, errors=0, totalSize=33 913 807 байт; начало 09:45:38 UTC / 12:45:38 МСК.

Архив: `Builds/DeadBoatRecoveryFix-20261013-e8b0256b.zip`, 33 444 786 байт (31,90 МиБ), 28 файлов. Проверены CRC всех файлов, корневой index.html, jquery/unity-webview и четыре непустых файла Build, на которые ссылается unityApp.js. SHA256: `c58c0859ca4a8747b2e4d1698494d5dc4bcf3cb057d89ae581c8aac74d54353f`. SDK не менялись. Черновик этой сборкой автоматически не обновлялся.

Следующий шаг — владелец: загрузить архив в черновик Яндекса 424778 и на двух клиентах повторить обновление страницы одного участника, пока другой играет; проверить возврат личного состояния, отмену/таймаут без сети, ручной повтор после восстановления сети и отказ после ухода всех. Задача остаётся in_review до приёмки. ИИ: исправить замечания по результатам; затем продолжить DB-ON-023/DB-ON-022 (сетевое состояние рекламы и воскрешение).


### Черновик обновлён — 2026-10-13

По запросу владельца архив `DeadBoatRecoveryFix-20261013-e8b0256b.zip` загружен через консоль в черновик Яндекса 424778 и сохранён. После обновления страницы подтверждены новый file-id `13952190`, время изменения 13:25:40 МСК и завершённая обработка «Готово». Консоль показывает 32.43 Mb (это отображаемое платформой значение; локальный ZIP — 31.90 МиБ). Публикация/модерация не запускались. Снимок подтверждения: `Builds/RecoveryFix-YandexDraft-20261013.jpg`.

Следующий шаг — владелец: оба клиента перезагрузить на новом черновике и проверить refresh одного игрока при живом товарище, затем отмену/таймаут/повтор. ИИ: разбирать результат приёмки и продолжить DB-ON-023. DB-ON-026 остаётся in_review.


### Попытка браузерной приёмки ИИ — 2026-10-13

Новый черновик открылся в Codex IAB; игровой мир отрисован, меню по P открывается. Первый клик по canvas вызвал JS alert/Unhandled rejection `UnknownError: If you see this error we have a bug. Please report this bug to chromium.` После перезагрузки P вновь открыл меню, но инструментальные координатные клики по «В лобби» и закрытию меню не дали перехода. Причина отказа ввода не установлена: этот результат не доказывает неисправность самих игровых кнопок и не является PASS возврата. Не выполнялась подмена состояния/вызов Unity методов через браузер. Скриншот: `Builds/Recovery-WebGL-InputBlocked-20261013.jpg`.

Подготовлен отдельный Editor-клиент с изолированными сохранениями и foreground-clock fixture, Lobby загрузилось при timeScale=1. Из-за недоступного браузерного действия совместный забег не создавался, refresh в живой комнате не проверен. Play Mode остановлен; VerifyRestored PASS подтверждает возврат owner return/identity/solo/mode keys и playModeStartScene. Код/SDK для этой проверки не изменялись.

Следующий шаг — владелец: проверить refresh одного игрока, пока товарищ остаётся в забеге, на обычных браузерах. ИИ: продолжить DB-ON-023 и разбирать замечания приёмки. DB-ON-026 остаётся in_review.


## Стабильное хранение билета WebGL — 2026-10-13

Владелец подтвердил настоящий сценарий: обычный браузер создал экипаж на двоих, IAB присоединился и поплыл; после перезапуска IAB оказался в начале solo. Это ошибка приёмки, а не одиночный тест.

Билет и Fusion identity ранее сохранялись только в PlayerPrefs. Unity документирует URL-зависимый persistentDataPath WebGL: [Application.persistentDataPath](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/application/persistentdatapath); [PlayerPrefs](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/playerprefs) использует IndexedDB. Каталог iframe IAB действительно менялся при запуске черновика. Потеря именно этого билета в IndexedDB не прочитана и окончательная причина конкретного отказа не установлена.

Добавлен собственный SharedReturnStorage / .jslib: только небольшие билет/идентичность читаются и синхронно записываются в localStorage игрового origin. Это устраняет зависимость этих двух записей от каталога сборки и от асинхронного сброса PlayerPrefs. Сохраняется fallback PlayerPrefs и миграция старого значения при отсутствии браузерной записи. Удаление оставляет пустой маркер, чтобы старый URL не воскресил билет. Смена origin/очистка данных браузера по-прежнему уничтожает доступ к локальному билету; это не облачная сохранённая копия онлайн-мира. SDK, обычные сохранения, большой solo-снимок не менялись.

Диагностика показывает только наличие записи, backend и успешность записи, без комнаты/идентичности/данных игрока. При недоступном browser storage сообщается warning и используется PlayerPrefs. Проверки: `node tools/tests/test_recovery_web_storage.cjs` PASS (свежий JS heap и другой каталог, tombstone против отсутствия, UTF8, 64-bit identity строкой, origin isolation, denied/quota); Unity .jslib импортирован как PluginImporter; компиляция прошла; SharedRecoveryGameplayProbe.RunColdRestart PASS (тот же PlayerRef/золото/сумка/патроны/HP/опыт и separate solo); VerifyRestored PASS. Editor-тест по-прежнему использует foreground fixture, один мир и transport peer; Node тест не является проверкой localStorage реального IAB.

Следующий шаг — ИИ: WebGL-сборка и тестовый черновик. Владелец: после загрузки новой сборки повторить refresh при живом товарище; при отказе нужны строки `[Shared recovery storage]` до/после перезапуска. Задача in_review после технических проверок, не done.


### WebGL с браузерным хранилищем — 2026-10-13

Исходники `539d3361`. Unity 6000.3.9f1: BuildReport Succeeded, errors=0, totalSize=33 918 089 байт, начало 13:51:42 МСК. Проверено наличие обеих DB_ReturnStorage функций и namespace хранения в распакованном готовом framework.js.

Архив `Builds/DeadBoatRecoveryStorage-20261013-539d3361.zip`: 33 450 810 байт, 31,90 МиБ, 28 файлов, CRC PASS, index.html/все четыре ссылки Build/jquery/unity-webview проверены. SHA256 `e8c8d89da521e1b4095278b16b4d13e8d3dbe2f3b6906bbf1035d5b3ccdbe0ab`. SDK не менялся. Следующий шаг — ИИ: загрузить и сохранить черновик; владелец: повторить кооп/refresh на этой версии.


### Новая версия в черновике и smoke — 2026-10-13

Архив 539d3361 загружен и сохранён в черновике 424778, file-id `13952646`; после обновления консоли подтверждён статус «Готово», последнее изменение 14:18:03 МСК, отображаемый размер 32.44 Mb. Публикация/модерация не выполнялись. Подтверждение: `Builds/RecoveryStorage-YandexDraft-20261013.jpg`.

Smoke IAB: первый запуск сразу после обработки временно перенаправился на 404; повтор открыл игру. Loader `9793fb0298d61119519d366104744429.loader.js` совпал с новым архивом. Console: `[Shared recovery storage] Startup record present=False; backend=browser-origin.` Ошибок вызова JS-моста не было; встретились прежние ошибки аналитики SessionsStarted/лимита leaderboard, не объявленные причиной возврата. Проверена работа чтения моста в настоящем WebGL, не запись нового кооп-билета и не refresh при живом втором игроке. Временный smoke-клиент закрыт.

Следующий шаг — владелец: обновить оба клиента, создать НОВЫЙ заплыв на двоих, немного поплыть, перезапустить один клиент, оставив второй в игре. Потерянный билет предыдущей версии восстановить из ничего невозможно. При отказе передать строки `[Shared recovery storage]` перед перезапуском и после него. ИИ: доработать по результату приёмки; DB-ON-026 остаётся in_review.


## Ожидания восстановления по Unity player loop — 2026-10-13

В новом реальном WebGL-тесте владельца билет читается: `Startup record present=True; backend=browser-origin`. StartGame завершился, Fusion добавил игрока, затем неоднократно `Timeout at receive run state`. Это точнее локализует отказ; наличие самого SharedDepartureState на клиенте до таймаута не было залогировано.

Игровые циклы ожидания реплики и загрузки сцены содержали Task.Delay. Заменены на Awaitable.NextFrameAsync; короткая выдержка abandon и лимиты Shutdown используют SharedRunFrameWait.Delay по Time.realtimeSinceStartupAsDouble. C# таймеры/ThreadPool для этих ожиданий больше не нужны. Источники: [ограничения C# Web](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/webgl/develop/web-multithreading/intro), [Awaitable](https://docs.unity.cn/6000.0/Documentation/Manual/async-await-support.html). Несовместимость старого таймера с конкретной средой IAB рассматривается как вероятный механизм, не как доказанный самим screenshot факт.

Добавлена диагностика входа транспорта, наличия реплики, фазы playing и оставшегося playing crew без комнат/ID. Отсутствующая/неполная реплика при ожидании больше не удаляет билет как доказанный завершённый забег; завершённый/чужой/недопустимый забег и отсутствие других участников по-прежнему отклоняются.

Проверки: SharedRecoveryHangProbe PASS, включая новый real-time frame wait при timeScale=0, таймаут/отмену/изоляцию старых попыток; SharedRecoveryGameplayProbe.RunColdRestart PASS с `State received=True; playing=True; otherCrew=True`, восстановлением личных предметов/патронов/HP/опыта и отдельной solo-ветки. VerifyRestored PASS. Ограничения Editor: один мир, transport peer и foreground fixture; это не реальный WebGL refresh. SDK не изменены.

Следующий шаг — ИИ: собрать/загрузить черновик и проверить автоматическое получение состояния на оставленном живом забеге. Владелец: подтвердить сам возврат и личные данные. Статус in_review после технических проверок.
