from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-101')
code=prefix+'''
checks={}
for name in ['Animal_Laboratory097','04_Boat_Skins_Showcase','02_Personal_Islands_8_Visual_Plots','Park_Paths098','Bank_Localized_UI096_Canvas','Four_Crystal_Offers100','American_Classical_Bank099','Actual_Hospital_Merchandise098','Hospital_Equipment100','Leaderboard_САМОЦВЕТЫ','Leaderboard_ПОБЕДЫ']:
    records,_=branch(name);assert all(after[i]==before[i] for i in records),name;checks[name]=len(records)
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-101/LocalizationData.before.asset').read_bytes();checks['localization_bytes_preserved']=True
for name in ['Signature_Blue_Gem095','Gem_Pedestal','Original_Supplies_Display100']:
    ids=[i for i,(kind,b) in after.items() if kind=='1' and f'  m_Name: {name}\\n' in b]
    assert len(ids)==1 and '  m_IsActive: 0' in after[ids[0]][1],name
checks['bank_rear_and_old_supply_stock_hidden']=True
report=Path('Docs/assets/lobby-island/REFINE101_VALIDATION.txt').read_text(encoding='utf-8')
assert report.count('full paving clearance=3,32m')==2 or report.count('full paving clearance=3.32m')==2
checks['both_benches_on_grass_with_verified_clearance']=True
Path('Docs/assets/lobby-island/PRESERVATION101_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(checks,ensure_ascii=True))
'''
Path('Library/WorkshopBackups/DB-ON-101/verify.py').write_text(code,encoding='utf-8');exec(compile(code,'verify101','exec'))
