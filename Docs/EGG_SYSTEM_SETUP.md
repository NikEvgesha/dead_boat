# Настройка egg-механики в Unity (dead_boat)

## Что уже реализовано кодом

- Отдельное хранилище яиц/животных (`EggFeatureState_v1`).
- Инкубация и skip за гемы (`EggHatchingManager`).
- Размещение животных и пассивный доход (`AnimalPlacementPoint` + `EggHatchingManager`).
- Понижение шанса спавна яиц внутри забега (`EggSpawnRuntimeState`).

Ниже - только шаги сценовой настройки.

## 1) Каталог яиц

1. Проверь `Assets/Resources/Eggs/EggHatchingCatalog.asset`.
2. Для каждого яйца заполни:
   - `eggId`,
   - `title`,
   - `incubationSeconds`,
   - `skipCostGems`,
   - `passiveIncomeCoins`,
   - `passiveIncomeIntervalSeconds`,
   - `animalPrefab`,
   - `eggPreviewPrefab`.

## 2) Менеджер механики

1. На сцене должен быть один `EggHatchingManager`.
2. В инспекторе проверь:
   - `Catalog` (можно оставить пустым, если используется `Resources/Eggs/EggHatchingCatalog`),
   - `Catalog Resource Path`,
   - `Nests`,
   - `Animal Points`,
   - `Animals Root`,
   - `Auto Load On Start = true`,
   - `Auto Collect Finished Eggs = true`.

## 3) Гнезда

1. На каждый объект гнезда повесь `EggNestPoint`.
2. Заполни уникальный `Nest Id`.
3. Привяжи `Egg Visual Anchor`.
4. При необходимости добавь legacy `Animal Spawn Points` (для старых данных).

## 4) Панель выбора яйца

1. Создай/проверь UI-объект с `EggNestSelectionPanel`.
2. В `EggNestSelectionPanel` проставь:
   - `Panel`,
   - `Grid` (`DynamicGridSpawner`),
   - `Slot Prefab` (`EggNestSelectionSlot`),
   - `Nest Label`,
   - `Empty State`.
3. На кнопку закрытия повесь `EggNestSelectionPanel.CloseFromButton()`.
4. Если панель отдельная для конкретного гнезда, привяжи ее в `EggNestPoint._selectionPanel`.

## 5) Точки размещения животных

1. На каждую точку повесь `AnimalPlacementPoint`.
2. Заполни уникальный `Point Id`.
3. Привяжи `Spawn Anchor`.

## 6) Панель выбора животного

1. Создай/проверь UI-объект с `AnimalPlacementSelectionPanel`.
2. В инспекторе проставь:
   - `Panel`,
   - `Grid` (`DynamicGridSpawner`),
   - `Slot Prefab` (`AnimalPlacementSelectionSlot`),
   - `Point Label`,
   - `Empty State`.
3. На кнопку закрытия повесь `AnimalPlacementSelectionPanel.CloseFromButton()`.
4. При необходимости привяжи панель напрямую в `AnimalPlacementPoint._selectionPanel`.

## 7) Интеграция со спавном

1. В `LocationItemSpawnCollection` убедись, что egg-префабы добавлены в `spawnEntries`.
2. На префабах яиц должны быть:
   - `PickableItem`,
   - `EggCollectibleItem` с корректным `eggId`.
3. На сцене добавь/проверь `EggSpawnBalancer`:
   - `Decrease Per Collected`,
   - `Min Chance Multiplier`,
   - `Reset Run On Start`.

## 8) Smoke-чеклист (обязательный)

1. Подобрать яйцо в ранe.
2. Открыть гнездо и выбрать яйцо.
3. Проверить таймер и превью яйца в гнезде.
4. Проверить skip за гемы.
5. Дождаться готовности и получить животное.
6. Поставить животное в `AnimalPlacementPoint`.
7. Проверить начисление пассивного дохода.
8. Перезайти в сцену и убедиться, что состояние восстановилось.
9. Снять животное и убедиться, что оно вернулось в хранилище.

## 9) Что проверить перед релизом

- Нет дубликатов `nestId` и `pointId`.
- UI-панели корректно закрываются при открытии других окон.
- На мобильном вводе нет зависаний курсора/окон.
- При отсутствии `MirraSDK` сохранение работает через `PlayerPrefs`.
