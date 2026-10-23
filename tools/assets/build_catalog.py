"""Rebuild the file inventory; no Unity imports or source asset changes."""
import csv
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOCS = ROOT / 'Docs' / 'assets'
MODEL_EXT = {'.fbx', '.obj', '.blend', '.dae', '.gltf', '.glb', '.stl', '.vox'}
RULES = [
    ('Доски, балки и опоры', r'plank|beam|pillar|pole\d|wood.?log|timber|stick(?:_|$)', 'Каркас дома, настил, стойки стены, ножки мебели, перила; проверить форму и масштаб.'),
    ('Крыши и дымоходы', r'roof|chimney|overhang', 'Крыша дома, навес, дымоход; отдельные элементы можно повторять вдоль фасада.'),
    ('Стены и проёмы', r'wall|door|window|arch|gate', 'Стены, входы и окна, ограда, перегородки, арки; проверить размер стыков.'),
    ('Полы, лестницы и мосты', r'floor|stairs?|steps?|bridge|platform|balcony', 'Пол дома, крыльцо, мостки, второй этаж и переходы.'),
    ('Дома и сооружения', r'house|cottage|building|tower|castle|hut|tent|well|fountain|port_base|(?:hall|workshop|armory)_(?:tall|wide)', 'Готовое здание или основа композиции; осмотреть, прежде чем разбирать на части.'),
    ('Заборы и ограды', r'fence|rail(?:_|\s|\d|$)|hedge', 'Двор, загон, границы участка, перила и садовая изгородь.'),
    ('Мебель', r'table|chair|chear|stool|bench|bed|shelf|rack|cabinet|cupboard|bookcase', 'Интерьер дома, таверны или мастерской; масштабировать под персонажа.'),
    ('Тара и запасы', r'barrel|crate|box|chest|sack|bag|basket|hay|amphora|pot(?:_|\d|$)', 'Склад, порт, кухня, рынок, ферма; повторять с поворотом и разным размером.'),
    ('Свет и украшения', r'torch|lantern|lamp|candle|banner|flag|statue|carpet|vase|painting', 'Освещение входа, интерьер, ориентиры и оформление фасада.'),
    ('Природа и рельеф', r'tree|pine|palm|bush|grass|flower|tulip|mushroom|rock|stone|terrain|cliff|mountain|sand|snow|water|river', 'Ландшафт, сад, берег, скалы, тропа; отдельный камень может стать фундаментом.'),
    ('Инструменты и оружие', r'sword|axe|hammer|pickaxe|shovel|bow|rifle|pistol|gun|weapon|shield|spear|knife|slingshot', 'Мастерская, стойка оружия, реквизит; игровые компоненты проверить отдельно.'),
    ('Транспорт и механизмы', r'cart|wagon|boat|ship|wheel|windmill|crane', 'Двор, пристань, рынок и мастерская; колёса/детали использовать после осмотра.'),
]


def classify(path):
    p = path.lower()
    name = Path(path).stem
    if any(x in p for x in ('ui kit', 'layer lab', 'ps-progressbar', '/ui/', '/canvas', 'moneychange')):
        return 'UI и интерфейс', 'Интерфейс; не строительный элемент окружения.'
    if any(x in p for x in ('monsters ultimate', '/zoo/', '_gloomy_animal', '/characters/', '/enemies/')):
        return 'Персонажи и игровые объекты', 'Персонаж или объект с поведением; для визуальной сборки предпочесть исходную модель.'
    if name.lower().startswith(('vfx', 'fx_', 'particle')):
        return 'Эффекты', 'Декоративный эффект; проверить работу материалов и ParticleSystem в редакторе.'
    for category, pattern, usage in RULES:
        if re.search(pattern, name, re.I):
            return category, usage
    return 'Прочее — осмотреть', 'Назначение не установлено по имени; сначала открыть Preview в Unity.'


def pack(path):
    parts = Path(path).parts
    if len(parts) == 2:
        return 'Assets (корень)'
    return '/'.join(parts[:3] if len(parts) > 2 and parts[1] == 'Import' else parts[:2])


def build():
    DOCS.mkdir(parents=True, exist_ok=True)
    files = sorted(p for p in (ROOT / 'Assets').rglob('*') if p.is_file() and p.suffix != '.meta')
    rows = []
    folder_counts = defaultdict(Counter)
    for file in files:
        path = file.relative_to(ROOT).as_posix()
        folder_counts[pack(path)][file.suffix.lower()] += 1
        if file.suffix.lower() not in MODEL_EXT | {'.prefab'}:
            continue
        category, usage = classify(path)
        kind = 'Prefab' if file.suffix == '.prefab' else ('Voxel source' if file.suffix == '.vox' else 'Model')
        rows.append(dict(name=file.stem, path=path, pack=pack(path), kind=kind, category=category, usage=usage))
    with (DOCS / 'inventory.csv').open('w', encoding='utf-8-sig', newline='') as out:
        writer = csv.DictWriter(out, fieldnames=['name', 'path', 'pack', 'kind', 'category', 'usage'])
        writer.writeheader()
        writer.writerows(rows)
    (DOCS / 'inventory.json').write_text(json.dumps({'items': rows}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    counts = Counter(r['kind'] for r in rows)
    overview = ['# Инвентаризация папок Assets', '', 'Срез файлов на 2026-10-06. Это количество файлов, а не уникальных предметов: FBX/OBJ/GLTF и варианты материалов могут повторять один объект.', '', f'В полном каталоге **{len(rows)}** файлов: ' + ', '.join(f'{k}: {v}' for k, v in counts.items()) + '.', '', 'Каждый предмет: [inventory.csv](inventory.csv) (Excel и окно Unity) или [inventory.json](inventory.json) (машиночитаемая копия). Идеи использования определены по именам, а не по визуальной проверке.', '', '| Папка/набор | Префабы | Модели/исходники | Материалы | Изображения | Сцены | Скрипты | Другие файлы |', '|---|---:|---:|---:|---:|---:|---:|---:|']
    for folder, c in sorted(folder_counts.items()):
        models = sum(c[e] for e in MODEL_EXT)
        images = sum(c[e] for e in ('.png', '.jpg', '.jpeg', '.tga', '.psd', '.exr', '.hdr'))
        tracked = c['.prefab'] + models + c['.mat'] + images + c['.unity'] + c['.cs']
        overview.append(f"| `{folder}` | {c['.prefab']} | {models} | {c['.mat']} | {images} | {c['.unity']} | {c['.cs']} | {sum(c.values()) - tracked} |")
    overview += ['', '## Назначение по имени', '', '| Категория | Файлов |', '|---|---:|']
    overview += [f'| {k} | {v} |' for k, v in sorted(Counter(r['category'] for r in rows).items())]
    (DOCS / 'FOLDERS.md').write_text('\n'.join(overview) + '\n', encoding='utf-8')
    print(f'Catalog: {len(rows)} files; {dict(counts)}; {len(folder_counts)} folders/packs')


if __name__ == '__main__':
    build()
