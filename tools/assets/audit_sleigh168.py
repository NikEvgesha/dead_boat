import bpy,bmesh,json
bpy.ops.wm.open_mainfile(filepath=r'E:/Fork/dead_boat/ArtSource/NewYearBoss/SleighThief.blend')
rows=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 bm=bmesh.new();bm.from_mesh(o.data);bm.faces.ensure_lookup_table();bm.normal_update()
 before=[f.normal.copy() for f in bm.faces];volume=bm.calc_volume(signed=True);boundary=sum(e.is_boundary for e in bm.edges);nonman=sum(not e.is_manifold for e in bm.edges);deg=sum(f.calc_area()<1e-9 for f in bm.faces)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update();flips=sum(a.dot(f.normal)<-.9 for a,f in zip(before,bm.faces))
 row=dict(name=o.name,faces=len(bm.faces),signed_volume=round(volume,8),boundary=boundary,nonmanifold=nonman,degenerate=deg,recalc_flips=flips,negative_scale=o.matrix_world.determinant()<0)
 rows.append(row);bm.free()
print(json.dumps([r for r in rows if r['recalc_flips'] or r['nonmanifold'] or r['degenerate'] or r['signed_volume']<0],indent=2))
open(r'E:/Fork/dead_boat/ArtSource/NewYearBoss/normals168-before.json','w').write(json.dumps(rows,indent=2))