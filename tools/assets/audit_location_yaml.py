"""Read Unity YAML component defaults and prefab overrides without loading gameplay scripts.

This is a targeted audit, not a general YAML parser. Keeps raw component fields and
override property paths as evidence; does not rewrite Unity files.
"""
import copy
import csv
import json
import re
from functools import lru_cache
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GUIDS = {}
for p in (ROOT / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', p.read_text(encoding='utf-8-sig', errors='replace')[:160], re.M)
    if match:
        GUIDS[match[1]] = p.relative_to(ROOT).as_posix()[:-5]

def resolve(text):
    m = re.search(r'guid: (\w+)', text)
    return GUIDS.get(m[1], 'UNRESOLVED:' + m[1]) if m else text

def blocks(text):
    return [(m[1], m[2], m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)]

WANTED = {'LocationSpawner', 'CitySpawner', 'LendController', 'LocationContentSpawner',
          'ZoneCleaner', 'CityZoneCleaner', 'FixCoordinate', 'DecorationSpawner',
          'DungeonTrap', 'ActivateZone', 'Lever', 'Portal', 'CheckItemOnLocation', 'DamageArea', 'DarkZone'}

@lru_cache(None)
def inspect(path):
    p = ROOT / path
    if not p.exists():
        return []
    text = p.read_text(encoding='utf-8-sig', errors='replace')
    guid = next((k for k, v in GUIDS.items() if v == path), '')
    docs = blocks(text)
    entries = []
    names = {fid: re.search(r'^  m_Name: (.*)$', body, re.M)[1] for typ, fid, body in docs if typ == '1' and re.search(r'^  m_Name: (.*)$', body, re.M)}
    for typ, fid, body in docs:
        if typ == '114':
            match = re.search(r'^  m_Script: (.*)$', body, re.M)
            if not match:
                continue
            script = resolve(match[1])
            if Path(script).stem not in WANTED:
                continue
            go = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', body, re.M)
            starts = list(re.finditer(r'^  (\w+):', body, re.M))
            fields = {m[1]: body[m.end():starts[i+1].start() if i+1<len(starts) else len(body)].strip() for i,m in enumerate(starts)}
            entries.append(dict(type=Path(script).stem, origin=path, aliases=[guid+':'+fid], name=names.get(go[1], '') if go else '', fields=fields, overrides={}))
        elif typ == '1001':
            match = re.search(r'^  m_SourcePrefab: (.*)$', body, re.M)
            if not match:
                continue
            source = resolve(match[1])
            if source == path or not source.endswith('.prefab'):
                continue
            nested = copy.deepcopy(inspect(source))
            changes = re.findall(r'- target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\n\s+propertyPath: (.*)\n\s+value: *(.*)\n\s+objectReference: (.*)', body)
            for target, tg, prop, value, ref in changes:
                for entry in nested:
                    if tg+':'+target in entry['aliases']:
                        entry['overrides'][prop] = resolve(ref) if ref != '{fileID: 0}' else value
            removed = re.search(r'm_RemovedComponents:(.*?)(?=\n    m_RemovedGameObjects:)', body, re.S)
            if removed:
                aliases = {g+':'+i for i,g in re.findall(r'fileID: (-?\d+), guid: (\w+)', removed[1])}
                nested = [e for e in nested if not aliases.intersection(e['aliases'])]
            for entry in nested:
                # Unity maps an inherited object's ID into its containing prefab by XOR.
                # Only aliases in the immediate source asset participate at this level.
                source_guid = next((k for k,v in GUIDS.items() if v == source), '')
                for alias in list(entry['aliases']):
                    ag, aid = alias.split(':')
                    if ag == source_guid:
                        entry['aliases'].append(guid+':'+str((int(aid)^int(fid)) & 0x7fffffffffffffff))
                entry['instance'] = fid
            entries.extend(nested)
    # A stripped component provides the identity used by overrides in a parent prefab/scene.
    for typ, fid, body in docs:
        match = re.search(r'm_CorrespondingSourceObject: \{fileID: (-?\d+), guid: (\w+)', body)
        inst = re.search(r'm_PrefabInstance: \{fileID: (-?\d+)\}', body)
        if not match:
            continue
        for entry in entries:
            if match[2]+':'+match[1] in entry['aliases'] and (not inst or entry.get('instance') == inst[1]):
                entry['aliases'].append(guid+':'+fid)
    return entries

def main():
    target = ROOT / 'Docs/locations'
    target.mkdir(exist_ok=True)
    collections = []
    for p in sorted((ROOT / 'Assets/Scripts/Locations').glob('LocationSpawnCollection*.asset')):
        text = p.read_text(encoding='utf-8-sig')
        items = []
        for chunk in text.split('  - prefab: ')[1:]:
            ref, *rest = chunk.splitlines()
            entry = {'prefab': resolve(ref), **dict(re.findall(r'^    (\w+): (.*)', '\n'.join(rest), re.M))}
            items.append(entry)
        collections.append(dict(asset=p.relative_to(ROOT).as_posix(), items=items))
    prefabs = sorted({i['prefab'] for c in collections for i in c['items']})
    paths = prefabs + [p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets/Prefabs/Locations/Spawner').glob('*.prefab')] + [f'Assets/Scenes/{s}.unity' for s in ['LevelForest', 'LevelDesert', 'LevelWinter']]
    report = dict(collections=collections, assets={p: inspect(p) for p in paths})
    (target / 'serialized-settings-2026-10-23.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    geometry = json.loads((target / 'audit-2026-10-23.json').read_text(encoding='utf-8'))
    roots = {e['asset']: e for e in geometry['entries'] if e['type'] == 'Transform'}
    rows = []
    for path in prefabs:
        assert (ROOT / path).exists(), path
        contents = [e for e in inspect(path) if e['type'] == 'LocationContentSpawner']
        assert len(contents) == 1, (path, len(contents))
        content = contents[0]
        f, o, g = content['fields'], content['overrides'], roots[path]
        def count(field, pattern):
            return int(o.get(field+'.Array.size', len(re.findall(pattern, f.get(field, '')))))
        enemies = [o.get(f'_enemyPrefabs.Array.data[{i}]', '') for i in range(count('_enemyPrefabs', r'fileID:'))]
        row = dict(prefab=path, family=Path(path).stem.rsplit('_', 1)[0],
                   side='X+' if g['activeBoundsCenter']['x'] > 0 else 'X-',
                   width=round(g['activeBoundsSize']['x'], 1), depth=round(g['activeBoundsSize']['z'], 1),
                   center_x=round(g['activeBoundsCenter']['x'], 1),
                   item_points=count('itemSpawnPoints', r'spawnTransform:'), enemy_points=count('enemySpawnPoints', r'fileID:'),
                   enemies='; '.join(enemies))
        for c in collections:
            matches = [i for i in c['items'] if i['prefab'] == path]
            label = Path(c['asset']).stem.replace('LocationSpawnCollection', '') or 'Legacy'
            row[label] = '; '.join(('d>'+i['MinSpawnDistence'] if i['HaveMinSpawnDistence']=='1' else 'any')+(' and d<'+i['MaxSpawnDistence'] if i['HaveMaxSpawnDistence']=='1' else '')+'; weight='+i['chance'] for i in matches) or 'absent'
        rows.append(row)
    with (target / 'catalog.csv').open('w', newline='', encoding='utf-8-sig') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader(); writer.writerows(rows)
    lines = ['# Каталог префабов мест сбора', '',
             'Снимок 2026-10-23. Генератор: `tools/assets/audit_location_yaml.py`. Полный контекст — [README](README.md).', '',
             'Размер — AABB активных Renderer в координатах корня, X × Z, единицы Unity. Это геометрия, включая подземные части и эффекты; не проходимая площадь и не зона очистки. Знак X обозначает центр геометрии относительно русла; габарит может пересекать X=0. Корни всех 28 вариантов имеют единичный масштаб и нулевой поворот.', '',
             '| Префаб | Сторона центра | Размер X×Z | Точки добычи / врагов | Лес | Пустыня | Зима |',
             '|---|---|---|---|---|---|---|']
    for r in rows:
        name = Path(r['prefab']).stem
        lines.append(f"| [{name}](../../{r['prefab']}) | {r['side']} ({r['center_x']}) | {r['width']}×{r['depth']} | {r['item_points']} / {r['enemy_points']} | {r['Forest']} | {r['Desert']} | {r['Winter']} |")
    lines += ['', '`any` — нет включённого ограничения дистанции; `absent` — нет в пуле данного биома. `weight` — относительный вес среди ещё не использованных подходящих записей, не процент вероятности. Границы строгие. CSV также содержит список врагов и прежний общий пул Legacy.', '',
              'Точки — сохранённое количество с учётом Array.size override; фактическое число врагов дополнительно ограничивает профиль биома. Превью показывают только вариант `_1`, варианты `_2` отличаются геометрией и наполнением.']
    (target / 'CATALOG.md').write_text('\n'.join(lines)+'\n', encoding='utf-8')
    for c in collections:
        print('\n' + c['asset'])
        for i in c['items']:
            print(Path(i['prefab']).stem, i['chance'], ('>'+i['MinSpawnDistence'] if i['HaveMinSpawnDistence']=='1' else '-') + (' <'+i['MaxSpawnDistence'] if i['HaveMaxSpawnDistence']=='1' else ''))
    print('\nSETTINGS')
    for p in paths:
        if '/Spawner/' not in p and not p.endswith('.unity'):
            continue
        print(p)
        for e in inspect(p):
            if e['type'] in {'LocationSpawner','CitySpawner','LendController'}:
                print(e['type'], e['name'], {k:resolve(v) for k,v in e['fields'].items() if not k.startswith('m_')}, e['overrides'])

if __name__ == '__main__':
    main()
