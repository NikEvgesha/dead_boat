# Настройка механики профессий в Unity (dead_boat)

## 1) Создать каталог профессий

1. Создай `ProfessionCatalog`:
   - меню `Create -> ScriptableObject -> Professions -> ProfessionCatalog`.
2. Добавь профессии в `Definitions`.
3. Для дефолтной профессии (`Юнга`) поставь `defaultUnlocked = true`.
4. Для каждой профессии укажи:
   - `professionId` (уникальный),
   - `title`,
   - `description`,
   - `icon`,
   - `starterItems` (префаб + amount),
   - `startCoinsBonus` / `startGemsBonus`,
   - `perkLines`.

## 2) Подключить каталог в сцене

1. На объекте со `StarterPackManager` проставь поле `Profession Catalog`.
2. На объекте с `ProfessionSelectionPanel` проставь тот же `Profession Catalog`.

Это гарантирует одинаковый источник данных для UI и старта.

## 3) Настроить UI панели профессий

На объект с `ProfessionSelectionPanel` повесь ссылки:
- `Panel`,
- `Profession Title Text`,
- `Profession Description Text`,
- `Starter Items Text`,
- `Perks Text`,
- `Status Text`,
- `Message Text` (опционально),
- `Unlock Price Text`,
- `Profession Icon`,
- `Lock Object`,
- кнопки `Apply/Next/Prev/UnlockRandom/Close`.

Рекомендуемая логика кнопок:
- `Apply` видно только для открытых профессий;
- для закрытых показывается `Lock Object`;
- `UnlockRandom` всегда видна, но отключается, когда все открыты.

## 4) Настроить NPC точку

1. На NPC-объект добавь `ProfessionNpcPoint`.
2. Добавь `Trigger`-коллайдер зоны взаимодействия.
3. В `ProfessionNpcPoint` проставь:
   - `Info Canvas` (подсказка "E выбрать класс"),
   - `Selection Panel` (или оставить пустым для авто-поиска `Instance`),
   - `Touch Open Button` (опционально для mobile hint),
   - `Close Panel On Trigger Exit` по необходимости.

## 5) Проверить стартовые бонусы

1. Выбери профессию в панели.
2. Запусти новый ран (ветка, где нет активного сохраненного забега).
3. Проверь:
   - стартовые предметы профессии добавлены в инвентарь,
   - валютные бонусы добавлены.

## 6) Smoke-чеклист

1. С чистого сейва открыть панель профессий, убедиться что открыта только `Юнга`.
2. Попробовать выбрать закрытую профессию - должно быть заблокировано.
3. Нажать "случайная профессия за гемы":
   - гемы списались,
   - открылась случайная закрытая профессия.
4. Выбрать открытую профессию, перезапустить игру - выбор должен сохраниться.
5. Запустить новый ран и проверить стартовые бонусы.
6. Повторить до состояния "все профессии открыты" и проверить disable кнопки unlock.
