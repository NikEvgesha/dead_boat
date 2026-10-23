from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-104')
exec(compile(prefix,'yaml104','exec'))
allowed,_=branch('Cosmetic_Only_Skin103')
unchanged=[i for i in before if i not in allowed]
assert all(i in after and before[i]==after[i] for i in unchanged),'Unrelated scene records changed'
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-104/LocalizationData.before.asset').read_bytes()
checks={'all_scene_records_outside_three_cosmetic_branches_preserved':len(unchanged),'all_translations_bytes_preserved':True}
for name in ['Boat_DuckLuxury103','Boat_RoyalBlue103','Boat_SteamTug103']:
    text=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab').read_text(encoding='utf-8-sig')
    for anchor in ['Future_Helm_Anchor103','Future_Coal_Deposit_Anchor103','Future_Coal_Mouth_Anchor103']:assert text.count('m_Name: '+anchor+'\n')==1
    assert text.encode()!=Path('Library/WorkshopBackups/DB-ON-104',name+'.prefab').read_bytes()
    checks[name+'_updated_with_unique_control_anchors']=True
checks['images_present']=all(Path('Docs/assets/lobby-island',f+'.png').is_file() for f in ['62-distinct-boat-skins104','63-duck-clear-bow104','64-blue-yacht-salon104','65-red-working-tug104','66-duck-helm-forward104'])
assert checks['images_present']
Path('Docs/assets/lobby-island/PRESERVATION104_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(checks,ensure_ascii=True))
