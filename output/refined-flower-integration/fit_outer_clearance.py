"""Find the smallest fixed downward mounting offsets for the approved outer silhouettes."""
import importlib.util,json,pathlib,itertools
import numpy as np
HERE=pathlib.Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('clearance',HERE/'check_clearance.py');c=importlib.util.module_from_spec(spec);spec.loader.exec_module(c)
data=json.loads((HERE/'pose-snapshots.json').read_text());stats={}
for number,pose in enumerate(data['poses']):
    meshes=[c.geometry(d,v) for d,v in zip(data['meshDefinitions'],pose['geometry']) if d['layer']=='outer']
    for ia,ib in itertools.combinations(range(14),2):
        a,b=meshes[ia],meshes[ib]
        if np.any(a['lo'].min(0)>b['hi'].max(0)) or np.any(b['lo'].min(0)>a['hi'].max(0)):continue
        hits=np.all(a['lo'][:,None,:]<=b['hi'][None,:,:],2)&np.all(a['hi'][:,None,:]>=b['lo'][None,:,:],2)
        lo=float('inf');hi=-float('inf');total=0;count=0
        for i,j in zip(*np.nonzero(hits)):
            pts=c.clip(a['tri'][i,:,:2],b['tri'][j,:,:2])
            if len(pts)<3:continue
            area=abs(sum(pts[k][0]*pts[(k+1)%len(pts)][1]-pts[(k+1)%len(pts)][0]*pts[k][1] for k in range(len(pts))))
            if area<1e-14:continue
            coef=b['plane'][j]-a['plane'][i];constant=b['constant'][j]-a['constant'][i]
            ds=[coef[0]*x+coef[1]*y+constant for x,y in pts]
            lo=min(lo,min(ds));hi=max(hi,max(ds));total+=sum(ds)/len(ds)*area;count+=area
        if not count:continue
        key=(ia,ib)
        if key not in stats:stats[key]=dict(lo=lo,hi=hi,sign=1 if total>0 else -1,names=[a['name'],b['name']])
        stats[key]['lo']=min(stats[key]['lo'],lo);stats[key]['hi']=max(stats[key]['hi'],hi)
    if (number+1)%16==0:print('Outer envelope',number+1,flush=True)
A=[];B=[];margin=.0002
for (a,b),s in stats.items():
    row=np.zeros(14)
    if s['sign']>0:row[a]=1;row[b]=-1;limit=s['lo']-margin
    else:row[b]=1;row[a]=-1;limit=-s['hi']-margin
    A.append(row);B.append(limit)
offsets=np.zeros(14);changed=False
# Difference constraints: the shortest paths from zero give the componentwise
# highest feasible mounting heights, hence the minimum downward adjustment.
for iteration in range(15):
    changed=False
    for row,limit in zip(A,B):
        a=int(np.argmax(row));b=int(np.argmin(row))
        if offsets[a]>offsets[b]+limit+1e-10:offsets[a]=offsets[b]+limit;changed=True
    if not changed:break
success=not changed and min(offsets)>-.015
result=dict(success=success,message='Solved fixed downward offsets' if success else 'Incompatible height constraints',margin_world_m=margin,pairs=[dict(indices=list(k),**v) for k,v in stats.items()])
if success:
    result['offsets_flower_m']={m['name']:float(v/1.68608963) for m,v in zip(meshes,offsets)}
(HERE/'outer-mount-offsets.json').write_text(json.dumps(result,indent=2))
print(json.dumps({k:v for k,v in result.items() if k!='pairs'},indent=2))
