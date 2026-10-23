from pathlib import Path
import re,json
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-115')
exec(compile(prefix,'yaml115','exec'))
assert all(i in after for i in before),'Owner objects removed'
boatnames=['Boat_DuckLuxury103','Boat_RoyalBlue103','Boat_SteamTug103','Boat_Corsair_Reference106','Boat_ForestSail_Reference106']
root_transforms=[]
for name in boatnames:
 ids=[i for i,(kind,b) in before.items() if kind=='1' and '  m_Name: '+name+'\n' in b]
 assert len(ids)==1,name
 root_transforms += [i for i,g in transforms.items() if g in ids]
changed=[i for i in before if before[i]!=after[i]]
assert set(changed)==set(root_transforms),(len(changed),changed)
for i in changed:
 a=before[i][1];b=after[i][1]
 pattern=r'  m_Children:.*?  m_Father:'
 assert re.sub(pattern,'',a,flags=re.S)==re.sub(pattern,'',b,flags=re.S),(i,'Other transform properties changed')
scene=Path('Assets/Scenes/LobbyIslandWorkshop.unity').read_text(encoding='utf-8-sig')
for skin in ['Duck','RoyalBlue','SteamTug','Corsair','ForestSail']:
 assert scene.count('  m_Name: Trailer_'+skin+'115\n')==1,skin
 p=Path('Assets/Prefabs/Workshop/Boats/Skins103/Trailer_'+skin+'115.prefab')
 text=p.read_text(encoding='utf-8-sig');assert 'm_Name: Matching_Style_Art115' in text
 assert text.count('  m_Name: Open_Cargo_Deck_Plank115\n')==26
 assert 'Rigidbody:' not in text and 'MonoBehaviour:' not in text
for name in boatnames:
 text=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab').read_text(encoding='utf-8-sig')
 for anchor in ['Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:assert text.count('m_Name: '+anchor+'\n')==1
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-115/LocalizationData.before.asset').read_bytes()
assert 'm_Name: Temporary_' not in scene
report={'all_original_scene_records_retained':True,'only_five_existing_boat_child_lists_extended':True,'all_original_boat_objects_and_owner_edits_exactly_preserved':True,'harbour_private_white_boats_and_their_hidden_trailers_unchanged':True,'five_separate_trailer_prefabs_and_five_updated_boat_prefabs_saved':True,'no_gameplay_scripts_rigidbody_or_towing_physics_added':True,'localization_water_income_tents_owner_labels_exactly_preserved':True}
Path('Docs/assets/lobby-island/PRESERVATION115_VALIDATION.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
