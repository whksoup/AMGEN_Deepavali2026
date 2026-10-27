"""Measure exact vertical separation over intersecting projected triangle polygons.

Run in Blender via MCP after build_outer_petal_layer.py. No animation is retained.
This checks synchronized poses, not arbitrary future independent animation.
"""
import bpy, math, json, itertools, time
import numpy as np
from pathlib import Path

OUT=Path(r"F:\Freelance_Work\amgen-LEDWall\Amgen Interactive Wall\output\flower-petal-prototype")
master=bpy.data.objects['Petal_01_Rig']
outer=[bpy.data.objects[f'OuterPetal_{i:02d}'] for i in range(1,16)]
top=[bpy.data.objects['InnerPetal_01']]+[bpy.data.objects[f'REF_TopPetal_{i:02d}'] for i in range(2,9)]
saved=[tuple(b.rotation_euler) for b in master.pose.bones]

def evaluated(obj,dg):
    ev=obj.evaluated_get(dg)
    me=ev.to_mesh()
    me.calc_loop_triangles()
    xyz=np.array([ev.matrix_world@v.co for v in me.vertices],dtype=float)
    indices=np.array([t.vertices[:] for t in me.loop_triangles])
    tri=xyz[indices]
    ev.to_mesh_clear()
    edge1,edge2=tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]
    normals=np.cross(edge1,edge2)
    tri=tri[np.abs(normals[:,2])>1e-12]
    normals=normals[np.abs(normals[:,2])>1e-12]
    plane=-normals[:,:2]/normals[:,2,None]
    constant=tri[:,0,2]-(plane*tri[:,0,:2]).sum(axis=1)
    return {'name':obj.name,'xyz':xyz,'tri':tri,'lo':tri[:,:,:2].min(axis=1),'hi':tri[:,:,:2].max(axis=1),'plane':plane,'constant':constant}

def clip(subject,triangle):
    # Both projected triangles are CCW; reverse when deformation changes winding.
    a,b,c=triangle
    cross=(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    if cross<0: triangle=triangle[::-1]
    points=[(p[0],p[1]) for p in subject]
    for i in range(3):
        a,b=triangle[i],triangle[(i+1)%3]
        dx,dy=b[0]-a[0],b[1]-a[1]
        if not points: return []
        output=[]
        p=points[-1]
        pd=dx*(p[1]-a[1])-dy*(p[0]-a[0])
        for q in points:
            qd=dx*(q[1]-a[1])-dy*(q[0]-a[0])
            if (pd>=0)!=(qd>=0):
                t=pd/(pd-qd)
                output.append((p[0]+t*(q[0]-p[0]),p[1]+t*(q[1]-p[1])))
            if qd>=0: output.append(q)
            p,pd=q,qd
        points=output
    return points

def pair(a,b,ordered=False):
    # B is the upper surface for ordered comparisons.
    if np.any(a['lo'].min(axis=0)>b['hi'].max(axis=0)) or np.any(b['lo'].min(axis=0)>a['hi'].max(axis=0)):
        return None
    hits=np.all(a['lo'][:,None,:]<=b['hi'][None,:,:],axis=2)&np.all(a['hi'][:,None,:]>=b['lo'][None,:,:],axis=2)
    ai,bi=np.nonzero(hits)
    min_gap=float('inf')
    crossings=0
    count=0
    for i,j in zip(ai,bi):
        points=clip(a['tri'][i,:,:2],b['tri'][j,:,:2])
        if len(points)<3: continue
        area=abs(sum(points[k][0]*points[(k+1)%len(points)][1]-points[(k+1)%len(points)][0]*points[k][1] for k in range(len(points))))
        if area<1e-14: continue
        coefficients=b['plane'][j]-a['plane'][i]
        constant=b['constant'][j]-a['constant'][i]
        ds=[coefficients[0]*x+coefficients[1]*y+constant for x,y in points]
        lo,hi=min(ds),max(ds)
        count+=1
        if lo<=1e-8 and hi>=-1e-8: crossings+=1
        gap=lo if ordered else (0 if lo<=0<=hi else min(abs(lo),abs(hi)))
        min_gap=min(min_gap,gap)
    if count==0: return None
    return {'min_gap_m':min_gap,'crossing_triangle_pairs':crossings,'overlap_triangle_pairs':count,'pair':[a['name'],b['name']]}

poses=[('rest',[0,0,0],[0,0,0])]
for pulse in (0,1):
    for q in range(8):
        phase=q*math.pi/4
        lift=[(1.5,2,2)[k]+(1,1.4,1.8)[k]*math.sin(phase-k*.35)+pulse*(5,9,8)[k] for k in range(3)]
        sway=[(.8,1.2,1.5)[k]*math.sin(phase*.67-k*.45) for k in range(3)]
        poses.append((f'{"pulse" if pulse else "breeze"}_{q}',lift,sway))
for q,lift in enumerate(itertools.product(*[(.5,7.5),(.6,12.4),(.2,11.8)])):
    poses.append((f'envelope_corner_{q}',list(lift),[.8,-1.2,1.5]))
poses.extend([('negative_bend_margin',[-4,-6,-6],[-.8,1.2,-1.5]),('maximum_positive_sway',[7.5,12.4,11.8],[.8,1.2,1.5]),('maximum_negative_sway',[7.5,12.4,11.8],[-.8,-1.2,-1.5])])
health=[]
for obj in outer:
    obj.data.calc_loop_triangles()
    health.append({'mesh':obj.name,'vertices':len(obj.data.vertices),'faces':len(obj.data.polygons),'triangles':len(obj.data.loop_triangles),'max_weights':max(len(v.groups) for v in obj.data.vertices),'max_weight_sum_error':max(abs(sum(g.weight for g in v.groups)-1) for v in obj.data.vertices),'min_face_normal_z':min(p.normal.z for p in obj.data.polygons),'zero_area_faces':sum(p.area<1e-12 for p in obj.data.polygons),'uv_layers':[u.name for u in obj.data.uv_layers]})

def run_validation(start=0,stop=None):
    stop=len(poses) if stop is None else stop
    results=[]
    try:
        for name,lifts,sways in poses[start:stop]:
            for k,b in enumerate(master.pose.bones):
                b.rotation_euler=(math.radians(lifts[k]),0,math.radians(sways[k]))
            master.update_tag()
            bpy.context.view_layer.update()
            dg=bpy.context.evaluated_depsgraph_get()
            for obj in outer:
                rig=obj.parent
                assert all(fc.driver.is_valid for fc in rig.animation_data.drivers), rig.name+' invalid driver'
                for k,b in enumerate(rig.evaluated_get(dg).pose.bones):
                    expected=math.radians(lifts[k])*(.72 if lifts[k]>=0 else 1.35)
                    assert abs(b.rotation_euler.x-expected)<1e-6, rig.name+' lift driver mismatch'
                    assert abs(b.rotation_euler.z-math.radians(sways[k])*.5)<1e-6, str((name,rig.name,k,'sway driver mismatch',b.rotation_euler.z,math.radians(sways[k])*.5))
            lower=[evaluated(obj,dg) for obj in outer]
            upper=[evaluated(obj,dg) for obj in top]
            center=evaluated(bpy.data.objects['CenterDisc'],dg)
            vertical=[p for a in lower for b in upper+[center] if (p:=pair(a,b,True))]
            neighboring=[p for a,b in itertools.combinations(lower,2) if (p:=pair(a,b,False))]
            pinned=max(float(np.linalg.norm(a['xyz'][0]-np.array(outer[i].parent.matrix_world.translation))) for i,a in enumerate(lower))
            result={'pose':name,'source_lift_degrees':list(lifts),'source_sway_degrees':list(sways),'minimum_lower_to_top_m':min(p['min_gap_m'] for p in vertical),'minimum_neighbor_m':min(p['min_gap_m'] for p in neighboring),'top_crossing_triangle_pairs':sum(p['crossing_triangle_pairs'] for p in vertical),'neighbor_crossing_triangle_pairs':sum(p['crossing_triangle_pairs'] for p in neighboring),'worst_top_pair':min(vertical,key=lambda p:p['min_gap_m'])['pair'],'worst_neighbor_pair':min(neighboring,key=lambda p:p['min_gap_m'])['pair'],'max_root_drift_m':pinned}
            results.append(result)
    finally:
        for b,r in zip(master.pose.bones,saved): b.rotation_euler=r
        master.update_tag()
        bpy.context.view_layer.update()
    path=OUT/f'outer-layer-validation-{start:02d}-{stop:02d}.json'
    path.write_text(json.dumps({'mesh_health':health,'poses':results},indent=2))
    print(json.dumps({'pose_count':len(results),'minimum_lower_to_top_mm':1000*min(r['minimum_lower_to_top_m'] for r in results),'minimum_neighbor_mm':1000*min(r['minimum_neighbor_m'] for r in results),'top_crossings':sum(r['top_crossing_triangle_pairs'] for r in results),'neighbor_crossings':sum(r['neighbor_crossing_triangle_pairs'] for r in results),'max_root_drift_m':max(r['max_root_drift_m'] for r in results),'report':str(path)},indent=2))
    return results

print('Validator ready:',len(poses),'poses. Call run_validation(start, stop) for batches.')
