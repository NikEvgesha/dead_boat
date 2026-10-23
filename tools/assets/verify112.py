from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-112')
exec(compile(prefix,'yaml112','exec'))
tree={i:[c for c in children if c in transforms] for i,children in tree.items()}
assert all(i in after for i in before),'Owner scene records removed'
allowed=set()
for name in ['Boat_DuckLuxury103','Boat_Corsair_Reference106','Boat_ForestSail_Reference106']:
 records,_=branch(name);allowed.update(records)
changes=[i for i in before if before[i]!=after[i]]
assert all(i in allowed for i in changes),[i for i in changes if i not in allowed]
checks={'all_existing_scene_records_preserved':True,'changed_existing_records_in_three_requested_boats':len(changes),'all_other_scene_records_exactly_preserved':True}
for name in ['steering (1)','Fire','Point','Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103','Bank_Crystal_Boutique095','Closed_Aft_Cabin_Door108','Short_Cabin_Stairs111','Boat_SteamTug103']:
 records,_=branch(name);assert all(before[i]==after[i] for i in records),name
checks['controls_fuel_bank_cabin_stairs_red_user_edits_preserved']=True
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-112/LocalizationData.before.asset').read_bytes()
checks['localization_bytes_preserved']=True
for name in ['Boat_DuckLuxury103','Boat_Corsair_Reference106','Boat_ForestSail_Reference106']:
 text=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab').read_text(encoding='utf-8-sig')
 for anchor in ['Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:assert text.count('m_Name: '+anchor+'\n')==1
 if name!='Boat_DuckLuxury103':assert 'm_Name: Original_Style_Screen_Mount112\n' in text
checks['three_current_prefabs_saved_with_unique_anchors']=True
Path('Docs/assets/lobby-island/PRESERVATION112_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True))
