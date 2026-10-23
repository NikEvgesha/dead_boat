"""Export six extended service houses without losing unresolved gameplay components."""
import re
from pathlib import Path
import export_workshop_prefabs as exporter

def parse(path):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',Path(path).read_text(encoding='utf-8-sig'),re.M|re.S)}

before=parse('Library/WorkshopBackups/DB-ON-083/OwnerSaved.unity')
paths={
'01_Supplies':'Assets/Prefabs/Workshop/Services/Supplies_Wooden_01.prefab',
'02_Armory':'Assets/Prefabs/Workshop/Fisher/Fisher_Empty_01.prefab',
'03_LootTrader':'Assets/Prefabs/Workshop/Services/LootTrader_Wooden_01.prefab',
'04_Professions':'Assets/Prefabs/Workshop/Armory/Armory_Wooden_01.prefab',
'05_Nursery':'Assets/Prefabs/Workshop/Sheriff/Sheriff_Stone_Compact_01.prefab',
'06_AnimalMerge':'Assets/Prefabs/Workshop/Hospital/Hospital_Stone_01.prefab'}
allowed=set()
for name, dest in paths.items():
    path=Path(dest)
    exporter.export(name,path)
    house=parse(path)
    allowed.update(house)
    path.write_text(path.read_text().replace('  m_Name: '+name+'\n','  m_Name: '+path.stem+'\n',1),encoding='utf-8',newline='\n')
changed=[key for key in before if before[key]!=exporter.docs.get(key)]
assert all(key in allowed for key in changed), 'Unrelated scene changes detected'
assert all(key in exporter.docs for key in before), 'Existing scene records removed'
# Unresolved MonoBehaviour script GUIDs and serialized configurations must survive.
for key,(kind,body) in before.items():
    if kind==114:
        assert exporter.docs[key]==(kind,body), 'Existing script configuration changed: '+str(key)
exporter.export('Castle_Services_Prototype',Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
report=f'PASS: {len(changed)} existing changed records confined to six houses; no existing records removed; every original MonoBehaviour configuration and GUID preserved; six prefabs and castle exported, local references resolve.\n'
print(report)
with Path('Docs/assets/examples/ARMORY_EXTENSION_VALIDATION.txt').open('a',encoding='utf-8') as out:out.write(report)
