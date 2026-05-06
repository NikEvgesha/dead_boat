# Scenario TODO / Roadmap (Eggs + Professions + Balance)

Снимок состояния: **27 марта 2026**  
Назначение файла: единая точка контроля по механике яиц, профессий и баланса игры.

См. также:
- `START_PROMPT.md`
- `GPT_JOURNAL.md`
- `Docs/EGG_SYSTEM_SPEC.md`
- `Docs/EGG_SYSTEM_SETUP.md`
- `Docs/PROFESSION_SYSTEM_SPEC.md`
- `Docs/PROFESSION_SYSTEM_SETUP.md`
- `Docs/BALANCE_ROADMAP.md`

## Легенда стадий

- **Не делалось** - задача не начата.
- **Есть код** - базовая логика реализована, но не доведена в сцене/UX.
- **В процессе** - работает частично, нужны доработки.
- **Нужна настройка в Unity** - код готов, не завершена сцена/префабы/ссылки.
- **На тесты** - код и настройки готовы, нужен smoke.
- **Протестировано** - базовый smoke пройден.
- **Готово** - можно считать закрытым.

---

## 0) Текущий roadmap (3 направления)

| Направление | Цель | Этап | Комментарий |
|---|---|---|---|
| Яйца (переработка текущей версии) | Улучшить UX/надежность и подготовить к релизу | **В процессе** | База работает, но нужен рефактор UX и повторный баланс цикла. |
| Профессии | Довести до реального прод-использования | **В процессе** | V2-код готов: no profession, random за монеты, direct buy/fallback. Нужны сцена, цены, productId и ручной smoke. |
| Баланс игры | Сформировать устойчивую экономику и прогрессию соло-игры | **Не делалось** | Нужна метрика + контрольные таргеты + итеративный прогон. |

Порядок внедрения:
1. Подключить профессии в лобби и сделать их playable.
2. Закрыть переработку/полировку яиц (UX + smoke + баланс спавна/дохода).
3. После включения обеих механик пройти системный баланс ранней/средней игры.

---

## 1) Ядро механики (runtime + save)

| Фича | Статус | Готовность | Комментарий |
|---|---|---:|---|
| Отдельное хранилище яиц/животных `EggFeatureState_v1` | **Есть код** | 90% | Состояние вынесено из обычного инвентаря, есть `Normalize()`, MirraSDK + PlayerPrefs fallback. Нужен прогон миграции старых сохранений. |
| Инкубация гнезд (старт/таймер/скип за гемы) | **В процессе** | 85% | `EggHatchingManager` покрывает старт, skip и автосбор готовых гнезд. Нужно финальное UX-подтверждение в боевой сцене. |
| Размещение животных в `AnimalPlacementPoint` | **В процессе** | 80% | Есть сохранение размещения, восстановление после загрузки и возврат животного в хранилище. Нужен smoke с несколькими point и проверкой edge-case. |
| Пассивный доход от размещенных животных | **Есть код** | 70% | Доход начисляется по интервалу из каталога. Требуется балансировка `passiveIncomeCoins/interval` и UX-обратная связь игроку. |
| Спавн яиц с деградацией шанса по ходу забега | **Есть код** | 78% | `EggSpawnRuntimeState` интегрирован в `LocationItemSpawnCollection`. Нужно отбалансировать параметры в сцене и проверить ощущения на длинном ранe. |
| Автовосстановление визуала гнезд/животных при загрузке | **В процессе** | 80% | Менеджер восстанавливает preview и размещенных животных. Нужен полный регресс после перезахода и смены сцен. |

---

## 2) UI и настройка сцены

| Фича | Статус | Готовность | Комментарий |
|---|---|---:|---|
| Панель выбора яйца `EggNestSelectionPanel` | **Нужна настройка в Unity** | 45% | Код и логика есть; требуется финальная привязка `_panel/_grid/_slotPrefab/_emptyState` и кнопки закрытия. |
| Панель размещения животных `AnimalPlacementSelectionPanel` | **Нужна настройка в Unity** | 40% | Код и слоты готовы; нужна сцена и инспекторные ссылки. |
| UI состояния гнезда `EggNestUI` | **В процессе** | 65% | Технически работает (`empty/incubation/ready`), но строка `Ready` и имя яйца пока без локализации/полировки. |
| Витрина яиц `EggStorageDisplay` | **Есть код** | 60% | Отображает превью из хранилища; нужна проверка лимитов/слотов на боевом UI. |
| Мобильный ввод и курсор для окон выбора | **Не делалось** | 30% | Нужно отдельно пройти touch-флоу открытия/закрытия панелей. |
| VFX/SFX фидбек для старта/скипа/выдачи животного | **Не делалось** | 20% | На данный момент механика функциональная, но без финального фидбека. |

---

## 3) Приоритетный план (ближайшее)

### P0 - блокеры интеграции
- [ ] Подключить механику профессий в реальную лобби-сцену (`ProfessionSelectionPanel`, `ProfessionNpcPoint`, `ProfessionCatalog.asset`) и проверить полный flow.
- [ ] Довести в сцене `EggNestSelectionPanel` и `AnimalPlacementSelectionPanel` до полностью рабочего состояния (все ссылки, кнопки, открытие/закрытие).
- [ ] Пройти сквозной сценарий: подобрать яйцо -> запустить инкубацию -> перезайти -> получить животное -> поставить в точку -> проверить пассивный доход.
- [ ] Сформировать baseline-метрики по балансу (время до первого апгрейда, доход/минута, частота дефицита топлива, шанс смерти по этапам).

### P1 - стабильность и релизная безопасность
- [ ] Проверить миграцию/обратную совместимость сохранений и отсутствие пересечений ключа `EggFeatureState_v1`.
- [ ] Проверить, что `EggSpawnRuntimeState.ResetRun()` корректно отрабатывает на старте новых забегов.
- [ ] Провести smoke на нескольких `AnimalPlacementPoint` (занятость, снятие, повторная установка, восстановление после загрузки).
- [ ] Проверить сохранение профессий (`ProfessionState_v1`) на чистом профиле и существующем профиле.

### P2 - качество и UX
- [ ] Добавить визуальный/звуковой фидбек при старте инкубации, скипе и получении животного.
- [ ] Локализовать пользовательские строки механики (`Ready`, `No income`, подписи в слотах).
- [ ] Отбалансировать `EggSpawnBalancer` и параметры дохода в `EggHatchingCatalog`.
- [ ] Полировка UI профессий по референсам (карточка, lock-state, текст бонусов, мобильная верстка).
- [ ] Внедрить баланс-итерации по `Docs/BALANCE_ROADMAP.md` (не менее 3 циклов: baseline -> tuning -> regression).

---

## 4) Ручной чеклист Unity (операционный)

- [ ] Подключить `EggNestSelectionPanel` в лобби (ссылки: `_panel`, `_grid`, `_slotPrefab`, `_emptyState`).
- [ ] Привязать `EggNestSelectionPanel.CloseFromButton()` на кнопку закрытия.
- [ ] Подключить `AnimalPlacementSelectionPanel` (ссылки: `_panel`, `_grid`, `_slotPrefab`, `_emptyState`).
- [ ] Привязать `AnimalPlacementSelectionPanel.CloseFromButton()` на кнопку закрытия.
- [ ] Проверить, что у всех `EggNestPoint` заполнен уникальный `nestId`.
- [ ] Проверить, что у всех `AnimalPlacementPoint` заполнен уникальный `pointId`.
- [ ] Проставить `EggHatchingManager` ссылки на `catalog`, `nests`, `animalPoints`, `animalsRoot` (или проверить авто-поиск).
- [ ] Проверить работу окна при тач-вводе/курсоре (open/close/переключение окон).
- [ ] Пройти smoke: pickup -> incubation -> skip(optional) -> collect -> place -> remove -> reload scene.

---

## 5) Безопасность сохранений и релиз

- [ ] Подтвердить, что старые сохранения без egg-данных не ломаются и стартуют с пустым `EggFeatureState`.
- [ ] Подтвердить, что в save нет дубликатов/битых записей после нескольких циклов установки/снятия животных.
- [ ] Проверить, что некорректные ссылки в каталоге (битый `eggId`) не вызывают потери остальных данных.
- [ ] Проверить сохранение профессий (`ProfessionState_v1`): текущая профессия и список открытых классов корректно переживают перезапуск.

---

## 6) Сделано

- [x] Создан `Assets/Resources/Eggs/EggHatchingCatalog.asset` c `egg_chicken` и `egg_condor`.
- [x] Настроен автоподхват каталога в `EggHatchingManager` из `Resources`.
- [x] Добавлены префабы яиц с `EggCollectibleItem`.
- [x] Добавлены записи яиц в `LocationItemSpawnCollection.asset`.
- [x] Добавлена отдельная система сохранения (`EggFeatureState_v1`).
- [x] Реализован отдельный сбор яиц вне обычного инвентаря.
- [x] Добавлено снижение шанса спавна яиц по ходу забега.
- [x] Добавлен выбор конкретного яйца для гнезда через отдельную UI-панель.
- [x] Добавлены точки размещения животных и отдельная панель выбора животного.
- [x] Добавлен каркас системы профессий: каталог, сохранение, выбор, рандомное открытие и интеграция в стартовый набор.
- [x] Добавлена v2-логика профессий: старт без профессии, random unlock за soft currency, direct buy за реал или fallback за soft currency.

---

## 7) Профессии (новая механика)

| Фича | Статус | Готовность | Комментарий |
|---|---|---:|---|
| Каталог профессий (`ProfessionCatalog`) | **Есть код** | 85% | Добавлен SO-каталог с описанием, бонусами, стартовыми предметами, random pool, direct buy/fallback полями. Нужно заполнить productId и реальные цены. |
| Сохранение прогресса профессий | **Есть код** | 88% | `ProfessionState_v1`: хранит выбранную/отсутствующую профессию, открытые и купленные профессии. Нужен smoke на clean save и старых профилях. |
| Выбор профессии в UI | **В процессе** | 80% | `ProfessionSelectionPanel` поддерживает no-profession, листание, lock/open, random unlock и direct buy. Нужна сцена и финальная привязка объектов. |
| Открытие случайной профессии за soft currency | **Есть код** | 85% | Кнопка и логика есть: из закрытых профессий random pool выбирается случайная, списываются монеты/настроенная валюта. Нужен баланс цены. |
| Прямая покупка профессии | **Есть код** | 70% | Реализован путь real purchase через `purchaseProductId` и fallback за soft currency на площадках без покупок. Нужны реальные productId и проверка платформ. |
| NPC-точка взаимодействия (`ProfessionNpcPoint`) | **Нужна настройка в Unity** | 45% | Скрипт готов, но требует расстановки в лобби, trigger и hint-canvas. |
| Выдача бонусов на старте рана | **В процессе** | 68% | Профессия уже влияет на стартовый набор и валютные бонусы через `StarterPackManager`/`GameManager`. Нужен сквозной прогон с разными профессиями. |

---

## 8) Правило ведения файла

- После каждой заметной задачи обновлять статус и процент в таблицах выше.
- Все ручные шаги по сцене фиксировать в разделе "Ручной чеклист Unity".
- После заметных изменений в коде обязательно добавлять запись в `GPT_JOURNAL.md`.
- Если обновлялась архитектура/настройка механики, синхронно обновлять `Docs/EGG_SYSTEM_SPEC.md` и `Docs/EGG_SYSTEM_SETUP.md`.

---

## 9) Баланс игры (новое направление)

| Фича | Статус | Готовность | Комментарий |
|---|---|---:|---|
| Базовые KPI/метрики ранa | **Не делалось** | 10% | Нужно зафиксировать целевые значения по времени прогресса, выживаемости и темпу экономики. |
| Экономика (монеты/гемы/топливо/траты) | **Не делалось** | 15% | Нужна таблица источников/стоков ресурсов и контроль инфляции дохода. |
| Баланс сложности по этапам ранa | **Не делалось** | 10% | Требуется кривая давления: early/mid/late без резких провалов или спайков. |
| Баланс синергий профессии + яйца | **Не делалось** | 5% | Проверить, чтобы комбинации не ломали экономику и не давали auto-win в early game. |
| Регрессионный баланс-smoke | **Не делалось** | 5% | Единый повторяемый сценарий для перепроверки после каждого баланс-патча. |

---

## 2026-03-28 update (implemented)

- [x] Pets now can be parented to placement anchors (EggHatchingManager) so they move with the boat when anchor points are on boat hierarchy.
- [x] Profession passive bonuses added to data model (ProfessionPassiveBonuses) and connected to gameplay stats.
- [x] Profession UI now shows numeric passive summary in perks panel.
- [x] Random profession unlock price can now scale by progression (base + step, capped).
- [x] Egg passive income now has configurable balancing controls (soft-cap for many pets + accumulated income cap).

Next manual QA in Unity:
- [ ] Verify pet movement while boat is moving in production scene.
- [ ] Verify passive bonuses for at least 3 professions on new run and resumed run.
- [ ] Tune unlock price (base/step/max) against real economy values.
- [ ] Tune pet income settings (fullIncomeAnimalCount, extraAnimalIncomeMultiplier, maxAccumulatedIncomeSeconds).

---

## 2026-05-05 egg loop v2 clarification

Updated target loop from design discussion:

- Eggs spawn during ordinary runs and are collected into separate egg storage, never into the normal run inventory.
- Lobby nests hatch eggs after win/lose return flow.
- One egg type can hatch into several possible animal variants through weighted results.
- Ready nests should wait for manual animal collection; production should not silently auto-collect ready animals.
- Animals live in a separate animal inventory.
- Boat placement points let the player choose which owned animal to place.
- Placed animals should provide run buffs, not just passive coin income.
- Merge station should merge 2 identical unplaced animals of the same stage into 1 stronger animal.
- Animals need 3-4 stages; max-stage animals cannot merge.
- Higher stages should have stronger buff data and lightweight visual changes: tint/material variation first, particles later.

Implementation backlog for eggs:

- [x] Extend egg save model from `eggId + amount` animals to `animalId + stage + amount`, with migration from old `eggId` animals to stage 1.
- [x] Extend `EggHatchingCatalog` with hatch result pools, animal definitions, stage definitions, stage visuals, and run buff values.
- [x] Change ready nest flow so hatching produces a visible ready animal that waits for player collection.
- [x] Add animal buff aggregator for placed boat animals and apply it to run stats.
- [x] Add merge service/state/UI for two identical unplaced animals.
- [x] Add temporary replaceable UI for animal inventory and merge panel if production UI is not ready.
- [ ] Update Google Sheets balance export/import for egg result weights, animal stages, merge costs, and buffs.
- [ ] Run full smoke: pickup -> storage -> nest -> timer -> collect -> place -> buff in run -> remove -> merge -> save reload.
