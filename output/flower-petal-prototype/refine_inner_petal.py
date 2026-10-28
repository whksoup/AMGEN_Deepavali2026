"""Petal2 sculpt revision, run through Blender MCP in the review .blend.

Only prototype objects are edited. Does not export or modify Unity assets.
Petal2 is axis-aligned, then fitted in width to the original Image 1.
The broad SVG base remains under the center disc. Vertex 0 is its midpoint.
"""
import bpy
import math
import re
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from mathutils import Vector

OUT = Path(r"F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall\output\flower-petal-prototype")
LENGTH = 0.220
WIDTH_FACTOR = 0.88
ROOT_RADIUS = 0.105
CAMBER = 0.002
SHINGLE_DEGREES = 6.0
LENGTH_SEGMENTS, WIDTH_SEGMENTS = 40, 8


def sample_svg():
    """Sample the actual SVG cubic and elliptical-arc commands, including lines."""
    element = next(e for e in ET.parse(OUT / 'Petal2.svg').getroot().iter() if e.tag.endswith('path'))
    tokens = re.findall(r'[A-Za-z]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?', element.get('d'))
    points, i, p, start = [], 0, None, None
    while i < len(tokens):
        cmd = tokens[i].upper()
        i += 1
        n = {'M': 2, 'C': 6, 'A': 7, 'L': 2, 'Z': 0}[cmd]
        v = list(map(float, tokens[i:i+n]))
        i += n
        if cmd == 'M':
            p = start = complex(*v)
            points.append(p)
        elif cmd == 'L':
            p = complex(*v)
            points.append(p)
        elif cmd == 'C':
            a, b, end = complex(*v[:2]), complex(*v[2:4]), complex(*v[4:])
            for j in range(1, 257):
                t = j/256
                q = 1-t
                points.append(q**3*p + 3*q*q*t*a + 3*q*t*t*b + t**3*end)
            p = end
        elif cmd == 'A':
            rx, ry, phi, large, sweep, x, y = v
            assert abs(rx-ry) < 1e-6 and phi == 0, 'This source uses circular arcs.'
            end = complex(x, y)
            d = end-p
            mid = (p+end)/2
            h = math.sqrt(max(0, rx*rx-abs(d)**2/4))
            center = mid + (1 if large != sweep else -1)*1j*d/abs(d)*h
            a0, a1 = math.atan2((p-center).imag, (p-center).real), math.atan2((end-center).imag, (end-center).real)
            delta = (a1-a0) % (2*math.pi)
            if not sweep:
                delta -= 2*math.pi
            for j in range(1, 65):
                angle = a0+delta*j/64
                points.append(center+rx*complex(math.cos(angle), math.sin(angle)))
            p = end
        elif cmd == 'Z':
            p = start
    return points


raw = sample_svg()
tip = min(raw, key=lambda p: p.imag)
base_right, base_left = raw[-2:]
base = (base_left+base_right)/2
axis = (tip-base)/abs(tip-base)
right = 1j*axis
factor = LENGTH/abs(tip-base)
def local(p):
    delta = p-base
    return ((delta.real*right.real+delta.imag*right.imag)*factor*WIDTH_FACTOR,
            (delta.real*axis.real+delta.imag*axis.imag)*factor)
outline = [local(p) for p in raw]
tip_i = raw.index(tip)
# Ordered base-to-tip side polylines, using the SVG's exact corners.
left = [outline[-1]] + outline[:tip_i+1]
right_side = list(reversed(outline[tip_i:-1]))
assert all(b[1] >= a[1]-1e-9 for a, b in zip(left, left[1:]))
assert all(b[1] >= a[1]-1e-9 for a, b in zip(right_side, right_side[1:]))


def side_at(side, t):
    y = side[0][1]*(1-t)+LENGTH*t
    for a, b in zip(side, side[1:]):
        if a[1]-1e-9 <= y <= b[1]+1e-9:
            f = (y-a[1])/(b[1]-a[1])
            return (a[0]+(b[0]-a[0])*f, y)
    return side[-1]


verts, params, rows = [(0, 0, 0)], [(0.5, 0)], []
for i in range(LENGTH_SEGMENTS):
    t = i/LENGTH_SEGMENTS
    a, b = side_at(left, t), side_at(right_side, t)
    row = []
    for j in range(WIDTH_SEGMENTS+1):
        u = j/WIDTH_SEGMENTS
        x, y = a[0]*(1-u)+b[0]*u, a[1]*(1-u)+b[1]*u
        z = CAMBER*math.sin(math.pi*t)**1.4*(1-(2*u-1)**2) + math.tan(math.radians(SHINGLE_DEGREES))*x
        if i == 0 and j == WIDTH_SEGMENTS//2:
            row.append(0)
        else:
            row.append(len(verts))
            verts.append((x, y, z))
            params.append((u, t))
    rows.append(row)
tip_index = len(verts)
verts.append((0, LENGTH, 0))
params.append((0.5, 1))
faces = []
for a, b in zip(rows, rows[1:]):
    for j in range(WIDTH_SEGMENTS):
        faces.append((a[j], a[j+1], b[j+1], b[j]))
for j in range(WIDTH_SEGMENTS):
    faces.append((rows[-1][j], rows[-1][j+1], tip_index))

master = bpy.data.objects['Petal_01_Rig']
top = [bpy.data.objects['InnerPetal_01']] + [bpy.data.objects[f'REF_TopPetal_{i:02d}'] for i in range(2, 9)]
mesh = bpy.data.meshes.new('InnerPetal_Petal2_Surface')
mesh.from_pydata(verts, [], faces)
mesh.update()
for mat in top[0].data.materials:
    mesh.materials.append(mat)
for p in mesh.polygons:
    p.use_smooth = True
uv = mesh.uv_layers.new(name='ArtworkUV')
coord = mesh.uv_layers.new(name='PetalCoord_Width_Length')
xmin, xmax = min(p[0] for p in verts), max(p[0] for p in verts)
for loop in mesh.loops:
    x, y, z = verts[loop.vertex_index]
    uv.data[loop.index].uv = ((x-xmin)/(xmax-xmin), y/LENGTH)
    coord.data[loop.index].uv = params[loop.vertex_index]

for obj in top:
    material = obj.material_slots[0].material
    obj.data = mesh
    obj.material_slots[0].link = 'OBJECT'
    obj.material_slots[0].material = material
    obj.vertex_groups.clear()
    groups = [obj.vertex_groups.new(name=b.name) for b in master.data.bones]
    for vi, (_, t) in enumerate(params):
        if t < .21:
            weights = (1, 0, 0)
        elif t <= .51:
            w = (t-.21)/.30
            w = w*w*(3-2*w)
            weights = (1-w, w, 0)
        elif t < .57:
            weights = (0, 1, 0)
        elif t <= .87:
            w = (t-.57)/.30
            w = w*w*(3-2*w)
            weights = (0, 1-w, w)
        else:
            weights = (0, 0, 1)
        for group, w in zip(groups, weights):
            if w > 1e-8:
                group.add([vi], w, 'REPLACE')
    obj['source_file'] = 'Petal2.svg (provided by user)'
    obj['root_vertex_index'] = 0
    obj['tip_vertex_index'] = tip_index
    obj['length_m'] = LENGTH
    obj['rest_camber_m'] = CAMBER
    obj['shingle_degrees'] = SHINGLE_DEGREES
    rig = obj.parent
    radial = Vector((rig.location.x, rig.location.y, 0)).normalized()
    rig.location.x, rig.location.y = radial.x*ROOT_RADIUS, radial.y*ROOT_RADIUS
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    joints = (0, .36, .72, 1)
    for k, b in enumerate(rig.data.edit_bones):
        b.head = (0, joints[k]*LENGTH, 0)
        b.tail = (0, joints[k+1]*LENGTH, 0)
    bpy.ops.object.mode_set(mode='OBJECT')

# An exact sampled SVG reference remains independently editable / inspectable.
ref_name = 'REF_Petal2_Source_Outline'
if ref_name in bpy.data.objects:
    bpy.data.objects.remove(bpy.data.objects[ref_name], do_unlink=True)
curve = bpy.data.curves.new(ref_name, 'CURVE')
curve.dimensions = '3D' if '3D' in [i.identifier for i in curve.bl_rna.properties['dimensions'].enum_items] else curve.dimensions
line = curve.splines.new('POLY')
line.points.add(len(outline)-1)
for point, (x, y) in zip(line.points, outline):
    point.co = (x, y+ROOT_RADIUS, .004, 1)
line.use_cyclic_u = True
ref = bpy.data.objects.new(ref_name, curve)
bpy.data.collections['02_SVG_References_Hidden'].objects.link(ref)
ref.hide_render = True
ref['source'] = 'Petal2.svg: sampled arcs and Beziers; axis-aligned, width fitted to Image 1'

scene = bpy.context.scene
camera = bpy.data.objects['REVIEW_Image1_Camera']
target = Vector((0, -.008, .006))
distance = 3.6
elev = math.radians(50)
camera.location = target + Vector((0, -math.cos(elev)*distance, math.sin(elev)*distance))
camera.rotation_euler = (target-camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.lens = 105
camera['elevation_degrees'] = 50.0
scene.camera = camera
scene.render.filepath = str(OUT/'inner-petal-image1-angle.png')
bpy.data.objects['FlowerRoot']['prototype_scope'] = 'Petal2 inner-ring sculpt revision. Three-bone channels and outer preview drivers retained; Unity integration deferred.'
for obj in scene.objects:
    obj.select_set(False)
top[0].select_set(True)
bpy.context.view_layer.objects.active = top[0]
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        space = area.spaces.active
        identifiers = [i.identifier for i in space.region_3d.bl_rna.properties['view_perspective'].enum_items]
        if 'CAMERA' in identifiers:
            space.region_3d.view_perspective = 'CAMERA'
        space.overlay.show_overlays = False
        space.shading.color_type = 'MATERIAL' if 'MATERIAL' in [i.identifier for i in space.shading.bl_rna.properties['color_type'].enum_items] else space.shading.color_type
        space.region_3d.view_camera_zoom = 10
bpy.context.view_layer.update()
mesh.calc_loop_triangles()
report = {'source': 'Petal2.svg', 'ground_truth': 'User Image 1', 'length_m': LENGTH,
          'svg_width_fit': WIDTH_FACTOR,
          'width_m': xmax-xmin, 'root_radius_m': ROOT_RADIUS, 'tip_radius_m': ROOT_RADIUS+LENGTH,
          'previous_length_m': .263, 'previous_tip_radius_m': .345,
          'camber_m': CAMBER, 'shingle_degrees': SHINGLE_DEGREES,
          'vertices': len(verts), 'triangles': len(mesh.loop_triangles),
          'camera': {'elevation_degrees': 50, 'distance_m': distance, 'lens_mm': 105},
          'unity_assets_modified': False}
(OUT/'inner-petal-Petal2-layout.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
