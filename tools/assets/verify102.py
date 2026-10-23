from pathlib import Path
import json,re
prefix=Path('tools/assets/verify099.py').read_text(encoding='utf-8').split('checks={}')[0].replace('DB-ON-099','DB-ON-102')
code=prefix+'''
def no_children(b):return re.sub(r'  m_Children:.*?(?=  m_Father:)', '',b,flags=re.S)
checks={}
for name in ['Animal_Laboratory097','02_Personal_Islands_8_Visual_Plots','Bank_Crystal_Boutique095','04_Hospital_Source_Home_Hospital','02_General_Source_Home_GeneralStore','Park_Paths098','Leaderboard_САМОЦВЕТЫ','Leaderboard_ПОБЕДЫ','Slatted_Harbour_Bench094']:
    records,tids=branch(name)
    for i in records:
        if i in tids:assert before[i][0]==after[i][0] and no_children(before[i][1])==no_children(after[i][1]),(name,i)
        else:assert before[i]==after[i],(name,i)
    checks[name+'_existing_records_preserved_allow_new_children']=len(records)
for name in ['Actual_Boat_RenderOnly_1to1','Actual_Castle_Wooden_Boat_Static100','Boats_Localized_UI096_Canvas','Catalogue_Localized_UI096_Canvas']:
    records,tids=branch(name)
    for i in records:
        a,b=before[i],after[i]
        if i in tids and a!=b:
            def strip(text):return re.sub(r'^  (m_LocalPosition|m_LocalRotation|m_AnchoredPosition|m_Father): .*$', '',text,flags=re.M)
            assert a[0]==b[0] and strip(a[1])==strip(b[1]),(name,i)
        else:assert a==b,(name,i)
    checks[name+'_meshes_materials_scale_and_bindings_preserved']=True
names=[b for kind,b in after.values() if kind=='1']
assert sum('  m_Name: Island_Owner_UI102\\n' in b for b in names)==8
assert sum('  m_Name: Owner_Nickname_UI102\\n' in b for b in names)==8
homes=[b for b in names if '  m_Name: Own_Island_Home_Icon102\\n' in b];assert len(homes)==8 and sum('  m_IsActive: 1' in b for b in homes)==1
assert sum('  m_Name: Static_Staff_' in b and '102\\n' in b for b in names)==5
checks['owner_ui_count']=8;checks['own_home_preview_count']=1;checks['static_staff_count']=5
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-102/LocalizationData.before.asset').read_bytes();checks['localization_bytes_preserved']=True
Path('Docs/assets/lobby-island/PRESERVATION102_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(checks,ensure_ascii=True))
'''
Path('Library/WorkshopBackups/DB-ON-102/verify.py').write_text(code,encoding='utf-8');exec(compile(code,'verify102','exec'))
