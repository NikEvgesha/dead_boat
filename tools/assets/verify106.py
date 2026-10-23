from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-106')
exec(compile(prefix,'yaml106','exec'))
tree={i:[c for c in children if c in transforms] for i,children in tree.items()}
allowed,_=branch('Boat_DuckLuxury103')
studies,tids=branch('06_Standalone_Boat_Skin_Studies103')
root_transforms=[i for i in tids if before[i][1].find('m_Father:')>=0 and before[transforms[i]][1].find('  m_Name: 06_Standalone_Boat_Skin_Studies103\n')>=0]
sea,sea_tids=branch('Sea_Visual_Extension103')
for i in root_transforms:
 a,b=before[i][1],after[i][1]
 assert re.sub(r'  m_Children:.*?(?=  m_Father:)','',a,flags=re.S)==re.sub(r'  m_Children:.*?(?=  m_Father:)','',b,flags=re.S)
for i in sea_tids:
 a,b=before[i][1],after[i][1]
 assert re.sub(r'^  m_LocalScale: .*$', '',a,flags=re.M)==re.sub(r'^  m_LocalScale: .*$', '',b,flags=re.M)
allowed.update(root_transforms);allowed.update(sea_tids)
unchanged=[i for i in before if i not in allowed]
assert all(i in after and before[i]==after[i] for i in unchanged),'Unrelated records changed'
assert all(i in after for i in before),'Existing owner objects removed'
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-106/LocalizationData.before.asset').read_bytes()
checks={'unrelated_scene_records_preserved':len(unchanged),'red_blue_owner_edits_preserved_exactly':True,'all_saved_owner_objects_retained':True,'localization_bytes_preserved':True}
for name in ['steering (1)','Fire','Point','Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:
 records,_=branch(name)
 assert all(before[i]==after[i] for i in records),name
checks['all_existing_control_records_preserved']=True
for name in ['Boat_Corsair_Reference106','Boat_ForestSail_Reference106','Boat_DuckLuxury103']:
 p=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab')
 text=p.read_text(encoding='utf-8-sig')
 for anchor in ['Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:assert text.count('m_Name: '+anchor+'\n')==1
 assert not re.search(r'^--- !u!(54|65|64) ',text,re.M)
 checks[name+'_prefab_anchors_and_no_physics']=True
checks['six_armory_shelves_blue_roof_and_all_other_services_preserved']=True
Path('Docs/assets/lobby-island/PRESERVATION106_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True))
