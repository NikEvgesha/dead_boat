from pathlib import Path
import re,json
def blocks(path):
    return {m[2]:(m[1],m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n.*?(?=^--- !u!|\Z)',Path(path).read_text(encoding='utf-8-sig'),re.M|re.S)}
before=blocks('Library/WorkshopBackups/DB-ON-118/LobbyIslandWorkshop.before.unity'); after=blocks('Assets/Scenes/LobbyIslandWorkshop.unity')
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

checks={};counts={};moved=0;hidden=0;parents=0
assert set(before)<=set(after),'Deleted old records'
for i,(kind,a) in before.items():
    b=after[i][1]
    if a==b:continue
    if kind=='1':
        name=re.search(r'^  m_Name: (.*)$',a,re.M)[1]
        assert name in ('Starter_Only_Pen_Approach_Hide_For_Upgrades','New_Path090'),name
        assert '  m_IsActive: 1' in a and '  m_IsActive: 0' in b
        assert a.replace('  m_IsActive: 1','  m_IsActive: 0')==b
        hidden+=1
    elif kind=='4':
        name=re.search(r'^  m_Name: (.*)$',before[transforms[i]][1],re.M)[1]
        if '  m_LocalPosition:' in a and re.search(r'^  m_LocalPosition: .*$',a,re.M)[0]!=re.search(r'^  m_LocalPosition: .*$',b,re.M)[0]:
            assert re.sub(r'^  m_LocalPosition: .*$', '',a,flags=re.M)==re.sub(r'^  m_LocalPosition: .*$', '',b,flags=re.M),(i,name)
            assert name in ('Palm','Palm_UniformScale092','Gold_Income_Pouch117','Slatted_Harbour_Bench094') or name.startswith(('Static_Animal_Mesh','Static_Staff_')),name
            counts[name]=counts.get(name,0)+1;moved+=1
        else:
            assert name.startswith('Personal_Island_') or name=='Bank_Crystal_Boutique095',name
            normalize=lambda t:re.sub(r'  m_Children:.*?(?=  m_Father:)','',t,flags=re.S)
            assert normalize(a)==normalize(b),(i,name)
            oldkids=re.findall(r'- \{fileID: (-?\d+)\}',a.split('  m_Children:')[1].split('  m_Father:')[0])
            newkids=re.findall(r'- \{fileID: (-?\d+)\}',b.split('  m_Children:')[1].split('  m_Father:')[0])
            assert newkids[:-1]==oldkids and newkids[-1] not in before
            parents+=1
    else:raise AssertionError((i,kind))
assert moved==48 and hidden==24 and parents==9,(moved,hidden,parents)
checks.update(all_old_records_retained=len(before),only_scoped_positions_changed=counts,only_internal_paths_hidden=hidden,parents_only_append_new_children=parents)
for name in ['06_Standalone_Boat_Skin_Studies103','04_Boat_Skins_Showcase','Park_Paths098']:
    records,_=branch(name);assert all(after[i]==before[i] for i in records),name
    checks[name+'_all_records_preserved']=len(records)
assert Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()==Path('Library/WorkshopBackups/DB-ON-118/LocalizationData.before.asset').read_bytes()
checks['localization_bytes_preserved']=True
new=[b for i,(kind,b) in after.items() if i not in before and kind=='1']
assert sum('  m_Name: Outside_Pen_Entry_Path118\n' in b for b in new)==8
assert not any('  m_Name: Temporary_' in b for kind,b in after.values() if kind=='1')
checks['outside_only_entry_strips']=8
checks['saved_without_temporary_objects']=True
Path('Docs/assets/lobby-island/PRESERVATION118_VALIDATION.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(checks,ensure_ascii=True))
