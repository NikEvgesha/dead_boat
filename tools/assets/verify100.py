from pathlib import Path
import json
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-100')
code=prefix+'''
checks={}
for name in ['Animal_Laboratory097','Luxury_Boat_Salon095','Actual_Boat_RenderOnly_1to1','02_Personal_Islands_8_Visual_Plots','Park_Paths098','Bank_Localized_UI096_Canvas','Actual_Hospital_Merchandise098','Leaderboard_САМОЦВЕТЫ','Leaderboard_ПОБЕДЫ','Ivory_Platform']:
    records,_=branch(name);assert all(after[i]==before[i] for i in records),name;checks[name]=len(records)
for source,checkpoint in [('Assets/Igrodelnya/Localization/LocalizationData.asset','LocalizationData.before.asset'),('Assets/Scenes/CastleServicesWorkshop.unity','CastleServicesWorkshop.before.unity')]:
    assert Path(source).read_bytes()==Path('Library/WorkshopBackups/DB-ON-100',checkpoint).read_bytes();checks[source+'_bytes_preserved']=True
Path('Docs/assets/lobby-island/PRESERVATION100_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(checks,ensure_ascii=True))
'''
Path('Library/WorkshopBackups/DB-ON-100/verify.py').write_text(code,encoding='utf-8')
exec(compile(code,'verify100','exec'))
