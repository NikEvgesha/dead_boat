---
id: DB-ON-131
title: Добавить Верную руку и Новый заход
status: in_review
when: now
ai: true
---

## Контекст
Владелец явно поручил следующую пару, без сборки, затем переходить к другим задачам. Основа: CARD_PROGRESSION_PROPOSAL и DB-ON-130. Числа предварительные до общего теста.

## Готово, когда
- Верная рука I/II/III: неподвижность0.75с готовит один выстрел +30/40/50%, откат3с; движение сбрасывает подготовку, промах тратит усиление.
- Новый заход I/II/III: завершённая перезарядка пустого магазина готовит первый выстрел +25/35/45% на3с. Частичная/отменённая/повтор callback не дают новый заряд; смена оружия/реклама/смерть очищают заряд.
- Весь залп дробовика получает один снятый заранее бонус; группа прямого урона ограничена+100%, никакого бесплатного ammo/proc/RPC. Личные ранги I–III, cap/выдача/ad guard/RU/EN и индикаторы готовности.
- Recovery/solo сохраняют ранги, очищают заряды/подготовку и блокируют aim на3с; смена ведущего у оставшегося игрока не выдаёт новый эффект. Протоколы несовместимых layouts разделены.
- Editor/двух-peer/реальные оружейные маршруты/восстановление проверены, owner settings восстановлены. Владелец проверяет ощущения/баланс/UI в будущей общей сборке; до этого in_review.

## Подсказки ИИ
Изучить RangedWeaponController, CombatCards/Rules/Hud, PlayerMovement, SharedRecoveryData и DB-ON-130. Таймеры игрового времени. Не изменять SDK/FEEL. Не создавать покадровые RPC, обход врагов, материалы/камеры/частицы. Не получать новый заряд при ApplySharedAmmo/выдаче оружия. Смена оружия не сокращает cooldown. Проверять отмену не только при GameObject.OnDisable, но и SetActiveUse(false).

## Источники
- Docs/design/CARD_PROGRESSION_PROPOSAL.md
- Docs/design/CARD_COMBAT_PROTOTYPE.md
- Assets/Scripts/Items/RangedWeaponController.cs
- Assets/Scripts/LevelStat/RangedCardState.cs

Следующий шаг: ИИ — реализация/проверки; владелец — позднее общий тест. После пары вернуться к DB-ON-125.

## Реализация и проверки 06.11.2026

RangedCardState и реальные RangedWeaponController.Fire/Reload/SetActiveUse/OnDisable; личные ранги в Stats/SharedPersonalData Capture/Cards, assets, cap/выдача/ad guard, RU/EN и5строк HUD. Movement-проверка использует CharacterController ground/velocity и ввод. Все временные эффекты локальные; новых сетевых событий/NetworkObject нет. Protocol cards3.

RunRangedTiming PASS: точные границы0.75/3с, движение/смена оружия/cooldown, пустая/частичная/отмена/дубликат/просрочка перезарядки, промах, реклама/death/blocked и консервативный reset. RunRangedExpansion PASS: прямой урон/cap+100%, однократное расходование, рангиIII/offers/ad/assets/text/JSON/recovery; прежние Math и RunLocal PASS.

RunWeaponExpansion PASS после установки timeScale1: настоящий Fire/Physics.Raycast пистолета и дробовика, completed empty Reload даёт +45% всему первому залпу, следующий обычный; частичный Reload без заряда. Реальный CharacterController на полу + PlayerInput: подготовленный стоящий стрелок даёт150вместо100, следующий выстрел100, движение по вводу снимает подготовку, физическое движение тоже обнаруживается. Первый новый прогон был при timeScale0 и правильно отменил эффект. Начальная постановка коллизии tinyMove1e-5 не выставляла Below; диагностирован настоящий collisionFlags/velocity, тест ставит нормальный контакт вниз0.01. Runtime по этому поводу не менялся.

Полный SharedReadyBarrierProbe PASS: два Photon peer, отдельные новые ранги2/1 и1/3, ready/handoff/cold join/leave/timeout и прежняя боевая регрессия. Пока это не браузерная приёмка и не доказанный баланс.

Реальный CombatCardHudProbe PASS: Lobby → Forest → Lobby, новые строки ready+40%/+25%, исчезновение после расходования,20повторных Refresh без нового pulse, прежние heal/combo/HP/pause/ad/death/recovery/RU/EN и стабильные5MMF_Player/камеры. Первый chained запуск ColdRestart после этого UI-стенда не засчитан: прямой возврат UI-пробы оставляет мир без PlayerManager, LendController/LocationSpawner не готовы к новому старту. Проверка cold return запускается отдельно с исходной LoadingScene/изолированным save, без изменения runtime сцены ради стенда.

Финальный свежий SharedRecoveryGameplayProbe.RunColdRestart PASS: настоящий Lobby/Forest, живой transport peer, новая recovery-компонента и persisted ticket; SteadyAimRank2/PatientReloadRank1 восстановлены после возврата и в отдельном solo-сохранении. Деньги237/health80/items17-capacity20/magazine3-reserve31 прежние. Один локальный мир, не два независимых браузера.

RU/EN выбора1920×1080 осмотрены: условия0.75/3с, ранги/проценты/cap без обрезки; Logs/RangedCards-RU/EN-20261106.png. Settled HUD с5строками осмотрен, текст помещается; Logs/RangedCards-HUD-20261106.png. Для HUD-визуала readiness задавалась тестом отдельно от оружия. Первый capture до Canvas-rebuild был пустым, финальный после ForceUpdateCanvases корректный. Gameplay/ready тест выполнялся отдельно.

Cleanup: VerifyRestored PASS, Play=false, MMF_Player/NetworkRunner=0, Shared/Solo=false, временный clock=false, PeerMode=Single. SDK/FEEL не менялись. Сборки/загрузки нет.

Следующий конкретный шаг: ИИ — DB-ON-125, безопасное удаление/восстановление удалённых локаций и бюджет мира. Владелец — позже пакетная проверка всех карточек, мобильного HUD и баланса в отдельно запрошенной WebGL-сборке; до неё in_review.
