from pathlib import Path
import re,json
def blocks(path):
    return {m[2]:(m[1],m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n.*?(?=^--- !u!|\Z)',Path(path).read_text(encoding='utf-8-sig'),re.M|re.S)}
before=blocks('Library/WorkshopBackups/DB-ON-099/LobbyIslandWorkshop.before.unity'); after=blocks('Assets/Scenes/LobbyIslandWorkshop.unity')
transforms={i:re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b)[1] for i,(kind,b) in before.items() if kind in ('4','224')}
tree={i:re.findall(r'- \{fileID: (-?\d+)\}',b.split('  m_Children:')[1].split('  m_Father:')[0]) for i,(kind,b) in before.items() if kind in ('4','224')}
def branch(name):
    def read_name(b):
        match=re.search(r'^  m_Name: (.*)$',b,re.M)
        if not match:return None
        value=match[1]
        if value.startswith('"'):
            value=re.sub(r'\\x([0-9a-fA-F]{2})',lambda m:'\\u00'+m[1],value)
            return json.loads(value)
        return value
    ids={i for i,(kind,b) in before.items() if kind=='1' and read_name(b)==name}
    assert ids,name
    todo=[i for i,g in transforms.items() if g in ids]; tids=set()
    while todo:
        tid=todo.pop();tids.add(tid);ids.add(transforms[tid]);todo.extend(tree[tid])
    records={i for i,(kind,b) in before.items() if i in ids or re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b) and re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b)[1] in ids}
    return records,tids
checks={}
for name in ['Animal_Chamber_1','Animal_Chamber_2','04_Boat_Skins_Showcase','02_Personal_Islands_8_Visual_Plots','Park_Paths098','Bank_Localized_UI096_Canvas','Actual_Blue_Gem095','Signature_Blue_Gem095','Actual_Hospital_Merchandise098']:
    records,_=branch(name);assert all(after[i]==before[i] for i in records),name;checks[name]=len(records)
for name in ['Leaderboard_САМОЦВЕТЫ','Leaderboard_ПОБЕДЫ']:
    records,tids=branch(name)
    for i in records:
        a,b=before[i],after[i]
        if i in tids and '  m_LocalPosition: {x: ' in a[1] and a[1]!=b[1]:
            assert re.sub(r'^  m_LocalPosition: .*$', '',a[1],flags=re.M)==re.sub(r'^  m_LocalPosition: .*$','',b[1],flags=re.M)
        else:assert a==b,(name,i)
    checks[name+'_only_root_position_changed']=True
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-099/LocalizationData.before.asset').read_bytes()
checks['all_translation_bytes_preserved']=True
Path('Docs/assets/lobby-island/PRESERVATION099_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(checks,ensure_ascii=True))
