"""Combine the three north outer leaves into one broad, single-tip leaf.

Run once in the current VideoFit Blender file. The pre-edit file is preserved.
"""
import bpy, bmesh, json, math, hashlib
from pathlib import Path

OUT=Path(r'F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall\output\flower-petal-prototype\video-proportion-review')
scene=bpy.context.scene
source_names=['OuterPetal_01','OuterPetal_02','OuterPetal_16']
assert all(n in bpy.data.objects for n in source_names), 'The top merge is already applied or the expected sculpt is not open.'
assert bpy.context.mode=='OBJECT'
backup=OUT/'before-top-merge.blend'
assert not backup.exists(), 'A merge backup already exists; inspect it before repeating the edit.'
bpy.ops.wm.save_as_mainfile(filepath=str(backup),copy=True)
primary=bpy.data.objects['OuterPetal_01']
sources=[bpy.data.objects[n] for n in source_names]
def fingerprint(obj):
    payload={'vertices':[list(v.co) for v in obj.data.vertices],
             'faces':[list(p.vertices) for p in obj.data.polygons],
             'world':[list(row) for row in obj.matrix_world]}
    return hashlib.sha256(json.dumps(payload,sort_keys=True).encode()).hexdigest()
protected={o.name:fingerprint(o) for o in scene.objects if o.type=='MESH' and o not in sources}
inverse=primary.matrix_world.inverted()
points=[inverse@(obj.matrix_world@v.co) for obj in sources for v in obj.data.vertices]
left=min(p.x for p in points);right=max(p.x for p in points)
old_width=max(v.co.x for v in primary.data.vertices)-min(v.co.x for v in primary.data.vertices)
new_width=right-left
scale=new_width/old_width
primary.data=primary.data.copy()
primary.data.name='OuterPetal_TopMerged_Surface'
slope=math.tan(math.radians(primary.get('shingle_degrees',.45)))
for vertex in primary.data.vertices:
    old_x=vertex.co.x
    vertex.co.x*=scale
    # Preserve shallow camber while retaining the existing shingle slope.
    vertex.co.z+=(vertex.co.x-old_x)*slope
primary.data.update()
primary['width_m']=new_width
primary['merged_sources']=json.dumps(source_names)
primary['description']='Single broad north outer leaf replacing the previous three top leaves.'
primary['source_width_scale']=scale
primary['position_label']='N_Merged'
for obj in sources[1:]:
    rig=obj.parent
    assert len(rig.children)==1
    bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.objects.remove(rig,do_unlink=True)
outer=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('OuterPetal_')]
assert len(outer)==14
ctrl=bpy.data.objects['OuterLayer_CTRL'];ctrl['petal_count']=14
ctrl['spacing_note']='Top three leaves merged into one broad north leaf; other fitted positions retained.'
scene['outer_petal_count']=14
scene['sculpt_status']='VideoFit review: top outer cluster merged into one larger leaf at user request.'
bpy.data.objects['FlowerRoot']['prototype_scope']='8 fitted inner petals + 14 outer petals after merging the north cluster.'
col=bpy.data.collections.get('04_OuterPetals_16_Rigged')
if col:col.name='04_OuterPetals_14_Rigged'
unchanged={n:fingerprint(bpy.data.objects[n])==signature for n,signature in protected.items()}
assert all(unchanged.values())
mesh=primary.data;bm=bmesh.new();bm.from_mesh(mesh)
health={'vertices':len(mesh.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.polygons),
        'loose_vertices':sum(not v.link_faces for v in bm.verts),
        'zero_area_faces':sum(f.calc_area()<1e-12 for f in bm.faces),
        'nonmanifold_interior_edges':sum(not e.is_boundary and not e.is_manifold for e in bm.edges),
        'inverted_faces':sum(p.normal.z<=0 for p in mesh.polygons),
        'max_influences':max(len(v.groups) for v in mesh.vertices),
        'max_weight_error':max(abs(sum(g.weight for g in v.groups)-1) for v in mesh.vertices)}
bm.free()
assert not any(health[k] for k in ['loose_vertices','zero_area_faces','nonmanifold_interior_edges','inverted_faces'])
assert health['max_influences']<=2 and health['max_weight_error']<1e-6
master=bpy.data.objects['Petal_01_Rig'];saved=[tuple(b.rotation_euler) for b in master.pose.bones]
drift=[]
try:
    for lifts in [(0,0,0),(2,6,12)]:
        for b,angle in zip(master.pose.bones,lifts):b.rotation_euler=(math.radians(angle),0,0)
        master.update_tag();scene.frame_set(scene.frame_current);bpy.context.view_layer.update()
        ev=primary.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
        drift.append((ev.matrix_world@me.vertices[0].co-primary.parent.matrix_world.translation).length)
        ev.to_mesh_clear()
        assert all(fc.driver.is_valid for fc in primary.parent.animation_data.drivers)
finally:
    for b,rotation in zip(master.pose.bones,saved):b.rotation_euler=rotation
    master.update_tag();scene.frame_set(scene.frame_current);bpy.context.view_layer.update()
assert max(drift)<1e-7
for key in ['15','5','9']:
    scene.camera=bpy.data.objects[f'REVIEW_Video_{key}']
    scene.render.filepath=str(OUT/f'top-merged-view-{key}.png')
    bpy.ops.render.render(write_still=True)
report={'combined_sources':source_names,'result_object':primary.name,'previous_outer_count':16,'outer_count':14,
        'previous_center_leaf_width_m':old_width,'merged_width_m':new_width,'length_m':primary['length_m'],
        'width_multiplier':scale,'mesh_health':health,'max_root_drift_m':max(drift),
        'other_meshes_and_transforms_unchanged':unchanged,'backup':str(backup),'unity_assets_changed':False}
(OUT/'top-merge-validation.json').write_text(json.dumps(report,indent=2))
scene.camera=bpy.data.objects['REVIEW_Video_15']
scene.render.filepath=str(OUT/'top-merged-view-15.png')
for obj in scene.objects:obj.select_set(False)
primary.select_set(True);bpy.context.view_layer.objects.active=primary
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        space=area.spaces.active
        if 'CAMERA' in [i.identifier for i in space.region_3d.bl_rna.properties['view_perspective'].enum_items]:space.region_3d.view_perspective='CAMERA'
        space.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT.parent/'Deepavali_InnerPetal_VideoFit.blend'))
print(json.dumps({k:report[k] for k in ['outer_count','merged_width_m','width_multiplier','length_m','max_root_drift_m']},indent=2))
