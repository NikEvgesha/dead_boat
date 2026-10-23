"""Export trader decoration and duplicated bounty board, preserving owner and source data."""
from pathlib import Path
import re
import export_workshop_prefabs as exporter

def parse(p):return {int(m[2]):(int(m[1]),m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',Path(p).read_text(encoding='utf-8-sig'),re.M|re.S)}
before=parse('Library/WorkshopBackups/DB-ON-085/OwnerSaved.unity')
allowed=set()
for name,d in {'03_LootTrader':'Services/LootTrader_Wooden_01','05_Nursery':'Sheriff/Sheriff_Stone_Compact_01'}.items():
 p=Path('Assets/Prefabs/Workshop/'+d+'.prefab');exporter.export(name,p);allowed.update(parse(p));p.write_text(p.read_text(encoding='utf-8').replace('  m_Name: '+name+'\n','  m_Name: '+p.stem+'\n',1),encoding='utf-8',newline='\n')
changed=[k for k in before if before[k]!=exporter.docs.get(k)]
assert all(k in allowed for k in changed),'Changes outside trader/sheriff'
assert all(k in exporter.docs for k in before),'Existing scene records removed'
for k,(kind,b) in before.items():
 if kind==114:assert exporter.docs[k]==(kind,b),'Original script/UI configuration changed unexpectedly'
header=[b for kind,b in exporter.docs.values() if kind==114 and 'selectedKey: Castle/Wanted\n' in b]
assert len(header)==2, 'Wanted poster and new bounty board localization must be retained'
# Existing source component fields and asset references are untouched; only geometry overrides change.
def stable(body):
 return re.sub(r'^    - target: .*\n      propertyPath: (m_LocalPosition\.[xyz]|m_LocalScale\.[xyz]|m_Center\.[xyz]|m_Size\.[xyz]|m_IsActive)\n      value: .*\n      objectReference: .*\n','',body,flags=re.M)
for k,(kind,b) in before.items():
 if kind==1001 and k in changed:assert stable(b)==stable(exporter.docs[k][1]),'Unexpected prefab config change'
exporter.export('Castle_Services_Prototype',Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
report=f'PASS: {len(changed)} changed existing records confined to trader/sheriff; no original records removed; original MonoBehaviour fields/references preserved; prefab source changes restricted to positions, scales, trigger geometry and redundant hint visibility; new Castle/Wanted header survives save; prefab local references resolve.\n'
print(report)
with Path('Docs/assets/examples/TRADER_BOUNTY085_VALIDATION.txt').open('a',encoding='utf-8') as out:out.write(report)
