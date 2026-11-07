from pathlib import Path
import re
p=Path('Assets/Prefabs/Locations/Enemy/house1_1.prefab')
raw=p.read_bytes()
s=raw.decode('utf-8-sig')
crlf='\r\n' in s
s=s.replace('\r\n','\n')
backup=Path('Temp/house1_1-before130.prefab')
assert not backup.exists()
guid,root=Path('Temp/court130.txt').read_text().split()
anchor='3496832663295642883'
base='317df06dddf6f5147b9df7b5f624351a'
old='c9e06a5afef55354fa1792fd33c0d341'
added,inst='6300000000000000130','6300000000000000131'
assert added not in s and inst not in s
# Disable only the old visual branch. Preserve the root, cleaner and spawners.
i=s.index('    m_Modifications:\n',s.index('--- !u!1001 &2818248274326928333'))+len('    m_Modifications:\n')
s=s[:i]+f'    - target: {{fileID: 2834049936434153591, guid: {old}, type: 3}}\n      propertyPath: m_IsActive\n      value: 0\n      objectReference: {{fileID: 0}}\n'+s[i:]
entry=f'    m_AddedGameObjects:\n    - targetCorrespondingSourceObject: {{fileID: 3170936268398154914, guid: {base}, type: 3}}'
assert s.count(entry)==1
s=s.replace(entry, f'    m_AddedGameObjects:\n    - targetCorrespondingSourceObject: {{fileID: 3170936268398154914, guid: {base}, type: 3}}\n      insertIndex: -1\n      addedObject: {{fileID: {added}}}\n'+entry.split('\n',1)[1],1)
# Existing loot references and ordering stay intact; position them in free floor/yard space.
for ident, xyz in {'4554822043992207764':(-86.34,1.84,-15.1),'3728411439325357825':(-81.34,1.33,-15.1)}.items():
 for axis,v in zip('xyz',xyz):
  pattern=rf'(target: \{{fileID: {ident}, guid: {base}, type: 3\}}\n      propertyPath: m_LocalPosition\.{axis}\n      value: )[^\n]+'
  s,n=re.subn(pattern,lambda m:m[1]+str(v),s);assert n==1
for ident,xyz in {'1106624075444175402':(-83.94,1.33,-6.2),'3339743795788123962':(-77.34,1.33,-9.4)}.items():
 pattern=rf'(^--- !u!4 &{ident}\n(?:(?!^---).)*?^  m_LocalPosition: )\{{[^\n]+\}}'
 s,n=re.subn(pattern,lambda m:m[1]+f'{{x: {xyz[0]}, y: {xyz[1]}, z: {xyz[2]}}}',s,flags=re.M|re.S);assert n==1
s+=f'''--- !u!1001 &{inst}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {anchor}}}
    m_Modifications: []
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}
--- !u!4 &{added} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: {root}, guid: {guid}, type: 3}}
  m_PrefabInstance: {{fileID: {inst}}}
  m_PrefabAsset: {{fileID: 0}}
'''
backup.write_bytes(raw)
p.write_bytes((s.replace('\n','\r\n') if crlf else s).encode('utf-8'))
