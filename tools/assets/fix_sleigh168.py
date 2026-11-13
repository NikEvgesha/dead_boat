import bpy,math,json
from mathutils import Matrix
bpy.ops.wm.open_mainfile(filepath=r'E:/Fork/dead_boat/ArtSource/NewYearBoss/SleighThief.blend')
# Consistent outward normals on closed solids; ear inlays get real thickness.
import bmesh
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 if o.name.startswith('Inner_Ear') and len(o.data.polygons)==1:
  bpy.context.view_layer.objects.active=o
  mod=o.modifiers.new('Ear_Inlay_Thickness','SOLIDIFY');mod.thickness=.012;mod.offset=0
  bpy.ops.object.modifier_apply(modifier=mod.name)
 bm=bmesh.new();bm.from_mesh(o.data)
 edges=[e for e in bm.edges if e.is_boundary]
 if edges:bmesh.ops.holes_fill(bm,edges=edges,sides=0)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
 if bm.calc_volume(signed=True)<0:
  bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.normal_update()
 if any(not e.is_manifold for e in bm.edges):raise RuntimeError('Nonmanifold mesh '+o.name)
 if any(f.calc_area()<1e-9 for f in bm.faces):raise RuntimeError('Degenerate face '+o.name)
 bm.to_mesh(o.data);o.data.update();bm.free()
bpy.ops.wm.save_as_mainfile(filepath=r'E:/Fork/dead_boat/ArtSource/NewYearBoss/SleighThief.blend')
report=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 bm=bmesh.new();bm.from_mesh(o.data);bm.normal_update();normals=[f.normal.copy() for f in bm.faces];bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
 row=dict(name=o.name,faces=len(bm.faces),signed_volume=bm.calc_volume(signed=True),nonmanifold=sum(not e.is_manifold for e in bm.edges),degenerate=sum(f.calc_area()<1e-9 for f in bm.faces),recalc_flips=sum(a.dot(f.normal)<-.9 for a,f in zip(normals,bm.faces)))
 assert row['signed_volume']>0 and row['nonmanifold']==0 and row['degenerate']==0 and row['recalc_flips']==0,row
 report.append(row);bm.free()
open(r'E:/Fork/dead_boat/ArtSource/NewYearBoss/normals168-after.json','w').write(json.dumps(report,indent=2))
R=Matrix.Rotation(math.pi,4,'Z')
for o in bpy.context.scene.objects:
 if o.type=='MESH':
  o.data.transform(R);o.location=R@o.location
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=r'E:/Fork/dead_boat/Assets/Events/NewYear/Models/SleighThief.fbx',use_selection=True,object_types={'MESH','EMPTY'},axis_forward='Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False)
print('PASS: all',len(report),'meshes closed, positive volumes, no degenerate faces, zero further normal flips')