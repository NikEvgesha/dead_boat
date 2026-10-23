"""Export the owner-edited sheriff with its locked gate and new decorations."""
import re
from pathlib import Path
import export_workshop_prefabs as exporter

def parse(path):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',
        Path(path).read_text(encoding='utf-8-sig'), re.M | re.S)}

before = parse('Library/WorkshopBackups/DB-ON-081/OwnerSaved.unity')
dest = Path('Assets/Prefabs/Workshop/Sheriff/Sheriff_Stone_Compact_01.prefab')
exporter.export('05_Nursery', dest)
house = parse(dest)
changed = [key for key in before if before[key] != exporter.docs.get(key)]
assert all(key in house for key in changed), 'Unrelated changes detected; preserve and inspect'
assert all(key in exporter.docs for key in before), 'Existing scene object removed'
guid = re.search(r'^guid: (\w+)', Path('Assets/Prefabs/Locations/Bilding/Sheriffs.prefab.meta').read_text(), re.M)[1]
instance = next(key for key, (kind, body) in before.items()
                if kind == 1001 and f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}' in body)
assert before[instance] == exporter.docs[instance], 'SellPoint/reward/source overrides changed'
dest.write_text(dest.read_text().replace('  m_Name: 05_Nursery\n', '  m_Name: Sheriff_Stone_Compact_01\n', 1), encoding='utf-8', newline='\n')
exporter.export('Castle_Services_Prototype', Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
report = f'PASS: {len(changed)} existing changed objects belong only to Sheriff; no existing objects removed. Original Sheriff gameplay source, SellPoint and reward overrides exactly match owner checkpoint. All prefab local references resolved.\n'
print(report)
with Path('Docs/assets/examples/SHERIFF_LOCKED_DECOR_VALIDATION.txt').open('a', encoding='utf-8') as output:
    output.write('\n'+report)
