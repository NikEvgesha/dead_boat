from pathlib import Path
import json,re,subprocess
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-109')
exec(compile(prefix,'yaml109','exec'))
tree={i:[c for c in children if c in transforms] for i,children in tree.items()}
allowed=set()
for name in ['Boat_DuckLuxury103','Bank_Crystal_Boutique095']:
 records,_=branch(name);allowed.update(records)
unchanged=[i for i in before if i not in allowed]
assert all(i in after and before[i]==after[i] for i in unchanged),'Unrelated records changed'
assert all(i in after for i in before),'Saved owner objects removed'
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-109/LocalizationData.before.asset').read_bytes()
checks={'unrelated_scene_records_preserved':len(unchanged),'saved_owner_objects_retained':True,'translations_bytes_preserved':True}
for name in ['steering (1)','Fire','Fire_01','Point','Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103','Low_Round_Duck_Cheeks108','Inset_Duck_Eye108','Duck_Eye_Glint108','Rounded_Closed_Stern_Deck108']:
 records,_=branch(name);assert all(before[i]==after[i] for i in records),name
checks['duck_hull_face_eyes_stern_and_control_poses_preserved']=True
bank_records,bank_tids=branch('Bank_Crystal_Boutique095')
changed_materials=0
for i in bank_records:
 if before[i]==after[i]:continue
 kind,a=before[i];b=after[i][1]
 if kind=='4':
  assert re.sub(r'  m_Children:.*?(?=  m_Father:)','',a,flags=re.S)==re.sub(r'  m_Children:.*?(?=  m_Father:)','',b,flags=re.S),('bank transform changed',i)
 elif kind=='23':
  assert re.sub(r'  m_Materials:.*?(?=  m_StaticBatchInfo:)','',a,flags=re.S)==re.sub(r'  m_Materials:.*?(?=  m_StaticBatchInfo:)','',b,flags=re.S),('bank renderer changed beyond material',i)
  changed_materials+=1
 else:raise AssertionError(('bank non-material change',kind,i))
assert changed_materials==39
checks['39_crystal_materials_only_geometry_positions_counts_unchanged']=True
fx=Path('Assets/Prefabs/Workshop/LobbyIsland/BankCrystal109/Bank_Crystal_VFX109.prefab').read_text(encoding='utf-8-sig')
assert len(re.findall(r'^--- !u!198 ',fx,re.M))==10
assert len(re.findall(r'^--- !u!199 ',fx,re.M))==10
assert not re.search(r'^--- !u!(54|65|64|108) ',fx,re.M)
assert subprocess.run(['git','diff','--exit-code','--','Assets/Materials/Gem.mat','Assets/Prefabs/Gem.prefab'],capture_output=True).returncode==0
checks['ten_particle_systems_saved_without_physics_or_lights']=True
checks['original_game_gem_material_and_prefab_unmodified']=True
Path('Docs/assets/lobby-island/PRESERVATION109_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True))
