import bpy, math, os, json
from mathutils import Vector
ROOT=r'E:/Fork/dead_boat'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def linear(c): return c/12.92 if c<.04045 else ((c+.055)/1.055)**2.4
colors={'Skin_Green':'86A447','Skin_Dark':'4C6630','Holiday_Red':'B8323D','Warm_Cream':'F4E6CA','Runner_Gold':'DFA92E','Ice_Blue':'86DDF4','Charcoal':'343441','Wood_Brown':'76503A'}
mats={}
for n,h in colors.items():
 c=[linear(int(h[i:i+2],16)/255) for i in (0,2,4)];m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True;next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=(*c,1);next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Roughness'].default_value=.8;mats[n]=m
root=bpy.data.objects.new('SleighThief_Visual',None);bpy.context.collection.objects.link(root)
def finish(o,n,mat,parent=root):
 o.name=n;o.data.materials.append(mats[mat]);o.parent=parent;return o
def box(n,p,s,mat,bevel=0,parent=root):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('One_Facet_Edges','BEVEL');mod.width=bevel;mod.segments=1;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,n,mat,parent)
def ico(n,p,s,mat,sub=1):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=p);o=bpy.context.object;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,n,mat)
def mesh(n,verts,faces,mat):
 me=bpy.data.meshes.new(n);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(n,me);bpy.context.collection.objects.link(o);return finish(o,n,mat)
def prism(n,outline,x,width,mat):
 vs=[(xx,y,z) for xx in (x-width/2,x+width/2) for y,z in outline];k=len(outline);faces=[tuple(range(k-1,-1,-1)),tuple(range(k,2*k))]+[(i,(i+1)%k,(i+1)%k+k,i+k) for i in range(k)];return mesh(n,vs,faces,mat)
def limb(n,a,b,w,d,mat):
 mid=(Vector(a)+Vector(b))/2;o=box(n,mid,(w,d,(Vector(b)-Vector(a)).length),mat,.025);o.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler();return o
# Open hull and continuous runners, Blender front -Y
box('Hull_Floor',(0,0,.12),(2.05,4,.24),'Holiday_Red',.08)
outline=[(-2.1,.18),(2.1,.18),(2.1,1.3),(1.45,1.3),(.9,.95),(-1.45,.95),(-2.1,1.15)]
for side in [-1,1]:
 prism('Hull_Side_'+str(side),outline,side*1.02,.18,'Holiday_Red')
 box('Cream_Side_Inlay_'+str(side),(side*1.119,-.05,.67),(.025,2.6,.12),'Warm_Cream')
box('Hull_Front',(0,-2.02,.64),(2.1,.2,1.0),'Holiday_Red',.04)
box('Hull_Rear',(0,2.02,.72),(2.1,.18,1.16),'Holiday_Red',.04)
for x in [-.45,0,.45]:box('Front_Cream_Badge',(x,-2.13,.72),(.18,.025,.36),'Warm_Cream')
for y in [-1.25,1.25]:box('Runner_Crossbar',(0,y,-.12),(2.68,.2,.2),'Wood_Brown')
path=[(-1.9,.59),(-2.2,.75),(-2.48,.59),(-2.5,.26),(-2.24,-.13),(-1.9,-.37),(1.9,-.37),(2.25,-.12)]
for side in [-1,1]:
 poly=[]
 for sign in [1,-1]:
  pts=[]
  for i,(y,z) in enumerate(path):
   before=Vector(path[max(0,i-1)]);after=Vector(path[min(len(path)-1,i+1)]);delta=(after-before).normalized();normal=Vector((-delta.y,delta.x));pts.append((y+sign*.105*normal.x,z+sign*.105*normal.y))
  poly+=pts if sign==1 else pts[::-1]
 prism('Runner_L' if side<0 else 'Runner_R',poly,side*1.29,.22,'Runner_Gold')
 for y in [-1.25,1.25]:box('Runner_Mount',(side*1.29,y,-.22),(.22,.25,.3),'Runner_Gold')
box('Engine_Collar',(0,.2,-.24),(.95,1.05,.2),'Charcoal',.06)
for x,y,r in [(0,0,.46),(-.3,.35,.26),(.3,.35,.26)]:
 bpy.ops.mesh.primitive_cone_add(vertices=5,radius1=0,radius2=r,depth=.8,location=(x,y,-.68));finish(bpy.context.object,'Engine_Ice','Ice_Blue')
# Presents and sack at rear
ico('Sack',(0,1.13,1.05),(.85,.68,.92),'Warm_Cream',2)
for idx,(p,s,col) in enumerate([((-.35,1.15,1.95),(.65,.6,.55),'Holiday_Red'),((.35,1.08,1.76),(.6,.55,.5),'Ice_Blue'),((.1,1.48,2.18),(.5,.48,.4),'Holiday_Red')]):
 box('Gift_'+str(idx),p,s,col,.02);box('Gift_Ribbon_'+str(idx),(p[0],p[1],p[2]+s[2]/2+.012),(s[0]+.02,.12,.035),'Runner_Gold');box('Gift_RibbonCross_'+str(idx),(p[0],p[1],p[2]+s[2]/2+.025),(.12,s[1]+.02,.035),'Runner_Gold')
 for sign in [-1,1]:ico('Ribbon_Bow',(p[0]+sign*.12,p[1],p[2]+s[2]/2+.12),(.15,.08,.10),'Runner_Gold')
# Driver separated articulated rigid meshes, neutral driving pose
for side in [-1,1]:
 box('Boot_'+str(side),(side*.23,-.8,.36),(.35,.62,.34),'Wood_Brown',.04)
 limb('Leg_'+str(side),(side*.23,-.6,.48),(side*.23,-.58,1.1),.25,.28,'Charcoal')
box('Coat_Hips',(0,-.6,1.1),(.85,.62,.48),'Holiday_Red',.035)
box('Coat_Torso',(0,-.6,1.52),(.86,.65,.6),'Holiday_Red',.06)
box('Belt',(0,-.6,1.26),(.9,.69,.14),'Charcoal')
box('Buckle',(0,-.97,1.26),(.27,.055,.22),'Runner_Gold',.01);box('Buckle_Inside',(0,-1.005,1.26),(.13,.02,.1),'Charcoal')
# collar, head, ears, muzzle
for side in [-1,1]:
 o=box('Collar_'+str(side),(side*.2,-.95,1.8),(.32,.08,.26),'Warm_Cream');o.rotation_euler[1]=side*math.radians(25)
box('Neck',(0,-.6,1.89),(.30,.32,.24),'Skin_Green')
box('Head',(0,-.67,2.18),(.95,.73,.65),'Skin_Green',.13)
for side in [-1,1]:
 mesh('Ear_'+str(side),[(side*.38,-.62,2.35),(side*1.0,-.47,2.57),(side*.52,-.5,2.08),(side*.45,-.35,2.32)],[(0,1,2),(0,3,1),(1,3,2),(2,3,0)],'Skin_Green')
 mesh('Inner_Ear_'+str(side),[(side*.49,-.635,2.35),(side*.84,-.53,2.48),(side*.56,-.55,2.2)],[(0,1,2)],'Skin_Dark')
 box('Eye_'+str(side),(side*.23,-1.043,2.25),(.21,.025,.15),'Warm_Cream')
 box('Pupil_'+str(side),(side*.205,-1.061,2.24),(.075,.018,.10),'Charcoal')
 brow=box('Brow_'+str(side),(side*.23,-1.075,2.37),(.36,.07,.12),'Charcoal');brow.rotation_euler[1]=-side*math.radians(18)
mesh('Pointed_Nose',[(-.13,-1.02,2.21),(.13,-1.02,2.21),(.14,-1.02,2.07),(-.14,-1.02,2.07),(.02,-1.44,2.12)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(3,2,1,0)],'Skin_Green')
box('Smirk',(0,-1.05,2.015),(.45,.025,.035),'Skin_Dark')
for side in [-1,1]:
 bpy.ops.mesh.primitive_cone_add(vertices=4,radius1=.065,radius2=0,depth=.13,location=(side*.25,-1.07,2.055));finish(bpy.context.object,'Fang','Warm_Cream')
# hat rings as tapered segments
box('Hat_Trim',(0,-.65,2.53),(1.02,.8,.18),'Warm_Cream',.05)
verts=[(-.45,-1,2.61),(.45,-1,2.61),(.45,-.3,2.61),(-.45,-.3,2.61),(-.24,-.85,2.98),(.16,-.85,2.98),(.16,-.48,2.98),(-.24,-.48,2.98),(-.58,-.65,3.07)]
mesh('Hat',verts,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,8),(5,6,8),(6,7,8),(7,4,8)],'Holiday_Red');ico('Hat_Pom',(-.63,-.65,3.02),(.17,.16,.16),'Warm_Cream')
# lever and two arms driving
limb('Lever',( .6,-1.27,.38),(.6,-1.38,1.45),.10,.10,'Charcoal');box('Lever_Handle',(.6,-1.38,1.45),(.23,.22,.18),'Holiday_Red',.025)
for side,a,b,c in [(-1,(-.47,-.6,1.73),(-.65,-.92,1.43),(-.75,-1.33,1.07)),(1,(.47,-.6,1.73),(.72,-.92,1.6),(.6,-1.38,1.47))]:
 limb('UpperArm_'+str(side),a,b,.30,.30,'Holiday_Red');limb('ForeArm_'+str(side),b,c,.26,.26,'Holiday_Red');ico('Cuff_'+str(side),Vector(c).lerp(Vector(b),.12),(.19,.18,.17),'Warm_Cream');box('Hand_'+str(side),c,(.32,.30,.23),'Skin_Green',.025)
# apply all geometry transforms for reliable FBX
for o in bpy.context.scene.objects:
 if o.type=='MESH':
  bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.select_set(False)
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
# Consistent outward normals on closed solids; ear inlays get real thickness.
import bmesh
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 if o.name.startswith('Inner_Ear'):
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
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/NewYearBoss/SleighThief.blend')
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:o.select_set(o.type=='MESH' or o==root)
from mathutils import Matrix
R=Matrix.Rotation(math.pi,4,'Z')
for o in bpy.context.scene.objects:
 if o.type=='MESH':o.data.transform(R);o.location=R@o.location
bpy.ops.export_scene.fbx(filepath=ROOT+'/Assets/Events/NewYear/Models/SleighThief.fbx',use_selection=True,object_types={'MESH','EMPTY'},axis_forward='Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,use_mesh_modifiers=True,add_leaf_bones=False)
for o in bpy.context.scene.objects:
 if o.type=='MESH':o.data.transform(R);o.location=R@o.location
# studio render (not exported)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.world.color=(.18,.18,.18)
bpy.ops.object.camera_add(location=(6,-8,5));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=7.5;scene.camera=cam
for pos,power,size in [((2,-5,7),850,5),((-5,-2,3),600,4),((0,5,5),1000,4)]:
 bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
scene.render.image_settings.file_format='PNG';scene.render.filepath=ROOT+'/Docs/locations/previews/sleigh-thief-blender167.png';bpy.ops.render.render(write_still=True)
print('MODEL167 meshes',len([o for o in scene.objects if o.type=='MESH']))