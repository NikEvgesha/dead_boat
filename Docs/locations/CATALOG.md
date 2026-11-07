# Каталог префабов мест сбора

Снимок 2026-10-23. Генератор: `tools/assets/audit_location_yaml.py`. Полный контекст — [README](README.md).

Размер — AABB активных Renderer в координатах корня, X × Z, единицы Unity. Это геометрия, включая подземные части и эффекты; не проходимая площадь и не зона очистки. Знак X обозначает центр геометрии относительно русла; габарит может пересекать X=0. Корни всех 28 вариантов имеют единичный масштаб и нулевой поворот.

| Префаб | Сторона центра | Размер X×Z | Точки добычи / врагов | Лес | Пустыня | Зима |
|---|---|---|---|---|---|---|
| [Sklep_1](../../Assets/Prefabs/Locations/Enemy/Sklep_1.prefab) | X+ (60.2) | 56.1×56.2 | 5 / 5 | absent | absent | d>28000; weight=50 |
| [Sklep_2](../../Assets/Prefabs/Locations/Enemy/Sklep_2.prefab) | X- (-75.4) | 56.1×56.2 | 5 / 5 | absent | absent | d>28000; weight=50 |
| [To4ka6_1](../../Assets/Prefabs/Locations/Enemy/To4ka6_1.prefab) | X- (-61.4) | 21.9×20.1 | 5 / 5 | absent | absent | any; weight=50 |
| [To4ka6_2](../../Assets/Prefabs/Locations/Enemy/To4ka6_2.prefab) | X+ (50.6) | 21.9×20.1 | 5 / 5 | absent | absent | any; weight=50 |
| [TreeHouse_1](../../Assets/Prefabs/Locations/Enemy/TreeHouse_1.prefab) | X- (-63.0) | 59.0×82.6 | 5 / 5 | any; weight=50 | absent | absent |
| [TreeHouse_2](../../Assets/Prefabs/Locations/Enemy/TreeHouse_2.prefab) | X+ (63.5) | 101.5×94.8 | 5 / 5 | any; weight=50 | absent | absent |
| [Village_1](../../Assets/Prefabs/Locations/Enemy/Village_1.prefab) | X- (-63.5) | 262.7×65.1 | 25 / 15 | d>32000; weight=50 | absent | absent |
| [Village_2](../../Assets/Prefabs/Locations/Enemy/Village_2.prefab) | X+ (79.7) | 65.1×262.7 | 25 / 15 | d>32000; weight=50 | absent | absent |
| [boshka_1](../../Assets/Prefabs/Locations/Enemy/boshka_1.prefab) | X- (-59) | 38.8×40.1 | 5 / 5 | absent | d>5000; weight=30 | d>18000; weight=30 |
| [boshka_2](../../Assets/Prefabs/Locations/Enemy/boshka_2.prefab) | X+ (48.4) | 30.5×34.6 | 5 / 5 | absent | d>5000; weight=30 | d>18000; weight=30 |
| [castle_1](../../Assets/Prefabs/Locations/Enemy/castle_1.prefab) | X- (-81.5) | 84.6×89.7 | 8 / 6 | d>18000; weight=50 | d>14000; weight=50 | absent |
| [castle_2](../../Assets/Prefabs/Locations/Enemy/castle_2.prefab) | X+ (78.6) | 66.6×75.9 | 8 / 6 | d>18000; weight=50 | d>14000; weight=50 | absent |
| [clocks_1](../../Assets/Prefabs/Locations/Enemy/clocks_1.prefab) | X- (-59.1) | 22.8×23.2 | 5 / 5 | absent | d>28000; weight=50 | d>10000; weight=50 |
| [clocks_2](../../Assets/Prefabs/Locations/Enemy/clocks_2.prefab) | X+ (71.8) | 22.8×23.2 | 5 / 5 | absent | d>28000; weight=50 | d>10000; weight=50 |
| [house1_1](../../Assets/Prefabs/Locations/Enemy/house1_1.prefab) | X- (-46.5) | 28.4×21.5 | 4 / 5 | any; weight=50 | any; weight=50 | any; weight=50 |
| [house1_2](../../Assets/Prefabs/Locations/Enemy/house1_2.prefab) | X+ (66.3) | 32.8×21.1 | 4 / 5 | any; weight=50 | any; weight=50 | any; weight=50 |
| [house2_1](../../Assets/Prefabs/Locations/Enemy/house2_1.prefab) | X- (-48.8) | 35.1×24.4 | 4 / 5 | any; weight=50 | absent | any; weight=50 |
| [house2_2](../../Assets/Prefabs/Locations/Enemy/house2_2.prefab) | X+ (56.5) | 35.6×23.4 | 4 / 5 | any; weight=50 | absent | any; weight=50 |
| [house3_1](../../Assets/Prefabs/Locations/Enemy/house3_1.prefab) | X- (-52.0) | 25.7×21.4 | 4 / 5 | any; weight=50 | any; weight=50 | any; weight=50 |
| [house3_2](../../Assets/Prefabs/Locations/Enemy/house3_2.prefab) | X+ (51.4) | 24.7×21.3 | 4 / 4 | any; weight=50 | any; weight=50 | any; weight=50 |
| [pond_1](../../Assets/Prefabs/Locations/Enemy/pond_1.prefab) | X- (-44.7) | 41.1×44.4 | 5 / 5 | absent | any; weight=50 | absent |
| [pond_2](../../Assets/Prefabs/Locations/Enemy/pond_2.prefab) | X+ (55.5) | 51.0×50.6 | 5 / 5 | absent | any; weight=50 | absent |
| [ruins_1](../../Assets/Prefabs/Locations/Enemy/ruins_1.prefab) | X- (-104.1) | 51.8×106.4 | 5 / 6 | any; weight=50 | absent | absent |
| [ruins_2](../../Assets/Prefabs/Locations/Enemy/ruins_2.prefab) | X+ (70.3) | 39.6×64.3 | 5 / 6 | any; weight=50 | absent | absent |
| [ship_1](../../Assets/Prefabs/Locations/Enemy/ship_1.prefab) | X- (-81.7) | 76.1×77.3 | 6 / 6 | d>8000; weight=50 | d>0; weight=50 | absent |
| [ship_2](../../Assets/Prefabs/Locations/Enemy/ship_2.prefab) | X+ (85.8) | 98.1×89.2 | 6 / 6 | d>8000; weight=50 | d>0; weight=50 | absent |
| [tower_1](../../Assets/Prefabs/Locations/Enemy/tower_1.prefab) | X- (-60.9) | 24.8×24.8 | 5 / 5 | absent | absent | d>15000; weight=20 |
| [tower_2](../../Assets/Prefabs/Locations/Enemy/tower_2.prefab) | X+ (85.3) | 28.2×28.2 | 5 / 5 | absent | absent | d>15000; weight=20 |

`any` — нет включённого ограничения дистанции; `absent` — нет в пуле данного биома. `weight` — относительный вес среди ещё не использованных подходящих записей, не процент вероятности. Границы строгие. CSV также содержит список врагов и прежний общий пул Legacy.

Точки — сохранённое количество с учётом Array.size override; фактическое число врагов дополнительно ограничивает профиль биома. Превью показывают только вариант `_1`, варианты `_2` отличаются геометрией и наполнением.
