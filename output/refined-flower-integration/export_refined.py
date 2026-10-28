import bpy, json, pathlib, hashlib
root = pathlib.Path(r'F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall')
out = root / 'output/refined-flower-integration'
dest = root / 'Assets/HK_Signature/Deepavali/PetalPrototype/Models/Refined Flower'
dest.mkdir(parents=True, exist_ok=True)
assert pathlib.Path(bpy.data.filepath).name == 'Deepavali_InnerPetal_VideoFit.blend'
names = ['InnerPetal_01'] + [f'REF_TopPetal_{i:02}' for i in range(2,9)] + ['OuterPetal_01'] + [f'OuterPetal_{i:02}' for i in range(3,16)]
meshes = [bpy.data.objects[n] for n in names] + [bpy.data.objects['CenterDisc']]
objects = set(meshes)
for obj in meshes:
    p = obj.parent
    while p:
        objects.add(p)
        p = p.parent
assert sum(o.type=='ARMATURE' for o in objects)==22
manifest = dict(source=str(pathlib.Path(bpy.data.filepath)), sha256=hashlib.sha256(pathlib.Path(bpy.data.filepath).read_bytes()).hexdigest(), inner=names[:8], outer=names[8:], meshes=[])
for o in meshes:
    me=o.data
    me.calc_loop_triangles()
    uv=me.uv_layers['ArtworkUV']
    uvv=[[0,0] for _ in me.vertices]
    for lp in me.loops: uvv[lp.vertex_index]=list(uv.data[lp.index].uv)
    manifest['meshes'].append(dict(name=o.name,vertices=[list(v.co) for v in me.vertices],uv=uvv,triangles=[list(t.vertices) for t in me.loop_triangles],matrix=[list(r) for r in o.matrix_world],min=[min(v.co[k] for v in me.vertices) for k in range(3)],max=[max(v.co[k] for v in me.vertices) for k in range(3)],bones=[b.name for b in o.parent.data.bones] if o.parent and o.parent.type=='ARMATURE' else []))
(out/'source-manifest.json').write_text(json.dumps(manifest),encoding='utf8')
selection=list(bpy.context.selected_objects); active=bpy.context.view_layer.objects.active
states={o:(o.hide_get(),o.hide_viewport,o.data.pose_position if o.type=='ARMATURE' else None) for o in objects}
try:
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.hide_set(False); o.hide_viewport=False; o.select_set(True)
        if o.type=='ARMATURE': o.data.pose_position='REST'
    bpy.context.view_layer.objects.active=bpy.data.objects['Petal_01_Rig']
    bpy.ops.export_scene.fbx(filepath=str(dest/'Deepavali_RefinedFlower.fbx'),use_selection=True,object_types={'EMPTY','ARMATURE','MESH'},global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=False,use_mesh_modifiers=True,use_triangles=True,mesh_smooth_type='FACE',primary_bone_axis='Y',secondary_bone_axis='X',use_armature_deform_only=True,add_leaf_bones=False,bake_anim=False,use_custom_props=False)
finally:
    bpy.ops.object.select_all(action='DESELECT')
    for o,(hidden,viewport,pose) in states.items():
        o.hide_set(hidden); o.hide_viewport=viewport
        if pose: o.data.pose_position=pose
    for o in selection: o.select_set(True)
    bpy.context.view_layer.objects.active=active
print(json.dumps(dict(file=str(dest/'Deepavali_RefinedFlower.fbx'),inner=8,outer=14,bones=66,triangles=sum(len(m['triangles']) for m in manifest['meshes']),source_unchanged=hashlib.sha256(pathlib.Path(bpy.data.filepath).read_bytes()).hexdigest()==manifest['sha256'])))
