---
id: DB-ON-096
title: UI-вывеска Лодки с локализацией
status: in_review
when: now
ai: true
---

## Контекст

Название продажи лодок заменить на Лодки, использовать UI Text и штатную локализацию. Искать существующий ключ, при отсутствии добавить Ru/En. Новые вывески банка и каталога также локализовать в пределах текущего оформления.

## Готово, когда

UI Text с LocalizedText и LocalizationData корректно отображает Ru/En. Старый TextMesh заголовка скрыт. Сохранены существующие ключи и переводы, добавлены только необходимые названия. Сцена и локализация сохранены, переключение обеих строк проверено.

## Подсказки ИИ

Использовать существующий LocalizedText с localizationData/selectedKey/currentLanguage, мировую Canvas и Text без перехвата кликов. Не трогать провайдер языка, игровые меню или покупки. Fusion errors не объявлять успешной компиляцией.

## Источники

- Assets/Igrodelnya/Localization/LocalizedText.cs
- Assets/Igrodelnya/Localization/LocalizationData.cs
- Assets/Igrodelnya/Localization/LocalizationManager.cs
- Assets/Igrodelnya/Localization/LocalizationData.asset
- Docs/kanban/tasks/DB-ON-095.md

## Выполнено

Проверены штатные классы локализации и таблица Ru/En. Есть UI/AnimalBuff/Boat (Boat), отдельного названия Boats нет. Сохранены checkpoint таблицы и сцены в Library/WorkshopBackups/DB-ON-096.

- Созданы три мировые Canvas с UI.Text, raycastTarget=false. Главное название — Лодки. Старые TextMesh вывесок скрыты.
- В LocalizationData добавлены UI/BoatShop/Title (Лодки/Boats), UI/BoatShop/Catalogue (Каталог лодок/Boat catalogue), UI/BankShop/Title (Банк • Кристаллы/Bank • Crystals). Все465 существующих записей и исходные байты таблицы сохранены; теперь468 записей.
- В сцене сохранены штатные LocalizedText MonoScript GUID dd4151974eac4ae4ba8ed7bcb9cf28ae и ссылка LocalizationData GUID247e385684679874b849f4c64cf8b6fc, selectedKey и currentLanguage=Ru для каждой вывески. Новые классы локализации и провайдеры не добавлялись.
- Тип LocalizedText не загружен в Unity из-за недоступной игровой сборки. Поэтому привязки записаны сериализованными полями сцены; их наличие и переводы проверены структурно. Нельзя объявлять успешным runtime-переключение языка до восстановления компиляции. Компоненты пока могут отображаться как Missing Script.
- Сцена повторно открыта, русские UI-тексты отображаются; исходная сцена Lobby/build settings и1514 игровых записей сохранены. Отчёт LOCALIZED_SIGNS096_VALIDATION.json; снимки24-localized-boats096.png,25-localized-bank096.png.

## Ожидает проверки

После исправления существующих ошибок компиляции Fusion/Networked штатный компонент должен разрешиться по своему GUID. Play Mode и переключение языка через LocalizationManager ещё не проверены. Эту проверку нельзя подменять проверкой переводов на диске.

Следующий шаг — команда проекта: восстановить компиляцию и проверить смену Ru/En через существующий LocalizationManager. Владелец проекта: визуальная приёмка UI-вывесок, банка и салона.

