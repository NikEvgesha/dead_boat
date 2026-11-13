---
id: DB-ON-268
title: Сократить обязательный контент и работу до первого управления
status: ready
when: next
ai: true
---

## Контекст

Дочерняя работа DB-ON-002/027, связана с DB-ON-036/054/262. Native граф enabled scenes + runtime Resources содержит 2696 путей; LoadingScene — 771. Это исходные зависимости, не точный BuildReport и не память. Playable включает 7 сцен, включая Event и FirstRun; скрытие Event в UI не уменьшает начальное скачивание. LoadingCollection → Managers → ItemsManager → All.asset заранее держит ссылки на 87 предметов. Часть закрытого UI/HUD создаётся сразу.

Нужно отделить стоимость загрузки/компиляции payload от подготовки мира и первого ввода. DB-ON-036 уже показывает загрузочный экран до LoadSceneAsync; это не реализация повторно того же экрана.

## Готово, когда

- Есть измеримые этапы download/SDK-ready/scene-activated/boat-ready/first-input с unscaled временем, без токенов и данных игроков в журнале.
- Зафиксирован минимальный обязательный стартовый граф; тяжёлые визуалы и закрытый UI загружаются/создаются по потребности, если профиль подтверждает выигрыш. Каталоги ID доступны восстановлению без обязательной ранней загрузки всех моделей.
- Решение о bundles/Addressables принято по измерениям и совместимости площадок; при реализации убраны включающие прямые ссылки. Ошибки/отмена загрузки дают повтор и корректное состояние UI.
- Версия темы/контента, стабильные ID, старые recovery tickets и одинаковый контент экипажа обработаны совместно с DB-ON-262. Нет дубликатов сцен/менеджеров/ассетов между пакетами.
- Убран доказанный повторный coroutine в начальном Teleport без потери штатной генерации маршрута/сохранений/recovery. Закрытие/повторное открытие lazy UI работает на RU/EN.
- После разрешения на сборку есть BuildReport и cold/warm Network/first-input до/после одного артефакта; владелец принял общий WebGL-пакет.

## Подсказки ИИ

Сначала instrumentation и зависимость LoadingCollection, затем небольшие измеренные изменения. Не начинать массовую миграцию всех ассетов без карты references и rollback. Resources всегда включаются в Player; граф AssetDatabase содержит также редакторские/удаляемые связи. Старый архив сентября/октября не является базой нового пакета. Сохранять паузы SDK, tutorial/recovery маршруты и solo без Photon. Не менять SDK. Media import — DB-ON-267; deadlines — DB-ON-269; поток городов — DB-ON-125.

Следующий шаг — ИИ: поставить измеримые отметки и составить минимальный boot-граф с конкретными кандидатами на перенос. Владелец позднее принимает cold/warm первый вход. Сейчас без Player Build.

## Источники

- Docs/WEB_OPTIMIZATION_AUDIT_20261113.md
- Docs/WEB_OPTIMIZATION_BASELINE_20261113.json
- Assets/Editor/WebDependencyAudit.cs
- Assets/Scripts/LoadingCollection.cs
- Assets/ItemsManager.cs
- Assets/Prefabs/LoadingCollection.prefab
- Assets/Igrodelnya/GameLoader.cs
- Assets/Scripts/Board/BoardController.cs
- Assets/Scripts/Player/PlayerMovement.cs
- Assets/Online/SmokeTest/Editor/MirraCloudSmokeBuild.cs
