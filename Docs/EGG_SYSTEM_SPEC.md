# Egg System Spec (dead_boat)

Updated: 2026-03-05  
Status: implementation in progress, core runtime is already in code.

## 1) Цель механики

Добавить отдельный игровой цикл для яиц:
- подбор яйца в ранe,
- хранение в отдельном хранилище,
- запуск инкубации в гнезде,
- получение животного,
- установка животного в точке размещения,
- пассивный доход от установленного животного.

Механика должна работать независимо от обычного инвентаря.

## 2) Данные и сохранение

Точка хранения: `EggFeatureStorage` (`StorageKey = EggFeatureState_v1`).

Модель:
- `ownedEggs[]` - количество яиц по `eggId`,
- `ownedAnimals[]` - количество животных по `eggId`,
- `nests[]` - активные инкубации (`nestId`, `eggId`, `finishAtUnix`, `durationSeconds`),
- `placedAnimals[]` - размещенные животные (`pointId`, `eggId`, `lastIncomeUnix` + legacy поля).

Требования к данным:
- никакой потери прогресса при пустом/битом json,
- fallback на `PlayerPrefs`, если `MirraSDK` недоступен,
- нормализация и дедупликация записей при каждом `Load/Save`.

## 3) Каталог и параметры

Каталог: `EggHatchingCatalog` (`ScriptableObject`), ресурс по умолчанию: `Resources/Eggs/EggHatchingCatalog`.

Поля `EggHatchingDefinition`:
- `eggId`,
- `title`,
- `incubationSeconds`,
- `skipCostGems`,
- `passiveIncomeCoins`,
- `passiveIncomeIntervalSeconds`,
- `animalPrefab`,
- `eggPreviewPrefab`.

## 4) Runtime-поток

1. Игрок подбирает объект с `EggCollectibleItem`.
2. `Inventory.AddItem` перехватывает его и вызывает `EggHatchingManager.RegisterEggPickup`.
3. Яйцо уходит в `EggFeatureState.ownedEggs`.
4. Игрок открывает гнездо (`EggNestPoint`) и выбирает яйцо в `EggNestSelectionPanel`.
5. `EggHatchingManager.TryStartIncubation` создает запись в `nests`.
6. По таймеру/скипу (гемы) гнездо становится готовым.
7. `TryCollectReadyAnimal` переносит результат в `ownedAnimals`.
8. Через `AnimalPlacementSelectionPanel` животное ставится в `AnimalPlacementPoint`.
9. `EggHatchingManager` начисляет пассивный доход по интервалам.

## 5) Спавн-баланс яиц

`EggSpawnRuntimeState` снижает effective chance после каждого подбора яйца в рамках текущего забега.

Параметры:
- `decreasePerCollected`,
- `minChanceMultiplier`,
- reset в начале рана (`GameManager.Start` / `EggSpawnBalancer`).

Интеграция:
- `LocationItemSpawnCollection` применяет модификатор только к префабам с `EggCollectibleItem`.

## 6) Сценовые объекты

Обязательные runtime-компоненты:
- `EggHatchingManager`,
- один или несколько `EggNestPoint` (уникальный `nestId`),
- `EggNestSelectionPanel` (+ slot prefab),
- `AnimalPlacementPoint` (уникальный `pointId`),
- `AnimalPlacementSelectionPanel` (+ slot prefab),
- при необходимости `EggNestUI` и `EggStorageDisplay`.

## 7) Известные риски и долги

- Не завершена финальная инспекторная проводка панелей в лобби.
- Нужен полный smoke на перезаходах и миграции сохранений.
- В UI остаются hardcoded строки (`Ready`, `No income`).
- Требуется финальная балансировка времени инкубации, шансов спавна и дохода.

## 8) Критерии приемки

- Сквозной сценарий работает без ручной правки данных.
- После перезахода состояние гнезд/животных восстанавливается корректно.
- Пассивный доход начисляется стабильно и без дублирования.
- TODO/Journal/Docs синхронизированы после каждого заметного изменения.
