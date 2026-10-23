from pathlib import Path
import re,json
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-116')
exec(compile(prefix,'yaml116','exec'))
assert all(i in after for i in before),'Owner objects removed'
allowed_names=['01_Central_Island','Bank_Crystal_Boutique095','Boat_Corsair_Reference106','Boat_ForestSail_Reference106']+['Personal_Island_'+str(i).zfill(2) for i in range(1,9)]
ids={i for i,(kind,b) in before.items() if kind=='1' and any('  m_Name: '+n+'\n' in b for n in allowed_names)}
root_tids={i for i,g in transforms.items() if g in ids}
changed=[i for i in before if before[i]!=after[i]]
assert set(changed)==root_tids,(len(changed),changed)
for i in changed:
 a=before[i][1];b=after[i][1]
 pattern=r'  m_Children:.*?  m_Father:'
 assert re.sub(pattern,'',a,flags=re.S)==re.sub(pattern,'',b,flags=re.S),(i,'Owner transform changed')
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-116/LocalizationData.before.asset').read_bytes()
scene=Path('Assets/Scenes/LobbyIslandWorkshop.unity').read_text(encoding='utf-8-sig')
assert scene.count('  m_Name: Continuous_Shore_Base116\n')==9
assert scene.count('  m_Name: Sealed_Helm_Joinery116\n')==2
assert scene.count('  m_Name: Safe_Deposit_Box_Front116\n')==24
assert 'm_Name: Temporary_' not in scene
for n in ['Boat_Corsair_Reference106','Boat_ForestSail_Reference106']:
 text=Path('Assets/Prefabs/Workshop/Boats/Skins103',n+'.prefab').read_text(encoding='utf-8-sig');assert '  m_Name: Sealed_Helm_Joinery116\n' in text
 assert '  m_Name: Matching_Style_Art115\n' in text
report={'all_previous_scene_objects_retained':True,'existing_changes_only_12_parent_child_lists':len(changed),'owner_lowered_helm_frames_and_all_control_screen_poses_unchanged':True,'all_old_bank_clerk_crystals_trays_vfx_and_title_exactly_preserved':True,'old_shore_beach_foam_terrace_transforms_and_meshes_unchanged':True,'all_five_trailers_and_other_boats_preserved':True,'nine_closed_shores_two_helm_cabinets_and_24_safe_boxes_saved':True,'localization_bytes_preserved':True,'no_temporary_colliders_or_player_saved':True}
Path('Docs/assets/lobby-island/PRESERVATION116_VALIDATION.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
