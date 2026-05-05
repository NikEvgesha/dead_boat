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
