"""Run with a background Blender; only writes staged output beside this file."""
import bpy, math, json, hashlib
from pathlib import Path

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
BASE=json.loads((HERE/'before/output/refined-flower-integration/source-manifest.json').read_text())
names=BASE['inner']+BASE['outer']

def half_width(t,h0,hm):
    join=.30;shoulder=30/56
    span=shoulder-join
    slope=(hm-h0)/(join+span/2)
    if t<=join:return h0+slope*t
    if t<=shoulder:
        q=(t-join)/span
        return h0+slope*join+slope*span*(q-q**3+.5*q**4)
    q=(t-shoulder)/(1-shoulder)
    return hm*(1-(10*q**3-15*q**4+6*q**5)+.35*q**3*(1-q))

changes=[]
for item in BASE['meshes'][:8]:
    obj=bpy.data.objects[item['name']]; me=obj.data
    L=item['max'][1]; W=item['max'][0]-item['min'][0]
    rows={}
    for i,v in enumerate(item['vertices']):rows.setdefault(round(v[1]/L*56),[]).append(i)
    h0=(max(item['vertices'][i][0] for i in rows[0])-min(item['vertices'][i][0] for i in rows[0]))/2
    for row,ids in rows.items():
        t=row/56;lo=min(item['vertices'][i][0] for i in ids);hi=max(item['vertices'][i][0] for i in ids)
        h=half_width(t,h0,W/2)
        for i in ids:
            old=item['vertices'][i];u=(old[0]-lo)/(hi-lo) if hi-lo>1e-10 else .5
            x=(2*u-1)*h
            relief=old[2]-math.tan(math.radians(.45))*old[0]
            me.vertices[i].co=(x,old[1],math.tan(math.radians(.45))*x+relief)
    for lp in me.loops:
        v=me.vertices[lp.vertex_index].co
        me.uv_layers['ArtworkUV'].data[lp.index].uv=(.5+v.x/W,v.y/L)
    me.update()
    obj['profile_revision']='2026-10-29: bilaterally symmetric smooth silhouette; broad planar base; original dimensions, placement, shingle and rig'
    obj['width_m']=W
    changes.append(dict(name=obj.name,width_m=W,length_m=L,base_width_m=2*h0,vertices=len(me.vertices)))

bpy.context.view_layer.update()
for item in BASE['meshes'][8:]:
    me=bpy.data.objects[item['name']].data
    assert [[float(x) for x in v.co] for v in me.vertices]==item['vertices'],item['name']

stage=HERE/'staged';stage.mkdir(exist_ok=True)
dest=stage/'Deepavali_InnerPetal_Regularized.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(dest))
meshes=[bpy.data.objects[n] for n in names]+[bpy.data.objects['CenterDisc']]
objects=set(meshes)
for obj in meshes:
    p=obj.parent
    while p:objects.add(p);p=p.parent
manifest=dict(source=str(dest),sha256=hashlib.sha256(dest.read_bytes()).hexdigest(),inner=BASE['inner'],outer=BASE['outer'],meshes=[])
for o in meshes:
    me=o.data;me.calc_loop_triangles();uv=me.uv_layers['ArtworkUV'];uvv=[[0,0] for _ in me.vertices]
    for lp in me.loops:uvv[lp.vertex_index]=list(uv.data[lp.index].uv)
    manifest['meshes'].append(dict(name=o.name,vertices=[list(v.co) for v in me.vertices],uv=uvv,triangles=[list(t.vertices) for t in me.loop_triangles],matrix=[list(r) for r in o.matrix_world],min=[min(v.co[k] for v in me.vertices) for k in range(3)],max=[max(v.co[k] for v in me.vertices) for k in range(3)],bones=[b.name for b in o.parent.data.bones] if o.parent and o.parent.type=='ARMATURE' else []))
(stage/'source-manifest.json').write_text(json.dumps(manifest))
bpy.ops.object.select_all(action='DESELECT')
for o in objects:
    o.hide_set(False);o.hide_viewport=False;o.select_set(True)
    if o.type=='ARMATURE':o.data.pose_position='REST'
bpy.context.view_layer.objects.active=bpy.data.objects['Petal_01_Rig']
bpy.ops.export_scene.fbx(filepath=str(stage/'Deepavali_RefinedFlower.fbx'),use_selection=True,object_types={'EMPTY','ARMATURE','MESH'},global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=False,use_mesh_modifiers=True,use_triangles=True,mesh_smooth_type='FACE',primary_bone_axis='Y',secondary_bone_axis='X',use_armature_deform_only=True,add_leaf_bones=False,bake_anim=False,use_custom_props=False)
(HERE/'shape-build.json').write_text(json.dumps(dict(inner=changes,outer_and_center_vertices_unchanged=True),indent=2))
print('Staged eight regularized inner petals; outer/center unchanged.')
