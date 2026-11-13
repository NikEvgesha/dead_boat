---
id: DB-ON-266
title: Синхронизировать ловушки, засады, рычаги и порталы локаций
status: in_progress
when: now
ai: true
---

## Контекст

DB-ON-243 проверяет local gameplay. DungeonTrap хранит локальные bool и напрямую Instantiate врагов с InitializeLevel(1); Portal хранит локальный _isActivate. Новые визуальные локации используют эти механики, но локальная проверка не подтверждает общий кооп.

## Готово, когда

- У ловушки/портала/рычага/решёток стабильные ID и каноническое состояние; один authority принимает активацию и спавнит одну общую группу врагов, без локальных дубликатов у каждого клиента.
- Уровень/профиль/seed охраны соответствуют принятому забегу; фиксированный InitializeLevel(1) и Unity Random не определяют самостоятельно общий бой.
- Все видят открытие/закрытие и один результат активации, повторный input/late callback не запускает засаду заново.
- Состояние и враги сохраняются при выгрузке локации, independent streaming, возврате, смене ведущего и ContinueSolo; уничтоженный/побеждённый объект не воскресает.
- Проверены 2–4 игрока, раздельные HP/рекламная защита, победа/выход/возврат управления. Исходные офлайн-правила сохранены, ручная приёмка отмечена.

## Подсказки ИИ

Assets/Scripts/Action/DungeonTrap.cs; Assets/Scripts/Locations/InteractPoint/Portal.cs; DB-ON-020/125/243; WorldSpawnIdentity, SharedEnemiesRuntime. ИИ — встроить в существующие state/streaming/recovery, не просто broadcast локального метода; владелец — позже пакетная приёмка.

Источник поручения: владелец 13.11.2026; Docs/BRANCH_ACCEPTANCE_AUDIT_20261113.md. Это постановка задачи, не отчёт о реализации. Сборку и публикацию не выполнять автоматически.

## Источники

- Поручение владельца 13.11.2026.
- Docs/BRANCH_ACCEPTANCE_AUDIT_20261113.md.
- Docs/DEVELOPER_HANDOFF_20261113.md.
- Указанные выше код и связанные карточки.

## На конец дня14.11.2026

Реализованы canonical Actions16, полные same-page reservations/cursor/timer guards, Node handlers/lever/portal/podium marker repair, ad/HP gate, streaming/membership/cold templates и optional solo DTO. РовноSklep4×3 иRuins20, fifth trap не запускается; online portal unlock общий, cooldown личный. Обычный offline сохранён. Код компилируется; Player Build/черновик/SDK не менялись.

Текущие source/budget/Portal/Solo(включаяdeferred-save race) PASS. Последний общий native exit1: Network probe `transferred partial cursors preserved`. Ранее4peer batch былPASS дофинальныхправок; он не закрывает текущий отказ. Возможная причина — сравнение последнего локального, ещё не подтверждённого cursor передShutdown, но это не доказано. Статус in_progress сохранён. Работа остановлена по просьбе владельца, новых прогонов сегодня нет.

Следующий шаг — ИИ: логировать partial состояния обоих peers до/после takeover, отделить confirmed state от неподтверждённого tick, исправить реальную потерю при наличии и повторить весь native batch. Потом in_review/125. Владелец — позднее общий WebGL-тест, сейчас действий нет. Подробности и точный handoff: Docs/WORLD_ACTIONS_20261114.md/.json. Настройки6/6 и29 прежних локальных файлов побайтно сохранены.
