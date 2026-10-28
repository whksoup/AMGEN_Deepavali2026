"""Build one explicitly selected sculpt round; judging is a separate script.

Run in Blender with ROUND=1, 2 or 3. Unity and previous sculpt files are untouched.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

OUT=Path(r'F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall\output\flower-petal-prototype\video-proportion-review')
REFERENCE=json.loads((OUT/'reference-measurements.json').read_text())
ROUND=globals().get('ROUND',1)
SCENE=bpy.context.scene
INNER_NAMES=['N','NE','E','SE','S','SW','W','NW']
inner=[bpy.data.objects['InnerPetal_01']]+[bpy.data.objects[f'REF_TopPetal_{i:02d}'] for i in range(2,9)]
base_path=OUT/'sculpt-baseline.json'
if not base_path.exists():
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'before-video-fit.blend'),copy=True)
    def mesh_values(obj):return {'vertices':[list(v.co) for v in obj.data.vertices],'faces':[list(p.vertices) for p in obj.data.polygons]}
    baseline={'inner':mesh_values(inner[0]),'outer':mesh_values(bpy.data.objects['OuterPetal_01']),
              'disc_vertices':[list(v.co) for v in bpy.data.objects['CenterDisc'].data.vertices]}
    base_path.write_text(json.dumps(baseline))
BASE=json.loads(base_path.read_text())

def source_profile(values,segments):
    rows={}
    length=max(v[1] for v in values['vertices'])
    for x,y,z in values['vertices']:
        # UV rows in Petal2 are slightly skewed in Y; group by their length station.
        key=max(0,round(y/length*segments))/segments*length
        rows.setdefault(key,[]).append(x)
    ys=sorted(rows);length=max(ys)
    width=max(x for x,y,z in values['vertices'])-min(x for x,y,z in values['vertices'])
    return [(y/length,min(rows[y])/width,max(rows[y])/width) for y in ys]
IP,OP=source_profile(BASE['inner'],40),source_profile(BASE['outer'],48)

def interpolate(profile,t):
    for a,b in zip(profile,profile[1:]):
        if a[0]<=t<=b[0]:
            f=(t-a[0])/(b[0]-a[0]);return (a[1]*(1-f)+b[1]*f,a[2]*(1-f)+b[2]*f)
    return profile[0][1:] if t<profile[0][0] else profile[-1][1:]

def smooth_polygon(p):
    result=[]
    for i in range(len(p)):
        a,b,c,d=[Vector(p[j%len(p)]) for j in (i-1,i,i+1,i+2)]
        # Preserve the pointed apex instead of rounding it.
        m0=(c-b) if i==0 else (c-a)*.5
        m1=(c-b) if (i+1)%len(p)==0 else (d-b)*.5
        for k in range(20):
            t=k/20;h00=2*t**3-3*t*t+1;h10=t**3-2*t*t+t;h01=-2*t**3+3*t*t;h11=t**3-t*t
            result.append(tuple(b*h00+m0*h10+c*h01+m1*h11))
    return result

def target_profile(measure,root):
    p=list(zip(measure['local_x'],measure['local_radial_y']))
    p=smooth_polygon(p)
    ymin=min(v[1] for v in p)
    def cross(y):
        xs=[]
        for a,b in zip(p,p[1:]+p[:1]):
            if min(a[1],b[1])-1e-10<=y<=max(a[1],b[1])+1e-10 and abs(a[1]-b[1])>1e-10:
                f=(y-a[1])/(b[1]-a[1]);xs.append(a[0]+(b[0]-a[0])*f)
        return (min(xs),max(xs)) if xs else (0,0)
    first=cross(ymin+.0003)
    def sample(y):
        if y<ymin+.0003:
            f=max(0,min(1,(y-root)/(ymin+.0003-root)))
            return (-measure['width_m']*.28*(1-f)+first[0]*f,measure['width_m']*.28*(1-f)+first[1]*f)
        return cross(y)
    return sample

def mesh_grid(obj,profile,length,width,root,angle,camber,shingle,target=None,blend=0,segments=56):
    verts=[(0,0,0)];uvs=[(.5,0)];rows=[]
    broad=profile is IP
    for i in range(segments):
        t=i/segments
        left,right=interpolate(profile,t);left*=width;right*=width
        if target:
            tl,tr=target(root+t*length);left=left*(1-blend)+tl*blend;right=right*(1-blend)+tr*blend
        if i==0 and not broad:
            rows.append([0]);continue
        row=[]
        for j in range(9):
            u=j/8;x=left*(1-u)+right*u;y=t*length
            z=camber*math.sin(math.pi*t)**1.4*(1-(2*u-1)**2)+math.tan(math.radians(shingle))*x
            if i==0 and j==4:row.append(0)
            else:row.append(len(verts));verts.append((x,y,z));uvs.append((u,t))
        rows.append(row)
    tip=len(verts);verts.append((0,length,0));uvs.append((.5,1));rows.append([tip])
    faces=[]
    for a,b in zip(rows,rows[1:]):
        if len(a)==1:
            for j in range(8):faces.append((a[0],b[j+1],b[j]))
        elif len(b)==1:
            for j in range(8):faces.append((a[j],a[j+1],b[0]))
        else:
            for j in range(8):faces.append((a[j],a[j+1],b[j+1],b[j]))
    material=obj.material_slots[0].material
    mesh=bpy.data.meshes.new(f'{obj.name}_VideoRound{ROUND}')
    mesh.from_pydata(verts,[],faces);mesh.update();mesh.materials.append(material)
    obj.data=mesh;obj.material_slots[0].link='OBJECT';obj.material_slots[0].material=material
    for poly in mesh.polygons:poly.use_smooth=True
    art=mesh.uv_layers.new(name='ArtworkUV');coord=mesh.uv_layers.new(name='PetalCoord_Width_Length')
    xmin=min(v[0] for v in verts);xmax=max(v[0] for v in verts)
    for loop in mesh.loops:
        vi=loop.vertex_index;v=verts[vi];art.data[loop.index].uv=((v[0]-xmin)/(xmax-xmin),v[1]/length);coord.data[loop.index].uv=uvs[vi]
    rig=obj.parent
    obj.vertex_groups.clear();groups=[obj.vertex_groups.new(name=b.name) for b in rig.data.bones]
    for vi,(u,t) in enumerate(uvs):
        if t<.21:weights=(1,0,0)
        elif t<=.51:
            w=(t-.21)/.30;w=w*w*(3-2*w);weights=(1-w,w,0)
        elif t<.57:weights=(0,1,0)
        elif t<=.87:
            w=(t-.57)/.30;w=w*w*(3-2*w);weights=(0,1-w,w)
        else:weights=(0,0,1)
        for g,w in zip(groups,weights):
            if w>1e-8:g.add([vi],w,'REPLACE')
    a=math.radians(angle);rig.location.x=root*math.sin(a);rig.location.y=root*math.cos(a);rig.rotation_euler.z=-a
    bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    joints=[0,.36,.72,1]
    for k,b in enumerate(rig.data.edit_bones):b.head=(0,joints[k]*length,0);b.tail=(0,joints[k+1]*length,0)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj['video_round']=ROUND;obj['length_m']=length;obj['width_m']=xmax-xmin;obj['root_vertex_index']=0;obj['tip_vertex_index']=tip
    obj['reference_angle_degrees']=angle;obj['rest_camber_m']=camber;obj['shingle_degrees']=shingle
    return {'name':obj.name,'length_m':length,'width_m':xmax-xmin,'angle_degrees':angle,'tip_radius_m':root+length,'vertices':len(verts),'triangles':sum(len(f)-2 for f in faces)}

blend={1:0.,2:.80,3:1.}[ROUND]
camber={1:.0012,2:.0009,3:.0008}[ROUND]
shingle={1:1.5,2:.8,3:.45}[ROUND]
inner_report=[]
for obj,key in zip(inner,INNER_NAMES):
    m=REFERENCE['inner'][key];root=.082
    length=m['tip_radius_m']-root
    r=mesh_grid(obj,IP,length,m['width_m'],root,m['angle_degrees'],camber,shingle,target_profile(m,root),blend)
    obj.parent.location.z=.003
    obj['position_label']=key;obj['reference']='Video 27.46 s gold silhouette, checked in other angles'
    r['position']=key;inner_report.append(r)

# Sixteen petals are explicitly required by the user. Clone the existing rig once.
if 'OuterPetal_16' not in bpy.data.objects:
    source=bpy.data.objects['OuterPetal_15'];rig=source.parent.copy();rig.data=source.parent.data.copy();rig.name='OuterPetal_16_Rig'
    col=bpy.data.collections['04_OuterPetals_15_Rigged'];col.objects.link(rig)
    obj=source.copy();obj.data=source.data.copy();obj.name='OuterPetal_16';col.objects.link(obj);obj.parent=rig
    for mod in obj.modifiers:
        if mod.type=='ARMATURE':mod.object=rig
    for b in rig.data.bones:b.name=b.name.replace('15','16')
    for fc in rig.animation_data.drivers:fc.data_path=fc.data_path.replace('Outer_15_','Outer_16_')
    for g in obj.vertex_groups:g.name=g.name.replace('15','16')
outer=[bpy.data.objects[f'OuterPetal_{i:02d}'] for i in range(1,17)]
samples=[]
for x,y in REFERENCE['outer_tips_world']:
    samples.append((math.degrees(math.atan2(x,y))%360,math.hypot(x,y)))
samples.sort();samples=[(samples[-1][0]-360,samples[-1][1])]+samples+[(samples[0][0]+360,samples[0][1])]
def radius_at(a):
    for (a0,r0),(a1,r1) in zip(samples,samples[1:]):
        if a0<=a<=a1:return r0+(r1-r0)*(a-a0)/(a1-a0)
outer_report=[]
outer_angles=[i*22.5 for i in range(16)]
if ROUND==3:
    # Fourteen readable perimeter tips plus two partly occluded far-side lobes.
    # The user confirms 16; the two added directions are inferred, not measured.
    outer_angles=sorted([a for a,r in samples[1:-1]]+[15.,345.])
for i,obj in enumerate(outer):
    angle=outer_angles[i];radius=radius_at(angle)
    width=.202*(radius/.446)*({1:1.,2:.94,3:.94}[ROUND])
    inferred=ROUND==3 and angle in (15.,345.)
    if ROUND==3:
        previous=outer_angles[i-1] if i else outer_angles[-1]-360
        following=outer_angles[i+1] if i<15 else outer_angles[0]+360
        width*=math.sqrt(((following-previous)*.5)/22.5)
        if inferred:radius*=.965
    r=mesh_grid(obj,OP,radius-.10,width,.10,angle,.0007,shingle,None,0,48)
    r['inferred_occluded_tip']=inferred;outer_report.append(r)
ctrl=bpy.data.objects['OuterLayer_CTRL'];ctrl['petal_count']=16;ctrl['spacing_degrees']=22.5;ctrl['phase_degrees']=0.
ctrl['spacing_note']='Individually fitted directions' if ROUND==3 else 'Uniform initial spacing'
ctrl['layer_drop_m']={1:.007,2:.005,3:.0045}[ROUND]
ctrl.update_tag()
for obj in outer:obj.parent.update_tag()
ctrl['validation']='Video sculpt only. Future Unity integration must recheck motion clearance.'
# Place a thin center and shallow stage, matching the video's low-angle relief.
disc=bpy.data.objects['CenterDisc']
for v,original in zip(disc.data.vertices,BASE['disc_vertices']):v.co=(original[0],original[1],.001+(original[2]-.002)*.30)
disc.data.update()
ground=bpy.data.objects['PREVIEW_Ground'];ground.location.z=0
for v in ground.data.vertices:v.co.z=-.008
ground.data.update()

stage=bpy.data.collections['03_Preview_Only'];SCENE.render.resolution_x=885;SCENE.render.resolution_y=584;SCENE.render.resolution_percentage=100
camera_names=[]
for key in ['15','5','9','2','0']:
    v=REFERENCE['views'][key];name=f'REVIEW_Video_{key}'
    camera=bpy.data.objects.get(name)
    if camera is None:
        data=bpy.data.cameras.new(name);camera=bpy.data.objects.new(name,data);stage.objects.link(camera)
    R=Matrix(v['camera_R_cv']);conversion=Matrix.Diagonal((1,-1,-1));rotation=R.transposed()@conversion
    camera.matrix_world=rotation.to_4x4();camera.location=v['camera_position_world']
    camera.data.lens=v['focal_px']*36/885;camera.data.sensor_width=36;camera.data.clip_start=.001;camera.data.clip_end=100
    camera['reference_file']=v['file'];camera['elevation_degrees']=v['elevation_degrees'];camera['fit_residual_px']=v['camera_plane_fit_rms_px']
    camera_names.append(name)

bpy.context.scene.frame_set(bpy.context.scene.frame_current);bpy.context.view_layer.update()
def boundary(obj):
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();counts={}
    for p in mesh.polygons:
        for a,b in zip(p.vertices,list(p.vertices[1:])+[p.vertices[0]]):
            edge=tuple(sorted((a,b)));counts[edge]=counts.get(edge,0)+1
    adjacency={}
    for (a,b),n in counts.items():
        if n==1:adjacency.setdefault(a,[]).append(b);adjacency.setdefault(b,[]).append(a)
    order=[next(iter(adjacency))];previous=None
    while True:
        current=order[-1];nxt=next(v for v in adjacency[current] if v!=previous)
        if nxt==order[0]:break
        order.append(nxt);previous=current
    pts=[ev.matrix_world@mesh.vertices[i].co for i in order];ev.to_mesh_clear();return pts
outlines={obj.name:boundary(obj) for obj in inner+outer}
projected={}
for key in ['15','5','9','2','0']:
    camera=bpy.data.objects[f'REVIEW_Video_{key}'];SCENE.camera=camera
    bpy.context.view_layer.update()
    projected[key]={}
    for name,pts in outlines.items():
        p=[world_to_camera_view(SCENE,camera,v) for v in pts]
        projected[key][name]=[[v.x*885+211,(1-v.y)*584+99] for v in p]
    SCENE.render.filepath=str(OUT/f'round-{ROUND}-view-{key}.png')
    bpy.ops.render.render(write_still=True)
report={'round':ROUND,'phase':'build','profile_fit_fraction':blend,'inner':inner_report,'outer':outer_report,
        'outer_count':16,'layer_drop_m':ctrl['layer_drop_m'],'shingle_degrees':shingle,
        'projected_outlines':projected,'camera_names':camera_names,'reference_uncertainty':'Approximate silhouettes from compressed video; no source model dimensions.'}
(OUT/f'round-{ROUND}-build.json').write_text(json.dumps(report,indent=2))
SCENE.camera=bpy.data.objects['REVIEW_Video_5']
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        sp=area.spaces.active
        if 'CAMERA' in [i.identifier for i in sp.region_3d.bl_rna.properties['view_perspective'].enum_items]:sp.region_3d.view_perspective='CAMERA'
        sp.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/f'round-{ROUND}.blend'))
print(json.dumps({'round':ROUND,'profile_fit':blend,'inner_count':len(inner),'outer_count':len(outer),'render_views':5,'saved':bpy.data.filepath},indent=2))
