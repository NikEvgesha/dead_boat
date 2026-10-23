"""Restore original boat script records erased by Unity unpack with missing SDK.

Requires Temp/boat_source_map.tsv generated from the untouched source instance.
All original local references are remapped and asserted; source assets are read-only.
"""
import re
from pathlib import Path
import export_workshop_prefabs as graph

def parse(path):
    text=path.read_text(encoding='utf-8-sig')
    return {int(m[2]):(int(m[1]),m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)}

guids={}
for meta in Path('Assets/Prefabs').rglob('*.prefab.meta'):
    guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1]
    guids[guid]=Path(str(meta)[:-5])
main='12b02cb40e90d9d4888ecb56343db668'
sources={}
def load(guid):
    if guid in sources or guid not in guids:return
    sources[guid]=parse(guids[guid])
    for kind,body in sources[guid].values():
        if kind==1001:load(re.search(r'm_SourcePrefab: \{fileID: \d+, guid: (\w+)',body)[1])
load(main)

def alias(key):
    guid,id=key
    if guid not in sources:return key
    body=sources[guid][id][1]
    m=re.search(r'm_CorrespondingSourceObject: \{fileID: (-?\d+), guid: (\w+)',body)
    return alias((m[2],int(m[1]))) if m else key

dest=Path('Assets/Prefabs/Workshop/GameAligned/Board_CastleCargo_Variant.prefab')
target=parse(dest)
rows=[line.split('\t') for line in Path('Temp/boat_source_map.tsv').read_text(encoding='utf-8-sig').splitlines()]
gos={r[1]:r for r in rows if r[0]=='G'}
mapping={}
for r in gos.values():mapping[(r[2],int(r[3]))]=int(r[4])
for r in rows:
    if r[0]=='C' and r[3]:
        components=list(map(int,re.findall(r'component: \{fileID: (-?\d+)\}',target[int(gos[r[1]][4])][1])))
        mapping[(r[3],int(r[4]))]=components[int(r[2])]
for guid,docs in sources.items():
    for id in docs:
        original=alias((guid,id))
        if original in mapping:mapping[(guid,id)]=mapping[original]

restored=[]
for path,r in gos.items():
    go=int(r[4]);components=list(map(int,re.findall(r'component: \{fileID: (-?\d+)\}',target[go][1])))
    missing=[id for id in components if target[id][0]==114 and 'm_Script: {fileID: 0}' in target[id][1]]
    if not missing:continue
    candidates=[]
    base_comps=list(map(int,re.findall(r'component: \{fileID: (-?\d+)\}',sources[r[2]][int(r[3])][1])))
    for guid,docs in sources.items():
        for id,(kind,body) in docs.items():
            gm=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',body)
            if kind!=114 or not gm or not re.search(r'm_Script: \{fileID: 11500000, guid:',body):continue
            if mapping.get((guid,int(gm[1])))!=go or (guid,id) in mapping:continue
            rank=base_comps.index(id) if guid==r[2] and id in base_comps else 10000
            candidates.append((rank,guid,id,body))
    candidates.sort(key=lambda c:c[0])
    assert len(candidates)==len(missing),(path,len(missing),[(g,id) for _,g,id,_ in candidates])
    for targetid,(_,guid,id,body) in zip(missing,candidates):
        mapping[(guid,id)]=targetid
        restored.append((targetid,guid,id,body))
# Resolve stripped aliases after mapping all previously missing components.
for guid,docs in sources.items():
    for id in docs:
        original=alias((guid,id))
        if original in mapping:mapping[(guid,id)]=mapping[original]

def remap(body,guid):
    def replace(m):
        id=int(m[1])
        return '{fileID: '+str(mapping[(guid,id)])+'}' if id else m[0]
    return re.sub(r'\{fileID: (-?\d+)\}',replace,body)

for targetid,guid,id,body in restored:
    body=remap(body,guid)
    body=re.sub(r'^--- !u!114 &\d+',f'--- !u!114 &{targetid}',body)
    # Apply source BoardController overrides such as FuelDeposit.trainController.
    for kind,instance in sources[main].values():
        if kind!=1001:continue
        for m in re.finditer(r'- target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\n      propertyPath: ([^\n]+)\n      value:([^\n]*)\n      objectReference: (\{[^\n]+\})',instance):
            if (m[2],int(m[1]))!=(guid,id):continue
            field=m[3];value=m[4].strip();ref=m[5]
            if field in ('m_Name','m_Enabled'):pass
            if '.' in field:
                # Converted below, after all source records are restored.
                assert field.startswith('_items.Array.data[') and ('  - {fileID:' in body),(field,id)
                continue
            replacement=remap(ref,main) if ref!='{fileID: 0}' or not value else value
            body,n=re.subn(r'^  '+re.escape(field)+r':.*$',f'  {field}: {replacement}',body,flags=re.M)
            if n==0:body += f'  {field}: {replacement}\n'
    target[targetid]=(114,body)
    print('Restored',targetid,guid,id)
# The base BoatShop prefab still stores scalar items, while current BoatShop.cs
# uses SpecialShopItem { item, adReward }. Preserve BoardController overrides in
# the current schema instead of discarding its purchased/ad rewarded assortment.
for targetid,(kind,body) in list(target.items()):
    if kind!=114 or 'guid: db727924e0d3236439b3ab5498cf8e5b' not in body:continue
    block=re.search(r'^  _items:\n((?:  - \{[^\n]+\}\n)+)',body,re.M)
    if not block:continue
    items=[{'item':s,'adReward':'0'} for s in re.findall(r'^  - (\{[^\n]+\})',block[1],re.M)]
    for _,instance in sources[main].values():
        for m in re.finditer(r'- target: \{fileID: 7791402658513924802, guid: 97d10e45590485c40abdfaed719a8e3f, type: 3\}\n      propertyPath: _items.Array.data\[(\d+)\].(item|adReward)\n      value:([^\n]*)\n      objectReference: (\{[^\n]+\})',instance):
            items[int(m[1])][m[2]]=m[4] if m[2]=='item' else m[3].strip()
    replacement='  _items:\n'+''.join(f"  - item: {item['item']}\n    adReward: {item['adReward']}\n" for item in items)
    body=body[:block.start()]+replacement+body[block.end():]
    target[targetid]=(kind,body)
    print('BoatShop source overrides restored in current SpecialShopItem schema:',len(items))
text=dest.read_text(encoding='utf-8-sig');header=text[:text.index('--- !u!')]
dest.write_text(header+''.join(body for _,body in target.values()),encoding='utf-8',newline='\n')
assert not any('m_Script: {fileID: 0}' in b for _,b in target.values())
for id,(_,body) in target.items():
    for ref in re.findall(r'\{fileID: (-?\d+)\}',body):assert int(ref)==0 or int(ref) in target,(id,ref)
print('PASS: script GUIDs restored and every internal reference resolves.')





