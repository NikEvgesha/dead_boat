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


### Сборка ожиданий по кадрам — 2026-10-13

Исходники `eee34894`, Unity 6000.3.9f1 WebGL diagnostics: BuildReport Succeeded, errors=0, totalSize=33 957 354 байт, начало 14:39:58 МСК. Архив `Builds/DeadBoatRecoveryFrames-20261013-eee34894.zip`: 33 482 900 байт / 31,93 МиБ, 28 файлов, CRC PASS, корневой index.html, jquery/unity-webview и 4 непустые ссылки Build проверены. SHA256 `b268b7c400c069b0e699e8f9889abccae88db92b9e5bb6cd1ea8b2034e41c90e`.

Следующий шаг — ИИ: сохранить черновик и проверить получение реплики на оставленном забеге; владелец: подтвердить полный возврат/личное состояние. SDK не менялись, задача in_review.


### Проверка живого забега в IAB — 2026-10-13

Новый архив eee34894 сохранён в черновике 424778, file-id `13953031`, статус «Готово», последнее изменение 15:08:44 МСК, отображаемый размер 32.48 Mb. SDK/публикация/модерация не изменялись. После первого временного 404 повторный запуск открыл новую версию: loader `e1d1b0d1c9ef104871740e55e4b08266.loader.js`.

На том же оставленном владельцем живом забеге и сохранённом билете WebGL автоматически получил состояние: в 15:09:58.284 МСК Joined transport, в 15:09:58.331 — `State received=True; playing=True; otherCrew=True`. В UI «Экипаж ещё в пути. Можно вернуться к команде». Прежний receive-state timeout на этой попытке не повторился. Это фактический результат IAB новой версии, не только Editor-проверка. Снимок: `Builds/RecoveryFrames-LiveState-20261013.jpg`; подтверждение черновика: `Builds/RecoveryFrames-YandexDraft-20261013.jpg`.

Координатный клик ИИ по кнопке внутри Unity-canvas снова не инициировал переход. Владелец приглашён нажать «Вернуться в забег» в готовой вкладке; полный возврат/личное состояние пока не объявлены принятыми. Следующий шаг — владелец: нажать возврат, проверить появление у текущей лодки и предметы/патроны/HP. ИИ: проверить журнал перехода и исправить дальнейший отказ, если появится. Статус in_review.


### Владелец подтвердил возврат — 2026-10-13

После загрузки eee34894 IAB получил состояние оставленного живого забега (State received/playing/otherCrew=True). Владелец нажал «Вернуться в забег» и ответил: «Все работает правильно !». Журнал той же версии подтверждает Scene loaded; connected=True; players=2; avatars=2; player=True; camera=True. Успешный полный возврат в этом браузерном сценарии принят владельцем; прежнее зависание receive run state не воспроизвелось.

Отдельные сценарии всего DB-ON-026 (обрыв интернета, уход master/всех, гонка ухода последнего, выбор solo и сверка каждой личной характеристики на двух браузерах) не объявляются заново проверенными этим кратким ответом. Карточка остаётся in_review для оставшейся приёмки; конкретная ошибка загрузки состояния и возврата в живой забег закрыта в данном тесте.

Следующий шаг — ИИ: DB-ON-023, сетевое состояние неуязвимости во время рекламы и одну секунду после; владелец: при следующих кооп-тестах пройти оставшиеся сценарии DB-ON-026. SDK не менялись, исходники/документация передаются в рабочей онлайн-ветке.


## DB-ON-058 — обработка отказа pointer lock, 2026-10-13

Владелец сообщил периодический Unity alert Chromium UnknownError. Добавлена узкая защита native canvas.requestPointerLock в собственной WebGL-обвязке; ожидаемые DOMException логируются, прочие ошибки не подавляются. SDK/payload не изменяются. JS проверки PASS; настоящий postprocess Unity выполнен дважды, подключение идемпотентно и перед loader. Следующий шаг — ИИ: обновить черновик и проверить warning источника; владелец: приёмка мыши при меню/рекламе/возврате. Подробности в DB-ON-058.


## Архив и реальный браузерный API — 2026-10-13

Архив `Builds/DeadBoatCursorGuard-20261013-7eceb227.zip`: 33 483 848 байт / 31,93 МиБ, 29 файлов, CRC PASS. SHA256 `fefbaca5b9c9f1a31473c0e8a8e8664640a9d0668233728b14428f60137cf39b`. Все прежние файлы кроме index.html побайтно совпали с архивом eee34894; добавлен guard. Это переупаковка проверенного payload, не новая компиляция C#/wasm.

Отдельная локальная HTML-проверка в том же Codex IAB вызвала настоящий canvas.requestPointerLock. Браузер дважды отказал UnknownError, guard дважды записал `[Dead Boat cursor] Pointer lock rejected (UnknownError)`, UI Unhandled: 0. Повторный запрос действительно дошёл до браузера; alert не появился. Это подтверждённое поведение native API в IAB; проверка не заменяет приёмку FPS-управления внутри Яндекс iframe. Guard обрабатывает отказ, не исправляет сам Chromium и не утверждает, что мышь захвачена после ошибки. Тестовая вкладка закрыта, свой локальный сервер остановлен. Подтверждение: `Builds/PointerGuard-NativeProbe-20261013.jpg`.

Архив передан в черновик 424778 (file-id 13953129) и сохранён. Следующий шаг — ИИ: дождаться «Готово», зафиксировать статус. Владелец: обновить игру и проверить клики/меню/возврат после рекламы; при повторном alert передать свежий Console. In_review.


Черновик обработки завершён: file-id 13953129, «Готово», последнее изменение 15:28:25 МСК, размер консоли 32.48 Mb. Подтверждение `Builds/PointerGuard-YandexDraft-20261013.jpg`. Рабочая игровая вкладка не перезапускалась автоматически, чтобы не прерывать текущий забег.

Следующий шаг — владелец: обновить игровой клиент после текущего забега и проверить клики по игре, открытие/закрытие меню и возврат после рекламы; ожидаемый отказ курсора должен оставаться warning `[Dead Boat cursor]`, без Unity alert. ИИ: DB-ON-023, а повторный alert разбирать по свежему Console. Приёмка игры ещё ожидается, in_review.

## 2026-10-13 — кооператив: часы и личный урон

Время суток переведено на сохраняемый сетевой снимок Shared master, исправлено прямое ночное освещение при возврате. Зомби бьёт выбранную цель без требования локального физического коллайдера; адресный RPC урона идёт владельцу аватара. SDK не изменены. Unity compilation PASS, проверки геометрии/освещения PASS, SharedRecoveryProbe с двумя Cloud peers PASS (ночные часы при возврате и смене ведущего). Личное HP в браузерах ожидает проверки владельца; DB-ON-020 in_review. Следующий шаг: ИИ — WebGL сборка; владелец — отдельный урон/лечение и возврат ночью.

WebGL d2d5a84f успешно собран и загружен в тестовый черновик 424778 (file-id 13953471, сохранено 16:09:50). Архив ClockDamage CRC PASS, 31.91 MiB. Статус после сохранения — обработка; браузерный инструмент затем перестал отвечать, Ready не подтверждён. Следующий шаг — владелец: после обработки обновить обе вкладки и принять независимое HP / возврат ночью.

## 2026-10-13 — возврат после смены ведущего

Владелец: после refresh ведущего живой клиент стал master; сообщение «экипаж завершил забег». GameClosed больше не удаляет билет и не выдаётся за завершение. При получении authority master принудительно публикует IsOpen. Диагностика Join rejected пишет только ShutdownReason. SDK не менялись. SharedRecoveryHangProbe PASS (закрытая комната сохраняет возврат/соло), SharedRecoveryProbe PASS (принудительно закрытый вход перед takeover; исходный master вернулся с прежним PlayerRef, CanReturn и живым другим участником). WebGL замечание ещё требует повторной приёмки; DB-ON-026 in_review.

MasterReturn 6efcfeba: WebGL Succeeded/0 errors, ZIP 31.88 MiB/29 files/CRC PASS. Загружен и сохранён тестовый черновик 424778, file-id 13953839, 16:45:58 МСК; статус обработки подтверждён, Ready ещё не подтверждён. SDK не трогали. Следующий шаг: Яндекс обрабатывает; владелец обновляет обе страницы и в новом экипаже повторяет refresh ведущего, клиент остаётся играть. ИИ исправляет повторные замечания по конкретному ShutdownReason.

## 2026-10-13 — монеты и карточки при переподключении

Личный прогресс выделен в отдельный RPC без изменения подтверждённой сумки; capture запрашивается на кадре после изменения монет/Stats/числа выборов. Баланс берётся из CurrencyManager. Восстановление карточек применяет внутренний счётчик и UI. SDK не менялись. SharedRecoveryProbe PASS; реальный холодный Forest-возврат PASS после изменения монет/карточек отдельно от сумки (205→237, HP карточек 30→35, выборы 1→2) и сброса локальных менеджеров до возвращения. Инвентарь/патроны/опыт и последующий solo не сломались. DB-ON-026 in_review, WebGL приёмка ожидает владельца.

WebGL PersonalProgress fd82aa5d успешно собран (0 errors), ZIP 29 файлов / 31.92 MiB / CRC PASS. Яндекс подтвердил upload file-id 13954288; Save отправлен. Последующая проверка консоли и её штатный Retry дважды получили таймаут площадки, сохранение draft mapping/Ready не подтверждены. Локальный архив `Builds/DeadBoatPersonalProgress-20261013-fd82aa5d.zip`. Следующий шаг: площадка восстанавливает ответ; владелец после готовности повторяет возврат с монетами/карточками; ИИ проверяет статус доставки и замечания. SDK не менялись.

Повторная проверка по запросу владельца: PersonalProgress сохранён в черновике 424778, file-id 13954288, «Готово» enabled, время изменения 17:46:39 МСК. Таймаут консоли больше не мешает подтверждению доставки. Следующий шаг — владелец: приёмка монет/карточек/оставшихся выборов после переподключения в новом экипаже; ИИ: исправления по результатам.

Владелец проверил PersonalProgress в черновике и сообщил «вроде все окей»: исправление потери монет/карточек принято в проверенном сценарии. DB-ON-026 остаётся in_review для отдельных оставшихся сценариев. Следующий шаг — ИИ: сетевой протокол рекламной защиты DB-ON-023; владелец затем принимает настоящую рекламу и возрождение вдвоём.


## 2026-10-14 — подтверждаемая рекламная защита

DB-ON-023: lease с ACK authority перед провайдером, сетевые 180 секунд максимум и секунда после, token от дублей/старых callbacks, освобождение руля и authority-запрет действий/урона. AdsManager имеет однократное завершение и watchdog; рекламная пауза отделена от меню. Правки только игрового кода и своего адаптера; SDK не менялись.

SharedAdProtocolProbe (два Cloud peer) PASS; SharedAdGameplayProbe (настоящий LevelForest и тестовый AdsProvider) PASS, воскрешение ровно один раз и адресный урон после истечения grace. Пауза меню PASS; восстановление owner keys/start scene PASS. Подробности `Docs/COOP_AD_CHECKS_20261014.md`. Карточки DB-ON-022/023 in_review. Во время проверки восстановлены содержательные записи повреждённого локального Editor.log вне репозитория; нулевой мусор вызывал OutOfMemoryException в Unity Search, после очистки инструменты заработали.

Следующий шаг — ИИ: WebGL сборка/архив; владелец: после доставки проверить настоящую рекламу вдвоём в новом экипаже. Приёмка Яндекс рекламы ещё не выполнена.

WebGL aecb8ea1 Succeeded / 0 errors, ZIP AdProtection 29 файлов / 31,82 MiB / CRC PASS.

Яндекс: сохранено в тестовый черновик 424778, file-id 13974642, изменение 18:06:55 МСК 14.10.2026. После обработки подтверждено `Готово` enabled и соответствие ссылки скачивания новому file-id. Подтверждение: `Builds/AdProtection-YandexReady-20261014.jpg`. Следующий шаг — владелец: обновить оба клиента, создать новый экипаж и проверить настоящую рекламу/воскрешение; ИИ: исправления по результатам. Публикация/модерация не выполнялись.


## 2026-10-14 — предметы между снимками и добровольный возврат

DB-ON-027: профиль четырёх Cloud runner в одной Editor-сцене; микрозамер 200 LateUpdate на 132 вещах 372.517 → 105.581 мс. Полная сверка 10 Гц, интерполяция прокси покадрово, внеочередная сверка при подборе/смене роли/режима. Большое расхождение сохраняет низкий FPS и риск памяти; задача не закрыта. Тест крепления выявил двойную физическую интерполяцию Rigidbody: она отключается для сетевого/кинематического предмета и восстанавливается при локальной симуляции.

Владелец выбрал кнопку возврата к лодке за rewarded. DB-ON-059: добровольный H/touch, начальный порог 750 м, актуальная палуба после просмотра, запрет физического груза, защита с ACK, сохранение личного прогресса. Занятая палуба сохраняет кредит только в открытом клиенте. Найдено и исправлено неснятие рекламной паузы/курсора в игровом AdsManager. SDK не менялись. Полный SharedItemGameplayProbe с двумя Cloud runner PASS; окно Game View просмотрено. Отчёт `Docs/COOP_PERFORMANCE_20261014.md`. DB59 in_review: реальная реклама/мобильный ввод ожидают владельца после новой сборки, черновик пока прежний.

Следующий шаг — ИИ: завершить протокольный regression и подготовить объединённую сборку; владелец позже проверяет всё вместе. Бюджет удерживаемого мира остаётся отдельной необходимой работой ИИ.

Финальный SharedReadyBarrierProbe 65159–65186 PASS: предметы, топливо, оружие/патроны, страницы/очистка, враги/победа, бонусы, лодка/master transfer, ready/leave/timeout. Общий gameplay PASS 65109–65110.


## 2026-10-14 — огонь позади лодки (DB-ON-060)

Владелец подтвердил 1000 м позади лодки, 10 HP в секунду. Authority назначает адресный урон, четыре сетевых TickTimer переживают смену ведущего. Безопасные/умершие/отключённые, реклама/grace и победа исключены, пачки пропущенных ударов нет. Возврат за рекламу с 750 м сохранён. Частицы локальные, до 192, собственный простой flame shader без текстур/света/коллизий. Предупреждение RU/EN; SDK не трогали.

Настоящий Forest с двумя runner: gameplay PASS 65313–65314. Протокол takeover PASS 65613–65614: сохранение deadline, сброс при выходе/новый вход, victory. Промежуточный стенд с timeScale=0 и записью начальных свойств после Spawn исправлен, его сбои не считаются успехом. Визуал просмотрен. Отчёт `Docs/COOP_REAR_FIRE_20261014.md`. DB60 in_review; DB27 всё ещё требует бюджета удержания мира.

Следующий шаг: ИИ — объединённая WebGL-сборка; владелец позже принимает настоящую рекламу и независимые устройства. Черновик пока не обновлён.


## 2026-10-14 — продолжение без сборки: враги и общий чеклист

Владелец просит накапливать механики для одного теста. Никаких новых сборок/загрузок. Добавлен `Docs/COOP_BATCH_ACCEPTANCE.md`, DB59/60 дополнены актуальной очередью.

DB27: отдельный enemy registry, аудит 10 Гц и сброс по смене роли/OnEnable. Убраны повторные native state writes и перезапуск одинакового Animator normalizedTime каждый кадр; движение/HP обновляются между аудитами. SDK/сетевой формат не трогали. Empty two-Cloud-peer микрозамер 100 enemies + 2000 other identities: authority 110.837 → 32.776 мс, proxy 143.244 → 63.207 мс на 200 вызовов. SharedEnemyRuntimeProbe PASS, включая продвижение Animator между идентичными снимками; SharedReadyBarrierProbe PASS 65879–65903 боя/победы/master transfer и остальных протоколов. Это не доказательство FPS/памяти WebGL или бюджета удерживаемого мира.

Следующий шаг — ИИ: DB-ON-033 (понятный подбор яйца), дальнейшая подгрузка мира; владелец позже принимает весь пакет по общей инструкции.


## 2026-10-14 — DB-ON-033: понятный подбор яйца

Receipt в существующей операции EggFeatureStorage (API совместимы, без новых save fields/keys), обычный и one-shot pickup передают его локальному UI. Иконка/прибавка/общий запас, объединение быстрых подборов, первая подсказка по hasDiscoveredEggs, unscaled/fade, неблокирующий ввод, остановка времени уведомления на рекламе. SDK не меняли. EggPickupFeedbackProbe PASS 65965 на временном состоянии, без чтения/записи мета-запаса владельца; Game View RU просмотрен. Полная solo/co-op/mobile приёмка ещё нужна, in_review. Документ `Docs/EGG_PICKUP_FEEDBACK_20261014.md`, общий чеклист дополнен.

Следующий шаг — ИИ: личное меню яиц/животных DB-ON-041. Сборку не выполнять, владелец позже проверяет пакет целиком; сейчас действий не требуется.


## 2026-10-15 — DB-ON-041: личные яйца/животные онлайн, без сборки

В LobbyOnlineBootstrap добавлен локальный LobbyPersonalEggMenu: четыре настоящих nestId, таймеры, выбор яйца через готовую панель, сбор и прежнее rewarded-ускорение, переходы к инвентарям/3 слотам бонусов/объединению GameCanvas. Меню согласовано с mode/friends/chat по открытию; UI RU/EN, список со скроллом, обновление 4 Гц. Метаданные не реплицируются.

EggFeatureUnlockVisibility скрывает персональные группы только в онлайн-представлении; EggHatchingManager/AnimalPlacementPoints/AnimalLoadoutBoardSpawner подавляют восстановление превью/животных без удаления состояния или остановки таймеров. Офлайн восстанавливает прежние unlock-правила. Повторный клик по занятому гнезду больше не заменяет инкубацию и не тратит яйцо. Исправлена повторная подписка visibility после выключения; общий DB34 остаётся непроверенным.

LobbyPersonalEggMenuProbe PASS в Unity 6000.3.9f1, Edit Mode preview настоящей сцены: 4 гнезда/5 групп/вложенные интерактивные точки; таймер и запас после Load; однократный сбор; бонус после Load; офлайн-восстановление; handlers подписки; UI online/offline. Настроенные панели/шаблоны проверены по GameCanvas, русский render просмотрен. EggFeatureStorage имеет editor-only временное RAM-хранилище; owner prefs не менялись, MirraSDK.Data не вызывалось. Во время первичного редакторского preview обнаружен побочный lazy getter AdsManager.Instance: убран из Refresh меню, созданные им два пустых объекта удалены, сцена не сохранена. SDK не трогали.

DB41 in_review: независимые WebGL-клиенты/реальная реклама/полный UI-проход остаются в общей приёмке. Общий чеклист дополнен. Сборку/загрузку не выполняли.

Следующий шаг — ИИ: отдельный DB-ON-034 (пропадающий офлайн-загон), затем оставшиеся задачи пакета. Владелец позже проверяет пакет целиком, сейчас действий не требуется.


## 2026-10-15 — DB-ON-034: воспроизведено удаление офлайн-загона, без сборки

Причина: EggInventoryHudButton создавал менеджер в постоянном HUD после подбора яйца в забеге. Входящий Lobby EggHatchingManager видел singleton и удалял весь свой EggSystem вместе с пятью персональными группами. До правки воспроизведено в Play: реальная HUD-фабрика под DontDestroyOnLoad + клонированный EggSystem настоящей Lobby, через кадр EggSystem уничтожен, runtime singleton остался (REPRODUCED).

Добавлен GetOrCreateRuntimeFallback с флагом до Awake; настроенный менеджер сцены теперь заменяет fallback, отключая и удаляя только его выделенный дочерний объект. Состояние загружается из прежнего хранилища, HUD/загон остаются. HUD очищает ссылку после OnDisable. SDK/формат сохранения/сцены не менялись.

OfflineEggPenProbe.Verify PASS: fallback заменён, загон/four nests/HUD сохранены, запас идентичен, повторный вход без fallback, online hide/offline restore. Временный RAM-профиль; после Play SharedRewardGameplayProbe.VerifyRestored PASS по owner keys/start scene. Один промежуточный запуск после таймаута Prepare вошёл в Lobby вместо тестовой сцены и был остановлен без игровых действий; после этого preflight/start выполнялись раздельно с подтверждением RecoveryProtocolProbe и backup перед Play. Сохранённые сцены не записывались.

DB34 in_review, общий чеклист дополнен: фактические WebGL-возвраты/устройства принимает владелец позже с остальным пакетом. Сборку/загрузку не запускали. Следующий шаг — ИИ: оставшиеся задачи пакета без сборки; владелец: позже общий сценарий «подбор → возврат → повторный вход», сейчас действий не требуется.


## 2026-10-15 — DB-ON-037: имена сетевых аватаров, без сборки

Выбрана следующая now-карточка после DB34. Сетевой DisplayName _32, 24-символьная безопасная строка; кеш профиля площадки/Mirra Cloud без новых запросов и служебных ID, гостевой Player N/Игрок N. Удалённая world-space подпись учитывает глубину, камерный вид, дистанцию до 30 м/затухание с 22 м, курсорное меню; FPS-своя скрыта. Масштаб/ширина подправлены по реальному render. Кеш сетевой строки без повторных ToString до изменения; камера/имя 5 Гц, источник 0.5 Гц. Позиция после визуального rebasing/водителя.

Новая структура аватара изолирована suffix -dbnames1 в копии Photon AppSettings: все runtime-пути поиска/входа теперь используют SharedRunRecovery.Settings. Версия return-ticket/приглашения также учитывает протокол, старые записи явно несовместимы. Глобальные настройки/SDK не менялись. Имена не являются удостоверенной идентичностью; DB9 остаётся отдельно.

AvatarNamesProbe.Validate PASS и Run PASS с двумя настоящими Cloud peers в изолированной пустой Play-сцене: late join/current name/live rename/guest, своя подпись отсутствует, ввод не захватывает, near/far/behind и две подписи, непрозрачная стена закрывает text (0 белых пикселей). RU render просмотрен. Приёмка 10 игроков/веб/телефон/реальные профили/переход/лодка/origin остаётся общей. Mirra field notes дополнены наблюдением по кешу имени и предложением события, без отправки. Сборку/загрузку не запускали.

Следующий шаг — ИИ: DB-ON-019, аудит переноса/возврата предметов и патронов; владелец: позднее общий проход на одинаковой новой сборке и новых экипажах. Сейчас действий не требуется.


## 2026-10-15 — DB-ON-019: перенос/магазин/личный резерв/возврат, без сборки

Аудит нашёл личный WorldId=0 до выброса (старый bag мог воскресить переданную покупку), consumed RPC при очистке bag во время Applying, duplicate WorldId/добавление, Free-коробку после выдачи резерва до конца анимации, reload всех неактивных оружий, потерю отписки NewAmmo/чужое выключение ammo HUD и неверный знак положительного ReserveAmmo.

Исправлены подтверждённые ID до полного bag (регистрация всех одним проходом, pending не регистрируется свободным мастер-аудитом), фильтрация текущего владельца и дедупликация, подавление потребления/solo tombstone при восстановлении, немедленное сетевое потребление коробки и локальный повторный guard, активная перезарядка/личный резерв/отписка/HUD. RPC_DropPersonalItem получил inInventory, поэтому общий протокол -dbitems2, версия билета/приглашения следует общему gate. SDK не трогали; legacy-файлы Items/Player сохранены в исходной cp1251 без перекодировки комментариев.

Real level SharedItemAmmoGameplayProbe log66556 PASS и полный SharedItemGameplayProbe log66589 PASS. SharedRecoveryGameplayProbe.RunColdRestart logs66733/66734/66748 PASS: persisted ticket/new recovery instance, real reload, inventory/gold, magazine3/reserve31, coins/cards/HP/exp, затем отдельная solo-ветка. Один игровой мир + два Cloud peers, не независимые браузеры. ValidateInactiveHud/AvatarNamesProbe.Validate PASS; owner keys/start scene восстановлены, редактор остановлен. Последние недоставленные действия не объявлены гарантированно сохранёнными. DB19 in_review, общий чеклист дополнен. Сборку/загрузку не выполняли.

Следующий шаг — ИИ: DB-ON-043, аудит общего сида/ассортимента магазинов. Владелец позже принимает весь пакет; сейчас действий не нужно.


## 2026-10-15 — DB-ON-043: общий seed и магазины, без сборки

Проверены реальные четыре игровые сцены, Castle, ItemStore/StorePoint и fixed BoatShop. Seed/level/version задаются общим состоянием до загрузки. Найдена зависимость авторских world/store ключей от порядка корней; старый key в тесте даёт расхождение 7 магазинов после вставки постороннего root. Новый StableScenePath не включает sibling index корня, использует scene/root name; дочерняя authored-структура по-прежнему часть контента. City anchors и procedural keys сохранены. RunRandom v4, protocol -dbseed4; проверка возврата сверяет также level/generator. Это несовместимая схема ключей: будущая общая приёмка только на одной новой сборке и новых экипажах. SDK не редактировался.

ItemStore.SelectAssortment — тот же игровой выбор отдельным методом без UI, исходный pool не мутирует. SharedStoreSeedProbe PASS: 48 store contexts / 140 слотов /420 сравнений, пять seed, три city-key, root reorder/разный RNG и порядок/fixed slots/price data. SharedRunValidation log66793 PASS четыре layout-пула x200. AvatarNamesProbe.Validate PASS. SharedRecoveryGameplayProbe.RunColdRestart logs66924/66925/66940 PASS реальный reload/persisted ticket/новый recovery instance, gold/bag/HP/ammo и сохранённая solo-ветка. Один game world с двумя Cloud peers; независимые витрины в браузере ещё не приняты. VerifyRestored PASS, редактор остановлен.

Обновлены дизайн/карточка DB43(in_review)/общий чеклист/TODO/Online README; старые статусы документа отмечены историческими, перечислены локальные RNG и authority/snapshot системы. Сборку, архив и загрузку не делали.

Следующий шаг — владелец: позже общий проход по COOP_BATCH_ACCEPTANCE после согласованной сборки. ИИ: продолжать исходные задачи до запроса сборки; приёмку не отмечать done. Сейчас владельцу действий не требуется.


## 2026-10-15 — DB-ON-040: безопасное idle-отключение, без сборки

После просмотра доски выбран оставшийся протокольный риск: anyKeyDown не учитывал длительное удержание, переход/отсчёт явно не защищались, DateTime-зависимый таймер. LobbyIdleClock теперь monotonic unscaled, активность включает удержание/scroll/touch; IdleProtected исключает рекламу/паузу/подключение/остановку/переходы и фазы1–3. Отмена4 не защищается навсегда. Возврат focus/pause и первый кадр после защищённого промежутка дают новый срок. Idle shutdown перепроверяет условия, блокирует дубли и pending reconnect, оставляет mode; предупреждающий chip по нажатию продлевает время.

LobbyIdleProbe.Validate PASS и сетевой Run log67074 PASS: два реальных Cloud peers в пустой Play-сцене, UI Stay/guards/реальный countdown/loading/отмена/уход участника/сохранение mode/кнопка reconnect/скрытое тестовое повторное подключение. Не проверка реального мобильного ввода/настоящей рекламы/браузерного фона. VerifyRestored PASS, owner keys/start scene восстановлены, Play остановлен. DB40 in_review, общий чеклист/README/TODO дополнены. SDK и протокол-layout не менялись. Без сборки/загрузки.

Следующий шаг — ИИ: DB-ON-039, аудит одиночного старта из ожидания и закрытия Photon. Владелец позже проходит весь пакет по COOP_BATCH_ACCEPTANCE; сейчас действий не нужно.


## 2026-10-15 — DB-ON-039: одиночный старт и освобождение Photon, без сборки

StopRunnerAsync имел гонку detached runner: повторный вызов возвращался до завершения первой остановки. Общая stopTask теперь ожидается всеми; после Shutdown/5s fallback ждём фактическое удаление Runner в конце кадра. Solo-picker защищён transition и queued reconnect очищается. LobbyManager старая кнопка защищена от повторного coroutine; ошибка загрузки разблокирует флаги/mode UI (также LevelMapUI catch), неверный roster/version снимает launchingDeparture.

SoloDepartureProbe log67165 PASS в изолированном Lobby→Forest→Lobby: два pending solo-вызова ждут общий barrier; настоящая stopTask одна, Runner удалён; повтор после имитации отказа; незавершённый save321 переживает выбор карты; shutdown ожидания не запускает соло; один член4-местного экипажа ждёт countdown онлайн, двойной LaunchNow даёт ровно один обычный соло-уровень без connected Photon на scene activation, return-ticket/context/fork отсутствуют, возврат восстанавливает online preference. Это Editor, не WebGL; CCU dashboard/реальный timeout не измерялись.

Первый shared regression подряд в той же Play после Single-run при смене test Global.PeerMode на Multiple дал NRE Connect и timeout log67168. После возврата PeerMode retry удался; стек NRE не локализован, причину фиксируем как гипотезу fixture mode transition, не объявляем runtime-исправлением. В новом изолированном Play SharedRecoveryGameplayProbe.RunColdRestart PASS67316/67317/67331: два Cloud peers, настоящий общий уровень/новый ticket component/reload личного и общего состояния/solo-fork. VerifyRestored PASS обоих запусков; owner keys/startscene восстановлены, Play остановлен.

DB39 in_review, общий чеклист/README/TODO обновлены. SDK/пакеты/protocol не меняли, без сборки и загрузки. Следующий шаг — ИИ: DB16 изменение состава и отмена отсчёта на текущем коде. Владелец позже общий WebGL-проход; сейчас действий не нужно.


## 2026-10-15 — DB-ON-016: состав/отмена/смена лидера, без сборки

Baseline DepartureCountdownProbe log67399 PASS, затем текущие callbacks/cleanup/UI logs67481/67495/67496 PASS. До4 реальных Cloud peers одновременно в одной пустой Editor-сцене: общий deadline, автостарт2, уход/возврат/новый timer, двойной/чужой LaunchNow, вход третьего при ручном старте2/4, заполнение4 и отмена при выходе, уход лидера и master takeover с прежним seed/level/version, новый ручной старт2, закрытие admission(GameClosed ожидаемо), loading-member leave→abort4.

Добавлен countdown-scoped membership guard IPlayerJoined/IPlayerLeft для пакетных leave+join между тиками. Событие привязано к TargetTick, старый callback прокси не должен переживать следующий countdown при takeover. Синтетические callbacks при том же PlayerRef наборе проверены отдельно от реальных join/leave. Cancel очищает roster/ready/deadlines. UI запуска разрешён только лидеру в waiting0; aborted4 объясняет выход/новое создание, кнопка выключена; runtime UI fixture проверен. Устаревшая preview-надпись заменена правилом смены состава. Новых network fields/SDK-правок нет, протокол тот же.

Карточка in_review, общий чеклист/README/TODO обновлены. VerifyRestored PASS, Play остановлен, owner keys/start scene восстановлены. Независимый WebGL/мобильный фон и вход возле нуля не объявлены принятыми. Без сборки/архива/загрузки. Следующий шаг — владелец позже общий проход; ИИ продолжает оставшиеся исходные задачи/разбор будущих логов, сейчас владельцу действий не нужно.


## 2026-10-15 — DB-ON-007: CCU/room-full/ошибки приглашения, без сборки

После просмотра доски выбран незакрытый отказ вместимости. Добавлен LobbyConnectionMessages с отдельным RoomFull (в конце enum, значения прежних причин сохранены), общим RU/EN текстом для mode/departure. JoinFriendLobby записывает LastFriendJoinFailure и не запускает немедленный публичный retry при Capacity/NoNetwork/Timeout, очищает queued retry. Snapshot сбрасывается при новом приглашении; правильный RoomFull fallback сохранился. Mirra invite adapter больше не выдаёт глобальный отказ за ожидание новой комнаты; во время reconnect ждёт регион. Recovery MaxCcuReached не инвалидирует билет.

Validate PASS, ConnectionCapacityProbe log67586 PASS в изолированной сцене: настоящий отказ1-местной комнаты второму peer/публичный fallback, инъекция MaxCcuReached в игровой failure handler (не SDK), queued retry/UI режима/отправления/доступный офлайн/ручной повтор/билет/очистка stale reason, настоящая новая hidden session.100-клиентного насыщения/счётчика dashboard/нового Mirra API-теста не было. Начальную ошибку доступности internal метода в Editor-probe исправили reflection-вызовом; итог скомпилирован и прошёл. VerifyRestored PASS, Play остановлен, owner keys/start scene восстановлены.

DB7 in_review, common checklist/README/TODO и MIRRA_CLOUD_FIELD_NOTES дополнены (без отправки). SDK/пакеты/protocol/tariff не менялись. Без сборки/загрузки. Следующий шаг — ИИ DB4: fallback и отмена подключения с сохранностью solo; владелец позже общий WebGL-пакет, сейчас действий не нужно.


## 2026-11-02 — DB-ON-004: отмена подключения / сохранность solo, без сборки

Рабочая ветка fast-forward ce7d8615→5f27538f (мастерские, новые ассеты/карточки и StorePoint). Собственные правки не затрагивали их или SDK. Unity запущена для тестов: первоначальный Low disk space(E:~570МБ) разрешён владельцем освобождением диска (~35ГБ); никаких файлов ради очистки ИИ не удалял.

Найдено: mode/revision отменяли принятие результата, но не ожидание12/20s; friend-finally не обрабатывал queued re-online после отмены и снимал connecting раньше cleanup. Добавлен LobbyConnectionWait с сигналом отмены, отдельными deadline и наблюдением поздних faults. Все переходы revision используют AdvanceConnectionRevision. Штатный StopRunner остаётся владельцем остановки SDK. Friend-finally завершает shutdown, затем освобождает connecting и обрабатывает очередь; старое завершение не перезаписывает статус нового выбора. Ни network layout/protocol, ни SDK/пакеты не менялись.

Точный helper скомпилирован Add-Type и проверен отдельно: cancel/late success/новая попытка/timeout/late fault. Unity LobbyFallbackProbe log67693 PASS: три реальных cancel, queued mode sequence/один Runner, отмена настоящего friend StartGame с последующим публичным входом, штатный Shutdown→лобби, неизменные distance987/reserve31/coins77/cardHP25, реальный SoloResume Forest без Photon. Резерв runtime/save fixture согласован заранее; это не cold app boot и не физическое выключение сети. CapacityProbe в отдельном чистом Play log67750 PASS (настоящий full-room/public fallback, injected CCU/кнопки/ticket/ручной retry). Owner keys/start scene восстановлены после обоих прогонов, итоговый VerifyRestored PASS/Play=false.

DB4 in_review, общий чеклист/README/TODO обновлены. Осталась независимая WebGL/мобильная/фоновая приёмка; сборку/архив/загрузку не делали. Следующий шаг — ИИ: продолжение DB-ON-027 (память/нагрузка мира без новых игровых ограничений); владелец — позднее общий пакет по чеклисту после отдельной команды на сборку, сейчас действий не нужно.


## 2026-11-02 — восстановление после сбоя и MCP

По запросу владельца AI Game Developer обновлён с локального 0.93.4 до 1.6.0; расширения Animation/ParticleSystem/ProBuilder — до официальных 2.1.0. Старый встроенный ProBuilder 1.2.28 сохранён вне Packages в локальном Logs/McpUpdateBackup-20261102; проект теперь использует registry-пакет. Server 9.3.0 загружен с проверкой SHA256. Старые расширения давали ошибки типов, после обновления компиляция PASS. MCP script-execute подтвердил нужный project path, Play=false, compiling=false, scriptCompilationFailed=false. Сборку/черновик не трогали.

DB-ON-027: код и оба стресс-прогона сохранились, но память/расхождение мира остаются проблемой. Микрозамер кэша и ограничения записаны в Docs/COOP_PERFORMANCE_20261102.md. После аварийного перезапуска SessionState backupActive=false: это не доказательство сравнения ключей с состоянием до аварии. Для нового контрольного теста ключи дополнительно сохранены локально на диск без вывода значений. Следующий шаг — ИИ: проверка кэша и восстановление контрольного теста; затем бюджет мира. Владелец: общий WebGL-проход позднее, сейчас действий не требуется.

Завершение 02.11.2026: после ещё одного перезапуска два тестовых ключа восстановлены по дисковой копии. Новый контроль cache PASS (421.157/12.240 мс), physics sleep/wake/sleep PASS, 100 предметов/20 трупов созданы. Итог всего stress FAIL по test-only memory guard 4763>4761 MiB до маршрута; не поднимать защиту ради PASS. Cleanup completed, Play=false, sceneRunners=0, PeerMode=Single, обе fixtures=false, errors=false. VerifyRestored PASS + дисковая сверка исходных ключей 4/4. DB027 остаётся in_progress; ИИ далее решает бюджет мира, владелец позже принимает WebGL. MCP 11784191 и текущие source/docs подготовлены к push; без сборки.


## 2026-11-02 — источник роста памяти в стенде

Пока владелец отошёл, DB027 продолжена без сборки. Обнаружен ProfilerDriver.enabled=true при Profiler.enabled=false в Edit Mode (profileEditor=false), хранилось 2000 кадров. SharedPerformanceTestSave временно выключает запись, сохраняя кадры и исходную настройку. SharedMemoryAudit собирает Editor object/native/Gfx/Mono/GC/сырые Fusion snapshots каждые10с; сам скан влияет на hitches и не пригоден для приёмки FPS. Контроль полного stress PASS без ослабления guard: sleep/wake/sleep, 100 предметов/20 трупов, маршрут5000/rear4000. Baseline4385MiB, split4424MiB, caught4414MiB, cleanup4345MiB; span7544/230предметов/118врагов13.6–15.5FPS. Прежний guardFAIL нельзя считать доказанной игровой утечкой. WebGL и длинное расхождение остаются открытыми. VerifyRestored PASS + дисковая сверка4/4, Runner0, PeerModeSingle, ошибокC#нет; ProfilerRecording=true/frame1982–3981 сохранены. Далее ИИ: CPU/рендеринг/навигация/бюджет мира; владелец позже принимает WebGL, действий сейчас не нужно.


## 2026-11-02 — большой блок CPU/навигации без владельца

SharedCpuAudit выделил RenderLoop/BehaviourUpdate как основные участки; навигация не единственная причина просадки. Эксперимент instancing2794→1307batches и31материал: проверка ресурсов1646vs2720batches при неизменных486549треугольниках, но FPS40.31vs42.60, поэтому все31правки материалов возвращены, не выдавать за принятую оптимизацию.

ZombieController кооп: планирование0.2с, новый target/сдвиг2м/Origin/authority-reset немедленно; атаки каждый кадр, соло путь каждый кадр. CheckSee больше не продолжает работу после неудачного EnsureOnNavMesh, online retry0.5с; Start допускает target=null. cp1251/CRLF сохранены; счётчик запросов только UNITY_EDITOR, SDK/протокол не менялись.

SharedNavigationProbe PASS sharedtrue/false на собственной NavMesh-площадке; исходный игровой fixture и синхронное hasPath/pathPending были неверной проверкой, заменены действительными запросами/destination. Нативный ABBA32агента×90кадров:2880→480запросов, submit0.0514–0.0625→0.0157–0.0169мс, nativeManager прогревается и его ускорение не заявляется. Полный стресс дважды PASS включая финал с исходными материалами;35с/4Cloud profile PASS; ReadyBarrier2Cloud regression PASS подбора/прикрепления/топлива/патронов/страниц/боя/победы/бонусов/реальной смены ведущего. Все подробности/ограничения в Docs/COOP_CPU_RENDER_20261102.md.

Конечный VerifyRestored+диск4/4 PASS, Play/paused/compiling=false, ошибкиC#нет, Runner0/NavAgent0/PeerModeSingle, clockfixturefalse, Profilertrue/frame1982–3981 сохранены. Несвязанные рабочие изменения не трогать. Без сборки/черновика. DB027 остаётся in_progress: ИИ дальше отрисовка/прочие скрипты/бюджет мира; владелец позже общий WebGL, сейчас действий не нужно.

## 2026-11-02 — продолжение без владельца: UI и layout

Владелец подтвердил: последующие «продолжи» продолжают самостоятельную разработку; по возвращении нужен отчёт с сообщения «Я отошел…». Создан Docs/AFK_PROGRESS_20261102.md с якорем послеf7a7b2f0 и результатами последующих этапов.

SharedBillboardProbe:200панелей,исходныйSeeInCamera mode0/12,unchangedWrites0,40000вызовов61.740/73.865мс,Canvas.BuildBatch0.0141/0.0150мс/120кадров. Гипотеза загрязнения неподвижного UI опровергнута, рантайм UI не менялся.

LocationLayoutPlan выделяет расчёт последовательности из Instantiate; cp1251/CRLF LocationSpawner сохранены, исправлено только ложное предупреждение пропуска за rear. LocationLayoutProbe4пула/11264сравнения legacy/plan/replay/unrelatedRNG/solo/retiredSkip PASS, SharedRunValidation PASS. Первоначальный вызов валидатора ошибочно пытался сложить string+void, исправлен без исполнения тестов; фактический повтор PASS.

Интеграционный4Cloud stress PASS100предметов/20трупов/маршрут5000/очисткаrear4000. Split span7751,230предметов/118врагов13.8–14.4FPS; caught61предмет/57врагов21.8–22.7FPS. Это не WebGL и не улучшение FPS. Созданы DB-ON-125 и Docs/WORLD_STREAMING_PLAN.md: до разрушительной выгрузки нужен манифест, canonical mutations и атомарное восстановление при lateJoin/authorityTransfer. Онлайн-сохранение между пустыми комнатами не вводить.

Конечный VerifyRestored+диск4/4 PASS, Play/compilingfalse,Runner0,Agent0,PeerModeSingle,Profilertrue/1982–3981 сохранены. SDK/несвязанные изменения не тронуты, сборка/черновик не обновлялись. Далее ИИ — теневой манифест DB125; владелец позднее принимает общий WebGL, сейчас действий не нужно.

## 2026-11-02 — DB125 локальный манифест после «продолжи»

LocationLayoutManifest подключён к seeded LocationSpawner (кооп/soloBranch):seed/version,последовательныйиндекс,Z,локальнаяссылкаprefab,Queue+Dictionary и монотонное окончательное удаление заrear. Не включает разгрузку/сетевойархив/постоянноеonlineсохранение. Старое несидированное соло не создаёт манифест. Код legacycp1251/CRLF сохранён, SDK не тронуты.

LayoutProbe10000шагов/окнонеболее4,0послеокончательнойочистки;дубликаты/пропуски/NaN/retrograde/rearEquality/retiredSkip PASS.4пула/11264сравнения и SharedRunValidation PASS. MCP дважды не доставил вызовы при domainReload, после готовности повторены; это не результаты тестов.

Первый стресс основнойPASS,live-checkFAIL из-за неверного ожидания Editor prefab connection у runtimeInstantiate. Диагностическая попытка прямого mesh-сравнения также неверна для генерируемых ProBuilder meshes; позже сцена уже вернулась вLobby, пустая выборка не зачтена. Исправленный CheckLive проверяет seed/cursor/cloneName/logicalZ и authored shared meshes, отдельно считает исключённые runtimegenerated.

Финальный повтор:4Cloud stress PASS100предметов/20трупов/маршрут5000/rear4000. Live23локации/2005мешей(65исключены) и12/1124(35исключены) PASS. Split7376/226предметов/113врагов16.4–17.9FPS,caught58/52/24.2–24.4FPS; не WebGL и не A/Bускорение. VerifyRestored+диск4/4PASS,Play/compilefalse,Runner0/Agent0/PeerSingle,Profilertrue/1982–3981 сохранены. При будущей выгрузке OnDestroy не должен вызывать SoloRunBranch.RememberDestroyed для временных копий. Далее ИИ — причина удаления реплик и архив мутаций (DB125), владелец позже общий WebGL. Без сборки/черновика; накопительный отчёт AFK_PROGRESS_20261102 обновлён.

## 2026-11-02 — DB125 жизненный цикл временных копий

WorldReplicaRemoval.TryRemoveLocalTree проверяет всё дерево (включая inactive): personal/attached items, network ownership, pending requests, untracked/unregistered identities, player/boat/Fusion roots. Отказ ничего не помечает. При принятии выставляет instance RemovingReplica, снимает локальную регистрацию, отключает и deferred Destroy. WorldSpawnIdentity/SharedItemsRuntime/SoloRunBranch пропускают consumed/dead только для этой копии; обычный Destroy прежний. Поздняя регистрация помеченной копии игнорируется. Производственный streaming этот API не вызывает, архив остаётся предусловием.

WorldReplicaRemovalProbe RunSolo PASS на собственном отключённом SoloRunBranch: ammo7/mode0/HP55 сохраняются, ordinary tombstones остаются; deferred OnDestroy/replacement, atomic refusal, pending, inactive untracked inventory/free item и unregistered identity. RunShared включён в ReadyBarrier: два настоящих Cloud peers сохраняют канонические записи, локальный IsDead не меняет HP55; mode3/remote inventory защищены, обычный owner Destroy реплицирует mode4. Финальный полный ReadyBarrier PASS предметов/топлива/патронов/страниц/боя/победы/бонусов/master transfer/ready/leave/timeout. Первая версия harness не компилировалась из-за отсутствующего SharedItemsTable.Remove; очистка исправлена через страницы. Запуск Play при этой ошибке не состоялся и не засчитывается как тест. Последующие сборка C# и проверки PASS.

VerifyRestored+диск4/4 PASS, Play/compilefalse, Runner0/registry0, Solo/SharedFalse, PeerSingle, Profilertrue/1982–3981 сохранены. Логи ReplicaRemovalSolo и ReplicaReadyFinal локальные, без секретов. Docs/WORLD_STREAMING_PLAN и AFK_PROGRESS обновлены. DB125 in_progress: дальше ИИ — mutation archive и interest gating; владелец позже общий WebGL, сейчас действий не нужно. Без сборки/черновика/SDK-правок.

## 2026-11-02 — DB125 локальный архив и фильтр пересоздания

WorldRegionArchive на SharedRunContext хранит копии полных item/enemy records с consumed/dead, seed/level/version и ревизией региона. Atomic staged replacement, duplicate/cross-region conflict rejection, provisional constructor limits128regions/4096records (не бюджет всей игры). Даже подготовка перечисления ограничена; отказ не меняет старый снимок. RetireBefore монотонный, End очищает локальный архив. Никакого disk/network persistence; сетевые страницы остаются истиной. Полное членство реальных локаций ещё не вычисляется автоматически.

Register и циклы dropped item/proxy enemy recreation учитывают явно спящие записи. Владение/инвентарь/груз/pending или изменившийся live record разрешают materialization; архив ничего не откатывает. Оригинальные приоритет RecoveryReplica и проверка key collision сохранены до фильтра. Автоматический streaming не включён.

WorldRegionArchiveProbe pure PASS copies/tombstones/revision/duplicates/budget/atomicity/live precedence/reopen/retirement. Финальный ReadyBarrier2Cloud PASS dormancy duplicate registration/reopen/live ammo7→8 HP55→54/context clear/forced identity collision plus полный regression items/fuel/ammo/pages/combat/win/bonus/master/ready/leave/timeout. Полный4Cloud stress100items/20corpses/route5000/rear4000 PASS; split span7714,230items/118enemies14.7FPS в одном окне, caught61/57/21.8–22.5FPS. Не A/Bускорение и не WebGL acceptance.

RunLive в реальной Forest/4Cloud: actual dropped shotgun dormant scan blocked → active one replica/ammo5 → changed live ammo4 restored without rollback PASS. Cleanup record/registry/GO PASS, затем12обычныхitems; финальный35с profile PASS. Не полная локация или четыре симулирующих устройства. Первые попытки harness: ammo7 превышало cap6; фильтр>=7 не нашёл оружия (Shotgun/Pistol6,Rifle1). Исправлено чтение реальной capacity и допустимые значения. Первая cleanup могла заново зарегистрировать test record до deferred Destroy; теперь снимает replica/registry перед удалением record и отдельно проверяет результат. Первая компиляция требовала явного bool cast для NetworkBool comparisons; исправлено. MCP timeout при reload не засчитывался как результат.

Финал: VerifyRestored+диск4/4 PASS,Play/compilefalse,Runner/registry0,Regions=null,Solo/SharedFalse,PeerSingle,Profilertrue/1982–3981 сохранены. Несвязанные изменения сохранены; SDK/сборка/черновик не тронуты. Логи WorldRegionArchive,RegionReadyFinal,RegionStress,RegionLiveCleanProfile локальные. AFK_PROGRESS и карточки027/125 обновлены. Далее ИИ — полное стабильное membership consumed/dead/перенесённых вещей и транзакция capture/remove/rehydrate; владелец позднее общий WebGL, сейчас действий не нужно.

## 2026-11-02 — DB125 membership/canonical capture, DB126 authored pickups

WorldRegionMembership на SharedRunContext: begin/seal, bounded128regions/8192IDs, history retained after GO destruction, procedural slots/parent corpse/authored procedural paths, collision/overflow/incomplete fallback, definitive rear cleanup/End clear. LocationContentSpawner берёт logicalZ из LayoutManifest, если доступен. WorldSpawnIdentity.IdForKey выделяет прежний UTF8/FNV1a без смены ключей/seed version. WorldRegionCapture readonly: полный canonical item/enemy набор по истории, включая derived corpse without local GO; unknown root child или missing canonical row не меняет старый archive. Города/night actors не назначаются procedural по текущей координате; spatial movement и целая rehydration ещё впереди.

Pure PASS seal/history/corpse/authored/budgets/collisions/retirement/hash compatibility. Первые настоящие capture FAIL на dormant PickableItem будущего трупа внутри Zombie и затем FinSword в ruins_1; общий stress при этом PASS. IsDormantCorpsePart строго проверяет cached Awake reference, disabled, alive enemy + identity и отсутствие portable статуса; не exempts arbitrary child/active/detached/dead/inventory. CP1251/CRLF ZombieController сохранены.

Выявлены три authored prefab pickups FinSword/statueGold/skull (catalog templates уже есть), ранее без world identity. InitializeGeneration кэширует references/paths до Start, seeded Start регистрирует authored:scene:procedural:index/path, исключая EnemyCore/StorePoint/Inventory. Existing IDs не тронуты; явная SoloRunBranch тоже использует регистрацию, ordinary solo нет. Добавлена DB126 in_review до WebGL.2peer тест использует настоящий initializer с подложенным LayoutManifest; authored pickup входит в5records, store display остаётся untracked и удаляется как fixture. Captures dead/consumed/never-local corpse, moved inventory, atomic refusals PASS; весь ReadyBarrier regression PASS (MemberAuthoredReady log).

Один network startup ExceptionOnConnect и недоставленные MCP calls не засчитывались. Ошибочный повтор PerformanceProbe без возврата вLobby дал harness Timeout:lobby initialized; корректный cold start выполнен. Diagnostic auto-profile расширил front8500 и указал точный FinSword безID, после чего исправлен initializer. Низкие окна12FPS не строгий A/B. Финальный4Cloud stress100items/20corpses/5000route PASS, rear4000/72items. Live canonical capture25roots/222records/9authored и14/129/6 PASS. Split span8088/243items/123enemies13.6–17.4FPS, caught72/62/23.1–24FPS. Ни автоматическая выгрузка, ни four-device WebGL acceptance не выполнены.

Финал VerifyRestored/диск4/4 PASS,Play/compilefalse,Runner/Agent/registry0,Membership/Regions=null,Solo/SharedFalse,PeerSingle,Profilertrue/1982–3981 сохранены; SharedRunValidation PASS. Логи WorldRegionMembership,MemberAuthoredReady,MemberAuthoredStress,WorldRegionMembershipLive локальные. AFK_PROGRESS/план/приёмка/карточки027/125/126 обновлены. Следующий шаг ИИ — полная capture/remove/rehydrate транзакция одной локации с detached/moved data; владелец позднее WebGL126/общая приёмка, сейчас ничего не требуется. SDK/сборка/черновик/несвязанные файлы не тронуты.


02.11.2026 — DB-ON-125: безопасная инициализация восстановленного ZombieController без TakeDamage/Die и сброса HP в Start; подключена к существующему пути отсутствующих сетевых копий. Реальный Zombie.prefab, ручной/автоматический Start, HP17→новая каноническая11/150, 2Cloud peers, duplicate/dead/invalid guards, сохранение burn-полей при SampleEnemy, обычный спавн PASS. Полный регион ещё не rehydrated, automatic streaming/build/draft выключены. Подробности и следующий шаг в AFK_PROGRESS_20261102.md и DB-ON-125.

Финальная проверка WorldRegionResidencyProbe PASS: настоящая геометрия ruins_1 с контролируемыми seeded точками двух врагов и добавленным оружием; два полных suspend/restore, consumed/dead отсутствуют, новейшие HP13/ammo1, detached dropped actor не удалён/не скрыт/не дублирован, layout cursor не продвинут. Близкий физический предмет блокирует suspend без частичной выгрузки. Искусственно удалённый canonical item вызывает rollback частичных копий; после возврата записи тот же билет восстанавливает регион. Два Cloud peers подтвердили итоговые записи. Первый повторный suspend в том же кадре отказывался из-за deferred Destroy пропущенных детей; стенд ждёт завершения удаления, защита сохранена. Полный ReadyBarrier regression после этих изменений PASS (включая реальные смену ведущего и ready/leave/timeout), но authority transfer именно во время спящего региона ещё не проверен. Лог WorldRegionResidency-20261102.log.

Далее ИИ: проверить сдвиг начала координат и смену authority/поздний вход при спящем регионе; после этого выбирать пространственные интересы и бюджеты. Автоматическая выгрузка не включена, DB-ON-125 остаётся in_progress. Владелец позднее проверяет общую WebGL-сборку; сейчас сборка/черновик не обновлялись.

## Сдвиг координат и смена ведущего при выгруженном регионе

WorldRegionResidencyProbe расширен контролируемым origin500 через неактивный FixCoordinate без save/bootstrap: проверены логическая позиция root, предмета и врага после восстановления; detached вещь сдвигается отдельно. Это моделирование смещения, не вызов полного игрового FixPosition с лодкой/сейвом.

Дополнительный ReadyBarrier Case region handoff: локальный контекст и билет принадлежат клиенту (remote), во время второй dormancy первый Runner действительно Shutdown, оставшийся получает authority состояния и всех SharedWorldPage. Тот же локальный билет проходит отказ/rollback при отсутствующей записи, затем повторное восстановление, сохраняет HP13/ammo1, consumed/dead и единственность detached вещи. Четыре Case ready/region handoff/leave/timeout и полный прежний regression PASS. Первое исполнение обнаружило гонку harness: клиент восстанавливался до доставки обновлённой ammo/HP; ожидание версии записи добавлено явно. Итоговый handoff=True PASS; после ухода первого peer финальная проверка идёт на surviving authority, не на двух оставшихся клиентах.

Игровой runtime этим шагом не менялся. Это не проверка позднего входа/перезапуска браузера при dormancy: у нового клиента ещё нет локального билета/дескриптора и metadata для catalog Template0. Далее ИИ — отдельный late-join сценарий и способ восстановить локальные дескрипторы из сетевого состояния/seed; затем интересы и бюджеты. Автовыгрузка/сборка/черновик выключены, DB-ON-125 in_progress. Владелец пока не должен перетестировать старую сборку.

## Предметы нового клиента: полнота сетевого каталога

При подготовке cold/late join найден источник Template0: SharedItemCatalogBuilder просматривал только Assets/Prefabs/Items, но генерация ссылается и на другие префабы через LocationItemSpawnCollection. Builder теперь объединяет прежнюю папку со всеми prefab-ссылками коллекций; ID по GUID не меняются, дубликаты GUID убираются, коллизии имени/ID отклоняются. Сборка каталога в staged list атомарна, SaveAssetIfDirty сохраняет только каталог. Каталог расширен56→67, все134ссылки спавна сопоставлены, прежние56ID проверены без изменений.

Residency probe теперь очищает локальные ItemNames билета перед первым возвратом и требует ненулевые canonical Template для всех generated items; восстановление включая яйца проверяет каталог, не имя из прежней копии. Это подготовка отсутствующей метаинформации нового клиента, а НЕ полный late join: локальный layout-дескриптор/история пока есть в стенде. Сетевой wire format, SDK и генератор не менялись. Для полноценно свежего клиента далее нужны replay layout по seed/версии без воскрешения canonical consumed/dead и отдельный новый Runner/browser test. Автовыгрузка/сборка/черновик выключены; DB-ON-125 in_progress.

## 04.11 — холодное подключение: реализация и сетевой прогон завершены

После отказа от Scene Backups Unity загрузилась, MCP снова доступен. Полный ReadyBarrier с новым Case region cold join PASS. В этом Case второй настоящий Photon Runner выходит из живой сессии, локальные archive/membership/registry уничтожаются, создаётся новый Runner и получает существующее состояние от оставшегося ведущего. Старый ticket отклоняется, локальный manifest создаётся заново; seeded content впервые создаётся обычным LocationContentSpawner.Start, без RestoreGeneration/локального ItemNames. Канонические used/dead/HP13/ammo1/moved item применяются сразу, дубликатов нет, origin500 учтён, membership восстанавливается включая пропущенные tombstones. Обычная генерация теперь распознаёт уже известные сетевые records, а повторно созданное яйцо не меняет EggSpawnRuntimeState. Схема сетевых записей и SDK не менялись.

Отдельный LocationLayoutProbe PASS:4реальных пула/11264сравнения legacy/plan/replay, независимый RNG, manifest/retirement. Это проверка реальных пулов отдельно от сетевого controlled ruins fixture, где дескриптор региона фиксирован. После cold case прошли handoff/предметы/патроны/бой/бонусы/ready/leave/timeout. Лог Logs/ColdWorld-20261104.log. Интеграционный сценарий процедурного региона в Editor закрыт; ручная WebGL-приёмка на независимых устройствах остаётся частью общей будущей сборки, здесь перезапуск браузера/авторизация возвращающегося игрока не эмулируются. Автовыгрузка, города/ночные динамические спавны и бюджеты большого мира — отдельные незавершённые части DB-ON-125.

Очистка PASS: VerifyRestored ключей/стартовой сцены, Runner/registry0, Shared/SoloFalse, Membership=null, FixCoordinate=null, PeerSingle, Profiler recording восстановлен. Сборку/черновик не обновляли. Ответственный далее: ИИ — остальные пункты DB-ON-125 (активные области/бюджеты), владелец — будущая общая WebGL-приёмка, текущий черновик новых изменений не содержит. Сценарий cold content не дробить на новые незакрытые промежуточные шаги.


04.11.2026 — DB-ON-042 расширение инвентаря готово к приёмке: карточка1/2/3/5/7/10, trader+3, paca1/2/4 по поручению владельца выбрать самому. Сумка максимум27 +5quick (32recovery), бонус личный; поля Stats/profession/animals, UIрост без пересоздания/удаления занятых ячеек и admission limit при уменьшении. Snapshot личных карт расширен; комнаты отделены суффиксом -dbseed4-inv1, карты восстанавливаются раньше вещей. Старые JSONбезполя0. InventoryExpansionProbe PASS, дваPhoton peers11/3 PASS, полныйReadyBarrier PASS. Реальный cold restart и отдельная solo fork восстановили17предметов в20слотах, деньги237/HP80/карты/патроны PASS. Стенд теперь ждёт доставки нового прогресса companion перед Shutdown; первый networkConnect failure и ранняя currency-race не засчитываются за PASS. Скрин UI15/20соscroll проверен, Logs/InventoryExpansion-20261104.*. Подробности вкарточке in_review. Сборки/черновика нет. Владелец — будущая общая приёмка WebGL/баланс; ИИ — затем животные-спутники по игровой очереди. Несвязанные SDK/Packages/Recovery изменения не включать.


04.11 — DB-ON-025: реализованы декоративные спутники коопа:3слота NetworkString32/стадия, стабильный pointId порядок, refresh1с/только изменения, локальное кольцо128точек/дистанция0.2м, след1.3/2.6/3.9м, bob/reset teleport>10м/Origin. Без network transform на животное; mesh-only копии стадий без скриптов/физики/звука/дохода. Модели скрыты в соцлобби, баффы старойсистемы независимы, новая версиякомнат -dbseed4-inv1-pets1. Все60стадий/кости/safety и puretrail PASS; дваPhoton peers+live renderers ID/stage/order/change/removal PASS; окончательный полныйReadyBarrier/cold/handoff/бой/инвентарь PASS. Ранний connect Error в финальном transport-case не засчитывался за полныйпрогон, повтор пройден. Подробности DB-ON-025 in_review, Logs/Companions-20261104.log. Сборка/черновик не обновлялись. Следующее: владелец позднее общая визуальнаяWebGL-приёмка; ИИ следующий игровой этап по очереди.

Попутно по прямому поручению владельца добавлена и отдельно запушена DB-ON-127: разбор Slay the Spire/референсов, интересные синергии/цены/прогрессиякарточек, пример HPвобменнавампиризм, RU/EN локализация и кооп. Это новая задача next/backlog, дизайн ещё не проводился; не заменяла работу над спутниками.


04.11 — DB-ON-127: готов дизайн карточной прогрессии и12RU/ENописаний с параметрами, первичными референсами, правиламивыдачи/рангов/сохранения/сети и точнымвампиризмом. Рекомендован малый прототипдоговор+ритм+грань; массовыйнаборне внедрён. Проверка proposal JSON и моделиформул tools/design/check_card_proposal.py PASS, это неUnity/playtest. Документ Docs/design/CARD_PROGRESSION_PROPOSAL.md; карточка in_review длядизайна, до боевогопрототипатребуетсярешениевладельца. Сборка/черновик/игровойкод не изменены этим шагом.


## 06.11.2026 — DB-ON-127: принятый прототип трёх боевых карточек

По одобрению владельца реализованы Кровавый договор/Ритм боя/На грани: рангиI–III, однократная цена maxHP, actual-damage вампиризм со скользящим общим пределом, третий взмах и низкоеHP, additive damage ceiling100%; RU/EN UI с ценой и рангом. Редкость отделена от ранга, Chance участвует в выдаче; обычный безопасный вариант остаётся свободным, рекламе нельзя обойти cap. Дробовик агрегирует дробинки по цели, сетевая authority подтверждает снятый HP атакующему и хранит dedup-watermark при смене ведущего. Новые ранги проходят Stats/Personal/JSON/solo; протокол cards1. Восстановление не повторяет цену и не выдаёт новый heal-бюджет: первая секунда заблокирована. SDK не правились.

Unity compilation, math/live fixture, двух-peer combat, полный ReadyBarrier/мир/handoff/cold join/бой/лодка/leave/timeout PASS. В старой residency-пробе устранено предположение150мс: явно ожидаются UnityDestroy и canonical records второго клиента; ранние ошибки не засчитывались в PASS. Реальный cold restart/reconnect17items20slots/coins237/HP80/ammo3+31/cards2-3-1 и дальнейшая solo-ветка PASS. RU/EN GameView1920×1080 визуально проверены; локальные скриншоты/лог в Logs/CombatCards-*20261106*. По окончании owner keys/startscene восстановлены, Runners/registry0, Shared/SoloFalse, PeerModeSingle, preview-язык возвращён.

Детали/границы: Docs/design/CARD_COMBAT_PROTOTYPE.md; задача in_review для владельческой проверки ощущений/баланса/WebGL в общей будущей сборке. Остальные9карт остаются предложением. Не собирал и не загружал черновик. Следующее: владелец — пакетная приёмка после отдельного поручения на сборку; ИИ — правки по её результатам.


06.11.2026 — по поручению владельца checkpoint текущей ветки перед ручным переходом 6000.3.9f1→6000.3.27f2, без новой ветки. Включены ранее существовавшие изменения MCP1.6.1/NuGet8.18.0, сгенерированные навыки, define symbols, сериализация TimeManager и FX Fire; сторонние SDK вручную не исправлялись. Код карточек уже был в origin (0e2dc330). Автокопии Assets/_Recovery оставлены на месте и отдельно архивированы в E:/GitFork/dead_boat-backups, в игровой checkpoint не добавлены. Следующий шаг: владелец — установить6000.3.27f2 сWeb Build Support и открыть проект; ИИ — после завершения импорта проверить компиляцию/сеть/сохранения/визуал до добавления нового ассета. Проверки на новом редакторе пока не проводились.


06.11.2026 — владелец отменил переход Unity: остаётся6000.3.9f1; установил FEEL5.9.1 и MCP1.7.0. Изучены локальный код, зависимости, размеры, Built-in/uGUI и официальная документация (она уже6.3, API сверять локально). Создан Docs/FEEL_WEB_GUIDE.md и DB-ON-128 ready/next для первого лёгкого визуала карточек. Состав демо225MiB на диске не равен WebGL-билду; Resources-ассеты присутствуют, итоговый вклад не измерялся. MMF_Player.PerformanceMode влияет на инспектор, не browser-runtime.

Изолированный MMF_Player+Scale приtimeScale0, явныйreset иотключениеPASS. Выявленоauto-disable: RestoreInitialValuesOnDisable=true откатывает и нормальное завершение; initialtest1.2→1.0 неPASS, после подтвержденияUpdate/OnDisable и false дляentrance итог1.2PASS. Для входныханимаций сбросвызыватьадаптеромпризакрытии, неblindflag. SDK/FEEL/сцены не правились, эффектывигру не включались, сборки/черновика нет. Следующее: ИИ — DB-ON-128, первые scale/alpha/color/короткийзвук длякарточек; владелец — затем оценкавобщейWebGLприёмке.


## 06.11.2026 — DB-ON-128: FEEL-отклик карточек, без сборки

CardFeelFeedback: вход scale/alpha, короткий press, рамки редкости и hover/select на внутреннем Point/LevelUpElement; прежний Animator Point/Choise сохраняется. CardChoiceFeedback — единый icon-toast справа на0.48с после подтверждённой выдачи.7MMF_Player на три карты, без новых камер/AudioSource/RenderTexture/частиц/RPC. Звук текущий SoundManager.PlayUIClick; unscaled-пауза, рекламная остановка и сохранение RNG. Выдача отвязана от визуала, прежний таймер0.2с удалён; version/granted guards от старых/двойных callbacks и debounce0.25с для следующего предложения. Пока выбор занят, реролл не тратит gems.

Настоящий UI + изолированный SaveProvider + mock AdsProvider: pause/entry/press,30rerolls/10reopens без роста7players/graphics, RNG, отказ/duplicate/stale callbacks, двойной клик в новое предложение, закрытие во время рекламы, RU/EN PASS. Первая ошибка CanvasGroup исправлена явной Unity null-проверкой; промежуточный press-тест уточнён explicitLeft, готовностью Button, запуском/завершением и паройDown/Up. GameViewRU/EN и финальный toast справа осмотрены; локальные Logs/CardFeel-*. После теста VerifyRestored PASS, Playfalse, MMF/Runner0, Shared/SoloFalse. Это не измерение FPS/GC в слабом браузере и не ручная WebGL-приёмка.

Зависимость FEEL5.9.1 сохранена неизменённым vendor-коммитом15536cf3, чтобы новый код открывался после клонирования репозитория. SDK/MCP обновление владельца этим этапом не переписывалось. DB-ON-128 in_review для пакетного тестирования владельцем позже. Следующий ИИ-шаг — DB-ON-125 (активные области/бюджеты); владелец пока не нужен. Сборка и черновик не обновлялись.

## 06.11.2026 — DB-ON-129: боевой отклик карточек, без сборки

По просьбе продолжить карточки добавлены личные HUD-строки трёх прототипов: серия 0–2/3 и подтверждённый третий удар, фактическое лечение Blood Pact, активность On the Edge. Только локальные снимки; нет новых RPC/save-полей. Три MMF_Player, 10 Гц, объединение лечения 0.3 с, UI без raycast; RU/EN, скрытие лобби/пауза/реклама/смерть/blocked recovery. Исправлен float-порог ровно40% общим правилом для урона и визуала. Баланс рангов не менялся.

Math/local combat и два Cloud-peer + полный ReadyBarrier PASS. Итоговый реальный Lobby/Forest/return HUD probe PASS: actual healing, coalescing, miss/timeout, confirmed/replay-safe third hit, threshold, pause/ad/death/recovery, RU/EN, stable players/cameras и отсутствие дубликатов. Первый тест выявил порог; второй уточнил ошибочное ожидание уничтожения сохраняющегося UI. Подробности в DB-ON-129. WebGL стоимость/мобильная читаемость не измерены; статус in_review. Сборки/черновика нет.

Следующее: владелец — пакетная приёмка после будущей отдельно запрошенной сборки; остальные карточки из дизайн-документа ещё не реализованы.

DB-ON-129 cleanup: VerifyRestored PASS, Play=false, MMF/Runner=0, Shared/Solo=false. Screenshot preview остался на загрузочном экране; визуальная приёмка HUD не засчитана, остаётся владельцу.

## 06.11.2026 — DB-ON-130: вторая пара карточек, без сборки

Добавлены Last Round и Heavy Coat из предложенного набора по поручению владельца расширять карточки. Последний патрон45/60/75% прямого урона, перезарядка+15% длительности/анимации; дробовик усиливает весь залп. Бушлат15/20/25% maxHP, скорость−8% один раз, без лечения; maxHP совместно с договором пересчитывается из исходного значения. Личные рангиI–III, capped offers/ad guard, RU/EN/HP-preview, старые enum сохранены. Capture/Cards/JSON/solo recovery включают два ранга; protocol cards2. Дополнительных proc/RPC/VFX/камер нет, SDK/FEEL не менялись.

Math/RunLocal/RunExpansion PASS: реальный урон/maxHP/скорость, cap/выдача/реклама/повтор восстановления. Реальный Fire/Physics.Raycast pistol+shotgun и Reload IEnumerator PASS: обычный/последний выстрел, весь залп, промах, wait1.15вместо1, патроны до/после завершения. Полный двух-peer ReadyBarrier PASS, новые ранги игроков независимы. Реальный RunColdRestart PASS: persisted ticket/new recovery/живой peer, оба новых ранга сохранились после возврата и solo fork, деньги237/health80/items17+capacity20/magazine3+reserve31. RU/EN карточки в GameView осмотрены, тексты помещаются; Logs/CardExpansion-* локально. Не мобильная/WebGL/баланс-приёмка. DB-ON-130 in_review. Сборки/загрузки нет.

Следующий шаг: ИИ — следующая ограниченная пара (Верная рука / Новый заход) с правилами подготовки/перезарядки и восстановления; владелец — позднее общий тест баланса/UI на WebGL. Остальные семь карт пока дизайн.

DB-ON-130 cleanup: VerifyRestored PASS, Play=false, MMF/Runner=0, Shared/Solo=false, cold clock=false, PeerModeSingle.

## 06.11.2026 — DB-ON-131: Верная рука / Новый заход, без сборки

Добавлены Steady Aim0.75с неподвижности,30/40/50%, cooldown3с и Fresh Start25/35/45% на первый выстрел за3с после completed empty reload. Промах тратит усиление; смена оружия/пауза/ad/death/blocked очищают подготовку/заряд, смена не сокращает cooldown. Recovery reset блокирует aim3с. Весь залп расходует заряд до raycast один раз. RangedCardState локальный; новые поля только ранги, протокол cards3, enum20/21. Ранги/cap/offers/ad guard/RU/EN,5строк HUD/FEEL pulse. SetActiveUse(false) отменяет корутины убранного оружия. SDK/FEEL не менялись.

Math/RunLocal/RunRangedTiming/RunRangedExpansion PASS. Настоящий Fire/Physics.Raycast/Reload + CharacterController/PlayerInput PASS: подготовленный залп150вместо100, cooldown/движение без бонуса; Fresh Start только на первый залп, partial reload без заряда. Paused fixture правильно отменяла усиление; итоговый тест timeScale1. Tiny CC-contact1e-5 не выставлял Below, тест уточнён до0.01 после диагностики коллизии; runtime не ослаблялся. Полный двух-peer ReadyBarrier PASS с новыми отдельными рангами, handoff/cold join/leave/timeout.

Реальный HUD Probe PASS: ready тексты/pulse/consumption без повторов, pause/ad/death/recovery/RU/EN/return. Отдельный свежий ColdRestart PASS: persisted ticket/new recovery/live peer, новые ранги на peer/возврате/solo, деньги237/HP80/items17-capacity20/magazine3-reserve31. Chained HUD→Cold без fresh boot не засчитан (PlayerManager отсутствовал); runtime не правился ради стенда. RU/EN выбор и settled5-row HUD1920×1080 осмотрены; Logs/RangedCards-*. Не WebGL/mobile/FPS/баланс-приёмка.

Play остановлен, VerifyRestored PASS, MMF/Runner0, Shared/SoloFalse, clockfalse, PeerModeSingle. DB-ON-131 in_review. Следующее: ИИ — DB-ON-125 (удалённые локации/бюджеты); владелец — позже общий тест карточек/UI. Остальные пять карт пока дизайн. Сборки/черновика нет.

## 06.11.2026 — DB-ON-125: автоматическая выгрузка процедурной геометрии

WorldRegionStreaming подключён к LocationSpawner в Shared:4Гц/одна транзакция, restore-first, round-robin отказов; Z-окна crew+boat600/800м и static bounds+100м. Missing avatar/NaN откладывают audit. Движущиеся actors рядом с crew и burning enemies удерживают root, старые cargo/inventory/pending/untracked guards сохранены. Поддержано выключение с возвратом sleeping roots, повторные ревизии/отказ-restore retry, постоянный WorldRear. Canonical records и схема сети не менялись; архив128/4096 при лимите отказывает без потери. Города/ночные отдельные спавны/solo/offline/базовая земля не подключены.

Два полных ReadyBarrier прогона PASS; последний на финальном коде: controlled ruins152–153MeshFilter→0→restore, два audit-цикла/гистерезис/NaN/moved enemy/burn/disable-drain, HP13/ammo1/consumed/dead/detached/rollback, handoff/coldjoin и общий бой/предметы/ready/leave/timeout. LayoutProbe4пула/11264сравнения PASS.

Два Forest/4Cloud stress PASS:100physical items/20corpses/route5000. Финальный split13roots/1584meshes+11sleeping, archive11/95; caught-up rear4000:11roots/1380meshes+2sleeping, archive2/20,0отказов. Дополнительный настоящий LateUpdate/crew-interest return вернул region2,8items/5enemies, canonical modes/ammo/HP не изменились; root один. Не4независимых браузера, не FPS-гарантия; Editor17.5–19.8FPS при разрыве, память с плагинами около2305MB. Docs/COOP_STREAMING_20261106.md и локальный Logs/WorldStreaming-20261106.log.

VerifyRestored PASS; Play/compilefalse, Runner0, Shared/SoloFalse, Regions/Membership/FixCoordinate=null, clockfalse, PeerSingle. Сборки/черновика/SDK-правок нет. DB-ON-125 in_progress: ИИ далее — города/независимые динамические спавны и общий бюджет мира; владелец позже общая WebGL-приёмка.


## 2026-11-06 — DB-ON-125: города и независимые спавны

Добавлены WorldCityRegion/WorldCityStreaming: полный seeded city root можно убрать и вернуть по стабильному city:Z с canonical HP/ammo/consumed/dead. Городская история включена в membership/archive; stock display распознаётся точно, pending purchase/reward/bank блокируют удаление. CitySpawner Shared опирается на WorldFront/WorldRear команды. WorldIndependentStreaming убирает только безопасные free known-template реплики предметов и независимых врагов, включая night keys; RNG волн не запускается повторно, ID/текущие записи сохраняются. Regional members/cargo/inventory/burning/unknown защищены.4Гц/одна транзакция на адаптер,4096ID dormancy; общего бюджета кадра/всех сетевых страниц пока нет. Совместимость regions1 без расширения wire records. SDK не трогали.

Финальный Forest/4Cloud/один мир adapter PASS:1177city meshes/9shops/3controlled actors, два удаления/возврата, stable stock, HP13/ammo5, consumed/dead, paid public drop/coins; pending reward guard. Night-key Zombie HP17/sameID/one copy, dead dormant не воскрес; disable-drain вернул купленный предмет с ammo и без дубликата.48stores/140slots/420seed comparisons PASS; Layout4пула/11264 PASS. Общий ReadyBarrier в двух повторах прерывался потерей Spawned/ServerLogic, это не новый полный PASS. См. Docs/COOP_STREAMING_20261106.md; выдержки локально Logs/WorldCityStreaming-20261106.log.

Без сборки/черновика. DB-ON-125 in_progress: ИИ далее city handoff/cold join, общий бюджет мира/кадра и справедливые отказы; владелец позже пакетный WebGL-тест. Несвязанные изменения MCP/SDK/Packages/ProjectSettings не включать в этот коммит.


Последний чистый повтор общего сетевого стенда: ready/boat PASS, следующий запуск комнаты отвергнут Photon с DisconnectedByPluginLogic. Три незелёных общих прогона не считать успешной регрессией; источник ServerLogic/PluginLogic ещё не установлен. Дополнительные повторы без новой диагностики не выполнять. Городской финальный adapter PASS выше остаётся действительным.


Окончательная очистка PASS: VerifyRestored owner return/identity/solo/mode keys и playModeStartScene; Play/compile=false, Runner0, Shared/Solo=false, Regions/Membership/Independent=null, clock=false, PeerSingle. Редактор оставлен вне теста, сборки нет.


## 2026-11-06 — DB-ON-132: Photon SDK передаётся через Git

По запросу владельца исправлена прежняя политика, исключавшая всю Assets/Photon вместе с metadata/config. Это делало checkout неполным: другим требовалась ручная установка Fusion. Добавляется штатная установленная Fusion2.1.3 Stable2390 целиком, исходные GUID, платформенные библиотеки, NetworkProjectConfig и клиентский PhotonAppSettings. Сервисных ключей в конфигурации нет; AppId — клиентский идентификатор приложения. Код SDK не редактируется. Документация запуска обновлена. Mirra Cloud/MCP/UPM и несвязанный ProjectSettings не включаются.

Проверка ИИ: полнота Git-export относительно установленной папки, SHA256 с учётом Git-нормализации строк; ручное открытие другого компьютера остаётся in_review. Далее владелец/другой разработчик — pull и открыть Unity без импорта Photon. Сборки нет.


Проверка поставки PASS: Git archive из подготовленного tree содержит все544файла SDK (16024753байт), SHA256 каждого совпадает с рабочей копией побайтово. Все6обязательных runtime/WebGL/config зависимостей присутствуют. .gitattributes отключает изменение переводов строк только для vendor Photon. Kanban check:132карточки PASS. Чистый запуск Unity у другого разработчика ещё ожидает ручного подтверждения.


## 06.11 — city reconnect/handoff: три сценария завершены

WorldCityRecoveryProbe PASS: (1) surviving client со спящим городом принимает master/pages; (2) клиент выходит, теряет archive/membership/registry/старый city ticket и новым Photon Runner подключается к живому забегу; (3) бывший master возвращается новым Runner после takeover второго игрока. В cold-сценариях проверены оба порядка: город раньше dropped discovery и dropped discovery раньше города. Использованы настоящие Cloud connections и штатный городской prefab:1170MeshFilter/9магазинов, controlled3authored pickups/2защитника. Один неактивный локальный мир, ручная child-generation/corpse-cache lifecycle и штатный SelectAssortment без UI; не два независимых браузера.

Во всех3случаях: origin500, seeded item+price прежние, gun ammo1 и enemy HP13 читаются из актуальных записей, consumed/dead не воскресают, отдельно выброшенная вещь вне city root и одна, canonical purchase snapshot с ammo1 один. Повторный sleep/death/restore не возвращает погибшего защитника. Старый ticket при новом контексте отклонён. Проверяется не только GO, но и неизменность canonical tombstones/ammo/HP.

Runtime-поправка ReleaseDroppedItems оставляет Dropped/Mode0/OwnerNone предмет самостоятельным после полной city generation/успешного restore. Она не выполняется внутри individual factory, чтобы частичный отказ не оставлял объект вне rollback root. SDK, wire records и RNG не менялись. Реальная реклама/IAP/списание coins этим стендом не вызываются; используется уже опубликованный purchase snapshot. Авторизация/привязка вернувшегося игрока и перезапуск браузера — отдельная будущая ручная приёмка.

Первые повторы показали тестовую гонку сразу после HasStateAuthority: прямые fixture writes ammo1/HP13 читались локально, но затем сменялись последним входящим snapshot ammo2/HP17. Стенд ждёт350мс после takeover и дополнительно требует сохранности изменений через200мс ДО restore. После этой правки все3сценария PASS; игровой runtime передачи authority не менялся. Это не подтверждение устранения прежнего DisconnectedByPluginLogic из общего ReadyBarrier: текущий стенд использует отдельные UUID пользователей/комнат, а причина прежнего отказа отдельно не установлена.

Дополнительная проверка SharedStoreSeedProbe PASS:48магазинов/140слотов/420сравнений, включая regenerated city/fixed stock. Cleanup PASS: VerifyRestored owner return/identity/solo/mode keys и playModeStartScene, Play/compile=false, Runner0, Shared/Solo=false, Regions/Membership/Independent/FixCoordinate=null, PeerSingle. Локальная выдержка: Logs/CityRecovery-20261106.log. Без сборки/черновика. Изменения Photon SDK не исключаются из Git и в этом этапе не редактировались.

DB-ON-125 остаётся in_progress: ИИ далее — общий бюджет длинного мира/кадра и справедливое планирование отказов городов; владелец позже — общая WebGL-приёмка после отдельно запрошенной сборки. Текущий черновик этих правок не содержит.


## 06.11 — общий кадровый слот и очередь городов

Добавлен WorldStreamingBudget в SharedRunContext; новый контекст получает новый бюджет, End очищает его. Только автоматические Tick/LateUpdate procedural/city/independent используют слоты0/1/2 по frame%3 и общий запрет повторного входа в кадре. Таймер4Гц сдвигается после получения слота, поэтому порядок LateUpdate не даёт одному адаптеру постоянно занимать очередь. Прямые Audit остаются явным тестовым/ручным интерфейсом без кадрового лимита. Пропущенный слот не заимствуется; на низком FPS частота обработки может быть ниже4Гц. Это ограничение СТАРТОВ аудита, а не его длительности: синхронный Instantiate одного крупного города всё ещё может занять длинный кадр. Общий бюджет памяти сетевых страниц и sliced Instantiate не реализованы.

CityStreaming теперь продвигает cursor до guarded/capture попытки: первая покупка/банк/предмет/отказ больше не повторяет бесконечно один и тот же город перед остальными. Защитные проверки сохранены, RNG/wire/SDK не менялись.

Проверки PASS:120моделируемых кадров/по1выдаче/по40каждой очереди при разном порядке вызовов, повторный/невалидный вход отклонён, новый бюджет чистый. В настоящем Photon-стенде первый controlled city удержан pending purchase, следующий аудит удалил второй, затем восстановил его. Это два пустых controlled roots, не benchmark двух полных городов. Полный WorldCityRecoveryProbe снова PASS во всех3сценариях с настоящим city prefab1170MeshFilter/9магазинами, обоими порядками dropped discovery, origin500, HP/ammo/consumed/dead и отсутствием дубликатов. Автоматический LateUpdate в независимом WebGL этим прогоном не профилировался.

Cleanup PASS: VerifyRestored owner return/identity/solo/mode keys и playModeStartScene, Play/compile=false, Runner0, Shared/Solo=false, StreamingBudget/Regions/Membership/Independent/FixCoordinate=null, PeerSingle. Логи локально Logs/StreamingBudget-20261106.log. Без сборки/черновика.

DB-ON-125 in_progress. Далее ИИ: Forest/4Cloud автоматический Tick и профиль длинного мира с новым кадровым бюджетом, затем удержание сетевой истории/памяти; владелец позже — пакетная WebGL-приёмка. Сейчас действий владельца не требуется.


## 06.11 — автоматический Forest/4Cloud и маршрут до40000

RunLongRoute функционально PASS:100physical items/20ragdoll corpses, split/caught-up5000, настоящий LateUpdate возвратregion2 с8items/5enemies без измененияammo/HP/дубликатов, затем checkpoints10k/20k/30k/40k. Rear9k/19k/29k/39k; archive1/8→14/128→0/0→15/176; pages2→2→2→3. Allocated Editor1361.1→1375.4→1369.8→1388.0MB при разном активном составе; это не доказанное плато/отсутствие утечки и не WebGL heap. Front на двух этапах выше requested, точное физическое плавание не проверяется. IndependentSleeping0 на снимках.

Производительность НЕ принята: последний Editor window10.5FPS/p95≈195.5мс/max≈319.1мс/27кадров>100мс. Refused2, причины монитором не сохранены, последующая обработка продолжилась. Runtime в этом этапе не изменялся; расширен только Editor-стенд. Подробные таблицы/ограничения: Docs/COOP_LONG_ROUTE_20261106.md, локальный Logs/LongRoute-20261106.log.

Cleanup/VerifyRestored PASS: Play/compilefalse, Runner0, Shared/SoloFalse, StreamingBudget/Regions/Membership/Independent=null, PeerSingle, owner keys/start scene восстановлены. Без сборки/черновика. DB-ON-125 in_progress. ИИ далее: причины длинных кадров/отказов и долговременное удержание сетевых страниц; владелец позже пакетный WebGL-тест, сейчас действий не требуется.


## 06.11 — первый прототип обучения новичка, DB-ON-047/048

Начат отдельный first-run greybox вместо переноса всех подсказок в случайный обычный забег. Маршрут: движение→канистра→сумка внутрь/наружу→двигатель→руль→50м→угроза2удара→180м→Lobby. Seed14052026/ContentVersion1, декор отдельным RunRandom stream; гарантированные объекты фиксированы. Простые примитивы/материалы, RU/EN, отдельный checkpoint key и терминальные Completed/Skipped, retry без рекламы. Это минимальные макеты боя/сумки/лодки, не штатные системы. Автовход новичка и миграция настоящего профиля ещё не подключены, мобильного ввода нет. Создана Assets/Scenes/FirstRunPrototype.unity вне Build Settings, Tools/Dead Boat/First Run/Play Prototype. Hook LoadingManager реагирует только на явный выход из прототипа и поднимает LoadingScene перед Lobby; обычная логика запуска не заменена. Прототип не выдаёт реальные награды и не запускает Photon/SDK в своей сцене. Документ: Docs/FIRST_RUN_PROTOTYPE.md.


06.11 итог первого этапа: Validate PASS (ordered gates, повторяемый seed-stream, threat clamp/retry, JSON resume, Completed/Skipped); Play Smoke PASS (проверка близости подбора, сумка внутрь/наружу, двигатель/руль, угроза/финиш), timeScale1, без SaveManager/Photon в отдельной сцене. Методами вызван выход завершённого прототипа: LoadingScene→настоящий Lobby, CurrentLocation=Lobby, временный SaveProvider изолирован, prototype уничтожен, одноразовый return flag=false. Stop/VerifyRestored PASS: owner keys/start scene, ключ прототипа и clock восстановлены; backup inactive, Play/compilefalse, Runner0, Shared/SoloFalse. Это не ручное прохождение, не мобильная приёмка, не первый вход реального профиля. Сборки нет. Далее ИИ — профильное состояние/миграция и штатные игровые механики; владелец позже общий тест темпа.


## Уточнение направления первого рана


Уточнение владельца: заменить направление автономного greybox на существующее начальное обучение и сценарный ран≤10мин,2здания+короткий подход к мини-дракону первой локации. Нужны насыщенный маршрут, животные и разные карточки. Рекомендована ручная карта без обычных случайных спавнов. Актуальный план v2: Docs/FIRST_RUN_PROTOTYPE.md (верх документа); v1 оставлен как архив, не реализация новых требований. По коду подтверждены существующие шаги торговли/подбора/заправки и сброс HP дракона из _starsHP в Start. Учебный вариант босса требует отдельного финала от обычного achievement/Win. Постоянные награды и точные HP не утверждены. Код/карта v2 в этом уточнении не создавались. Далее ИИ — перенос Tutorial prefab на ручную сцену и два здания со штатными механиками, владелец позже ручная приёмка темпа. Без сборки.

## Ручная учебная сцена на штатных механиках


06.11 перенос на штатные механики: создана FirstRunAuthored.unity (пристань/семь исходных шагов из Forest, 2 ручных здания Z300/600, реальные слиток/уголь/яйцо и пистолет/патроны/бочка, 2 слабых Zombie, маршрут900м). Отключены случайные спавны/переразмещение земли/ZoneCleaner в новой сцене, магазины закреплены. Исправлен TutorialStep8 для покупки, выпадающей в мир, через StorePoint.LastPurchasedItem. Validate PASS после повторного открытия; Play smoke PASS через реальные обработчики подбора/продажи/мешка/покупки/заправки и QuickStop для руля, без эмуляции пользовательского ввода; NavMesh обоих охранников доступен, Runner0. Stop/VerifyRestored PASS: сохранения владельца/режим/start scene/build scenes/clock восстановлены, Forest не изменён. Без сборки. Это основа v2, а не готовое обучение: мини-дракон/финал, направляющий сценарий остановок, карточки/временное животное, профильная миграция/чекпоинты и ручная/мобильная приёмка впереди. Подробности и меню изолированного запуска в Docs/FIRST_RUN_PROTOTYPE.md. Далее ИИ — сценарные события и карточки/животное/мини-босс; владелец позже общий плейтест.

## 07.11: сценарное обучение, карты, яйцо/спутник и мини-дракон


07.11: учебный сценарий v2 реализован в FirstRunAuthoredRoute: исходные7шагов + последовательные RU/EN подсказки, две обязательные остановки/охранники, яйцо с one-shot receipt, временная курица с бонусами каталога, два бесплатных предложения существующих карточек в штатном UI/FEEL, отдельный мини-дракон120HP/scale2.5 и учебный финал/Lobby без обычного Win. Ожидание лодки на300/600м не тратит топливо; потерянные обязательные предметы и запас боеприпасов защищают от тупика. Обычные квесты выключены только в учебной сцене, перебросы скрыты на учебном выборе, egg-toast не перекрывает карты. Исправлен общий race укладывания/быстрой экипировки PickableItem, скрывавший активное оружие. Play full PASS: исходная торговля/топливо, восстановление сожжённого яйца/выпавшего пистолета, обе схватки, receipt без дублей, временный pet/buff/follow, реальные карточные кнопки/+1 слот/SteadyAim, пистолет/патроны/выстрел/одна аварийная коробка/реальная перезарядка, HP босса после Start/смерть/финал/Lobby/cleanup, Runner0. Отдельный abort во время карточки PASS, модальный UI и скрытые кнопки восстановлены. Editor проверка с изолированными run+egg storage; это ускоренный smoke, не ручной плейтест. Сборки нет. DB-ON-047 (реальный первый вход/профильная миграция/refresh checkpoints/одноразовый выход) ещё не подключена; темп≤10мин, бой, читабельность RU/touch и приёмка владельца впереди. Подробно Docs/FIRST_RUN_PROTOTYPE.md. Далее ИИ — профильный жизненный цикл DB-ON-047; владелец позже пакетный плейтест.

07.11 финальный cleanup PASS: Play/compile/build=false, compilationFailed=false, активная сцена Lobby. VerifyRestored подтвердил owner return/identity/solo/mode/start scene, временный egg scope, clock и точный исходный файл EditorBuildSettings (включая откат добавляемого Unity serialization-default). Исходный BossDragon.prefab byte-hash совпал с HEAD: его10000HP/scale10/default RNG не изменены. Задачи ещё in_progress из-за профильного входа/checkpoints и ручной приёмки.
