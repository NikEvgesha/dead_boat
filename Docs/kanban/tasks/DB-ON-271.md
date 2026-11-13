---
id: DB-ON-271
title: Синхронизировать продажу предметов и сумки с выручкой
status: in_review
when: now
ai: true
---

## Контекст

Блокер обнаружен при DB-ON-265: SellPoint локально создаёт MoneyBag и уничтожает предмет. После выброса Owner=None, поэтому Unregister не гарантирует Mode4; dropped discovery может создать проданную вещь снова. Кинематический прокси также не должен создавать вторую выплату. У MoneyBag выход из зоны во время сбора оставляет guard, мешающий повторному сбору.

## Готово, когда

- Продажа свободной/переносимой вещи подтверждается authority один раз; неподходящий предмет и прокси не получают локальный обход. Consumed реплика не возвращается.
- Баланс и визуал одной сумки одинаковы у участников, включая поздний вход/смену ведущего. При сборе монеты получает собравший игрок, как в прежней механике.
- Повторный сбор, старый revision и старая personal snapshot не удваивают и не отменяют начисление; возврат и ContinueSolo сохраняют подтверждённые деньги/оставшуюся сумку.
- Цена учитывает бонусы продавца, в том числе после выброса, а не автоматически бонусы ведущего. Shared-mode клиентская экономика не выдается за защищённый backend.
- Отмена сбора/выход из зоны/реклама позволяют повторить действие; обычная offline продажа и сбор не регрессировали.
- Нативные и сетевые проверки записаны отдельно от полной WebGL-приёмки. SDK не редактируется; сборка сейчас не выполняется.

## Подсказки ИИ

SellPoint, MoneyBag, SharedDepartureState.Sales, SharedSalesRuntime; связь DB-ON-019/026/125/264/265. Продажа и сбор — разные атомарные операции: canonical consumed + bag balance, затем receipt/revision + personal credit. Нельзя подтверждать одну лишь локальную гибель GameObject. Новые поля требуют отдельного wire suffix, проверки Fusion32KiB и legacy additive JSON; RunRandom.Version ради wire не повышать.

Следующий шаг — ИИ: закончить контракт и выполнить матрицу с реальными SellPoint/FuelDeposit/MoneyBag, затем продолжить265. Владелец позднее принимает общий WebGL-пакет; сейчас действий не требуется.

## Источники

- Docs/kanban/tasks/DB-ON-265.md
- Assets/Scripts/SellPoint.cs
- Assets/Scripts/MoneyBag.cs
- Assets/Online/SharedItemsRuntime.cs
- Assets/Online/SharedDepartureState.Recovery.cs
- Docs/ITEM_INSTANCE_SCALE_20261114.md

## Проверенный срез — 14.11.2026

Финальный native batch exit0: 20/20 моделей через штатные carry/bag/drop/cargo, 10 продаж/10 burns, FinSword/Pistol quick-hand/magazine3; дубли, отмена и повторное создание мешка, canonical seller price, missing binding retry, credits/stale progress PASS. Отдельно ordinary offline, cold payload/authority takeover, настоящий solo Capture/JSON/legacy и граница WorldRear PASS. SDK не менялся; настройки6/6 и прежние локальные файлы29/29 побайтно сохранены.

Полный результат/scopes и Fusion budgets: Docs/LOOT_AND_SHARED_SALES_20261114.md и JSON рядом. Одна тестовая сцена с двумя Cloud peers не заменяет независимые WebGL-миры, физику, весь coordinator refresh/rejoin и ContinueSolo. Статус in_review, а не done.

Следующий шаг — ИИ: DB-ON-266, затем125/267/270. Владелец позднее принимает общий пакет, включая мешок/выручку после полного возврата и ContinueSolo; сейчас действий не требуется. Без Player Build, архива/черновика/публикации.
