"""Build the single-petal prototype in the live Blender session via MCP.

Run once in a fresh scene namespace. Original scenes and objects are preserved.
Source outlines remain editable in a hidden reference collection.
"""
import bpy
import math
import re
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from mathutils import Vector, Quaternion

PROJECT = Path(r"F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall")
SOURCE = PROJECT / "output" / "flower-curves-package"
OUTPUT = PROJECT / "output" / "flower-petal-prototype"
SCENE_NAME = "Deepavali_PetalPrototype"
LENGTH_SEGMENTS = 28
WIDTH_SEGMENTS = 6
SCALE = 0.001  # SVG nominal millimetres to Blender metres.


def enum_set(owner, field, requested):
    available = [i.identifier for i in owner.bl_rna.properties[field].enum_items]
    if requested not in available:
        raise ValueError(f"{field}: {requested} not in {available}")
    setattr(owner, field, requested)


def svg_path(filename, path_id):
    root = ET.parse(SOURCE / filename).getroot()
    element = next(e for e in root.iter() if e.get("id") == path_id)
    tokens = re.findall(r"[MCZ]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?", element.get("d"))
    index, point, segments = 0, None, []
    while index < len(tokens):
        command = tokens[index]
        index += 1
        if command == "M":
            point = tuple(map(float, tokens[index:index + 2]))
            index += 2
        elif command == "C":
            values = list(map(float, tokens[index:index + 6]))
            control1, control2, end = tuple(values[:2]), tuple(values[2:4]), tuple(values[4:6])
            segments.append((point, control1, control2, end))
            point, index = end, index + 6
        elif command != "Z":
            raise ValueError(f"Unsupported SVG command: {command}")
    return segments, element.get("fill")


def bezier(segment, t):
    s = 1.0 - t
    return tuple(s**3 * segment[0][axis] + 3*s*s*t * segment[1][axis]
                 + 3*s*t*t * segment[2][axis] + t**3 * segment[3][axis]
                 for axis in range(2))


def world_xy(point):
    return ((point[0] - 500.0) * SCALE, (500.0 - point[1]) * SCALE)


def collection(name, parent):
    result = bpy.data.collections.new(name)
    parent.children.link(result)
    return result


def mesh_object(name, vertices, faces, owner):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    owner.objects.link(obj)
    return obj


def material(name, hex_color, roughness):
    rgb = [int(hex_color.lstrip("#")[i:i+2], 16) / 255 for i in (0, 2, 4)]
    linear = [v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055)**2.4 for v in rgb]
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*linear, 1.0)
    principled = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    principled.inputs["Base Color"].default_value = (*linear, 1.0)
    principled.inputs["Roughness"].default_value = roughness
    return mat


def reference_curve(name, segments, owner, height):
    curve = bpy.data.curves.new(name, "CURVE")
    enum_set(curve, "dimensions", "3D")
    curve.resolution_u = 12
    spline = curve.splines.new("BEZIER")  # Enum inspected before this script.
    spline.bezier_points.add(len(segments) - 1)
    for i, point in enumerate(spline.bezier_points):
        prev = segments[(i - 1) % len(segments)]
        current = segments[i]
        enum_set(point, "handle_left_type", "FREE")
        enum_set(point, "handle_right_type", "FREE")
        point.co = (*world_xy(current[0]), height)
        point.handle_left = (*world_xy(prev[2]), height)
        point.handle_right = (*world_xy(current[1]), height)
    spline.use_cyclic_u = True
    obj = bpy.data.objects.new(name, curve)
    owner.objects.link(obj)
    obj.hide_render = True
    return obj


if bpy.data.scenes.get(SCENE_NAME):
    raise RuntimeError("Prototype scene already exists; refusing to duplicate or overwrite it.")
OUTPUT.mkdir(parents=True, exist_ok=True)
petal_segments, petal_color = svg_path("02-inner-petals.svg", "inner-petal-01")
disc_segments, disc_color = svg_path("03-center-disc.svg", "center-disc")
scene = bpy.data.scenes.new(SCENE_NAME)
bpy.context.window.scene = scene
enum_set(scene.unit_settings, "system", "METRIC")
scene.unit_settings.scale_length = 1.0
asset = collection("01_Export_Flower", scene.collection)
references = collection("02_SVG_References_Hidden", scene.collection)
stage = collection("03_Preview_Only", scene.collection)
root = bpy.data.objects.new("FlowerRoot", None)
asset.objects.link(root)
root["source_units"] = "Original SVG millimetres converted to metres; scale the whole FlowerRoot uniformly."
root["prototype_scope"] = "ONE inner petal and center disc; no animations or Unity scene modifications."

reference_curve("REF_inner-petal-01_exact_SVG", petal_segments, references, 0.004)
reference_curve("REF_center-disc_exact_SVG", disc_segments, references, 0.018)
references.hide_viewport = True
references.hide_render = True

root_svg_y, tip_svg_y = petal_segments[0][0][1], petal_segments[2][3][1]
length = (root_svg_y - tip_svg_y) * SCALE
root_y = (500.0 - root_svg_y) * SCALE


def half_width(t):
    target_y = root_svg_y - t * (root_svg_y - tip_svg_y)
    for segment in petal_segments[:3]:
        if segment[3][1] - 1e-8 <= target_y <= segment[0][1] + 1e-8:
            low, high = 0.0, 1.0
            for _ in range(48):
                mid = (low + high) * 0.5
                if bezier(segment, mid)[1] > target_y:
                    low = mid
                else:
                    high = mid
            return abs(bezier(segment, (low + high)*0.5)[0] - 500.0) * SCALE
    raise ValueError("Failed to sample the SVG petal boundary.")


vertices, parameters, faces = [(0.0, 0.0, 0.0)], [(0.5, 0.0)], []
rows = []
for i in range(1, LENGTH_SEGMENTS):
    t = i / LENGTH_SEGMENTS
    width = half_width(t)
    row = []
    for j in range(WIDTH_SEGMENTS + 1):
        u = -1.0 + 2.0*j / WIDTH_SEGMENTS
        # A 3 mm camber gives lighting form, without altering the SVG top silhouette.
        z = 0.003 * math.sin(math.pi*t)**1.4 * (1.0-u*u)
        row.append(len(vertices))
        vertices.append((u*width, t*length, z))
        parameters.append(((u+1.0)*0.5, t))
    rows.append(row)
tip_index = len(vertices)
vertices.append((0.0, length, 0.0))
parameters.append((0.5, 1.0))
for j in range(WIDTH_SEGMENTS):
    faces.append((0, rows[0][j+1], rows[0][j]))
for low, high in zip(rows, rows[1:]):
    for j in range(WIDTH_SEGMENTS):
        faces.append((low[j], low[j+1], high[j+1], high[j]))
for j in range(WIDTH_SEGMENTS):
    faces.append((rows[-1][j], rows[-1][j+1], tip_index))
petal = mesh_object("InnerPetal_01", vertices, faces, asset)
petal.data.materials.append(material("Petal_Berry_SVG", petal_color, 0.54))
for poly in petal.data.polygons:
    poly.use_smooth = True
uv = petal.data.uv_layers.new(name="ArtworkUV")
coords = petal.data.uv_layers.new(name="PetalCoord_Width_Length")
for loop in petal.data.loops:
    x, y, _ = vertices[loop.vertex_index]
    uv.data[loop.index].uv = (0.5 + x / 0.182, y/length)
    coords.data[loop.index].uv = parameters[loop.vertex_index]
petal["source_file"] = "02-inner-petals.svg / inner-petal-01"
petal["root_vertex_index"] = 0
petal["tip_vertex_index"] = tip_index
petal["length_m"] = length
petal["rest_camber_m"] = 0.003
petal["surface"] = "Single surface, +Z facing. Use two-sided lit shading if undersides must be visible."

# The center uses the original four-cubic circular outline, with a small modeled bevel.
disc_outline = [world_xy(bezier(s, i/16)) for s in disc_segments for i in range(16)]
# SVG Y flip reverses the contour winding; restore counterclockwise order.
disc_outline.reverse()
count = len(disc_outline)
disc_vertices, disc_faces = [], []
for radius_scale, z in ((0.99, 0.002), (1.0, 0.004), (1.0, 0.015), (0.99, 0.018)):
    disc_vertices.extend([(x*radius_scale, y*radius_scale, z) for x, y in disc_outline])
for ring in range(3):
    for i in range(count):
        j = (i+1) % count
        disc_faces.append((ring*count+i, ring*count+j, (ring+1)*count+j, (ring+1)*count+i))
bottom_center = len(disc_vertices)
disc_vertices.extend([(0, 0, 0.002), (0, 0, 0.018)])
for i in range(count):
    j = (i+1) % count
    disc_faces.append((bottom_center, j, i))
    disc_faces.append((bottom_center+1, 3*count+i, 3*count+j))
disc = mesh_object("CenterDisc", disc_vertices, disc_faces, asset)
disc.parent = root
disc.data.materials.append(material("Center_Purple_SVG", disc_color, 0.57))
for poly in disc.data.polygons[:3*count]:
    poly.use_smooth = True
disc_uv = disc.data.uv_layers.new(name="ArtworkUV")
for loop in disc.data.loops:
    co = disc.data.vertices[loop.vertex_index].co
    disc_uv.data[loop.index].uv = (co.x / 0.242 + 0.5, co.y / 0.242 + 0.5)
disc["source_file"] = "03-center-disc.svg / center-disc"
disc["static_geometry"] = True

armature = bpy.data.armatures.new("Petal_01_Skeleton")
rig = bpy.data.objects.new("Petal_01_Rig", armature)
asset.objects.link(rig)
rig.parent = root
rig.location = (0.0, root_y, 0.004)
rig.show_in_front = True
enum_set(armature, "display_type", "OCTAHEDRAL")
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
bone_names = ("Petal_01_Base", "Petal_01_Mid", "Petal_01_Tip")
joints = (0.0, 0.36, 0.72, 1.0)
previous = None
for i, name in enumerate(bone_names):
    bone = armature.edit_bones.new(name)
    bone.head, bone.tail = (0, joints[i]*length, 0), (0, joints[i+1]*length, 0)
    if previous:
        bone.parent = previous
        bone.use_connect = True
    previous = bone
bpy.ops.object.mode_set(mode="OBJECT")
petal.parent = rig
modifier = petal.modifiers.new(name="Three_Bone_Skin", type="ARMATURE")
modifier.object = rig
# Standard linear skinning matches the intended Unity deformation path.
modifier.use_deform_preserve_volume = False
groups = [petal.vertex_groups.new(name=name) for name in bone_names]
for index, (_, t) in enumerate(parameters):
    if t < 0.21:
        weights = (1.0, 0.0, 0.0)
    elif t <= 0.51:
        w = (t - 0.21) / 0.30
        w = w*w*(3.0-2.0*w)
        weights = (1.0-w, w, 0.0)
    elif t < 0.57:
        weights = (0.0, 1.0, 0.0)
    elif t <= 0.87:
        w = (t - 0.57) / 0.30
        w = w*w*(3.0-2.0*w)
        weights = (0.0, 1.0-w, w)
    else:
        weights = (0.0, 0.0, 1.0)
    for group, weight in zip(groups, weights):
        if weight > 1e-8:
            group.add([index], weight, "REPLACE")
for bone in rig.pose.bones:
    enum_set(bone, "rotation_mode", "XYZ")
    bone.lock_location = (True, True, True)
    bone.lock_scale = (True, True, True)
    bone.lock_rotation = (False, True, False)
rig["controls"] = "Blender local X: fold/lift; local Z: side sway; Y twist locked. Unity export axes must be verified on import."
rig["fixed_root"] = "Base head and vertex 0 coincide at the attachment. Rotate bones only; keep rig transform and bone translations unchanged."
rig["animation_owner"] = "Future Unity scripts. No keyframes, actions, drivers or constraints."
rig["weight_limit"] = 2

ground = mesh_object("PREVIEW_Ground", [(-0.7,-0.6,-0.008),(0.7,-0.6,-0.008),(0.7,0.8,-0.008),(-0.7,0.8,-0.008)], [(0,1,2,3)], stage)
ground.data.materials.append(material("PREVIEW_Backdrop", "#242634", 0.85))
camera_data = bpy.data.cameras.new("PREVIEW_TopPerspective")
camera = bpy.data.objects.new("PREVIEW_TopPerspective", camera_data)
stage.objects.link(camera)
enum_set(camera_data, "type", "PERSP")
camera.location = (0.0, 0.112, 0.85)
camera.rotation_euler = (0.0, 0.0, 0.0)
camera_data.lens = 58
camera_data.clip_start = 0.01
scene.camera = camera
for name, location, energy, size in (("PREVIEW_Key", (0.24,0.08,0.50), 7, 0.26), ("PREVIEW_Fill", (-0.23,0.20,0.38), 2, 0.35)):
    available = [i.identifier for i in bpy.types.Light.bl_rna.properties["type"].enum_items]
    if "AREA" not in available:
        raise ValueError(available)
    data = bpy.data.lights.new(name, "AREA")
    data.energy, data.size = energy, size
    obj = bpy.data.objects.new(name, data)
    stage.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (Vector((0,0.13,0)) - obj.location).to_track_quat('-Z','Y').to_euler()
world = bpy.data.worlds.new("PREVIEW_World")
world.use_nodes = True
background = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
background.inputs["Color"].default_value = (0.10,0.12,0.16,1)
background.inputs["Strength"].default_value = 0.35
scene.world = world
try:
    scene.render.engine = "BLENDER_EEVEE"
except TypeError as exc:
    raise RuntimeError(str(exc))
scene.render.resolution_x, scene.render.resolution_y = 900, 1000
scene.render.resolution_percentage = 100
enum_set(scene.render.image_settings, "file_format", "PNG")
scene.render.filepath = str(OUTPUT / "petal-rest-preview.png")
scene.render.film_transparent = False
scene.frame_start, scene.frame_end = 1, 1
scene.render.fps = 30
scene["mobile_budget"] = "No textures, subdivision, cloth, physics, or animation clips. At most 2 bone weights per vertex. Preview lights/ground/camera are excluded from FBX."
for obj in scene.objects:
    obj.select_set(False)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        space = area.spaces.active
        space.region_3d.view_rotation = Quaternion((1,0,0,0))
        space.region_3d.view_location = (0,0.112,0.004)
        space.region_3d.view_distance = 0.72
        enum_set(space.region_3d, 'view_perspective', 'PERSP')
        enum_set(space.shading, 'color_type', 'MATERIAL')
bpy.context.view_layer.update()
for obj in (petal, disc):
    obj.data.calc_loop_triangles()
print(json.dumps({"scene": scene.name, "petal_vertices": len(petal.data.vertices), "petal_triangles": len(petal.data.loop_triangles), "disc_vertices": len(disc.data.vertices), "disc_triangles": len(disc.data.loop_triangles), "bones": list(bone_names), "root_world": list(rig.location), "petal_length_m": length, "max_weights": max(len(v.groups) for v in petal.data.vertices)}, indent=2))
