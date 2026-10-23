from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-103')
code=prefix+'''
checks={}
for name in ['Actual_Boat_RenderOnly_1to1','02_Personal_Islands_8_Visual_Plots','Animal_Chamber_1','Animal_Chamber_2','Bank_Crystal_Boutique095','04_Hospital_Source_Home_Hospital','Three_Supply_Offers101','Park_Paths098','Leaderboard_САМОЦВЕТЫ','Leaderboard_ПОБЕДЫ','Slatted_Harbour_Bench094','Boats_Localized_UI096_Canvas','Catalogue_Localized_UI096_Canvas']:
    records,_=branch(name);assert all(before[i]==after[i] for i in records),name;checks[name]=len(records)
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-103/LocalizationData.before.asset').read_bytes();checks['localization_bytes_preserved']=True
for name in ['Boat_DuckLuxury103','Boat_RoyalBlue103','Boat_SteamTug103','Boat_WoodenControlAligned103']:
    prefab=Path('Assets/Prefabs/Workshop/Boats/Skins103',name+'.prefab');assert prefab.exists()
    text=prefab.read_text(encoding='utf-8-sig')
    assert 'Future_Helm_Anchor103' in text and 'Future_Coal_Deposit_Anchor103' in text and 'Future_Coal_Mouth_Anchor103' in text
    checks[name+'_prefab_and_control_anchors_saved']=True
Path('Docs/assets/lobby-island/PRESERVATION103_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(checks,ensure_ascii=True))
'''
Path('Library/WorkshopBackups/DB-ON-103/verify.py').write_text(code,encoding='utf-8');exec(compile(code,'verify103','exec'))
