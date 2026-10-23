"""Add box collider records without unpacking/re-serializing missing boat scripts."""
import re
from pathlib import Path

path=Path('Assets/Prefabs/Workshop/GameAligned/Board_CastleCargo_Variant.prefab')
text=path.read_text(encoding='utf-8-sig')
docs=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',text,re.M|re.S))
go_docs=[m for m in docs if int(m[1])==1 and re.search(r'^  m_Name: Canvas_Awning$',m[0],re.M)]
assert len(go_docs)==2
ids={int(m[2]) for m in docs}
colliders=[]
for i,m in enumerate(go_docs):
    go=int(m[2]);body=m[0]
    existing=[int(x) for x in re.findall(r'component: \{fileID: (-?\d+)\}',body)]
    if any(int(d[1])==65 and int(d[2]) in existing for d in docs):continue
    id=750000000000000001+i
    assert id not in ids
    body=body.replace('  m_Layer:',f'  - component: {{fileID: {id}}}\n  m_Layer:',1)
    text=text.replace(m[0],body,1)
    colliders.append(f'''--- !u!65 &{id}
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: 1, y: 1, z: 1}}
  m_Center: {{x: 0, y: 0, z: 0}}
''')
# All former records remain byte-identical except the two component lists.
path.write_text(text+''.join(colliders),encoding='utf-8',newline='\n')
print(f'Added {len(colliders)} solid roof colliders; original script records preserved.')
