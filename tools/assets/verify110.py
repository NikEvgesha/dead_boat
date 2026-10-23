from pathlib import Path
import json,re,subprocess
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-110')
exec(compile(prefix,'yaml110','exec'))
assert set(before)==set(after),'Scene objects added/removed'
changed=[i for i in before if before[i]!=after[i]]
assert len(changed)==39,len(changed)
for i in changed:
 assert before[i][0]=='23',('non-renderer change',i)
 assert re.sub(r'  m_Materials:.*?(?=  m_StaticBatchInfo:)','',before[i][1],flags=re.S)==re.sub(r'  m_Materials:.*?(?=  m_StaticBatchInfo:)','',after[i][1],flags=re.S),('change beyond material',i)
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-110/LocalizationData.before.asset').read_bytes()
assert subprocess.run(['git','diff','--exit-code','--','Assets/Materials/Gem.mat','Assets/Import/Stones_and_buried_treasure/Materials/Emerald.mat'],capture_output=True).returncode==0
checks={'only_39_renderer_material_references_changed':True,'all_other_scene_records_including_VFX_and_boats_preserved':len(before)-39,'translations_bytes_preserved':True,'original_game_gem_and_emerald_materials_unmodified':True}
Path('Docs/assets/lobby-island/PRESERVATION110_VALIDATION.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
print(json.dumps(checks))
