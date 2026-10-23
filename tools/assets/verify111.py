from pathlib import Path
import json,re,subprocess
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-111')
exec(compile(prefix,'yaml111','exec'))
tree={i:[c for c in children if c in transforms] for i,children in tree.items()}
assert all(i in after for i in before),'Saved owner objects removed'
main_records=set()
for name in ['Boat_DuckLuxury103','Boat_Corsair_Reference106','Boat_ForestSail_Reference106']:
 records,_=branch(name);main_records.update(records)
def mesh_only(i):
 assert before[i][0]=='33',(i,'expected MeshFilter')
 assert re.sub(r'^  m_Mesh: .*$', '',before[i][1],flags=re.M)==re.sub(r'^  m_Mesh: .*$', '',after[i][1],flags=re.M),(i,'change beyond mesh reference')
count=0
for i in before:
 if i in main_records or before[i]==after[i]:continue
 mesh_only(i);count+=1
checks={'all_saved_owner_objects_retained':True,'outside_three_main_boats_only_fuel_mesh_references_changed':count}
red,_=branch('Boat_SteamTug103')
red_changes=[i for i in red if before[i]!=after[i]]
for i in red_changes:mesh_only(i)
checks['red_owner_edits_preserved_except_requested_fuel_mesh_fit']=len(red_changes)
for name in ['steering (1)','Fire','Point','Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103','Bank_Crystal_Boutique095','Closed_Aft_Cabin_Door108','BoardDefault']:
 records,tids=branch(name)
 if name=='BoardDefault':
  # Hull meshes may change; all root transforms and their local poses remain identical.
  roots=[i for i in tids if '  m_Name: BoardDefault\n' in before[transforms[i]][1]]
  for i in roots:assert before[i]==after[i]
 elif name=='Fire':
  for i in records:
   if before[i]!=after[i]:mesh_only(i)
 else:assert all(before[i]==after[i] for i in records),name
checks['all_control_poses_bank_and_original_door_preserved']=True
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-111/LocalizationData.before.asset').read_bytes()
checks['translations_bytes_preserved']=True
corsair=Path('Assets/Prefabs/Workshop/Boats/Skins103/Boat_Corsair_Reference106.prefab').read_text(encoding='utf-8-sig')
assert 'm_Name: Closed_Stern_Transom106\n' not in corsair,'Owner-deleted rear panel restored'
assert corsair.count('m_Name: Solid_Short_Stair111\n')==10
checks['owner_deleted_transom_stays_deleted_and_ten_new_steps_saved']=True
for name in ['Boat_DuckLuxury103','Boat_RoyalBlue103','Boat_SteamTug103','Boat_Corsair_Reference106','Boat_ForestSail_Reference106','Boat_WoodenControlAligned103']:
 text=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab').read_text(encoding='utf-8-sig')
 for anchor in ['Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:assert text.count('m_Name: '+anchor+'\n')==1
checks['six_current_prefabs_saved_with_unique_control_anchors']=True
Path('Docs/assets/lobby-island/PRESERVATION111_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True))
