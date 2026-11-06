---
id: DB-ON-125
title: Ограничить удержание мира без потери изменённых предметов и врагов
status: in_progress
when: now
ai: true
---

## Контекст

Продолжение DB-ON-027: засыпание физики не ограничивает геометрию и сетевые записи между далеко разошедшимися участниками. Нельзя просто уничтожить промежуточные локации: изменённое содержимое не восстанавливается одним сидом. Игровые правила дистанции уже согласованы, не заменять их автотелепортом.

## Готово, когда

- Есть последовательный манифест и безопасное повторное создание локаций с прежним сидом/версией.
- Подобранное, проданное, купленное, выброшенное, инвентарь, патроны, груз лодки и убитые враги восстанавливаются без дублирования.
- Сдвиг координат, поздний вход, reconnect и смена authority проверены при выгрузке/возврате.
- Активный мир и архив имеют измеренный бюджет при длинном маршруте/расхождении; независимый WebGL-тест принят владельцем.

## Сделано

02.11: LocationLayoutPlan отделяет продвижение layout от Instantiate, не включает выгрузку. Четыре настоящих пула / 11 264 сравнений со старой генерацией и replay PASS, включая соло RNG. Пропуск уже окончательно ушедших локаций больше не пишет ложное предупреждение о префабе. Генератор/сетевой протокол не повышались.

Интеграционный стресс4Cloud connections/100предметов/20трупов/маршрут5000 PASS, cleanup rear4000/61предмет. Разрыв всё ещё13.8–14.4FPS в Editor, это не закрытые критерии. Настройки/ключи восстановлены (диск4/4), соединения очищены. Без сборки.

Следующий этап: добавлен локальный LocationLayoutManifest в seeded LocationSpawner, хранит seed/version/cursor и дескрипторы до окончательной очистки за rear. Не создаёт сетевой архив и не выгружает сцену. LayoutProbe PASS: порядок/повторы/пропуски/NaN, строгое сравнение rear, монотонная очистка и10000шагов с окном не более4записей. Четыре реальных пула и11264сравнения старой генерации по-прежнему PASS. Capacity контейнеров не считается измерением памяти; большой разрыв всё ещё удерживает большое окно.

Финальный интеграционный4Cloud stress PASS после исправления live-check (runtime Instantiate/ProBuilder не имеют ожидавшейся тестом связи с mesh asset).23локации/2005authored meshes при разрыве,12/1124после очистки:seed/cursor/clone name/logicalZ PASS. Полный100предметов/20трупов/маршрут5000/rear4000 PASS; диск4/4и настройки восстановлены,Runner0. Остаются мутации/rehydration/сеть/бюджет/WebGL, статус in_progress. Следующий шаг — ИИ: явное удаление временной реплики без consumed/dead tombstone, затем архив состояния региона.

## Подсказки ИИ

Жизненный цикл временной копии: WorldReplicaRemoval выполняет предварительную проверку всего дерева, выставляет instance-флаги и удаляет локальную реплику без tombstone; не используется автоматической выгрузкой. Архив мутаций остаётся обязательным предусловием, interest gating ещё не реализован. Solo-проверка сохраняет free item/ammo7/enemyHP55, обычное Destroy пишет consumed/dead, проверены deferred OnDestroy/replacement, отказ без частичной пометки, pending, inactive untracked inventory и unregistered ID. Финальный двухсторонний тест с guard mode3/remote inventory PASS; ReadyBarrier regression PASS подбора/прикрепления/топлива/патронов/страниц/боя/победы/бонусов/реальной смены ведущего, ready/leave/timeout. VerifyRestored и диск4/4 PASS, Playfalse/Runner0/registry0/soloFalse/sharedFalse/PeerSingle, Profiler и кадры1982–3981 восстановлены. Без сборки. Следующий шаг — ИИ: архив мутаций и подавление автоматического пересоздания вне активных регионов; владелец позже общий WebGL, сейчас действий не нужно.

Добавлен WorldRegionArchive на SharedRunContext: копии полных item/enemy records, ревизия, seed/level/version, consumed/dead, атомарная замена, явная dormancy и предварительные лимиты128регионов/4096записей. Перечисление ограничено ещё при подготовке; отказ не меняет прежний снимок. Это локальный кэш, не постоянное сохранение и не замена сетевых страниц. Register/восстановление dropped items/proxy enemies учитывают только явно спящие записи, живое владение или изменение записи разрешают материализацию. Автоматической активации/выгрузки пока нет.

Проверки архива PASS: изолированные копии/лимиты/атомарность/ревизии/членство/очистка; финальный2peer ReadyBarrier с dormancy/reopen/живыми ammo/HP/защитой коллизий ID и прежним общим regression PASS. Настоящий dropped shotgun в Forest/4Cloud: отсутствие пересоздания при dormancy, одна копия с ammo5 после открытия и новая живая ammo4 без отката PASS; собственный record/registry/GO cleanup PASS. Полный стресс100предметов/20трупов/5000маршрут/rear4000 PASS. Широкая автоматическая выгрузка не включена, критерии карты остаются открыты.

Финальный35с/4Cloud profile PASS,12предметов после cleanup. VerifyRestored/диск4/4 PASS, Play/compilefalse, Runner/registry0, Regions=null, Solo/SharedFalse, PeerSingle, Profiler и кадры1982–3981 восстановлены. Без сборки/черновика. Далее ИИ — полный membership/транзакция региона; владелец позже общий WebGL, сейчас действий не нужно.

Добавлены WorldRegionMembership и WorldRegionCapture для procedural-регионов: begin/seal, история ID после удаления GO, parent→corpse, authored prefab pickups, канонический capture без удаления/активации dormancy. Неполный состав/лимит/конфликт/отсутствующий record отклоняются, старый архив не меняется. Исходный UTF8/FNV hash и RNG сохранены. Выключенная часть будущего трупа распознаётся строго по cached ZombieController.pickable; исключение не действует на произвольного ребёнка, активированный/отсоединённый труп или инвентарь.

Pure и финальный2peer ReadyBarrier с authored pickup/store exclusion/consumed/dead/never-materialized corpse/moved inventory/atomic refusal PASS. Настоящий4Cloud stress100предметов/20трупов/route5000 PASS. Capture при split25регионов/222записи/9authored pickups, после caught-up14/129/6 PASS; rear4000. Найденные неучтённые FinSword/statueGold/skull в ruins_1 исправлены в DB-ON-126 (in_review до WebGL). Города, ночные мобы и полная выгрузка/rehydration ещё не включены, in_progress.

Следующий шаг — ИИ: контролируемая транзакция capture/remove/rehydrate одной целой процедурной локации с обработкой detached/moved вещей; затем пространственные интересы и адаптеры городов/динамических спавнов. Пул LocationSpawnCollection имеет историю неиспользованных вариантов; индекс+сид недостаточны для произвольного доступа. Не редактировать SDK. Окончательную очистку за WorldRear не смешивать с удалением реплик из неактивного региона.

Дополнительный шаг перед полной транзакцией: исправлен сброс HP при Start новой сетевой копии ZombieController. Одноразовая инициализация не вызывает урон/смерть и не применяется к уже запущенному объекту. Отсутствующие proxy-враги используют общий TryCreateReplica; существующие ID/dead/invalid HP/level/pose отклоняются до создания. Настоящий Zombie.prefab + два Cloud peers: HP17/150 переживает ручной Start, новая каноническая HP11/150 — автоматический Start; копии не дублируются, SampleEnemy сохраняет burn-поля, DamagePopup/Death не возникают, обычный спавн с HP по уровню не меняется. Лог EnemyRehydration-20261102.log. Полная локация/её геометрия ещё не удалялась и не восстанавливалась этим шагом; DB-ON-125 остаётся in_progress, сборку не делать.

## Источники

- `Docs/WORLD_STREAMING_PLAN.md`
- `Docs/COOP_PERFORMANCE_20261102.md`
- `Assets/Scripts/Locations/LocationSpawner.cs`
- `Assets/Online/SharedDepartureState.World.cs`
- `Assets/Online/SharedDepartureState.Items.cs`
- `Assets/Online/SharedWorldPage.cs`
- `Assets/Online/WorldRegionArchive.cs`
- `Assets/Online/WorldReplicaRemoval.cs`
- `Assets/Online/WorldRegionMembership.cs`
- `Assets/Online/WorldRegionCapture.cs`

Ответственные: ИИ — реализация и воспроизводимые проверки; владелец — будущая приёмка общей WebGL-сборки. Сейчас сборку/черновик не обновлять.

Финальный ReadyBarrier regression с новым enemy probe PASS: capture/archive/removal, предметы/топливо/оружие/страницы, бой/победа/бонусы, реальная смена ведущего, ready/leave/timeout. Проверка восстановления PASS: диск4/4 ключа, Play/compilefalse, Runner/Agent/registry0, Membership/Regions=null, Shared/SoloFalse, PeerSingle. Editor ProfilerDriver recording=true и кадры1982–3981 сохранены (это отдельный флаг от runtime Profiler.enabled). Логи EnemyRehydration-20261102.log и EnemyRehydrationReady-20261102.log. Доска126карточек проверена. Автоматическая выгрузка/сборка/черновик не включены. Далее ИИ — транзакция целой процедурной локации; владельцу сейчас ничего делать не нужно.

## Явная транзакция процедурного региона — продолжение после возвращения

Добавлен WorldRegionResidency: ручной capture/suspend/restore по сохранённому дескриптору, без продвижения пула и layout. Восстановление собирается под неактивным staging root; использованные/мёртвые/существующие/переносимые записи не создаются заново, живые HP/патроны/позиции читаются из сети. Частичное создание при отказе удаляется без consumed/dead, билет допускает повтор. Автоматического вызова из Update/по дистанции нет. Пока root с переносимым/прикреплённым или близким физическим предметом отказывается от выгрузки; это консервативный прототип, не готовая система пространственных интересов.

Архив хранит полное историческое членство, но dormancy применяется только к фактически удалённым копиям. Вещи, уже отсоединённые от исходного root, не скрываются только по домашнему региону. RestoreGeneration выполняет тот же seeded RNG и бюджет спавнов, даже пропуская dead/consumed. Procedural enemies теперь записывают TemplateId исходного префаба.

Первые тесты выявили: StablePath не находил родителя в неактивном staging (исправлено includeInactive); часть яиц имеет Template=0 в текущем каталоге. Для таких возвращаемых предметов билет дополнительно хранит исходное нормализованное имя префаба и требует совпадения, а не принимает любой нулевой ID. Состав каталога/сетевой протокол/SDK не менялись. Города/ночные спавны, полномасштабный streaming, смена authority/late join при спящем регионе и независимый WebGL остаются непроверенными.

Финальная проверка WorldRegionResidencyProbe PASS: настоящая геометрия ruins_1 с контролируемыми seeded точками двух врагов и добавленным оружием; два полных suspend/restore, consumed/dead отсутствуют, новейшие HP13/ammo1, detached dropped actor не удалён/не скрыт/не дублирован, layout cursor не продвинут. Близкий физический предмет блокирует suspend без частичной выгрузки. Искусственно удалённый canonical item вызывает rollback частичных копий; после возврата записи тот же билет восстанавливает регион. Два Cloud peers подтвердили итоговые записи. Первый повторный suspend в том же кадре отказывался из-за deferred Destroy пропущенных детей; стенд ждёт завершения удаления, защита сохранена. Полный ReadyBarrier regression после этих изменений PASS (включая реальные смену ведущего и ready/leave/timeout), но authority transfer именно во время спящего региона ещё не проверен. Лог WorldRegionResidency-20261102.log.

Далее ИИ: проверить сдвиг начала координат и смену authority/поздний вход при спящем регионе; после этого выбирать пространственные интересы и бюджеты. Автоматическая выгрузка не включена, DB-ON-125 остаётся in_progress. Владелец позднее проверяет общую WebGL-сборку; сейчас сборка/черновик не обновлялись.

## Сдвиг координат и смена ведущего при выгруженном регионе

WorldRegionResidencyProbe расширен контролируемым origin500 через неактивный FixCoordinate без save/bootstrap: проверены логическая позиция root, предмета и врага после восстановления; detached вещь сдвигается отдельно. Это моделирование смещения, не вызов полного игрового FixPosition с лодкой/сейвом.

Дополнительный ReadyBarrier Case region handoff: локальный контекст и билет принадлежат клиенту (remote), во время второй dormancy первый Runner действительно Shutdown, оставшийся получает authority состояния и всех SharedWorldPage. Тот же локальный билет проходит отказ/rollback при отсутствующей записи, затем повторное восстановление, сохраняет HP13/ammo1, consumed/dead и единственность detached вещи. Четыре Case ready/region handoff/leave/timeout и полный прежний regression PASS. Первое исполнение обнаружило гонку harness: клиент восстанавливался до доставки обновлённой ammo/HP; ожидание версии записи добавлено явно. Итоговый handoff=True PASS; после ухода первого peer финальная проверка идёт на surviving authority, не на двух оставшихся клиентах.

Игровой runtime этим шагом не менялся. Это не проверка позднего входа/перезапуска браузера при dormancy: у нового клиента ещё нет локального билета/дескриптора и metadata для catalog Template0. Далее ИИ — отдельный late-join сценарий и способ восстановить локальные дескрипторы из сетевого состояния/seed; затем интересы и бюджеты. Автовыгрузка/сборка/черновик выключены, DB-ON-125 in_progress. Владелец пока не должен перетестировать старую сборку.

## Предметы нового клиента: полнота сетевого каталога

При подготовке cold/late join найден источник Template0: SharedItemCatalogBuilder просматривал только Assets/Prefabs/Items, но генерация ссылается и на другие префабы через LocationItemSpawnCollection. Builder теперь объединяет прежнюю папку со всеми prefab-ссылками коллекций; ID по GUID не меняются, дубликаты GUID убираются, коллизии имени/ID отклоняются. Сборка каталога в staged list атомарна, SaveAssetIfDirty сохраняет только каталог. Каталог расширен56→67, все134ссылки спавна сопоставлены, прежние56ID проверены без изменений.

Residency probe теперь очищает локальные ItemNames билета перед первым возвратом и требует ненулевые canonical Template для всех generated items; восстановление включая яйца проверяет каталог, не имя из прежней копии. Это подготовка отсутствующей метаинформации нового клиента, а НЕ полный late join: локальный layout-дескриптор/история пока есть в стенде. Сетевой wire format, SDK и генератор не менялись. Для полноценно свежего клиента далее нужны replay layout по seed/версии без воскрешения canonical consumed/dead и отдельный новый Runner/browser test. Автовыгрузка/сборка/черновик выключены; DB-ON-125 in_progress.

Финальный ReadyBarrier PASS: возврат с очищенной ItemNames, origin500, handoffFalse/True, consumed/dead/ammo/HP/rollback и старый regression. VerifyRestored/диск4/4 PASS; Runner0, contextfalse, coordinateNull, recordingtrue/1982–3981 сохранены. Следующий шаг — ИИ: холодный replay дескрипторов и настоящий новый peer; владелец пока не тестирует старый черновик.

## 04.11 — возобновление полного cold join сценария

Незакоммиченные изменения после d7ccf974: обычная seeded генерация при наличии канонического item/enemy использует WorldRegionRestore, учитывает consumed/dead и текущие HP/ammo; membership.Observe вызывается и для пропущенных записей; повторно восстановленное яйцо не увеличивает EggSpawnRuntimeState. Добавлен Case region cold join: реальный выход второго Runner, очистка локальных archive/membership/registry, новое подключение к живой сессии и холодное создание content без билета; старый билет отклоняется. Эти изменения пока НЕ проверены в Unity и НЕ объявляются завершёнными.

04.11 Unity запущена заново (E:/Unity/Editors/6000.3.9f1/Editor/Unity.exe -projectPath E:/GitFork/dead_boat), но остановилась на модальном Recovering Scene Backups. MCP endpoints20353/22348 недоступны до закрытия диалога. CUA entrypoint getApp не запускается: failed to write kernel assets / path not found. У владельца запрошены текст/кнопки окна; самостоятельно не выбирать восстановление/удаление неизвестных backup сцен. Это действующий блок проверки, не ошибка игрового теста. git diff --check и kanban126 PASS, сборка/черновик не обновлялись. Далее ИИ — после снятия диалога прогнать cold join и исправлять до результата; затем повторный regression, восстановление настроек и commit/push. Не останавливаться после очередной промежуточной проверки.

## 04.11 — холодное подключение: реализация и сетевой прогон завершены

После отказа от Scene Backups Unity загрузилась, MCP снова доступен. Полный ReadyBarrier с новым Case region cold join PASS. В этом Case второй настоящий Photon Runner выходит из живой сессии, локальные archive/membership/registry уничтожаются, создаётся новый Runner и получает существующее состояние от оставшегося ведущего. Старый ticket отклоняется, локальный manifest создаётся заново; seeded content впервые создаётся обычным LocationContentSpawner.Start, без RestoreGeneration/локального ItemNames. Канонические used/dead/HP13/ammo1/moved item применяются сразу, дубликатов нет, origin500 учтён, membership восстанавливается включая пропущенные tombstones. Обычная генерация теперь распознаёт уже известные сетевые records, а повторно созданное яйцо не меняет EggSpawnRuntimeState. Схема сетевых записей и SDK не менялись.

Отдельный LocationLayoutProbe PASS:4реальных пула/11264сравнения legacy/plan/replay, независимый RNG, manifest/retirement. Это проверка реальных пулов отдельно от сетевого controlled ruins fixture, где дескриптор региона фиксирован. После cold case прошли handoff/предметы/патроны/бой/бонусы/ready/leave/timeout. Лог Logs/ColdWorld-20261104.log. Интеграционный сценарий процедурного региона в Editor закрыт; ручная WebGL-приёмка на независимых устройствах остаётся частью общей будущей сборки, здесь перезапуск браузера/авторизация возвращающегося игрока не эмулируются. Автовыгрузка, города/ночные динамические спавны и бюджеты большого мира — отдельные незавершённые части DB-ON-125.

Очистка PASS: VerifyRestored ключей/стартовой сцены, Runner/registry0, Shared/SoloFalse, Membership=null, FixCoordinate=null, PeerSingle, Profiler recording восстановлен. Сборку/черновик не обновляли. Ответственный далее: ИИ — остальные пункты DB-ON-125 (активные области/бюджеты), владелец — будущая общая WebGL-приёмка, текущий черновик новых изменений не содержит. Сценарий cold content не дробить на новые незакрытые промежуточные шаги.

## 06.11 — автоматический планировщик процедурных регионов

Добавлен WorldRegionStreaming в LocationSpawner только для Shared-забега. streamProceduralRegions=true управляет новыми выгрузками; выключение возвращает уже спящие регионы постепенно. Города/ночные и отдельные динамические объекты/offline/solo не включены. Манифест/RNG/seed/протокол не менялись.

Audit4Гц, максимум одна попытка транзакции за проход, восстановления раньше удаления, round-robin при отказах capture. Логическое Z вокруг лодки и всех подключённых членов экипажа, включая мёртвых/смотрящих рекламу. Без аватара подключённого игрока проход откладывается. Возврат600м/выгрузка800м от диапазона статической геометрии с запасом100м. Actor-renderers не растягивают кэш границ. Moved actors около команды и burning enemies удерживают root. Проверки inventory/cargo/pending/untracked остаются. При лимите архива отказ без уничтожения незаписанных данных; это не жёсткая граница памяти всех сетевых records.

NextRevision из архива, canonical record всегда главнее старого снимка. Ticket до возврата/WorldRear; окончательно ушедшее не воскрешается. Временный отказ restore повторяется через1с, другие билеты не блокируются навсегда. Новых RPC/NetworkObject/UI/автотелепорта нет.

Первый полный SharedReadyBarrier regression PASS: ruins_1,153MeshFilter →0при удалении → восстановление, два цикла automatic Audit, near/far/hysteresis/NaN, moved enemy/burn guards, выключение возвращает root. HP13/ammo1, consumed/dead, detached вещь/manifest/canonical records сохранены, архив11записей не вырос. Проверено на прежнем authority и после настоящей смены ведущего; cold join и прежний предметный/боевой/ready/leave/timeout regression PASS. Editor+Photon, не браузерная приёмка. Четырёхсоединительный Forest stress/бюджеты проверяются отдельно.

Финальный Forest/4Cloud stress PASS:100физических предметов/20трупов/номинальный route5000. При split13roots/1584MeshFilter +11sleeping, архив11/95records; после WorldRear4000 —11roots/1380MeshFilter +2sleeping, архив2/20records,0отказов. Автоматический возврат через обычный LateUpdate и перемещение crew-аватара вернул region2:8items/5enemies, modes/ammo/HP не изменились, root один. Suspended11/Restored2 на финальном снимке. Это4настоящих Photon connections и один игровой мир, не независимые WebGL. Измерения/ограничения: Docs/COOP_STREAMING_20261106.md.

На окончательном коде LayoutProbe4пула/11264сравнения PASS, полный ReadyBarrier с audit/handoff/cold join/предметами/боем/ready/leave/timeout PASS. Controlled geometry152–153→0→возврат. VerifyRestored PASS, Play/compilefalse, Runner0, Shared/SoloFalse, Regions/Membership/FixCoordinate=null, clockfalse, PeerSingle. Logs/WorldStreaming-20261106.log локально. Сборки/черновика нет.

DB-ON-125 остаётся in_progress: ИИ далее — адаптеры городов/ночных независимых спавнов и общий бюджет длинного мира. Владелец — будущая пакетная WebGL-приёмка (пороги/видимое появление/предметы/физика на независимых клиентах). Сегодня действий владельца не требуется.

## 06.11 — города и независимые акторы (проверки в работе)

WorldCityRegion/WorldCityStreaming: полный город со стабильным city:Z, локальное namespace1000000000+Z, общее членство/capture/dormancy и inactive restore. Поддержаны authored pickups и вложенные LocationContentSpawner, seeded магазины создаются заново; деньги и опубликованные покупки остаются личным/внешним состоянием. Вводится guard активной покупки/рекламного callback; stock display распознаётся по точному StorePoint._displayModel, произвольный untracked предмет не считается витриной. CitySpawner в Shared ориентируется на WorldFront/WorldRear команды вместо одной лодки. Без membership-бюджета город остаётся живым.

WorldIndependentStreaming: отдельные known-template free items/ночные враги/подходящие трупы удаляются только как реплики, их ID подавляют повторную материализацию вдали. Возврат читает актуальные canonical HP/ammo/ownership; мёртвые/consumed не возвращаются. Regional members/cargo/inventory/burning/pending/unknown template не удаляются. HashSet dormancy ограничен4096ID,4Гц/одна транзакция; отключение streamActors возвращает sleeping actors. Генератор ночных волн/его счётчик не перезапускается. Это ещё не полный бюджет всех сетевых страниц.

Wire records не меняются, но совместимость игровых клиентов разделена суффиксом regions1 (новые city-identity правила). Старые черновики с новой версией не смешивать. SDK не редактируются, сборки нет.

Стенд: реальный Forest/4Cloud и настоящий city prefab; в его временную копию добавлены два authored предмета и один контролируемый defender, ассеты не меняются. Покупка тратит только изолированные игровые coins, не IAP/настоящую рекламу. Первое предположение теста о готовой добыче в cityPrefab оказалось неверным; второй прогон правильно отказал публикации покупки от аватара в10км — стенд теперь подводит транспортный аватар к магазину, не ослабляя сетевую проверку. Исправлена test-only ошибка LINQ First для AvatarEnumerable. PASS пока не объявлен.


Финальный повтор адаптеров PASS: настоящий Forest,4Cloud connections/один игровой мир; city prefab1177MeshFilter/9магазинов +2authored pickups/1seeded defender. Два цикла geometry removal/restore, pending reward guard, seeded stock неизменён, consumed отсутствует, ammo5/HP13 актуальны, canonical death не воскрешает. Coins и публичная покупка сохранены. Night-key Zombie возвращается один с тем же ID/HP17, dead while dormant не возвращается; disable-drain независимых акторов возвращает купленный drop с canonical ammo и без дубликата. Реальная реклама/IAP не вызывались. Сборки/загрузки нет.

SharedStoreSeedProbe PASS:48stores/140slots/420comparisons (включая regenerated city). LayoutProbe PASS:4пула/11264сравнения. Повторы общего ReadyBarrier сначала прерывались потерей Spawned/Photon ServerLogic; полный результат учитывать отдельно от зелёного городского стенда. Конкретный city cold join/handoff ещё не проверен. Подробности: Docs/COOP_STREAMING_20261106.md.

Далее ИИ: city cold join/handoff, справедливый аудит отказов, общий бюджет кадра/длинной сетевой истории. DB-ON-125 остаётся in_progress. Владелец позже проверяет общую WebGL-сборку; сейчас действий не требуется.


Последний чистый повтор общего сетевого стенда: ready/boat PASS, следующий запуск комнаты отвергнут Photon с DisconnectedByPluginLogic. Три незелёных общих прогона не считать успешной регрессией; источник ServerLogic/PluginLogic ещё не установлен. Дополнительные повторы без новой диагностики не выполнять. Городской финальный adapter PASS выше остаётся действительным.


Окончательная очистка PASS: VerifyRestored owner return/identity/solo/mode keys и playModeStartScene; Play/compile=false, Runner0, Shared/Solo=false, Regions/Membership/Independent=null, clock=false, PeerSingle. Редактор оставлен вне теста, сборки нет.


## 06.11 — отдельный стенд city reconnect/handoff

Добавлен WorldCityRecoveryProbe: два реальных Cloud peers, настоящий городской prefab с controlled authored content и двумя защитниками, отдельные UUID пользователей/комнат. Локальный мир держится неактивным, Awake-cache dormant corpse и последовательная генерация вызываются стендом, магазинный выбор проверяется через штатный SelectAssortment без UI/покупок/рекламы. Проверяются спящий город при смене master/pages, свежий Runner и пустые local archive/membership после выхода, origin500, оба порядка city/dropped discovery, canonical purchase snapshot, consumed/dead, ammo/HP, повторный sleep/death/restore. Ручной браузерный reconnect и player-account binding этот стенд не заменяет. Прогоны пока не завершены; PASS не объявлен. SDK и wire records не менялись, сборку не запускать.


По разбору factory найден порядок-зависимый путь: когда город появляется раньше dropped discovery, WorldRegionRestore создаёт известный свободный Dropped предмет внутри городского дерева. Добавлен WorldCityRegion.ReleaseDroppedItems после полной генерации/успешного restore. Отделение не выполняется внутри individual factory: иначе последующий отказ мог бы оставить частичную копию вне rollback root. Cold Start завершает child generation, отделяет canonical Mode0/Dropped/OwnerNone и лишь затем помечает город Ready. При нехватке membership-бюджета отделение также выполняется, но город остаётся невыгружаемым. Wire/RNG/SDK не меняются. Это исходниковая проверка и исправление; сетевой PASS ожидается.


Продолжение после возврата владельца: предыдущий Editor был закрыт, 06.11 запущен Unity6000.3.9f1 для dead_boat заново. Остановился на Recovering Scene Backups; до решения владельца локальный MCP недоступен. Другие процессы unity mcp относятся к VetStend и не используются. Владелец решает судьбу резервных сцен; сцены/Play/owner PlayerPrefs в этом продолжении не изменялись. Новый сетевой PASS отсутствует. В стенд добавлен комбинированный возврат бывшего master после takeover и проверки неизменности canonical consumed/dead/ammo/HP при replay. Далее ИИ — после загрузки редактора прогнать все3сценария, восстановить тестовые настройки и только затем зафиксировать проверенный результат. Без сборки.


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
