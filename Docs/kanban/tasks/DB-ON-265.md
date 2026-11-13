---
id: DB-ON-265
title: Проверить сетевой перенос и отображение новых 20 находок
status: in_review
when: now
ai: true
---

## Контекст

Все 20 Loot203_* зарегистрированы в All и SharedItemCatalog; это Treasure/Fuel без ActivateItem. Inventory.AddItem и InventorySlot.OnDrop не допускают их в quick slots. Поэтому отсутствие этих 20 в LobbyHeldItems.asset НЕ является подтверждённым дефектом: world-grab Mode1 использует отдельную реплику. Первоначальную гипотезу о пропущенной экипировке отклонили после проверки полного пути. Новое экипирование сокровищ не входит в текущие правила.

## Готово, когда

- Проверены все 20 моделей у двух клиентов: создание, захват/перенос Mode1, сумка, выброс, крепление к лодке, продажа/сжигание и освобождение предмета. Нет исчезновения, копий и двойного world+held визуала.
- Отдельно проверен прежний экипируемый меч/оружие/купленные ActivateItem: существующий held catalog сохраняет IDs/позы/патроны. Не добавлять 20 неэкипируемых предметов в quick slots ради прохождения проверки.
- Выполнены refresh, cold join, смена ведущего и независимая выгрузка/возврат; учтён дефект масштаба 264. Сравнены размеры и коллайдеры, а не только имена иконок.
- Исторические 67 IDs SharedItemCatalog сохранены, всего 87 ID/20 новых уникальны; одинаковые клиенты используют совместимые версии.
- Структурное покрытие, нативные тесты и реальная WebGL-приёмка записаны отдельно; визуал не принят только по наличию prefab.

## Подсказки ИИ

PickableItem.CheckTags; Inventory.AddItem; InventorySlot.OnDrop; SharedItemsRuntime.Mode1/Mode2; LobbyHeldItemCatalog; LobbyNetworkAvatar; DB-ON-018/019/031/203/264. Доказательства структурного покрытия: Logs/IncomingLocationsAudit20261113/catalog-and-scale-evidence.json (локальный результат, не игроки/секреты).

## Следующий шаг и ответственный

ИИ — закрыть 264 и провести эту матрицу на штатных механиках. Владелец — позднее общий WebGL-тест двух клиентов. Источник поручения 13.11.2026: Docs/BRANCH_ACCEPTANCE_AUDIT_20261113.md. Это задача проверки нового контента, не признанный дефект held catalog. Сборку и публикацию не выполнять автоматически.

## Источники

- Поручение владельца 13.11.2026.
- Docs/BRANCH_ACCEPTANCE_AUDIT_20261113.md.
- Docs/DEVELOPER_HANDOFF_20261113.md.
- Указанные выше код и связанные карточки.

## Готовая основа для матрицы — 14.11.2026

264 передана в in_review: размер экземпляра добавлен во все указанные пути; настоящий Coffer0.72м, native collider, дваCloud peers/cold/independent/takeover PASS. Проверка одного сундука не закрывает все20 находок и полный UI/rejoin/Physics. Далее ИИ берёт эту матрицу на штатных механиках; владелец позднее общий WebGL-пакет. Статус ready сохранён. Сводки Docs/ITEM_INSTANCE_SCALE_20261114.md и Docs/ACTIVE_WORK_QUEUE_20261114.md. Без сборки.

## Проверенный срез — 14.11.2026

Финальный native batch exit0: 20/20 моделей через штатные carry/bag/drop/cargo, 10 продаж/10 burns, FinSword/Pistol quick-hand/magazine3; дубли, отмена и повторное создание мешка, canonical seller price, missing binding retry, credits/stale progress PASS. Отдельно ordinary offline, cold payload/authority takeover, настоящий solo Capture/JSON/legacy и граница WorldRear PASS. SDK не менялся; настройки6/6 и прежние локальные файлы29/29 побайтно сохранены.

Полный результат/scopes и Fusion budgets: Docs/LOOT_AND_SHARED_SALES_20261114.md и JSON рядом. Одна тестовая сцена с двумя Cloud peers не заменяет независимые WebGL-миры, физику, весь coordinator refresh/rejoin и ContinueSolo. Статус in_review, а не done.

Следующий шаг — ИИ: DB-ON-266, затем125/267/270. Владелец позднее принимает общий пакет, включая мешок/выручку после полного возврата и ContinueSolo; сейчас действий не требуется. Без Player Build, архива/черновика/публикации.
