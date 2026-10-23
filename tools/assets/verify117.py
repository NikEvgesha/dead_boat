from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-117')
exec(compile(prefix,'yaml117','exec'))
assert all(i in after for i in before),'Old objects removed'
income_ids={i for i,(k,b) in before.items() if k=='1' and '  m_Name: 03_Income_Collection_Visual\n' in b}
parent_tids={i for i,g in transforms.items() if g in income_ids}
hide_names=['Plot_Header','Plot_Number','Income_Chest_VisualOnly','Income_Gold_Coin114']
hide_ids={i for i,(k,b) in before.items() if k=='1' and any('  m_Name: '+n+'\n' in b for n in hide_names)}
changed=[i for i in before if before[i]!=after[i]]
for i in changed:
 a=before[i][1];b=after[i][1]
 if i in parent_tids:
  pattern=r'  m_Children:.*?  m_Father:'
  assert re.sub(pattern,'',a,flags=re.S)==re.sub(pattern,'',b,flags=re.S),i
 elif i in hide_ids:
  assert re.sub(r'^  m_IsActive: .*$', '',a,flags=re.M)==re.sub(r'^  m_IsActive: .*$', '',b,flags=re.M),i
  assert '  m_IsActive: 0\n' in b
 else:raise AssertionError(('Unrelated record changed',i,before[i][0]))
for name in ['Plot_Header','Plot_Number','Income_Chest_VisualOnly']:
 ids=[i for i,(k,b) in after.items() if k=='1' and '  m_Name: '+name+'\n' in b]
 assert len(ids)==8
 assert all('  m_IsActive: 0\n' in after[i][1] for i in ids)
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-117/LocalizationData.before.asset').read_bytes()
scene=Path('Assets/Scenes/LobbyIslandWorkshop.unity').read_text(encoding='utf-8-sig')
assert scene.count('  m_Name: Income_Money_Display117\n')==8
assert scene.count('  m_Name: Gold_Income_Pouch117\n')==16
assert 'm_Name: Temporary_' not in scene
result={'all_old_objects_retained':True,'eight_numbered_signs_and_backplates_hidden':True,'eight_income_chests_hidden':True,'only_income_child_lists_and_requested_visibility_changed':True,'eight_money_displays_with_16_tied_pouches_saved':True,'counter_canopy_collection_sign_entry_and_anchors_preserved':True,'bank_shores_boats_trailers_nickname_ui_unchanged':True,'localization_bytes_preserved':True}
Path('Docs/assets/lobby-island/PRESERVATION117_VALIDATION.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
