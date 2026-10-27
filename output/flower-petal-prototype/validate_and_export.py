"""Validate the prototype, render previews, export a neutral FBX and save Blender."""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector

OUTPUT = Path(r"F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall\output\flower-petal-prototype")
scene = bpy.data.scenes['Deepavali_PetalPrototype']
bpy.context.window.scene = scene
rig = bpy.data.objects['Petal_01_Rig']
petal = bpy.data.objects['InnerPetal_01']
disc = bpy.data.objects['CenterDisc']
root = bpy.data.objects['FlowerRoot']
names = ['Petal_01_Base', 'Petal_01_Mid', 'Petal_01_Tip']


def set_pose(xs=(0, 0, 0), zs=(0, 0, 0)):
    for name, x, z in zip(names, xs, zs):
        rig.pose.bones[name].rotation_euler = (math.radians(x), 0, math.radians(z))
    bpy.context.view_layer.update()


def mesh_health(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    result = {
        'vertices': len(bm.verts),
        'boundary_edges': sum(edge.is_boundary for edge in bm.edges),
        'nonmanifold_interior_edges': sum(len(edge.link_faces) > 2 for edge in bm.edges),
        'loose_vertices': sum(not vertex.link_faces for vertex in bm.verts),
        'zero_area_faces': sum(face.calc_area() < 1e-12 for face in bm.faces),
    }
    bm.free()
    obj.data.calc_loop_triangles()
    result['triangles'] = len(obj.data.loop_triangles)
    return result


report = {'petal': mesh_health(petal), 'disc': mesh_health(disc), 'pose_checks': []}
report['petal']['max_bone_influences'] = max(len(v.groups) for v in petal.data.vertices)
report['petal']['unweighted_vertices'] = sum(not v.groups for v in petal.data.vertices)
report['petal']['maximum_weight_sum_error'] = max(abs(sum(g.weight for g in v.groups)-1) for v in petal.data.vertices)
assert report['petal']['max_bone_influences'] <= 2
assert report['petal']['unweighted_vertices'] == 0
assert report['petal']['maximum_weight_sum_error'] < 1e-6
assert len(rig.data.bones) == 3
for entry in (report['petal'], report['disc']):
    assert entry['zero_area_faces'] == entry['loose_vertices'] == entry['nonmanifold_interior_edges'] == 0
assert report['disc']['boundary_edges'] == 0

try:
    for label, xs, zs in [
        ('lift_and_curl', (8,14,10), (0,0,0)),
        ('downward_bend', (-5,-8,-5), (0,0,0)),
        ('side_sway', (0,0,0), (6,4,2)),
        ('combined', (6,12,8), (-5,-3,-2)),
    ]:
        set_pose(xs, zs)
        evaluated = petal.evaluated_get(bpy.context.evaluated_depsgraph_get())
        expected_root = rig.matrix_world @ Vector((0,0,0))
        actual_root = evaluated.matrix_world @ evaluated.data.vertices[0].co
        drift = (actual_root - expected_root).length
        tip = evaluated.matrix_world @ evaluated.data.vertices[petal['tip_vertex_index']].co
        assert drift < 1e-7, (label, drift)
        assert all(math.isfinite(component) for vertex in evaluated.data.vertices for component in vertex.co)
        report['pose_checks'].append({'pose':label, 'root_drift_m':drift, 'tip_world_m':list(tip)})
    set_pose((8,14,10))
    scene.render.filepath = str(OUTPUT / 'petal-bend-preview.png')
    bpy.ops.render.render(write_still=True)
finally:
    set_pose()

scene.render.filepath = str(OUTPUT / 'petal-rest-preview.png')
bpy.ops.render.render(write_still=True)
for obj in scene.objects:
    obj.select_set(False)
for obj in (root, disc, rig, petal):
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=str(OUTPUT / 'Deepavali_SinglePetal.fbx'),
    use_selection=True,
    object_types={'EMPTY','ARMATURE','MESH'},
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',
    axis_forward='-Z',
    axis_up='Y',
    bake_space_transform=False,
    use_mesh_modifiers=True,
    use_triangles=True,
    mesh_smooth_type='FACE',
    primary_bone_axis='Y',
    secondary_bone_axis='X',
    use_armature_deform_only=True,
    add_leaf_bones=False,
    bake_anim=False,
    use_custom_props=True,
)
report['export'] = {
    'file':'Deepavali_SinglePetal.fbx',
    'asset_triangles':report['petal']['triangles'] + report['disc']['triangles'],
    'bones':names,
    'animation_clips':0,
    'preview_objects_included':False,
    'target_note':'Lightweight asset budget, not a measured Android device performance result.',
}
(OUTPUT / 'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
for obj in scene.objects:
    obj.select_set(False)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
scene.render.filepath = str(OUTPUT / 'petal-rest-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT / 'Deepavali_SinglePetal.blend'))
print(json.dumps(report, indent=2))
