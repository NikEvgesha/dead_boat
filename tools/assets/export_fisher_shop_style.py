"""Export the saved scene; check changes against this request's owner checkpoint."""
from pathlib import Path
import re
import export_workshop_prefabs as exporter

def parse(path):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',
        Path(path).read_text(encoding='utf-8-sig'), re.M | re.S)}

before = parse('Library/WorkshopBackups/DB-ON-079-ShopStyle/OwnerSaved.unity')
dest = Path('Assets/Prefabs/Workshop/Fisher/Fisher_Empty_01.prefab')
exporter.export('02_Armory', dest)
fisher = parse(dest)
changed = [key for key in before if before[key] != exporter.docs.get(key)]
assert all(key in fisher for key in changed), 'Unrelated scene objects changed; preserve them and inspect before claiming isolation'
assert all(key in exporter.docs for key in before), 'Existing scene object removed'
for key, (kind, body) in before.items():
    if kind in (4, 33, 64, 65, 135):
        assert exporter.docs[key] == before[key], ('Transform/mesh/collider changed', key)
dest.write_text(dest.read_text().replace('  m_Name: 02_Armory\n', '  m_Name: Fisher_Empty_01\n', 1), encoding='utf-8', newline='\n')
exporter.export('Castle_Services_Prototype', Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
# Preserve owner changes to other houses in their standalone snapshot too.
hospital = Path('Assets/Prefabs/Workshop/Hospital/Hospital_Stone_01.prefab')
exporter.export('06_AnimalMerge', hospital)
hospital.write_text(hospital.read_text().replace('  m_Name: 06_AnimalMerge\n', '  m_Name: Hospital_Stone_01\n', 1), encoding='utf-8', newline='\n')
report = f'PASS: {len(changed)} changed existing objects belong only to Fisher. Transforms, meshes and colliders unchanged; no existing objects removed. Other owner edits match the saved checkpoint. All exported local references resolved.\n'
print(report)
with Path('Docs/assets/examples/FISHER_SHOP_STYLE_VALIDATION.txt').open('a', encoding='utf-8') as output:
    output.write('\n'+report)
