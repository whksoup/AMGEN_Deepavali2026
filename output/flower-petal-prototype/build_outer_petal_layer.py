"""Add the 15-petal lower layer to the current single-petal Blender file via MCP.

Preserves the original top petal and uses its pose channels as the sync source.
Refuses a second build. Geometry is generated from the supplied SVG cubic outline.
"""
import bpy
import math
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from datetime import datetime
from mathutils import Quaternion

PROJECT = Path(r"F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall")
OUT = PROJECT / "output/flower-petal-prototype"
SOURCE = PROJECT / "output/flower-curves-package/01-outer-petals.svg"
scene = bpy.context.scene
assert Path(bpy.data.filepath).stem == "Deepavali_SinglePetal"
assert "04_OuterPetals_15_Rigged" not in bpy.data.collections, "Layer already exists"
assert bpy.context.mode == "OBJECT"
master = bpy.data.objects["Petal_01_Rig"]
inner = bpy.data.objects["InnerPetal_01"]
root = bpy.data.objects["FlowerRoot"]
backup = OUT / ("Deepavali_SinglePetal_before_outer_" + datetime.now().strftime("%Y%m%d_%H%M%S") + ".blend")
bpy.ops.wm.save_as_mainfile(filepath=str(backup), copy=True)

def enum_set(owner, field, value):
    assert value in [i.identifier for i in owner.bl_rna.properties[field].enum_items]
    setattr(owner, field, value)

def collection(name):
    c = bpy.data.collections.new(name)
    scene.collection.children.link(c)
    return c

outer_col = collection("04_OuterPetals_15_Rigged")
top_col = collection("05_TopLayer_Reference_8")
ref_col = bpy.data.collections["02_SVG_References_Hidden"]
ctrl = bpy.data.objects.new("OuterLayer_CTRL", None)
outer_col.objects.link(ctrl)
ctrl.parent = root
ctrl.empty_display_size = 0.035
ctrl["lift_follow"] = 0.72
ctrl["downward_follow"] = 1.35
ctrl["sway_follow"] = 0.5
ctrl["layer_drop_m"] = 0.024
ctrl["petal_count"] = 15
ctrl["spacing_degrees"] = 24.0
ctrl["phase_degrees"] = 12.0
ctrl["motion_source"] = master.name
ctrl["usage"] = "Pose Petal_01_Rig X/Z channels: top references and all outer petals follow. Local X lifts; local Z sways."
ctrl["clearance_design"] = "24 mm root drop; 72% positive lift, 135% downward bend, 50% sway; 6 degree shingled surface. Synchronized motion only."
for prop, minimum, maximum in (("lift_follow",0,1),("downward_follow",0.72,2),("sway_follow",0,1),("layer_drop_m",0.014,0.08)):
    ctrl.id_properties_ui(prop).update(min=minimum, max=maximum, description="Changing this requires rechecking layer clearance.")

def driver(owner, path, axis, source_path, factor=None):
    fc = owner.driver_add(path, axis)
    d = fc.driver
    enum_set(d, "type", "SCRIPTED")
    v = d.variables.new()
    v.name = "source"
    enum_set(v, "type", "SINGLE_PROP")
    v.targets[0].id = master
    v.targets[0].data_path = source_path
    d.expression = "source"
    if factor:
        v = d.variables.new()
        v.name = "follow"
        enum_set(v, "type", "SINGLE_PROP")
        v.targets[0].id = ctrl
        v.targets[0].data_path = '["' + factor + '"]'
        d.expression = "source * follow"
        if factor == "lift_follow":
            v = d.variables.new()
            v.name = "down"
            enum_set(v, "type", "SINGLE_PROP")
            v.targets[0].id = ctrl
            v.targets[0].data_path = '["downward_follow"]'
            d.expression = "max(source, 0) * follow + min(source, 0) * down"

def make_material(name, color):
    rgb = [int(color.lstrip('#')[i:i+2],16)/255 for i in (0,2,4)]
    linear = [v/12.92 if v<=0.04045 else ((v+0.055)/1.055)**2.4 for v in rgb]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = (*linear,1)
    n = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    n.inputs["Base Color"].default_value = (*linear,1)
    n.inputs["Roughness"].default_value = 0.54
    return m

# Seven linked copies complete the existing 8-petal top silhouette for comparison.
# The user's original mesh, skeleton, weights, and rest transforms remain intact.
top_elements = [e for e in ET.parse(SOURCE.with_name("02-inner-petals.svg")).getroot().iter() if e.tag.endswith("path")]
for i in range(1,8):
    a = -math.radians(45*i)
    rig = master.copy()
    rig.data = master.data.copy()
    rig.name = f"REF_TopPetal_{i+1:02d}_Rig"
    top_col.objects.link(rig)
    rig.location = (-math.sin(a)*0.082, math.cos(a)*0.082, 0.004)
    rig.rotation_euler.z = a
    rig.show_in_front = False
    rig["reference_only"] = True
    rig["animation_owner"] = "Synchronized pose reference from Petal_01_Rig. Exclude from outer-only export."
    obj = inner.copy()
    obj.name = f"REF_TopPetal_{i+1:02d}"
    top_col.objects.link(obj)
    obj.parent = rig
    for mod in obj.modifiers:
        if mod.type == "ARMATURE":
            mod.object = rig
    enum_set(obj.material_slots[0], "link", "OBJECT")
    obj.material_slots[0].material = make_material(f"REF_Top_{i+1:02d}_SVG", top_elements[i].get("fill"))
    obj["reference_only"] = True
    for b in rig.pose.bones:
        for axis in (0,2):
            driver(b, "rotation_euler", axis, f'pose.bones["{b.name}"].rotation_euler[{axis}]')

element = next(e for e in ET.parse(SOURCE).getroot().iter() if e.get("id")=="outer-petal-01")
tokens = re.findall(r"[MCZ]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?", element.get("d"))
index, point, segments = 0, None, []
while index < len(tokens):
    command = tokens[index]
    index += 1
    if command == "M":
        point = tuple(map(float,tokens[index:index+2]))
        index += 2
    elif command == "C":
        values = list(map(float,tokens[index:index+6]))
        end = tuple(values[4:6])
        segments.append((point,tuple(values[:2]),tuple(values[2:4]),end))
        point = end
        index += 6
    else:
        assert command == "Z"

def bezier(s,t):
    q = 1-t
    return tuple(q**3*s[0][k]+3*q*q*t*s[1][k]+3*q*t*t*s[2][k]+t**3*s[3][k] for k in (0,1))

length = 0.346
def width(t):
    y = 400-346*t
    for s in segments[:3]:
        if s[3][1]-1e-9 <= y <= s[0][1]+1e-9:
            lo,hi = 0.,1.
            for _ in range(48):
                mid = (lo+hi)/2
                if bezier(s,mid)[1]>y:
                    lo=mid
                else:
                    hi=mid
            return abs(bezier(s,(lo+hi)/2)[0]-500)*0.001
    raise ValueError(t)

verts,params,faces,rows = [(0,0,0)],[(0.5,0)],[],[]
for i in range(1,48):
    t=i/48
    w=width(t)
    row=[]
    for j in range(9):
        u=-1+j/4
        x=u*w
        z=0.0015*math.sin(math.pi*t)**1.4*(1-u*u) + math.tan(math.radians(6))*x
        row.append(len(verts))
        verts.append((x,t*length,z))
        params.append(((u+1)/2,t))
    rows.append(row)
tip=len(verts)
verts.append((0,length,0))
params.append((0.5,1))
for j in range(8):
    faces.append((0,rows[0][j+1],rows[0][j]))
for low,high in zip(rows,rows[1:]):
    for j in range(8):
        faces.append((low[j],low[j+1],high[j+1],high[j]))
for j in range(8):
    faces.append((rows[-1][j],rows[-1][j+1],tip))

template=bpy.data.meshes.new("OuterPetal_SVG_Surface_Template")
template.from_pydata(verts,[],faces)
template.update()
for p in template.polygons:
    p.use_smooth=True
uv=template.uv_layers.new(name="ArtworkUV")
coord=template.uv_layers.new(name="PetalCoord_Width_Length")
for loop in template.loops:
    x,y,_=verts[loop.vertex_index]
    uv.data[loop.index].uv=(0.5+x/0.202,y/length)
    coord.data[loop.index].uv=params[loop.vertex_index]
materials=[make_material("Outer_"+name,col) for name,col in (("Saffron_SVG","#b84a12"),("Plum_SVG","#55205e"),("Violet_SVG","#492363"),("Berry_SVG","#971246"))]
palette=[0,1,2,3,2,1,0,1,2,3,2,1,0,1,2]
mapping=[]
for i in range(15):
    n=i+1
    degrees=12+24*i
    a=-math.radians(degrees)
    data=bpy.data.armatures.new(f"OuterPetal_{n:02d}_Skeleton")
    rig=bpy.data.objects.new(f"OuterPetal_{n:02d}_Rig",data)
    outer_col.objects.link(rig)
    rig.parent=ctrl
    rig.location=(-math.sin(a)*0.1,math.cos(a)*0.1,-0.02)
    rig.rotation_euler.z=a
    enum_set(data,"display_type","OCTAHEDRAL")
    rig.show_in_front=False
    fc=rig.driver_add("location",2)
    enum_set(fc.driver,"type","SCRIPTED")
    v=fc.driver.variables.new()
    v.name="drop"
    enum_set(v,"type","SINGLE_PROP")
    v.targets[0].id=ctrl
    v.targets[0].data_path='["layer_drop_m"]'
    fc.driver.expression="0.004-drop"
    bpy.context.view_layer.objects.active=rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    names=[f"Outer_{n:02d}_{part}" for part in ("Base","Mid","Tip")]
    joints=(0,.36,.72,1)
    previous=None
    for k,name in enumerate(names):
        b=data.edit_bones.new(name)
        b.head=(0,joints[k]*length,0)
        b.tail=(0,joints[k+1]*length,0)
        if previous:
            b.parent=previous
            b.use_connect=True
        previous=b
    bpy.ops.object.mode_set(mode="OBJECT")
    rig.select_set(False)
    mesh=template.copy()
    mesh.name=f"OuterPetal_{n:02d}_Mesh"
    obj=bpy.data.objects.new(f"OuterPetal_{n:02d}",mesh)
    outer_col.objects.link(obj)
    obj.parent=rig
    mesh.materials.append(materials[palette[i]])
    mod=obj.modifiers.new("Three_Bone_Skin","ARMATURE")
    mod.object=rig
    mod.use_deform_preserve_volume=False
    groups=[obj.vertex_groups.new(name=name) for name in names]
    for vi,(_,t) in enumerate(params):
        if t<.21: weights=(1,0,0)
        elif t<=.51:
            w=(t-.21)/.30
            w=w*w*(3-2*w)
            weights=(1-w,w,0)
        elif t<.57: weights=(0,1,0)
        elif t<=.87:
            w=(t-.57)/.30
            w=w*w*(3-2*w)
            weights=(0,1-w,w)
        else: weights=(0,0,1)
        for g,w in zip(groups,weights):
            if w>1e-8:
                g.add([vi],w,"REPLACE")
    for k,b in enumerate(rig.pose.bones):
        enum_set(b,"rotation_mode","XYZ")
        b.lock_location=(True,True,True)
        b.lock_scale=(True,True,True)
        b.lock_rotation=(False,True,False)
        for axis,factor in ((0,"lift_follow"),(2,"sway_follow")):
            driver(b,"rotation_euler",axis,f'pose.bones["Petal_01_{("Base","Mid","Tip")[k]}"].rotation_euler[{axis}]',factor)
    nearest=sorted(range(8),key=lambda t:abs((degrees-45*t+180)%360-180))[:2]
    rig["top_neighbors_1based"]=[t+1 for t in nearest]
    rig["clockwise_angle_degrees"]=float(degrees)
    rig["animation_owner"]="Blender drivers from Petal_01_Rig. Recreate this coupling in Unity or bake animation before export."
    rig["weight_limit"]=2
    obj["source_file"]=str(SOURCE)
    obj["source_path_id"]="outer-petal-01"
    obj["root_vertex_index"]=0
    obj["tip_vertex_index"]=tip
    obj["length_m"]=length
    obj["shingle_degrees"]=6.0
    obj["surface"]="Single +Z surface; preserve the SVG silhouette. Use two-sided material in Unity."
    mapping.append({"petal":obj.name,"rig":rig.name,"angle_degrees":degrees,"top_neighbors_1based":[t+1 for t in nearest]})
bpy.data.meshes.remove(template)

# Editable exact outline retained in the existing hidden SVG-reference collection.
curve=bpy.data.curves.new("REF_outer-petal-01_exact_SVG","CURVE")
enum_set(curve,"dimensions","3D")
curve.resolution_u=24
sp=curve.splines.new("BEZIER")
sp.bezier_points.add(5)
for i,p in enumerate(sp.bezier_points):
    enum_set(p,"handle_left_type","FREE")
    enum_set(p,"handle_right_type","FREE")
    def co(xy): return ((xy[0]-500)*.001,(500-xy[1])*.001,-.020)
    p.co=co(segments[i][0])
    p.handle_left=co(segments[(i-1)%6][2])
    p.handle_right=co(segments[i][1])
sp.use_cyclic_u=True
ref=bpy.data.objects.new("REF_outer-petal-01_exact_SVG",curve)
ref_col.objects.link(ref)
ref.hide_render=True

# Only adjust the existing preview stage to frame the expanded flower.
bpy.data.objects["PREVIEW_Ground"].location.z=-.075
camera=bpy.data.objects["PREVIEW_TopPerspective"]
enum_set(camera.data,"type","ORTHO")
camera.data.ortho_scale=1.04
camera.location=(0,0,1.4)
scene.render.resolution_x=1100
scene.render.resolution_y=1100
for name,energy,size,position in (("PREVIEW_Key",25,.65,(.3,-.15,.8)),("PREVIEW_Fill",15,.75,(-.35,.25,.65))):
    light=bpy.data.objects[name]
    light.data.energy=energy
    light.data.size=size
    light.location=position
    light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
master.show_in_front=False
root["prototype_scope"]="Original top petal plus 15 rigged outer petals. Seven linked top reference copies complete the 8-petal comparison layer."
scene["outer_layer_backup"]=str(backup)
scene["outer_layer_motion_note"]="All references share Petal_01_Rig pose; lower layer uses reduced amplitudes. Not a runtime collision solver."
notes=bpy.data.texts.new("OUTER_LAYER_README")
notes.write('''15 OUTER PETALS / 24 DEGREE SPACING / 12 DEGREE OFFSET
Source: 01-outer-petals.svg outer-petal-01; exact cubic silhouette resampled to a skinnable surface.
Units: metres. SVG width/length 0.202/0.346 m. Tips radius 0.446 m.
Each outer petal has Base / Mid / Tip bones, smooth normalized skin weights, at most 2 influences.
ArtworkUV maps the unstretched planar silhouette; PetalCoord_Width_Length is also supplied.

POSE: select Petal_01_Rig and rotate Base/Mid/Tip local X for lift, local Z for sway.
The 7 REF_TopPetal rigs copy those channels; the original top mesh stays unchanged.
OuterLayer_CTRL contains lift_follow=0.72, downward_follow=1.35, sway_follow=0.5,
layer_drop_m=0.024. Downward bends have stronger following to protect the lower layer.
All 15 lower rigs follow the same source phase. Changing the source pose updates both layers.
Rest pose is saved; no action is assigned and no automatic timeline motion is enabled.

CLEARANCE: the lower roots sit 24 mm below the top roots. Outer surfaces have a 6 degree
cross-width shingle tilt, so adjacent larger petals overlap in a consistent direction.
The camber is 1.5 mm. The surface is intentionally single-sided geometry for the existing
mobile skinning pipeline; render with two-sided shading. No cloth/physics is required.
See outer-layer-validation.json for tested pose ranges and measured clearances.
Synchronization and static spacing are not a general collision solver. Independent top
petal phases, larger downward bends, changed thickness/spacing or manual outer poses need
another clearance check. Future interaction coupling should consider BOTH overlapping
top neighbors stored on each outer rig, and keep lower motion beneath their minimum envelope.

EXPORT: Blender drivers are authoring controls and do not transfer as live FBX logic.
Recreate the coupling in the Unity motion controller, or bake a specific animation first.
Only the 04_OuterPetals_15_Rigged collection is the new outer asset; 05_TopLayer_Reference_8
contains comparison references and should be excluded from an outer-only export.
''')
(OUT/"outer-layer-layout.json").write_text(json.dumps({"source":str(SOURCE),"count":15,"spacing_degrees":24,"phase_degrees":12,"root_drop_m":.024,"lift_follow":.72,"downward_follow":1.35,"sway_follow":.5,"shingle_degrees":6,"mapping":mapping},indent=2))
for obj in scene.objects: obj.select_set(False)
ctrl.select_set(True)
bpy.context.view_layer.objects.active=ctrl
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        space=area.spaces.active
        space.region_3d.view_rotation=Quaternion((1,0,0,0))
        space.region_3d.view_location=(0,0,0)
        space.region_3d.view_distance=1.2
        enum_set(space.region_3d,"view_perspective","ORTHO")
        enum_set(space.shading,"color_type","MATERIAL")
        space.overlay.show_relationship_lines=False
ctrl.update_tag()
bpy.context.scene.frame_set(bpy.context.scene.frame_current)
bpy.context.view_layer.update()
print(json.dumps({"created_outer_meshes":15,"created_outer_rigs":15,"outer_deform_bones":45,"vertices_per_petal":len(verts),"faces_per_petal":len(faces),"backup":str(backup)},indent=2))
