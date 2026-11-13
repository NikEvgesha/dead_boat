---
id: DB-ON-269
title: Ограничить ожидания SDK, готовности лодки и социальных запросов
status: ready
when: next
ai: true
---

## Контекст

Продолжение DB-ON-054, связи DB-ON-004/010/012/026/036/268. LoadingCollection ждёт MirraSDK.WaitForProviders без собственного deadline; GameLoader после активации сцены без предельного срока ждёт BoardController.StartGame. У social restore/login/account/chat есть await/semaphore без игрового предельного ожидания. Это конкретные риски вечного ожидания; внутренняя политика timeout SDK и причина старого зависания этим чтением не доказаны.

Photon start/join/shutdown уже ограничены, а scaled-time проблема DB-ON-054 уже исправлена. Не повторять эти реализации и не запускать неподготовленный мир ради скрытия загрузочного экрана.

## Готово, когда

- Каждая игровая операция имеет явные pending/success/error/cancel/timeout состояния с unscaled deadline и локализованной RU/EN повторной попыткой.
- Поздний callback после отмены/смены сцены/повторной попытки не активирует старые менеджеры и не переписывает актуальную сессию/сохранения.
- При неготовом provider сохраняется целостность прогресса: нет подмены пустым save и запуска мира с непроверенной готовностью. UI объясняет конкретный недоступный этап.
- Социальная ошибка не блокирует доступную локальную игру; semaphore и кнопки освобождаются корректно, параллельные повторы не дублируют запросы/подписки.
- Проверены отсутствие/задержка ответа, ответ после timeout, повтор, смена сцены, реклама/нулевой timeScale и отключение сети. Существующие Photon cancellation/recovery сценарии не ухудшены.
- Владелец позднее принял общий WebGL-пакет; до этого in_review, а не done.

## Подсказки ИИ

Работать в собственных адаптерах, SDK не трогать. Task.WhenAny сам по себе не отменяет SDK операцию; нужны generation/cancellation guards и cleanup для позднего результата. Не бросать живой Runner fire-and-forget. Отделять deadline загрузки мира от offline-режима социальных функций. Не утверждать, что Mirra SDK бесконечно ждёт: измерены/прочитаны только наши границы ожидания.

Следующий шаг — ИИ: начать с загрузки provider/boat, затем social UI с изолируемыми delayed/no-response пробами. Владелец позднее принимает сообщения/повтор в WebGL. Сейчас без сборки.

## Источники

- Docs/WEB_OPTIMIZATION_AUDIT_20261113.md
- Assets/Scripts/LoadingCollection.cs
- Assets/Igrodelnya/GameLoader.cs
- Assets/Online/MirraSocialService.cs
- Assets/Online/MirraLobbyChat.cs
- Docs/kanban/tasks/DB-ON-054.md
- Docs/kanban/tasks/DB-ON-026.md
