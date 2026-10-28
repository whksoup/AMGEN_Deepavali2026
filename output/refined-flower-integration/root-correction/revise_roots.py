"""Remove the occlusion-fitted necks; keep the eight tips, rigs and outer ring unchanged."""
import bpy,json,math,pathlib
from mathutils import Vector
ROOT=pathlib.Path(r'F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall')
HERE=ROOT/'output/refined-flower-integration/root-correction'
baseline=json.loads((HERE/'before-source-manifest.json').read_text())
report=[]
for item in baseline['meshes'][:8]:
    obj=bpy.data.objects[item['name']];me=obj.data
    original=[Vector(v) for v in item['vertices']]
    length=item['max'][1];width=item['max'][0]-item['min'][0]
    rows={}
    for i,v in enumerate(original):rows.setdefault(round(v.y/length*56),[]).append(i)
    shoulder=rows[28];sl=min(original[i].x for i in shoulder);sr=max(original[i].x for i in shoulder)
    base_half=(sr-sl)*.72*.5
    for row,indices in rows.items():
        t=row/56
        lo=min(original[i].x for i in indices);hi=max(original[i].x for i in indices)
        alpha=min(1,t/.5)
        left=-base_half+(sl+base_half)*alpha
        right=base_half+(sr-base_half)*alpha
        # Straight flanks below the bowl; smoothly retain the authored shoulder above 40%.
        blend=max(0,min(1,(t-.4)/.12));blend=blend*blend*(3-2*blend)
        if t>=.52: left,right=lo,hi
        else:left=left*(1-blend)+lo*blend;right=right*(1-blend)+hi*blend
        for i in indices:
            old=original[i];u=(old.x-lo)/(hi-lo) if hi-lo>1e-8 else .5
            x=left+(right-left)*u if len(indices)>1 else old.x
            # The broad root is a plane. Preserve the existing upper relief and shingle.
            plane=math.tan(math.radians(.45))*x
            flat=max(0,min(1,(t-.22)/.28));flat=flat*flat*(3-2*flat)
            relief=old.z-math.tan(math.radians(.45))*old.x
            me.vertices[i].co=(x,old.y,plane+relief*flat) if t<.52 else old
    xmin=min(v.co.x for v in me.vertices);xmax=max(v.co.x for v in me.vertices)
    for loop in me.loops:
        v=me.vertices[loop.vertex_index].co
        me.uv_layers['ArtworkUV'].data[loop.index].uv=((v.x-xmin)/(xmax-xmin),v.y/length)
    me.update();obj['root_revision']='2026-10-28: continuous broad trapezoidal root from Petal2.svg and original Image 1; no occluded-neck fit'
    report.append(dict(name=obj.name,base_width_mm=2*base_half*1000,shoulder_width_mm=(sr-sl)*1000,root_fraction=.72,unchanged_from_length_fraction=.52,root_camber_mm=0,tip_unchanged=all((me.vertices[i].co-original[i]).length<1e-8 for i in rows[56])))
(HERE/'root-shape-change.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'output/flower-petal-prototype/Deepavali_InnerPetal_VideoFit.blend'))
print(json.dumps(report,indent=2))
