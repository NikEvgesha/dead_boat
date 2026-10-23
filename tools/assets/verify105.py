from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-105')
exec(compile(prefix,'yaml105','exec'))
tree={i:[child for child in children if child in transforms] for i,children in tree.items()}
allowed=set()
for name in ['Boat_DuckLuxury103','Boat_RoyalBlue103','Boat_SteamTug103','03_Guns_Source_Home_GunsStore']:
    records,_=branch(name);allowed.update(records)
unchanged=[i for i in before if i not in allowed]
assert all(i in after and before[i]==after[i] for i in unchanged),'Unrelated scene records changed'
assert all(i in after for i in before),'Owner scene objects removed'
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-105/LocalizationData.before.asset').read_bytes()
checks={'unrelated_scene_records_preserved':len(unchanged),'all_saved_owner_objects_retained':True,'translations_bytes_preserved':True}
for name in ['steering (1)','Fire','Point','Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:
    records,_=branch(name)
    assert all(before[i]==after[i] for i in records),(name,'control or anchor changed')
    checks[name+'_records_preserved']=len(records)
blue=Path('Assets/Prefabs/Workshop/Boats/Skins103/Boat_RoyalBlue103.prefab').read_text(encoding='utf-8-sig')
assert len(re.findall(r'^--- !u!65 ',blue,re.M))==20
assert 'm_Name: Aft_Lounge_Back\n' not in blue and 'm_Name: Aft_Lounge_Seat\n' not in blue
checks['blue_20_solid_colliders_saved']=True
checks['owner_deleted_aft_seat_and_back_not_restored']=True
for name in ['Boat_DuckLuxury103','Boat_RoyalBlue103','Boat_SteamTug103']:
    text=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab').read_text(encoding='utf-8-sig')
    assert not re.search(r'^--- !u!54 ',text,re.M),'Rigidbody added'
    for anchor in ['Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:assert text.count('m_Name: '+anchor+'\n')==1
for name in ['Six_Independent_Armory_Shelves105','Weapon_Shelf_1105','Weapon_Shelf_2105','Weapon_Shelf_3105','Ammo_Shelf_1105','Ammo_Shelf_2105','Ammo_Shelf_3105']:
    assert sum('  m_Name: '+name+'\n' in b for kind,b in after.values() if kind=='1')==1
checks['six_independent_shelves_saved']=True
Path('Docs/assets/lobby-island/PRESERVATION105_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True))
