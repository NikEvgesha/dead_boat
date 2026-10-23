from pathlib import Path
import re,json
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-113')
exec(compile(prefix,'yaml113','exec'))
changed=[i for i in before if before[i]!=after[i]]
assert set(before)==set(after),'Scene objects added/removed'
assert len(changed)==5,len(changed)
for i in changed:
 assert before[i][0] in ('4','224'),i
 a=before[i][1];b=after[i][1]
 assert re.sub(r'^  m_LocalPosition: .*$', '',a,flags=re.M)==re.sub(r'^  m_LocalPosition: .*$', '',b,flags=re.M)
 pa=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)',a)
 pb=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)',b)
 assert pa[1]==pb[1] and pa[2]==pb[2] and abs(float(pa[3])-float(pb[3])-2)<.0001
assert 'm_Name: Temporary_Project_Player' not in Path('Assets/Scenes/LobbyIslandWorkshop.unity').read_text(encoding='utf-8-sig')
result={'only_five_catalogue_transforms_changed':True,'all_five_shifted_2m_towards_entrance':True,'all_other_scene_records_and_owner_edits_exactly_preserved':True,'no_temporary_player_or_collision_objects_saved':True}
Path('Docs/assets/lobby-island/PRESERVATION113_VALIDATION.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result))
