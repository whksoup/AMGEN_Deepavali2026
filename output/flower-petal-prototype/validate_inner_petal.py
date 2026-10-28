"""Validate the Blender sculpt and prepare consistent comparison renders."""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector

OUT = Path(r"F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall\output\flower-petal-prototype")
scene = bpy.context.scene
master = bpy.data.objects['Petal_01_Rig']
top = [bpy.data.objects['InnerPetal_01']] + [bpy.data.objects[f'REF_TopPetal_{i:02d}'] for i in range(2, 9)]
new_mesh = top[0].data
old_mesh = bpy.data.meshes.get('InnerPetal_01_Mesh')
if old_mesh is None:
    with bpy.data.libraries.load(str(OUT/'Deepavali_SinglePetal_before_Petal2.blend'), link=False) as (src, dst):
        dst.meshes = ['InnerPetal_01_Mesh']
    old_mesh = dst.meshes[0]
old_mesh.use_fake_user = True


def set_geometry(mesh, length, radius):
    for obj in top:
        obj.data = mesh
        rig = obj.parent
        direction = Vector((rig.location.x, rig.location.y, 0)).normalized()
        rig.location.x, rig.location.y = direction.x*radius, direction.y*radius
        bpy.context.view_layer.objects.active = rig
        bpy.ops.object.mode_set(mode='EDIT')
        joints = (0, .36, .72, 1)
        for k, b in enumerate(rig.data.edit_bones):
            b.head, b.tail = (0, joints[k]*length, 0), (0, joints[k+1]*length, 0)
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.view_layer.update()


try:
    set_geometry(old_mesh, .263, .082)
    scene.render.filepath = str(OUT/'inner-petal-before-image1-angle.png')
    bpy.ops.render.render(write_still=True)
finally:
    set_geometry(new_mesh, .220, .105)

mesh = top[0].data
bm = bmesh.new()
bm.from_mesh(mesh)
health = {'vertices': len(mesh.vertices), 'triangles': sum(len(p.vertices)-2 for p in mesh.polygons),
          'boundary_edges': sum(e.is_boundary for e in bm.edges),
          'nonmanifold_interior_edges': sum(not e.is_manifold and not e.is_boundary for e in bm.edges),
          'loose_vertices': sum(not v.link_faces for v in bm.verts),
          'zero_area_faces': sum(f.calc_area() < 1e-12 for f in bm.faces),
          'max_influences': max(len(v.groups) for v in mesh.vertices),
          'max_weight_error': max(abs(sum(g.weight for g in v.groups)-1) for v in mesh.vertices),
          'uv_layers': [u.name for u in mesh.uv_layers],
          'bones': [b.name for b in master.data.bones]}
bm.free()
assert health['nonmanifold_interior_edges'] == health['loose_vertices'] == health['zero_area_faces'] == 0
assert health['max_influences'] <= 2 and health['max_weight_error'] < 1e-6
assert list(mesh.vertices[0].co) == [0, 0, 0]

# Compare static asset geometry to the saved live-state backup.
unchanged_names = ['CenterDisc_Mesh'] + [bpy.data.objects[f'OuterPetal_{i:02d}'].data.name for i in range(1, 16)]
current = {n: bpy.data.meshes[n] for n in unchanged_names}
with bpy.data.libraries.load(str(OUT/'Deepavali_SinglePetal_before_Petal2.blend'), link=False) as (src, dst):
    dst.meshes = list(unchanged_names)
unchanged = {}
for name, previous in zip(unchanged_names, dst.meshes):
    now = current[name]
    unchanged[name] = (len(now.vertices) == len(previous.vertices)
                       and all(a.co == b.co for a, b in zip(now.vertices, previous.vertices))
                       and [tuple(p.vertices) for p in now.polygons] == [tuple(p.vertices) for p in previous.polygons])
    bpy.data.meshes.remove(previous)
assert all(unchanged.values())

poses = [('rest', [0, 0, 0], [0, 0, 0]), ('lift_and_curl', [4, 9, 13], [0, 0, 0]),
         ('downward', [-4, -6, -6], [0, 0, 0]), ('sway', [0, 0, 0], [.8, -1.2, 1.5]),
         ('combined', [2, 6, 12], [-.8, 1.2, -1.5])]
pose_results = []
try:
    for name, lifts, sways in poses:
        for k, b in enumerate(master.pose.bones):
            b.rotation_euler = (math.radians(lifts[k]), 0, math.radians(sways[k]))
        master.update_tag()
        scene.frame_set(scene.frame_current)
        bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        max_drift = 0
        for obj in top:
            ev = obj.evaluated_get(dg)
            me = ev.to_mesh()
            drift = (ev.matrix_world @ me.vertices[0].co - obj.parent.matrix_world.translation).length
            max_drift = max(max_drift, drift)
            assert all(math.isfinite(c) for v in me.vertices for c in v.co)
            ev.to_mesh_clear()
        driver_errors = []
        for i in range(1, 16):
            rig = bpy.data.objects[f'OuterPetal_{i:02d}_Rig']
            assert all(fc.driver.is_valid for fc in rig.animation_data.drivers)
            for k, b in enumerate(rig.evaluated_get(dg).pose.bones):
                driver_errors.append(abs(b.rotation_euler.x-math.radians(lifts[k])*(.72 if lifts[k] >= 0 else 1.35)))
                driver_errors.append(abs(b.rotation_euler.z-math.radians(sways[k])*.5))
        assert max_drift < 1e-7 and max(driver_errors) < 1e-6, (name, max_drift, max(driver_errors))
        pose_results.append({'pose': name, 'root_drift_m': max_drift, 'outer_driver_error_radians': max(driver_errors)})
        if name == 'combined':
            scene.render.filepath = str(OUT/'inner-petal-bend-image1-angle.png')
            bpy.ops.render.render(write_still=True)
finally:
    for b in master.pose.bones:
        b.rotation_euler = (0, 0, 0)
    master.update_tag()
    bpy.context.view_layer.update()

scene.render.filepath = str(OUT/'inner-petal-image1-angle.png')
bpy.ops.render.render(write_still=True)
report = {'mesh_health': health, 'unchanged_center_and_outer_geometry': unchanged,
          'poses': pose_results, 'unity_integration': 'Deferred as requested; no FBX or Assets changes.'}
(OUT/'inner-petal-Petal2-validation.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
