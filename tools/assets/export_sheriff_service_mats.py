"""Export the sheriff's surrender/reward floor zones, preserving owner edits."""
import re
from pathlib import Path
import export_workshop_prefabs as exporter

def parse(path):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',
        Path(path).read_text(encoding='utf-8-sig'), re.M | re.S)}

before = parse('Library/WorkshopBackups/DB-ON-082/OwnerSaved.unity')
dest = Path('Assets/Prefabs/Workshop/Sheriff/Sheriff_Stone_Compact_01.prefab')
exporter.export('05_Nursery', dest)
house = parse(dest)
changed = [key for key in before if before[key] != exporter.docs.get(key)]
assert all(key in house for key in changed), 'Unrelated changes detected; preserve and inspect'
assert all(key in exporter.docs for key in before), 'Existing scene object removed'
guid = re.search(r'^guid: (\w+)', Path('Assets/Prefabs/Locations/Bilding/Sheriffs.prefab.meta').read_text(), re.M)[1]
instance = next(key for key, (kind, body) in before.items()
                if kind == 1001 and f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}' in body)

def without_zone_geometry(body):
    # Setting the preserved world rotation serialized redundant -0 components
    # for MoneyBagPoint in the first run. They equal the original zero rotation.
    zero_rotation = r'^    - target: \{fileID: 4454733523957024469, guid: '+guid+r', type: 3\}\n      propertyPath: m_LocalRotation\.[xyz]\n      value: -?0\n      objectReference: .*\n'
    body = re.sub(zero_rotation, '', body, flags=re.M)
    pattern = r'^    - target: .*\n      propertyPath: (.+)\n      value: .*\n      objectReference: .*\n'
    return re.sub(pattern, lambda m: '' if m[1].startswith(('m_LocalPosition.', 'm_Center.', 'm_Size.')) else m[0], body, flags=re.M)

assert without_zone_geometry(before[instance][1]) == without_zone_geometry(exporter.docs[instance][1]), 'Gameplay reference or unrelated field changed'
dest.write_text(dest.read_text().replace('  m_Name: 05_Nursery\n', '  m_Name: Sheriff_Stone_Compact_01\n', 1), encoding='utf-8', newline='\n')
exporter.export('Castle_Services_Prototype', Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
report = f'PASS: {len(changed)} existing changed records belong only to Sheriff; no existing objects removed. Source fields/references preserved except surrender/reward positions and trigger size/center. Local prefab references resolved.\n'
print(report)
with Path('Docs/assets/examples/SHERIFF_SERVICE_MATS_VALIDATION.txt').open('a', encoding='utf-8') as output:
    output.write('\n'+report)
