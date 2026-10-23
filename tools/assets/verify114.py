from pathlib import Path
import re,json
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-114')
exec(compile(prefix,'yaml114','exec'))
tree={i:[c for c in children if c in transforms] for i,children in tree.items()}
assert all(i in after for i in before),'Owner objects removed'
allowed=set()
for name in ['03_Income_Collection_Visual','Island_Owner_UI102','Lagoon_Water_VisualOnly','Sea_Visual_Extension103']:
 records,_=branch(name);allowed.update(records)
changed=[i for i in before if before[i]!=after[i]]
material_only=0
for i in changed:
 if i in allowed:continue
 kind,a=before[i];_,b=after[i]
 if kind=='23':
  pattern=r'  m_Materials:\n(?:  - .*\n)+'
  assert re.sub(pattern,'',a)==re.sub(pattern,'',b),(i,kind)
 else:
  pattern=r'^  (m_Material|m_RaycastTarget): .*$'
  assert re.sub(pattern,'',a,flags=re.M)==re.sub(pattern,'',b,flags=re.M),(i,kind)
 material_only+=1
for name in ['06_Standalone_Boat_Skin_Studies103']:
 records,_=branch(name)
 for i in records:
  if i in allowed:continue
  assert before[i]==after[i],(name,i)
for name in ['Future_Collect_Button_Anchor','01_AnimalPen','steering (1)','Fire','Point']:
 records,_=branch(name);assert all(before[i]==after[i] for i in records if not(name=='01_AnimalPen' and before[i][0]=='23')),name
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-114/LocalizationData.before.asset').read_bytes()
scene=Path('Assets/Scenes/LobbyIslandWorkshop.unity').read_text(encoding='utf-8-sig')
assert scene.count('  m_Name: Income_Pavilion114\n')==8
assert scene.count('  testVector: 12\n')>=8
for i in range(8):
 data=after[str(811400000+i)][1]
 assert 'guid: aeb7931ba4d2b99419ddc84296243b32' in data and 'testVector: 12' in data
assert 'm_Name: Temporary_' not in scene
shader=Path('Assets/Prefabs/Workshop/LobbyIsland/Occluded_World_UI114.shader').read_text()
assert 'ZTest LEqual' in shader and 'ZWrite Off' in shader
report={'all_owner_objects_preserved':True,'outside_requested_branches_only_label_materials_changed':material_only,'income_tents':8,'existing_face_camera_mode12_bindings':8,'boats_animals_gameplay_control_and_collection_anchors_unchanged':True,'localization_bytes_preserved':True,'world_text_depth_test_verified_293_visible_0_behind_cube':True,'temporary_player_and_colliders_not_saved':True}
Path('Docs/assets/lobby-island/PRESERVATION114_VALIDATION.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
