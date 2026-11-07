"""Install/check static island ground and merged deck colliders. Default is read-only.
Checks geometry mathematically; does not execute Unity Physics or a character controller.
"""
import argparse
from collections import defaultdict
import hashlib
import json
import math
from pathlib import Path
import re
import struct

ROOT = Path(__file__).resolve().parents[2]
SCENE = ROOT / 'Assets/Scenes/LobbyIslandWorkshop.unity'
BASE = 9200000000200000
CUBE = '{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}'


def field(b, name):
    m = re.search(r'^  ' + re.escape(name) + r': (.*)$', b, re.M)
    return m[1].rstrip('\r') if m else ''


def ref(b, name):
    return int(re.search(r'fileID: (-?\d+)', field(b, name))[1])


def vec(s):
    return tuple(float(x) for x in re.findall(r'[xyzw]: ([^,}]+)', s))


def rotate(q, v):
    x,y,z,w = q; a,b,c = v
    tx,ty,tz = 2*(y*c-z*b),2*(z*a-x*c),2*(x*b-y*a)
    return (a+w*tx+y*tz-z*ty,b+w*ty+z*tx-x*tz,c+w*tz+x*ty-y*tx)


class Scene:
    def __init__(self, text):
        self.text = text
        hs = list(re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n', text, re.M))
        self.blocks = {}; self.go = {}; self.ts = {}; self.gt = {}; self.meshes = {}
        for i,h in enumerate(hs):
            kind,ident = int(h[1]),int(h[2]); end = hs[i+1].start() if i+1<len(hs) else len(text)
            assert ident not in self.blocks
            b = text[h.end():end]; self.blocks[ident] = (kind,b,h.start(),end)
            if kind==1: self.go[ident] = (field(b,'m_Name'),field(b,'m_IsActive')=='1')
            elif kind in (4,224):
                if not field(b,'m_GameObject'):continue
                go=ref(b,'m_GameObject'); self.gt[go]=ident
                self.ts[ident]=(go,ref(b,'m_Father'),vec(field(b,'m_LocalPosition')),vec(field(b,'m_LocalRotation')),vec(field(b,'m_LocalScale')))
            elif kind==33 and field(b,'m_GameObject'): self.meshes[ref(b,'m_GameObject')] = field(b,'m_Mesh')
        self.paths = {}; self.active = {}
        for go in self.gt:
            chain=[]; active=True; t=self.gt[go]; visited=set()
            while t:
                if t not in self.ts:
                    active=False;chain.append('<unresolved>');break
                assert t not in visited; visited.add(t)
                g,parent,*_=self.ts[t]; name,on=self.go[g];chain.append(name);active &= on;t=parent
            self.paths[go]='/'.join(reversed(chain));self.active[go]=active

    def world(self, tid, point):
        while tid:
            go,parent,pos,q,scale=self.ts[tid]
            assert all(x>0 for x in scale),'Negative/zero scale unsupported'
            point=rotate(q,tuple(point[i]*scale[i] for i in range(3)))
            point=tuple(point[i]+pos[i] for i in range(3));tid=parent
        return point


def mesh_data(path):
    t=path.read_text(encoding='utf-8-sig'); assert '  m_IsReadable: 1' in t
    channels=re.findall(r'- stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)',t)
    assert channels and tuple(map(int,channels[0]))==(0,0,0,3)
    assert all(int(s)==0 and int(f)==0 for s,o,f,d in channels if int(d))
    stride=max(int(o)+4*int(d) for s,o,f,d in channels if int(d))
    count=int(re.search(r'm_VertexCount: (\d+)',t)[1]);raw=bytes.fromhex(re.search(r'_typelessdata: ([0-9a-fA-F]+)',t)[1])
    assert len(raw)==count*stride
    vertices=[struct.unpack_from('<fff',raw,i*stride) for i in range(count)]
    assert re.search(r'm_IndexFormat: 0',t)
    raw_indices=bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-fA-F]+)',t)[1])
    ids=struct.unpack('<'+'H'*(len(raw_indices)//2),raw_indices)
    assert len(ids)%3==0 and max(ids)<count
    return vertices,[ids[i:i+3] for i in range(0,len(ids),3)]


def plan(scene):
    ground=[]; groups=defaultdict(list)
    for go,mesh in scene.meshes.items():
        path=scene.paths.get(go,'')
        if not path.startswith('Lobby_Island_Visual_Prototype/') or not scene.active[go]:continue
        leaf=path.rsplit('/',1)[-1]
        if leaf in ('Grass_Terrace','Soft_Sand_Beach'):ground.append((go,mesh))
        if leaf=='Deck_Plank':groups[scene.ts[scene.gt[go]][1]].append(go)
    assert len(ground)==18 and len(groups)==18,(len(ground),len(groups))
    result=[]
    for go,mesh in sorted(ground):result.append(dict(go=go,kind=64,mesh=mesh,path=scene.paths[go]))
    for parent,boards in sorted(groups.items()):
        poses=[scene.ts[scene.gt[g]] for g in boards]
        assert all(scene.meshes[g]==CUBE for g in boards)
        assert all(max(abs(x) for x in q[:3])<1e-6 and abs(abs(q[3])-1)<1e-6 for g,p,pos,q,s in poses)
        assert max(p[2][0] for p in poses)-min(p[2][0] for p in poses)<1e-4
        assert max(p[2][1] for p in poses)-min(p[2][1] for p in poses)<1e-4
        intervals=sorted((p[2][2]-p[4][2]/2,p[2][2]+p[4][2]/2) for p in poses)
        assert max(intervals[i+1][0]-intervals[i][1] for i in range(len(intervals)-1))<0.051
        lo=[min(pos[i]-scale[i]/2 for g,p,pos,q,scale in poses) for i in range(3)]
        hi=[max(pos[i]+scale[i]/2 for g,p,pos,q,scale in poses) for i in range(3)]
        go=scene.ts[parent][0]
        result.append(dict(go=go,kind=65,center=[(lo[i]+hi[i])/2 for i in range(3)],size=[hi[i]-lo[i] for i in range(3)],boards=len(boards),path=scene.paths[go]))
    return result


def xyz(v):return '{'+', '.join(k+': '+format(v[i],'.9g') for i,k in enumerate('xyz'))+'}'


def collider(p):
    kind=p['kind'];ident=BASE+p['go'];name='MeshCollider' if kind==64 else 'BoxCollider'
    s=f'''--- !u!{kind} &{ident}
{name}:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {p['go']}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
'''
    if kind==64:s+='  serializedVersion: 5\n  m_Convex: 0\n  m_CookingOptions: 30\n  m_Mesh: '+p['mesh']+'\n'
    else:s+='  serializedVersion: 3\n  m_Size: '+xyz(p['size'])+'\n  m_Center: '+xyz(p['center'])+'\n'
    return s.replace('\n','\r\n')


def triangles(scene, plans):
    paths={}
    folder=ROOT/'Assets/Prefabs/Workshop/LobbyIsland'
    for meta in list(folder.glob('Grass_Terrace_*.asset.meta'))+list(folder.glob('Soft_Sand_Beach_*.asset.meta')):
        m=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)
        if m:paths[m[1]]=Path(str(meta)[:-5])
    output=[];total=0
    for p in plans:
        tid=scene.gt[p['go']]
        if p['kind']==64:
            vertices,faces=mesh_data(paths[re.search(r'guid: (\w+)',p['mesh'])[1]])
            total+=len(faces)
        else:
            c,s=p['center'],p['size'];vertices=[(c[0]+x*s[0]/2,c[1]+s[1]/2,c[2]+z*s[2]/2) for x,z in [(-1,-1),(-1,1),(1,1),(1,-1)]];faces=[(0,1,2),(0,2,3)]
        v=[scene.world(tid,pt) for pt in vertices]
        for face in faces:output.append(tuple(v[i] for i in face))
    return output,total


def height(tris,x,z):
    hits=[]
    for a,b,c in tris:
        # Upward-facing triangle only, matching the one-sided ground surface.
        den=(b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2])
        ny=(b[2]-a[2])*(c[0]-a[0])-(b[0]-a[0])*(c[2]-a[2])
        if abs(den)<1e-10 or ny<=0:continue
        u=((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/den
        v=((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/den
        if min(u,v,1-u-v)>=-1e-7:hits.append(u*a[1]+v*b[1]+(1-u-v)*c[1])
    return max(hits) if hits else None


def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--apply',action='store_true');args=ap.parse_args()
    raw=SCENE.read_bytes();text=raw.decode('utf-8');scene=Scene(text);plans=plan(scene)
    tris,total=triangles(scene,plans)
    seams=[]
    for p in plans:
        if p['kind']!=65:continue
        tid=scene.gt[p['go']];c,s=p['center'],p['size'];samples=[]
        for sign in (-1,1):
            pt=scene.world(tid,(c[0],c[1]+s[1]/2,c[2]+sign*(s[2]/2+0.1)))
            h=height(tris,pt[0],pt[2])
            footprint=[]
            for offset in (-0.4,0,0.4):
                edge=scene.world(tid,(c[0]+offset,c[1]+s[1]/2,c[2]+sign*(s[2]/2+0.1)))
                eh=height(tris,edge[0],edge[2]);footprint.append(None if eh is None else abs(eh-edge[1]))
            samples.append(dict(point=pt,support=h,step=None if h is None else abs(h-pt[1]),footprint_steps=footprint))
        seams.append(dict(path=p['path'],ends=samples))
    # Every functional main bridge must meet support on both ends. Boarding gangways and the outer harbour
    # pier intentionally end over water; their boat-facing end is not a land seam.
    for seam in seams:
        ends = seam['ends']
        required = [0] if ('Boat_Boarding_Gangway094' in seam['path'] or seam['path'].endswith('/Central_Shared_Pier102')) else [0,1]
        assert all(all(step is not None and step <= 0.3 for step in ends[i]['footprint_steps']) for i in required), ('Unsupported land seam',seam)
        seam['required_land_ends'] = required
    anchor_supports=[]
    for tid,(go,parent,pos,q,scale) in scene.ts.items():
        path=scene.paths.get(go,'');leaf=path.rsplit('/',1)[-1]
        if not path.startswith('Lobby_Island_Visual_Prototype/') or not scene.active[go]:continue
        if re.fullmatch('Central_Arrival_Anchor090_[1-8]',leaf) or leaf in ('Future_Plot_Arrival_Anchor090','Future_Animal_Spawn_Anchor','Future_Dock_Interaction_Anchor','Future_Collect_Button_Anchor','Future_Heal_Purchase_Anchor098','Future_Bandage_Purchase_Anchor098','Future_Bank_Menu_Anchor095','Future_E_Interaction_Anchor097','Future_Armory_Menu_Anchor102'):
            pt=scene.world(tid,(0,0,0));h=height(tris,pt[0],pt[2]);assert h is not None,('No support under anchor',path)
            anchor_supports.append(dict(path=path,point=pt,support=h))
    report=dict(mesh_colliders=sum(p['kind']==64 for p in plans),box_colliders=sum(p['kind']==65 for p in plans),ground_triangles=total,boards_replaced=sum(p.get('boards',0) for p in plans),seams=seams,anchor_supports=anchor_supports,scope='Mathematical geometry only; no native Unity/Physics/CharacterController execution')
    edits=[];append=[]
    for p in plans:
        ident=BASE+p['go'];reference=f'  - component: {{fileID: {ident}}}\r\n'
        if ident in scene.blocks:
            k,b,start,end=scene.blocks[ident];assert text[start:end]==collider(p),'Existing generated collider differs'
            assert scene.blocks[p['go']][1].count(reference)==1,'Missing/duplicate owner reference'
            continue
        k,b,start,end=scene.blocks[p['go']];assert k==1 and b.count('  m_Layer:')==1
        assert not any(k in (54,64,65) and field(b,'m_GameObject')==f'{{fileID: {p["go"]}}}' for k,b,_,__ in scene.blocks.values()),'Target already has physics'
        edits.append((start,end,text[start:end].replace('  m_Layer:',reference+'  m_Layer:',1)));append.append(collider(p))
    updated=text
    for start,end,replacement in sorted(edits,reverse=True):updated=updated[:start]+replacement+updated[end:]
    updated+=''.join(append)
    restored=updated
    for p in plans:
        ident=BASE+p['go'];reference=f'  - component: {{fileID: {ident}}}\r\n'
        if ident not in scene.blocks:restored=restored.replace(collider(p),'',1).replace(reference,'',1)
    assert restored==text,'Original scene records changed'
    if args.apply and updated!=text:SCENE.write_bytes(updated.encode('utf-8'))
    report.update(pending=len(append),applied=args.apply,sha256=hashlib.sha256((updated if args.apply else text).encode('utf-8')).hexdigest())
    out=ROOT/'Logs/lobby-collision-check.json';out.parent.mkdir(exist_ok=True);out.write_text(json.dumps(report,indent=2))
    print(json.dumps({k:v for k,v in report.items() if k not in ('seams','anchor_supports')},indent=2))
    print('Supported anchors:',len(anchor_supports),'land seams:',sum(len(s['required_land_ends']) for s in seams))
    for seam in seams:
        bad=[i for i,e in enumerate(seam['ends']) if e['support'] is None or e['step']>0.3]
        if bad:print('OPEN_WATER_END',seam['path'],bad)


if __name__=='__main__':main()
