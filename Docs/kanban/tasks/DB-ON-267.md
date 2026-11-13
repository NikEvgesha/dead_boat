---
id: DB-ON-267
title: Подготовить игровые меши, текстуры и звук к ограничениям WebGL
status: ready
when: now
ai: true
---

## Контекст

Дочерняя работа DB-ON-002/027 по аудиту 13–14.11.2026. EventCastle имеет 11 197 Transform / 5699 Renderer, из них активны 1041; синхронный Instantiate в Editor 164,54/129,79 мс. Два меша гирлянд содержат 352 260 вершин. У деревень 1155 активных Renderer / 480 Collider; быстрое клонирование не означает дешёвую отрисовку/физику. Требуются игровые варианты без лишней геометрии и ненужных архивных веток, с сохранением художественного результата.

Дополнительные точечные кандидаты: CubeMap sky 4096/Uncompressed; MusicDay DecompressOnLoad; 240/306 readable model importers. Это список для проверки, а не разрешение массово уменьшить качество. Tanegasima уже native cap128, Panorama sky cap512, 285 texture importers уже readable=false. Размер исходников не равен размеру Web Player.

## Готово, когда

- До/после сохранены одинаковые ракурсы и construction measurements тяжёлых игровых префабов, объяснены изменения Transform/Renderer/Collider/vertices. Не выдавать два Editor samples за Web FPS/p95.
- Убраны только доказанно ненужные визуальные ветви/детали; магазины, NPC, поверхности, точки добычи/охраны и интерфейсы работают. Нельзя удалить все inactive GameObject автоматически.
- Стабильные пути/ID предметов и врагов не изменились либо подготовлена явная новая версия контента/совместимости. Проверены late join, cold restore, смена ведущего и origin shift.
- Read/Write мешей изменён только после разбора runtime-потребителей fitting/QuickOutline/physics cooking; нет ошибок чтения vertices.
- Варианты импорта неба/музыки проверены по качеству и поведению звука. Фактическая экономия размера/памяти подтверждена последующей разрешённой WebGL-сборкой, без выдуманных байтов выигрыша.
- Владелец принял внешний вид и общий WebGL-пакет; до этого максимум in_review.

## Подсказки ИИ

Начать с иерархии EventCastle и гирлянд, затем деревни и коллайдеры, потом отдельные texture/audio эксперименты. Не объединять весь город в один меш без проверки culling/streaming. WorldSpawnIdentity.StablePath использует sibling index: удаление соседней ветви может сдвинуть ключи. Согласовать с DB-ON-262/264 и DB-ON-125. Не менять SDK и не удалять папки FEEL/Workshop по их названию. Сохранить исходные мастерские; разделение на bundles — DB-ON-268.

Следующий шаг — ИИ: разобрать самые тяжёлые игровые ветви и подготовить ограниченный вариант с сопоставлением ID/вида. Владелец позднее принимает пакет. Сейчас без Player Build.

## Источники

- Docs/WEB_OPTIMIZATION_AUDIT_20261113.md
- Docs/WEB_OPTIMIZATION_BASELINE_20261113.json
- Assets/Editor/IncomingContentAudit.cs
- Assets/Editor/WebDependencyAudit.cs
- Assets/Events/NewYear/Castle174/EventCastle_Winter.prefab
- Assets/Scripts/Locations/Items/LocationContentSpawner.cs
- Assets/Online/WorldSpawnIdentity.cs
- Assets/Import/QuickOutline/Scripts/Outline.cs
