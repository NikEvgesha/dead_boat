from pathlib import Path
import re,json
def blocks(path):
    return {m[2]:(m[1],m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n.*?(?=^--- !u!|\Z)',Path(path).read_text(encoding='utf-8-sig'),re.M|re.S)}
before=blocks('Library/WorkshopBackups/DB-ON-098/LobbyIslandWorkshop.before.unity')
after=blocks('Assets/Scenes/LobbyIslandWorkshop.unity')
goids=[i for i,(kind,b) in before.items() if kind=='1' and re.search(r'^  m_Name: Animal_Chamber_[12]$',b,re.M)]
transforms={i:re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b)[1] for i,(kind,b) in before.items() if kind in ('4','224')}
tree={i:re.findall(r'- \{fileID: (-?\d+)\}',b.split('  m_Children:')[1].split('  m_Father:')[0]) for i,(kind,b) in before.items() if kind in ('4','224')}
ids=set()
todo=[i for i,g in transforms.items() if g in goids]
while todo:
    tid=todo.pop(); ids.add(transforms[tid]); todo.extend(tree[tid])
records={i for i,(kind,b) in before.items() if i in ids or re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b) and re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b)[1] in ids}
assert len(goids)==2 and all(after[i]==before[i] for i in records)
assert re.search(r'continuous network BFS=5151/5151',Path('Docs/assets/lobby-island/PARK_HOSPITAL098_VALIDATION.txt').read_text(encoding='utf-8'))
for name in ['Ivory_Lab_Platform','Central_Merge_Reactor','Reactor_Light_Ring','Result_Plinth','Result_Luminous_Tray','Merge_Control_Desk']:
    b=next(b for kind,b in after.values() if kind=='1' and f'  m_Name: {name}\n' in b)
    assert '  m_IsActive: 0' in b
collection=Path('Assets/Scripts/Items/Collections/Hospital.asset').read_text(encoding='utf-8')
assert '0b94a51814afd31408a7d1517f0b107d' in collection and 'b4570b29bd6dfe540bb1018ec3ec9b08' in collection
report={'animal_chambers_preserved_exactly':True,'preserved_chamber_records':len(records),'unified_network_connected':True,'hidden_lab_elements_verified':True,'hospital_collection_bandage_and_heal_verified':True,'playmode_tested':False}
Path('Docs/assets/lobby-island/PRESERVATION098_VALIDATION.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
