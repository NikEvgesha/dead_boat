# DB-ON-139: подготовка игрового островного лобби

07.11.2026. Статический аудит текущей сцены и маршрутов загрузки. Переключение входа, сцены, prefab и Build Settings не изменялись. Сборки нет.

## Подтверждённые зависимости

- `Assets/Scenes/LobbyIslandWorkshop.unity` содержит 8 `Personal_Island_01..08`; текущий default `LobbyOnlineBootstrap.lobbyCapacity` — 10. Для полного состава нужны ещё две зоны. Лимит комнаты не уменьшали.
- Сцена отсутствует среди включённых Build Settings. `Managers.prefab` задаёт `LoadingManager._lobbyScene: Lobby`; `GameCollection.prefab` содержит override `_lobbySceneName: Lobby`. Изменения только списка сцен недостаточно.
- EggNestTemporaryUIBootstrap, AnimalMergeTemporaryUIBootstrap, ProfessionTemporaryUIBootstrap и ProfessionHudButton содержат имя `Lobby`. Остальные проверки `Location.Lobby`, включая ограничения предметов и возврат, должны продолжить работать при новой физической сцене.
- Корни старого Canvas, EventSystem, Colliders, Home, Objects, EggSystem, Lobby Online и Sun в workshop явно выключены. Компонент LobbyOnlineBootstrap на выключенном Lobby Online не запустит режимы/чат/друзей/личное меню. Включать все старые корни вместе нельзя: это вернёт старую геометрию поверх островов.

## Геометрия и привязки

Повторяемая диагностика: `python tools/assets/audit_lobby_migration.py`. JSON и SHA256 исходной сцены записываются в `Logs/lobby-migration-audit.json`; источник не меняется. Вывод намеренно ограничен каталогом Logs.

В явных YAML-записях прототипа найден 131 anchor-кандидат (98 активных, 33 неактивных). Один полный путь неоднозначен: `Lobby_Island_Visual_Prototype/01_Central_Island/02_Open_Market/02_General_Source_Home_GeneralStore/Future_Supply_Purchase_Anchor101`. У него три объекта с fileID 632333237, 1389126050, 1775058465. Автоматический поиск по первому совпадению непригоден; нужны сериализованные типизированные привязки.

Все 37 явных коллайдеров под прототипом (35 BoxCollider, 2 MeshCollider) находятся в `06_Standalone_Boat_Skin_Studies103`. Явных коллайдеров земли/мостов в этой выборке нет. Rigidbody и разрешённых этим парсером prefab instances под прототипом — 0. Это **не проверка физики в Unity**: наследуемые prefab, stripped-записи и компоненты, добавленные в runtime, инструмент не разворачивает. Нельзя объявлять сцену проходимой по этому отчёту.

## Следующий шаг и ответственный

ИИ — подготовить единое определение роли сцены и типизированные привязки нового лобби; затем отдельные точки появления и коллизии, две дополнительные зоны и перенос сервисов по DB-ON-140–145. Сначала проверка в редакторе, затем переключение маршрутов с сохранением однократного обучения и возврата в живой забег. Владелец — позднее общая игровая приёмка, сейчас действий нет. DB-ON-139 в работе; аудит не означает готовность новой сцены.


## DB-ON-139: общая роль сцены для UI — 07.11.2026

Добавлен LobbySceneRole: точное Ordinal-сравнение старого Lobby и нового LobbyIslandWorkshop. EggNestTemporaryUIBootstrap, ProfessionTemporaryUIBootstrap и ProfessionHudButton используют его вместо отдельной константы старой сцены. Существующие проверки логической Location.Lobby сохранены. AnimalMergeTemporaryUIBootstrap остаётся намеренно неактивным, его устаревший временный UI не включали. Helper не меняет маршрут загрузки, build settings или lifecycle менеджеров; это только подготовка UI к новой сцене. Привязки пастбищ/магазинов/камер и коллизии ещё не реализованы.

Далее ИИ — типизированные ссылки на сервисы/участки, проверка коллизий и двух недостающих зон, затем контролируемое переключение загрузки. Владелец — позднее общая приёмка. Статус in_progress, без сборки.

Проверка этого среза: полный runtime Assembly-CSharp с новым helper скомпилирован Unity Roslyn по текущим rsp/refs/defines/analyzers, exit0 (Logs/LobbySceneRoleCompile). Канбан OK:145, diff --check PASS. Это проверка C#, не import/Play/Photon/проходимость/UI-приёмка; Player build не запускался.
