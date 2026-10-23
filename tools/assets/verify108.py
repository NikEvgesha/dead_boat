from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-108')
exec(compile(prefix,'yaml108','exec'))
tree={i:[c for c in children if c in transforms] for i,children in tree.items()}
allowed=set()
for name in ['Boat_DuckLuxury103','Boat_ForestSail_Reference106','Boat_Corsair_Reference106']:
 records,_=branch(name);allowed.update(records)
unchanged=[i for i in before if i not in allowed]
assert all(i in after and before[i]==after[i] for i in unchanged),'Unrelated scene records changed'
assert all(i in after for i in before),'Saved owner objects removed'
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-108/LocalizationData.before.asset').read_bytes()
checks={'unrelated_scene_records_preserved':len(unchanged),'all_saved_owner_objects_retained':True,'translations_bytes_preserved':True}
for name in ['steering (1)','Fire','Fire_01','Point','Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:
 records,_=branch(name);assert all(before[i]==after[i] for i in records),name
checks['all_helm_fuel_player_and_control_anchor_records_preserved']=True
records,tids=branch('bell')
changed=[]
for i in records:
 if before[i]==after[i]:continue
 assert i in tids,('bell non-transform change',i)
 assert re.sub(r'^  m_LocalRotation: .*$', '',before[i][1],flags=re.M)==re.sub(r'^  m_LocalRotation: .*$', '',after[i][1],flags=re.M),('bell change beyond rotation',i)
 changed.append(i)
assert len(changed)==2
checks['only_two_bell_rotations_changed_positions_scales_meshes_preserved']=True
for name,boxes,meshes in [('Boat_DuckLuxury103',0,0),('Boat_ForestSail_Reference106',0,0),('Boat_Corsair_Reference106',20,2)]:
 text=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab').read_text(encoding='utf-8-sig')
 assert len(re.findall(r'^--- !u!65 ',text,re.M))==boxes
 assert len(re.findall(r'^--- !u!64 ',text,re.M))==meshes
 assert not re.search(r'^--- !u!54 ',text,re.M)
 checks[name+'_saved_box_mesh_colliders']=[boxes,meshes]
 # The seven previous cabin colliders remain on an inactive preserved branch.
for name in ['Rounded_Duck108','Full_Width_Aft_Cabin108','Short_Upper_Duck_Bill108','Short_Lower_Duck_Bill108']:
 assert sum('  m_Name: '+name+'\n' in text for kind,text in after.values() if kind=='1')==1
Path('Docs/assets/lobby-island/PRESERVATION108_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True))
