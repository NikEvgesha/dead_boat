# Streaming мира — проверка 06.11.2026

Unity6000.3.9f1 Editor, Forest, четыре настоящих Photon Shared подключения и один игровой мир. Три дополнительных Runner — транспортные peers, не три независимых клиента WebGL. Изолированный SaveProvider; SDK и FEEL не менялись. Сборки/загрузки нет.

## Реализовано

WorldRegionStreaming автоматически вызывает существующую capture/remove/rehydrate транзакцию для procedural roots. Четыре аудита в секунду, одна попытка транзакции, возврат раньше удаления. Окна вокруг всех подключённых crew и лодки:600м для возврата,800м для удаления, плюс диапазон геометрии с запасом100м. Проверки moved actors, burning enemies, inventory/cargo/pending/untracked; отсутствие данных о живом игроке откладывает аудит. Выключение новых удалений возвращает старые sleeping roots постепенно.

Канонические сетевые страницы не выгружаются. Архив ограничен128регионами/4096записями; отказ сохранить оставляет геометрию живой. Это пока не жёсткий предел всей памяти забега. Городские магазины, отдельные ночные спавны, обычный offline/solo и базовый ландшафт не подключены к этому планировщику.

## Зафиксированные результаты

| Финальный четырёхсоединительный прогон | Активные roots | MeshFilter внутри roots | Sleeping | Архив: регионы / records |
|---|---:|---:|---:|---:|
| Команда разошлась, nominal route5000 | 13 | 1584 | 11 | 11 / 95 |
| Сблизилась, WorldRear4000 | 11 | 1380 | 2 | 2 / 20 |

На первом снимке24дескриптора представлены13активными и11спящими регионами. Это не отдельный A/B замер без streaming. Во втором снимке было11успешных удалений и2возврата, отказов capture/restore0. После окончательной очистки осталось67общих item records; stress добавляет100физических предметов и20трупов вне процедурных roots.

Дополнительный автоматический возврат: тест переместил настоящий crew-аватар по логическому Z к sleeping region2, обычный LateUpdate восстановил один root.8item records/5enemy records сохранили mode/ammo/HP; копий локации нет. Аватар перемещался вне коллайдеров добычи, не физически плыл на лодке. Потом его позиция возвращена. Полный stress завершён PASS, контроль удалённого тела sleep/wake/sleep тоже PASS.

Двух-peer controlled ruins_1:152–153MeshFilter →0при удалении →возврат, два цикла, гистерезис и NaN, moved enemy/burn guards, disable-drain, consumed/dead не воскресли, HP13/ammo1, detached вещь одна, архив11records не растёт. Тот же audit проверяется после настоящей смены ведущего; прежний cold join, rollback/retry и полный ReadyBarrier regression PASS на финальном коде. LayoutProbe отдельно:4реальных пула/11264сравнения legacy/plan/replay/RNG/manifest PASS.

На участке разрыва финального Editor-прогона наблюдались примерно17.5–20FPS, включая создание геометрии. Editor allocated memory около2305MB с плагинами/ассетами/Profiler; это не WebGL heap и не браузерная производительность. Нельзя выводить процент ускорения относительно старых прогонов с другим состоянием редактора.

## Ещё требуется

ИИ: адаптеры городов и независимых спавнов, удержание/архив всей мировой истории и фактический бюджет длинного мира, затем полный profile. Проверки при проходе порогов на реальных устройствах: видимое появление/исчезновение геометрии, физические взаимодействия и независимые клиенты. Владелец: общая WebGL-приёмка после отдельно запрошенной сборки. DB-ON-125 остаётся in_progress.

Cleanup: VerifyRestored PASS, Play/compile=false, Runner0, Shared/Solo=false, Regions/Membership/FixCoordinate=null, temporary clock=false, PeerModeSingle. Выбранные логи локально: Logs/WorldStreaming-20261106.log.

## Продолжение: города и независимые спавны

После описанного выше этапа добавлены WorldCityRegion/WorldCityStreaming и WorldIndependentStreaming. Весь городской prefab (включая магазины) удаляется и собирается заново с прежним city:Z, а canonical записи сохраняют изменения содержимого. Витрина распознаётся строго по StorePoint._displayModel. Незавершённые покупки, rewarded callbacks, банк, переносимые вещи и близкая физика удерживают root. Seeded ассортимент не использует порядок кадров/глобальный RNG. CitySpawner в Shared использует фронт и заднюю границу команды.

Первый успешный Forest/4Cloud прогон: настоящий городской prefab1177MeshFilter/9магазинов, временно добавленные2authored pickups и1seeded defender. Два полных удаления/возврата: магазинные пары item+price прежние, consumed отсутствует, оружие возвращается с актуальной ammo5 (не старой7), defender с HP13 (не17), затем не появляется после canonical death. Pending rewarded callback запрещает удаление. Публичная покупка и изолированные coins не меняются при выгрузке города. Только игровые монеты стенда: IAP/реальная реклама не вызываются. Шаблоны/сцены на диске не изменяются.

Независимый настоящий Zombie.prefab с night key: удаление не пишет смерть, возврат сохраняет ID/HP17 и одну копию, canonical death во время сна запрещает воскрешение. Независимые known-template предметы используют тот же принцип; региональные члены, inventory/cargo и burning не затрагиваются. У каждого адаптера до4аудитов/с и одна транзакция; это пока не общий лимит работы кадра. Лимит dormancy4096ID не является ограничением всей сетевой истории.

SharedStoreSeedProbe PASS:48реальных магазинов/140слотов/420сравнений, перестановка roots, независимый RNG, обратный порядок инициализации, regenerated city/fixed stock. LayoutProbe PASS:4пула/11264сравнения. Повтор полного ReadyBarrier на новом коде не дал общего PASS: первый запуск потерял Spawned BoatSpeed, второй прошёл ready/boat и region handoff, затем Photon ServerLogic kick прервал cold join. Причина этого отказа ещё не установлена; прежний полный PASS выше относится к предыдущему этапу. Не подменять им новый результат.

Повтор городского стенда также встретил загрузку Forest133с и timeout sealed crew; это отдельный незавершённый прогон, не PASS. Первые функциональные PASS городов/ночного врага сохранены в Editor.log. Дополнительный контроль disable-drain купленного предмета добавлен в стенд и проверяется отдельно. Смена authority/cold join именно города ещё не приняты. Совместимость клиентов разделена суффиксом regions1, wire records не расширены. SDK не менялись, WebGL не собирали/не загружали.

Финальный повтор городского стенда PASS на актуальном коде:1177MeshFilter/9магазинов/3контролируемых actors, оба цикла, HP/ammo/consumed/dead/coins/public drop. Дополнительный disable-drain независимых акторов тоже PASS: купленный drop восстановлен с canonical WeaponAmmo, ровно одна копия. Четыре настоящих Photon connections и один игровой мир; не независимые браузеры. Локальные выдержки журнала: Logs/WorldCityStreaming-20261106.log.

Далее ИИ: отдельные city handoff/cold join и общий бюджет длинного мира/справедливое планирование отказов; полный сетевой regression при стабильном стенде. Владелец: будущая пакетная WebGL-приёмка после отдельной сборки. DB-ON-125 остаётся in_progress.


Последний чистый повтор общего сетевого стенда: ready/boat PASS, следующий запуск комнаты отвергнут Photon с DisconnectedByPluginLogic. Три незелёных общих прогона не считать успешной регрессией; источник ServerLogic/PluginLogic ещё не установлен. Дополнительные повторы без новой диагностики не выполнять. Городской финальный adapter PASS выше остаётся действительным.


Окончательная очистка PASS: VerifyRestored owner return/identity/solo/mode keys и playModeStartScene; Play/compile=false, Runner0, Shared/Solo=false, Regions/Membership/Independent=null, clock=false, PeerSingle. Редактор оставлен вне теста, сборки нет.


## 06.11 — city reconnect/handoff: три сценария завершены

WorldCityRecoveryProbe PASS: (1) surviving client со спящим городом принимает master/pages; (2) клиент выходит, теряет archive/membership/registry/старый city ticket и новым Photon Runner подключается к живому забегу; (3) бывший master возвращается новым Runner после takeover второго игрока. В cold-сценариях проверены оба порядка: город раньше dropped discovery и dropped discovery раньше города. Использованы настоящие Cloud connections и штатный городской prefab:1170MeshFilter/9магазинов, controlled3authored pickups/2защитника. Один неактивный локальный мир, ручная child-generation/corpse-cache lifecycle и штатный SelectAssortment без UI; не два независимых браузера.

Во всех3случаях: origin500, seeded item+price прежние, gun ammo1 и enemy HP13 читаются из актуальных записей, consumed/dead не воскресают, отдельно выброшенная вещь вне city root и одна, canonical purchase snapshot с ammo1 один. Повторный sleep/death/restore не возвращает погибшего защитника. Старый ticket при новом контексте отклонён. Проверяется не только GO, но и неизменность canonical tombstones/ammo/HP.

Runtime-поправка ReleaseDroppedItems оставляет Dropped/Mode0/OwnerNone предмет самостоятельным после полной city generation/успешного restore. Она не выполняется внутри individual factory, чтобы частичный отказ не оставлял объект вне rollback root. SDK, wire records и RNG не менялись. Реальная реклама/IAP/списание coins этим стендом не вызываются; используется уже опубликованный purchase snapshot. Авторизация/привязка вернувшегося игрока и перезапуск браузера — отдельная будущая ручная приёмка.

Первые повторы показали тестовую гонку сразу после HasStateAuthority: прямые fixture writes ammo1/HP13 читались локально, но затем сменялись последним входящим snapshot ammo2/HP17. Стенд ждёт350мс после takeover и дополнительно требует сохранности изменений через200мс ДО restore. После этой правки все3сценария PASS; игровой runtime передачи authority не менялся. Это не подтверждение устранения прежнего DisconnectedByPluginLogic из общего ReadyBarrier: текущий стенд использует отдельные UUID пользователей/комнат, а причина прежнего отказа отдельно не установлена.

Дополнительная проверка SharedStoreSeedProbe PASS:48магазинов/140слотов/420сравнений, включая regenerated city/fixed stock. Cleanup PASS: VerifyRestored owner return/identity/solo/mode keys и playModeStartScene, Play/compile=false, Runner0, Shared/Solo=false, Regions/Membership/Independent/FixCoordinate=null, PeerSingle. Локальная выдержка: Logs/CityRecovery-20261106.log. Без сборки/черновика. Изменения Photon SDK не исключаются из Git и в этом этапе не редактировались.

DB-ON-125 остаётся in_progress: ИИ далее — общий бюджет длинного мира/кадра и справедливое планирование отказов городов; владелец позже — общая WebGL-приёмка после отдельно запрошенной сборки. Текущий черновик этих правок не содержит.
