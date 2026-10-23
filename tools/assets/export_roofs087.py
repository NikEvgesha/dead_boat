"""Export the three roof edits and assert all existing changes are roof materials."""
from pathlib import Path
import re
import export_workshop_prefabs as e

def parse(path):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)', Path(path).read_text(encoding='utf-8-sig'), re.M | re.S)}
before = parse('Library/WorkshopBackups/DB-ON-087/OwnerSaved.unity')
assert set(before) == set(e.docs), 'Scene objects added/removed'
changed = [k for k in before if before[k] != e.docs[k]]
assert len(changed) == 3, ('Expected only three roof renderers', changed)
for k in changed:
    kind, body = before[k]
    assert kind == 23, ('Non-renderer changed', k)
    def without_materials(text):
        return re.sub(r'^  m_Materials:\n(?:  - .*\n)+', '', text, flags=re.M)
    assert without_materials(body) == without_materials(e.docs[k][1]), ('Non-material renderer field changed', k)
for name, target in {'01_Supplies':'Services/Supplies_Wooden_01', '02_Armory':'Fisher/Fisher_Empty_01', '03_LootTrader':'Services/LootTrader_Wooden_01'}.items():
    p=Path('Assets/Prefabs/Workshop/'+target+'.prefab')
    e.export(name,p)
    p.write_text(p.read_text(encoding='utf-8').replace('  m_Name: '+name+'\n','  m_Name: '+p.stem+'\n',1),encoding='utf-8',newline='\n')
e.export('Castle_Services_Prototype',Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
message='PASS: exactly three renderer material lists changed. All other saved owner records, transforms, colliders, gameplay components and interiors unchanged; four prefab exports resolve all local references.\n'
print(message)
with Path('Docs/assets/examples/ROOFS087_VALIDATION.txt').open('a',encoding='utf-8') as out:out.write(message)
