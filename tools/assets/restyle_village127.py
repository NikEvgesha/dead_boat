"""Create castle-style material overrides, preserving all original gameplay records."""
from collections import Counter
from pathlib import Path
import re
import uuid

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Prefabs/Workshop/Locations/CastleStyle'
ENEMY = ROOT / 'Assets/Prefabs/Locations/Enemy'
VISUAL = ENEMY.parent / 'visual/To4ka7.prefab'
MATS = OUT.parents[1] / 'CottageGeometry'

def guid(path):
    return re.search(r'^guid: (\w+)', Path(str(path)+'.meta').read_text(), re.M)[1]

def file_id(body, key):
    match = re.search(rf'^  {key}: \{{fileID: (-?\d+)', body, re.M)
    return match[1] if match else None

def docs(text):
    return re.findall(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)

def create():
    vg = guid(VISUAL)
    blocks = docs(VISUAL.read_text(encoding='utf-8-sig'))
    gos = {i:b for t,i,b in blocks if t=='1'}
    transforms = {file_id(b,'m_GameObject'):(i,b) for t,i,b in blocks if t=='4' and file_id(b,'m_GameObject')}
    by_transform = {i:g for g,(i,b) in transforms.items()}
    roofs = [
        ('RustTiles_Base','RustTiles_Dark','RustTiles_Light'),
        ('SlateTiles_Gray','SlateTiles_Dark','SlateTiles_Light'),
        ('CaptainTiles_Navy','CaptainTiles_Dark','CaptainTiles_Light'),
    ]
    source_roles = {
        '75b471f61071b9743a7c8319e6e4b91b':'roof',
        '2029a5a2a27b8484bb3e2562d8bbdfa3':'wall',
        '991fe0467bc27354f88af9913757671c':'wood',
        '76ba543a11481b94fbb79034d865c48e':'masonry',
        '09c0c3f8df5925948ae159e03bf40def':'trim',
    }
    outputs = {}
    for index,name in enumerate(('house1_1','house2_1','house3_1','Village_1')):
        original=(ENEMY/(name+'.prefab')).read_text(encoding='utf-8-sig')
        end = f'  m_SourcePrefab: {{fileID: 100100000, guid: {vg}, type: 3}}'
        assert original.count(end)==1
        pos=original.index(end)
        start=original.rindex('\n--- !u!1001 ',0,pos)
        instance=original[start:pos]
        marker='    m_RemovedComponents:'
        assert instance.count(marker)==1
        section=instance.split('    m_RemovedGameObjects:',1)[1].split('    m_AddedGameObjects:',1)[0]
        removed=set(re.findall(r'fileID: (-?\d+)',section))
        modifications=[]
        counts=Counter()
        gunstore=VISUAL.with_name('GunStore.prefab')
        gg=guid(gunstore)
        house_docs=docs(gunstore.read_text(encoding='utf-8-sig'))
        root_go=next(file_id(b,'m_GameObject') for t,i,b in house_docs
                     if t=='4' and file_id(b,'m_Father')=='0')
        mask=(1<<63)-1
        # Confirm the nested ID convention against a serialized stripped object.
        assert (1111268783736552061 ^ 1013491281105164761)==107138480527795108
        nested=[(i,b) for t,i,b in blocks if t=='1001' and
                f'm_SourcePrefab: {{fileID: 100100000, guid: {gg},' in b
                and str((int(i)^int(root_go))&mask) not in removed]
        render_groups = []
        for instance_id, nested_body in nested:
            nested_removed=set(re.findall(r'fileID: (\d+)',nested_body.split('m_RemovedGameObjects:',1)[1].split('m_AddedGameObjects:',1)[0]))
            render_groups.append([
                (str((int(i)^int(instance_id))&mask), b)
                for t,i,b in house_docs if t=='23' and file_id(b,'m_GameObject')
                and file_id(b,'m_GameObject') not in nested_removed
                and str((int(file_id(b,'m_GameObject'))^int(instance_id))&mask) not in removed
            ])
        direct = {}
        for t,i,b in blocks:
            if t!='23':
                continue
            go=file_id(b,'m_GameObject')
            hierarchy=[]
            while go in transforms:
                hierarchy.append(go)
                go=by_transform.get(file_id(transforms[go][1],'m_Father'))
            if len(hierarchy)<2 or any(g in removed for g in hierarchy):
                continue
            group=re.search(r'^  m_Name: (dom(?: \(\d+\))?)$',gos[hierarchy[-2]],re.M)
            if group:
                direct.setdefault(group[1],[]).append((i,b))
        render_groups.extend(direct.values())
        for house_number,renderers in enumerate(render_groups):
            roof=roofs[index if index<3 else house_number%3]
            for i,b in renderers:
                match=re.search(r'^  m_Materials:\n((?:  - .*\n)+)',b,re.M)
                if not match:
                    continue
                for slot, line in enumerate(match[1].splitlines()):
                    g=re.search(r'guid: (\w+)',line)
                    role=source_roles.get(g[1]) if g else None
                    if not role:
                        continue
                    variation=int(i)%11
                    material={
                        'roof':roof[1 if variation<2 else 2 if variation>8 else 0],
                        'wall':'CastleStone_Block',
                        'wood':'Roof_InteriorBrown',
                        'masonry':'CastleStone_BlockDark' if variation<4 else 'CastleStone_Block',
                        'trim':'CastleStone_Trim',
                    }[role]
                    new=guid(MATS/(material+'.mat'))
                    modifications.append(
                        f'    - target: {{fileID: {i}, guid: {vg}, type: 3}}\n'
                        f'      propertyPath: m_Materials.Array.data[{slot}]\n'
                        f'      value: \n'
                        f'      objectReference: {{fileID: 2100000, guid: {new}, type: 2}}\n'
                    )
                    counts[(house_number,role)]+=1
        assert modifications and all(sum(c for (_,r),c in counts.items() if r==role)>0 for role in ('wall','roof')), (name, counts)
        inserted=''.join(modifications)
        assert inserted not in original
        at=start+instance.index(marker)
        result=original[:at]+inserted+original[at:]
        assert result.replace(inserted,'',1)==original
        outputs[name]=(result,len(modifications),len({g for g,_ in counts}))
    return outputs

if __name__=='__main__':
    outputs=create()
    folder_meta=Path(str(OUT)+'.meta')
    OUT.mkdir(exist_ok=True)
    if not folder_meta.exists():
        folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n',encoding='utf-8')
    for name,(data,count,groups) in outputs.items():
        path=OUT/(name+'_CastleStyle.prefab')
        meta=Path(str(path)+'.meta')
        if path.exists() and path.read_text(encoding='utf-8')!=data:
            raise SystemExit(f'Refusing to overwrite edited prefab: {path}')
        path.write_text(data,encoding='utf-8',newline='\n')
        if not meta.exists():
            meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nPrefabImporter:\n  externalObjects: {{}}\n',encoding='utf-8')
        print(f'{name}: {count} material overrides, {groups} house groups; original records preserved')

