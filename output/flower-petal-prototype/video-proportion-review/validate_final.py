"""Structural and basic deformation checks for the final sculpt (not Unity clearance)."""
import bpy,bmesh,json,math
from pathlib import Path
OUT=Path(r'F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall\output\flower-petal-prototype\video-proportion-review')
master=bpy.data.objects['Petal_01_Rig']
inner=[bpy.data.objects['InnerPetal_01']]+[bpy.data.objects[f'REF_TopPetal_{i:02d}'] for i in range(2,9)]
outer=[bpy.data.objects[f'OuterPetal_{i:02d}'] for i in range(1,17)]
health=[]
for obj in inner+outer:
    mesh=obj.data;bm=bmesh.new();bm.from_mesh(mesh)
    row={'name':obj.name,'vertices':len(mesh.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.polygons),
         'loose_vertices':sum(not v.link_faces for v in bm.verts),
         'nonmanifold_interior_edges':sum(not e.is_boundary and not e.is_manifold for e in bm.edges),
         'zero_area_faces':sum(f.calc_area()<1e-12 for f in bm.faces),
         'inverted_faces':sum(p.normal.z<=0 for p in mesh.polygons),
         'max_bone_influences':max(len(v.groups) for v in mesh.vertices),
         'max_weight_error':max(abs(sum(g.weight for g in v.groups)-1) for v in mesh.vertices),
         'uv_channels':[u.name for u in mesh.uv_layers]}
    bm.free()
    assert not any(row[k] for k in ['loose_vertices','nonmanifold_interior_edges','zero_area_faces','inverted_faces']),row
    assert row['max_bone_influences']<=2 and row['max_weight_error']<1e-6,row
    health.append(row)
poses=[]
saved=[tuple(b.rotation_euler) for b in master.pose.bones]
try:
    for name,lifts,sways in [('rest',[0,0,0],[0,0,0]),('lift',[2,6,12],[0,0,0]),('sway',[0,0,0],[.8,-1.2,1.5]),('combined',[2,6,12],[-.8,1.2,-1.5])]:
        for k,b in enumerate(master.pose.bones):b.rotation_euler=(math.radians(lifts[k]),0,math.radians(sways[k]))
        master.update_tag();bpy.context.scene.frame_set(bpy.context.scene.frame_current);bpy.context.view_layer.update()
        dg=bpy.context.evaluated_depsgraph_get();drifts=[];driver_errors=[]
        for obj in inner+outer:
            ev=obj.evaluated_get(dg);mesh=ev.to_mesh()
            drifts.append((ev.matrix_world@mesh.vertices[0].co-obj.parent.matrix_world.translation).length)
            assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
            ev.to_mesh_clear()
            if obj.parent.animation_data:
                assert all(fc.driver.is_valid for fc in obj.parent.animation_data.drivers),obj.name
        for obj in outer:
            for k,b in enumerate(obj.parent.evaluated_get(dg).pose.bones):
                driver_errors += [abs(b.rotation_euler.x-math.radians(lifts[k])*.72),abs(b.rotation_euler.z-math.radians(sways[k])*.5)]
        assert max(drifts)<1e-7,(name,max(drifts))
        assert max(driver_errors)<1e-6,(name,max(driver_errors))
        poses.append({'pose':name,'max_root_drift_m':max(drifts),'max_outer_driver_error_rad':max(driver_errors)})
finally:
    for b,value in zip(master.pose.bones,saved):b.rotation_euler=value
    master.update_tag();bpy.context.scene.frame_set(bpy.context.scene.frame_current);bpy.context.view_layer.update()
report={'inner_count':8,'outer_count':16,'meshes':health,'poses':poses,
        'limits':'Sculpt review. No Unity changes; future runtime layer-clearance, UV artwork, picking, bounds and per-petal motion mapping need integration checks.'}
(OUT/'final-validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'inner_count':8,'outer_count':16,'mesh_checks':'passed','pose_checks':poses,'total_petal_triangles':sum(r['triangles'] for r in health)},indent=2))
