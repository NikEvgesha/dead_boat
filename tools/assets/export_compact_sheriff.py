"""Export the saved sheriff scene graph without unpacking unresolved game scripts."""
import re
from pathlib import Path
import export_workshop_prefabs as exporter

def parse(path):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',
        Path(path).read_text(encoding='utf-8-sig'), re.M | re.S)}

before = parse('Library/WorkshopBackups/DB-ON-080/OwnerSaved.unity')
path = Path('Assets/Prefabs/Workshop/Sheriff/Sheriff_Stone_Compact_01.prefab')
exporter.export('05_Nursery', path)
house = parse(path)
changed = [key for key in before if before[key] != exporter.docs.get(key)]
assert all(key in house for key in changed), 'Unrelated changes detected; preserve and inspect'
assert all(key in exporter.docs for key in before), 'Existing scene object removed'
guid = re.search(r'^guid: (\w+)', Path('Assets/Prefabs/Locations/Bilding/Sheriffs.prefab.meta').read_text(), re.M)[1]
instance = next(key for key, (kind, body) in before.items()
                if kind == 1001 and f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}' in body)

def without_delivery_geometry(body):
    pattern = r'^    - target: .*\n      propertyPath: (.+)\n      value: .*\n      objectReference: .*\n'
    return re.sub(pattern, lambda m: '' if m[1].startswith(('m_LocalPosition.', 'm_Center.', 'm_Size.')) else m[0], body, flags=re.M)

assert without_delivery_geometry(before[instance][1]) == without_delivery_geometry(exporter.docs[instance][1]), 'Unrelated sheriff gameplay fields modified'
path.write_text(path.read_text().replace('  m_Name: 05_Nursery\n', '  m_Name: Sheriff_Stone_Compact_01\n', 1), encoding='utf-8', newline='\n')
exporter.export('Castle_Services_Prototype', Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
report = f'PASS: {len(changed)} existing changed serialized objects belong only to Sheriff; no existing objects removed. Sheriff source retains gameplay fields/references, except delivery Transform and trigger geometry overrides. All local prefab references resolved.\n'
print(report)
with Path('Docs/assets/examples/SHERIFF_COMPACT_VALIDATION.txt').open('a', encoding='utf-8') as output:
    output.write('\n'+report)
