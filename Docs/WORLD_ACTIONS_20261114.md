# DB-ON-266 — состояние на конец дня14.11.2026

Работа остановлена по просьбе владельца. **Статус in_progress**, не done и не in_review: код реализован и компилируется, но последний полный прогон остановился на проверке сохранения частичного курсора волны после смены ведущего. Новые прогоны сегодня не запускать. Player Build, черновик и публикация не выполнялись.

## Реализация

- В SharedWorldPage добавлена bounded Actions16. ID устройств строятся по StablePath после назначения generation-origin; обычные item/enemy ordinal и RNG не меняются.
- Запросы ActivateZone, DungeonTrap, Lever/ActivateObject перехватываются до локальных UnityEvent/coroutine. Authority проверяет фазу, состав, близость, HP и рекламу; принятые callbacks применяются один раз. Pending запросы ограничены128/5с/cooldown0,5с; TriggerStay позволяет повторить вход после readiness gap, известный accepted bit не отправляет RPC повторно.
- Ловушка резервирует все guard IDs и pending enemy records на той же странице, что flags/cursor/deadline. Выпуск снимает Pending и продвигает курсор после успешной инициализации живого актора. Сохраняются исходные12/20 guards и cooldown; уровень берётся из GenerationLevel и текущего balance profile, не из индекса карты. В DungeonTrap нет выбора Random: заданный тип/позиция сохраняются, дополнительный RNG-поток не расходуется.
- Pending/dead guards не материализуются; membership и independent streaming знают ActionId. Есть lazy каталог трёх настоящих enemy templates для cold restore; он не загружается на старте автоматически. Actions учитываются при удалении пустой страницы; HomeZ/Region защищают устройства текущего/сценового города от преждевременного retirement.
- Подношение Skull получает canonical Mode4, marker ConsumedByAction/ActionSlot и маску подиума. Marker/header и дочерние устройства чинятся отдельно от одноразовых presentation callbacks, включая готовность authority после передачи страниц. LastSlot сохраняет точный слот в DTO.
- В коопе портал открывается **для всего экипажа в обе стороны**, а anti-bounce/cooldown1с принадлежит игроку. Телепорт подтверждается только его контексту, позиция логическая с учётом origin, поворот сохраняется. Старый обычный offline relay не менялся. При repair потерянного дочернего unlock применяется конечное открытое состояние без повторной авторской0,1с анимационной задержки; live подношение сохраняет её.
- Состояния/relative remaining timers и pending guards входят в необязательные WorldActions/Enemy DTO ContinueSolo. Legacy JSON допустим. Принятое подношение остаётся consumed и при захвате periodic save в кадре deferred Destroy; отдельный production SampleWorldItem сохраняет marker. Для онлайн-рычага восстановлен accepted звук; cold replay использует конечную позу Animator, а не повторяет волну.

Wire заканчивается **-actions1** после-sales1; старые/новые клиенты не смешивать. RunRandom6, pool revision3 и SoloBranch.v1 сохранены. SDK не редактировался.

## Проверки и точные ограничения

Нативная Unity6000.3.9f1, пустая контролируемая Play-сцена. Физические шаги, обычный AI/navigation и MonoBehaviour Start/Update отключены; ключевые handlers/помпы вызываются явно. Это не независимые браузерные миры и не полное прохождение.

- Source audit PASS:150 проверок четырёх настоящих prefab. У Sklep5 компонентов Trap, но только4 вызова по3 =12. Ruins и NY_Ruins по20. В каждом ruins4 подиума, только1 настроен на Skull;3 декоративных пустых. Native GetPersistentTarget подтвердил валидный visual `skull (1)`, гипотеза missing reference снята.
- Более ранний полный batch завершился exit0:4Cloud peers, настоящие Zone/Lever callbacks, ruins20/Sklep12, повторные запросы, pending/living/dead cold factory, controlled source re-clone, смена ведущего посреди волны, portal и RAM solo. Он предшествовал последним правкам и не выдаётся за финальную приёмку.
- **Текущий финальный Solo probe PASS**, включая relative0,2с,19 оставшихся врагов/1dead, JSON→Suspend→Create/rebind, cursor20, отсутствие повторной волны, marker/header repair обоих направлений, legacy fields и same-frame consumed capture. Известные owner keys не менялись. Это production RAM Create/Suspend и sampler, не полный GameLoader/ContinueSolo UI или запись настоящего провайдера.
- **Текущий Portal probe PASS**: один Skull/marker/маска и один callback, неверный предмет, удалённые child headers/readiness repair без повтора подношения, HP0/ad rejection, отдельные cooldown slots, адресный телепорт. Два выбранных peer проверяются внутри экипажа4 в одном общем тестовом мире; независимая поза второго не заявляется.
- **Текущий Network probe FAIL**: `transferred partial cursors preserved`, после остановки первого master и получения authority страниц. До этого прошли20 guards, подношение, рычаги/cold и начало Sklep. Стенд сравнивает последнюю локальную запись перед Shutdown, не ожидая подтверждения partial cursor у будущего master. Возможная причина — ещё не отправленный последний tick, но это пока гипотеза. Нельзя объявлять runtime исправным только по прежнему PASS или называть отказ просто флейком.
- Промежуточный запуск также получил Photon ExceptionOnConnect до создания комнаты; это отдельный transport отказ. Ранние стендовые ошибки: cleanup API, четыре декоративных подиума вместо одного настроенного, ожидание2owners при4crew — исправлены. Сырые попытки сохранены в Logs/WorldActions266-20261114.
- Scale probe859/75 и пять настоящих пулов×200 PASS. Последний полный процесс exit1, все fixture/Runner очищены; Editor процесса проекта нет. Шесть файлов настроек и29 прежних локальных файлов побайтно восстановлены/сравнены. Автоматическую line-ending-only правку одного mesh вернули после сравнения с HEAD.

| Полный NetworkObject | Байт состояния | Запас до32768 |
|---|---:|---:|
| SharedWorldPage | 24296 | 8472 |
| SharedDepartureState | 17620 | 15148 |
| LobbyNetworkAvatar | 1004 | 31764 |

Это SDK word counts вместе с object words, не traffic/s, history heap, FPS или Web download. Большие визуальные локации/первый ввод/GC остаются задачами125/267–270.

## С чего продолжить

**ИИ — сначала закрыть последний Network probe:** сохранить partial headers/cursors/guard flags у обоих peers до Shutdown и после takeover; дождаться подтверждённого partial состояния перед проверкой его сохранения. Отдельно проверить несохранённый последний tick и отсутствие дублей/зависшего Pending. Если потерялось подтверждённое состояние — исправить runtime, а не ослабить assertion. Затем повторить общий native batch и только при успехе перевести266 в in_review; далее125.

**Владелец — позднее один общий WebGL-проход:** независимые2–4клиента, настоящие физические зоны, закрытие/рычаг/факелы, реальный бой/HP/реклама, портал всей группой, полный refresh/ContinueSolo и residency. Сейчас действий от владельца не требуется.

Код: SharedDepartureState.WorldActions/SharedWorldActionsRuntime, адаптеры Action/Portal, SharedAmbushCatalog, WorldRegion/Independent/Enemies и SoloRunBranch. Entry нативного стенда: `DeadBoat.Online.Editor.WorldAction266Batch.Run` в отдельном idle batch Editor **без -quit**; Prepare — только source/budget checks. Измеренные отчёты и scopes: [WORLD_ACTIONS_20261114.json](WORLD_ACTIONS_20261114.json).
