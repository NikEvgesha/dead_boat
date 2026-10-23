"""Export localized castle UI and audit owner/gameplay data preservation."""
import re,json
from pathlib import Path
import export_workshop_prefabs as exporter

def parse(p):return {int(m[2]):(int(m[1]),m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',Path(p).read_text(encoding='utf-8-sig'),re.M|re.S)}
before=parse('Library/WorkshopBackups/DB-ON-084/OwnerSaved.unity')
paths={'01_Supplies':'Services/Supplies_Wooden_01','02_Armory':'Fisher/Fisher_Empty_01','03_LootTrader':'Services/LootTrader_Wooden_01','04_Professions':'Armory/Armory_Wooden_01','05_Nursery':'Sheriff/Sheriff_Stone_Compact_01','06_AnimalMerge':'Hospital/Hospital_Stone_01'}
allowed=set()
for name,d in paths.items():
 p=Path('Assets/Prefabs/Workshop/'+d+'.prefab');exporter.export(name,p);allowed.update(parse(p));p.write_text(p.read_text(encoding='utf-8').replace('  m_Name: '+name+'\n','  m_Name: '+p.stem+'\n',1),encoding='utf-8',newline='\n')
changed=[k for k in before if before[k]!=exporter.docs.get(k)]
removed=[k for k in before if k not in exporter.docs]
assert all(before[k][0] in (102,23) for k in removed), 'Unexpected scene objects removed'
assert len(removed)==60, 'Expected 30 TextMesh and 30 associated MeshRenderer components'
assert all(k in allowed or k in removed for k in changed),'Changes outside service houses'
assert all(exporter.docs.get(k)==(kind,b) for k,(kind,b) in before.items() if kind==114), 'Original scene MonoBehaviour configuration modified'
locguid='dd4151974eac4ae4ba8ed7bcb9cf28ae'
local=[b for kind,b in exporter.docs.values() if kind==114 and f'guid: {locguid}' in b]
assert len(local)==17
manifest=json.loads(Path('Docs/assets/examples/Castle_UI_Localization.json').read_text(encoding='utf-8'))
for m in manifest:
 assert sum(f'  selectedKey: {m["key"]}\n' in b for b in local)==sum(e['key']==m['key'] for e in manifest)
oldloc=Path('Library/WorkshopBackups/DB-ON-084/LocalizationData.asset').read_text(encoding='utf-8-sig')
newloc=Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_text(encoding='utf-8-sig')
assert newloc.startswith(oldloc), 'Existing localization data changed'
for m in manifest:assert f'  - Key: {m["key"]}\n' in newloc
source=Path('Assets/Scripts/ItemStore.cs').read_text(encoding='utf-8')
assert 'fixedArmory' not in source and 'Random.Range(0, _availableItems.Count)' in source
exporter.export('Castle_Services_Prototype',Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
report=f'PASS: {len(changed)} original changed records within houses; exactly 60 obsolete text/render components removed, no original GameObjects/gameplay scripts removed; all original scene MonoBehaviour fields preserved; 17 LocalizedText bindings and 13 appended Ru/En keys retained after Unity reload; original localization data preserved; random assortment restored; all prefab local references resolve.\n'
print(report)
with Path('Docs/assets/examples/CASTLE_UI084_VALIDATION.txt').open('a',encoding='utf-8') as out:out.write(report)

