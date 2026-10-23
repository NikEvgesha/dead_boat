from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-107')
exec(compile(prefix,'yaml107','exec'))
tree={i:[c for c in children if c in transforms] for i,children in tree.items()}
allowed=set()
for name in ['Boat_DuckLuxury103','Boat_ForestSail_Reference106','Boat_Corsair_Reference106']:
 records,_=branch(name);allowed.update(records)
unchanged=[i for i in before if i not in allowed]
assert all(i in after and before[i]==after[i] for i in unchanged),'Unrelated records changed'
assert all(i in after for i in before),'Saved owner objects removed'
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-107/LocalizationData.before.asset').read_bytes()
checks={'unrelated_scene_records_preserved':len(unchanged),'all_saved_owner_objects_retained':True,'translations_bytes_preserved':True}
for name in ['steering (1)','Fire','Fire_01','Point','bell','Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103','Raised_Quarterdeck106','Quarterdeck_Stair106']:
 records,_=branch(name)
 assert all(before[i]==after[i] for i in records),name
 checks[name+'_records_preserved']=len(records)
duck_records,duck_tids=branch('Boat_DuckLuxury103')
boards={i for i in duck_records if before[i][0]=='1' and '  m_Name: BoardDefault\n' in before[i][1]}
for i in duck_records:
 if re.search(r'm_GameObject: \{fileID: (-?\d+)\}',before[i][1]) and re.search(r'm_GameObject: \{fileID: (-?\d+)\}',before[i][1])[1] in boards:
  assert before[i]==after[i],('accepted_duck_hull',i)
checks['accepted_duck_hull_transform_mesh_renderer_unchanged']=True
for name,count in [('Boat_DuckLuxury103',0),('Boat_ForestSail_Reference106',0),('Boat_Corsair_Reference106',7)]:
 text=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab').read_text(encoding='utf-8-sig')
 assert len(re.findall(r'^--- !u!65 ',text,re.M))==count
 assert not re.search(r'^--- !u!54 ',text,re.M)
 for anchor in ['Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:assert text.count('m_Name: '+anchor+'\n')==1
 checks[name+'_saved_prefab_colliders']=count
Path('Docs/assets/lobby-island/PRESERVATION107_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True))
