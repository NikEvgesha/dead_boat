---
id: DB-ON-264
title: Сохранять масштаб подогнанной добычи при переносе и восстановлении
status: ready
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
