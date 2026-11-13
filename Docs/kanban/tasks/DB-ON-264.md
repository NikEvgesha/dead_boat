---
id: DB-ON-264
title: Сохранять масштаб подогнанной добычи при переносе и восстановлении
status: in_review
when: now
ai: true
---

## Контекст

Новый PlaceItemAtSpawnPoint уменьшает весь item.transform.localScale под maxVisualFootprint. SharedItemRecord, SharedRecoveryItem и SoloWorldItem scale не сохраняют. Cold dropped discovery, independent streaming, recovery inventory и SoloRunBranch создают исходный catalog prefab. Обычный восстановленный регион получает fitted candidate и не доказывает исправность этих отдельных путей.

## Готово, когда

- Размер и коллайдер предмета согласованы у ведущего/клиента при spawn, подборе, инвентаре, выбросе, переносе, креплении, покупке и продаже.
- Масштаб либо корректно передаётся как состояние экземпляра, либо подгонка применяется только к явно согласованному декоративному визуалу. Не исправлять расхождение скрытым постоянным сжатием всего каталога.
- Проверены cold join, independent unload/resume, reconnect, takeover и ContinueSolo, без повторного накопления scale/умножения при родительских Transform.
- Регрессия: Loot203_T10_Coffer rootScale2, boundsX0.508 м, слот 0.72 м даёт rootScale≈1.4173. Восстановление не возвращает ширину 1.016 м вместо 0.72 м.
- При новых полях согласованы протокол/версии/старые данные, не сбрасываются произвольные пользовательские сохранения. Native Renderer/Physics и WebGL проверены отдельно от расчёта YAML.

## Подсказки ИИ

LocationContentSpawner.cs:166–168; SharedDepartureState.Items.cs; SharedRecoveryData.cs; SharedItemsRuntime.cs:263; WorldIndependentStreaming.cs:48; SharedRecoveryInventory.cs:53; SoloRunBranch.cs:199; DB-ON-019/125/256. ИИ — закрыть все пути экземпляра, затем повторить предметную приёмку; владелец — позже общий тест.

Источник поручения: владелец 13.11.2026; Docs/BRANCH_ACCEPTANCE_AUDIT_20261113.md. Это постановка задачи, не отчёт о реализации. Сборку и публикацию не выполнять автоматически.

## Источники

- Поручение владельца 13.11.2026.
- Docs/BRANCH_ACCEPTANCE_AUDIT_20261113.md.
- Docs/DEVELOPER_HANDOFF_20261113.md.
- Указанные выше код и связанные карточки.

## Нативная construction-проверка 13.11.2026

Editor fixture без Play/Physics вызвал production PlaceItemAtSpawnPoint: сундук rawScale2/width1.01600003 м → fittedScale1.41732287/width0.72000003 м. Это подтверждает различие конструкции живыми Renderer.bounds, не исполняет сетевое восстановление и не исправляет контракт. NativeContentAudit хранит результат в knownGameplayFindings264, отдельно от assetIntegrityPASS. Далее ИИ — реализовать полный контракт масштаба и проверить реальные пути; владелец позже общийпакет.

## Реализация и проверки — 14.11.2026

Добавлен canonical WorldScale в world record, recovery bag и SoloWorldItem. Размер фиксируется после fitting и проходит через cold dropped discovery, independent restore, региональный candidate, восстановление сумки и ContinueSolo. Применяется абсолютно после окончательных parent/rotation, включая cargo, takeover и интерполяцию; неизменившееся значение не переписывается. Capture/restore сумки предпочитают актуальный canonical размер. У remote equipped visual отдельный относительный multiplier сохраняет авторскую позу/нормализацию каталога.

Протокол получил суффикс itemscale1, включая avatar wire; старые/новые клиенты не смешиваются. RunRandom.Version=6 и SoloBranch.v1 сохранены. Отсутствующее поле legacy JSON означает unknown/no-op: остаётся generated fit или холодный каталоговый размер; неизвестный исторический fitting не выдумывается. Обычные старые offline provider saves не объявлены мигрированными.

Native Unity 6000.3.9f1 PASS: 859 утверждений, 75 повторных Apply, настоящий сундук raw width1.016 → fitted/restored0.72000003 м с теми же BoxCollider bounds, обе production cold factories, разные родители/detach, JSON/legacy/invalid и presentation multiplier. Измерены Fusion records96/32B (+12B относительно расчётной прежней схемы); целые NetworkObject WorldPage20344B/Departure10600B/Avatar1004B ниже32KiB. Это не трафик/сек или Web heap.

Два реальных Cloud peers в пустом Play-мире PASS: регистрация/claim/inventory/drop, ammo FIELD, atomic bag, фактическое cold dropped создание и independent suspend/restore, повторное применение и передача state/page authority. Physics на паузе, дальний focus задан fixture. Полный recovery UI, реальный gunfire, инвентарный UI, игровая сцена/reconnect и WebGL этим тестом не проверены. SharedRunValidation PASS: 5 пулов ×200 локаций/независимые RNG/реконструкция.

Batch exit0, Runner0/контексты очищены. Сверка шести конфигурационных файлов и29 прежних локальных файлов побайтно PASS; автоматические ready defines MCP после теста возвращены к исходной локальной версии. SDK/пакеты/Player Build/черновик не изменялись. Подробности Docs/ITEM_INSTANCE_SCALE_20261114.md и JSON рядом; локальные сырые логи Logs/ItemScale264-20261114.

Срез реализации завершён, in_review до общей приёмки. Далее ИИ —265: все20 находок на штатных механиках, затем266/125 и267. Владелец позднее принимает единый WebGL-пакет, включая настоящие rejoin, физику, покупки/продажи и cargo; сейчас действий не требуется.
