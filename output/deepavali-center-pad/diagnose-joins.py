import json
import numpy as np
from check_clearance import geometry,clip,HERE

data=json.loads((HERE/'pose-snapshots.json').read_text(encoding='utf-8-sig'))
meshes=[geometry(d,v) for d,v in zip(data['meshDefinitions'],data['poses'][0]['geometry'])]
points=[]
for a in [m for m in meshes if m['layer']=='outer']:
 for b in [m for m in meshes if m['layer']=='top']:
  hits=np.all(a['lo'][:,None,:]<=b['hi'][None,:,:],axis=2)&np.all(a['hi'][:,None,:]>=b['lo'][None,:,:],axis=2)
  for i,j in zip(*np.nonzero(hits)):
   polygon=clip(a['tri'][i,:,:2],b['tri'][j,:,:2])
   if len(polygon)<3:continue
   coeff=b['plane'][j]-a['plane'][i];const=b['constant'][j]-a['constant'][i]
   ds=np.array([coeff@p+const for p in polygon])
   if min(ds)>0 or max(ds)<0:continue
   for k in range(len(polygon)):
    if ds[k]*ds[k-1]<0:
     p=np.array(polygon[k-1])+(np.array(polygon[k])-polygon[k-1])*ds[k-1]/(ds[k-1]-ds[k])
     h=a['plane'][i]@p+a['constant'][i]
     r=np.linalg.norm(p/np.array([2.19191647,1.68608963]))
     points.append([float(p[0]),float(p[1]),float(h),float(r)])
p=np.array(points)
print(json.dumps({'count':len(p),'min':p.min(axis=0).tolist(),'max':p.max(axis=0).tolist(),'outside_pad':int(sum(p[:,3]>.119)),'above_pad':int(sum(p[:,2]>-.063)),'examples_outside':p[p[:,3]>.119][:20].tolist()},indent=2))
