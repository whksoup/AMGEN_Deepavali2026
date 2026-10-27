"""Validate actual Unity BakeMesh snapshots using the exact projected-triangle checker."""
import ast
import itertools
import json
import multiprocessing
from pathlib import Path
import numpy as np

HERE=Path(__file__).resolve().parent
source=ast.parse((HERE/'validate_outer_petal_layer.py').read_text(encoding='utf-8'))
functions=[node for node in source.body if isinstance(node,ast.FunctionDef) and node.name in ('clip','pair')]
exec(compile(ast.Module(body=functions,type_ignores=[]),'triangle_clearance','exec'))

def geometry(definition, flat):
    xyz=np.asarray(flat,dtype=float).reshape(-1,3)
    tri=xyz[np.array(definition['triangles']).reshape(-1,3)]
    normal=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])
    keep=np.abs(normal[:,2])>1e-12
    tri,normal=tri[keep],normal[keep]
    plane=-normal[:,:2]/normal[:,2,None]
    constant=tri[:,0,2]-(plane*tri[:,0,:2]).sum(axis=1)
    return {'name':definition['name'],'tri':tri,'lo':tri[:,:,:2].min(axis=1),'hi':tri[:,:,:2].max(axis=1),'plane':plane,'constant':constant,'layer':definition['layer']}

def check(task):
    definitions,pose=task
    meshes=[geometry(d,v) for d,v in zip(definitions,pose['geometry'])]
    outer=[m for m in meshes if m['layer']=='outer']
    top=[m for m in meshes if m['layer']=='top']
    vertical=[p for a in outer for b in top if (p:=pair(a,b,True))]
    neighbors=[p for a,b in itertools.combinations(outer,2) if (p:=pair(a,b,False))]
    return {'pose':pose['name'],'minimum_top_gap_mm':1000*min(p['min_gap_m'] for p in vertical),'minimum_neighbor_gap_mm':1000*min(p['min_gap_m'] for p in neighbors),'top_crossing_triangle_pairs':sum(p['crossing_triangle_pairs'] for p in vertical),'neighbor_crossing_triangle_pairs':sum(p['crossing_triangle_pairs'] for p in neighbors),'worst_top_pair':min(vertical,key=lambda p:p['min_gap_m'])['pair']}

if __name__=='__main__':
    data=json.loads((HERE/'unity-outer-pose-snapshots.json').read_text())
    results=[]
    with multiprocessing.Pool(4) as pool:
        for result in pool.imap(check,[(data['meshDefinitions'],p) for p in data['poses']]):
            results.append(result)
            if len(results)%10==0: print(f'Checked {len(results)}/{len(data["poses"])} Unity poses',flush=True)
    summary={'poses':len(results),'minimum_top_gap_mm':min(p['minimum_top_gap_mm'] for p in results),'minimum_neighbor_gap_mm':min(p['minimum_neighbor_gap_mm'] for p in results),'top_crossings':sum(p['top_crossing_triangle_pairs'] for p in results),'neighbor_crossings':sum(p['neighbor_crossing_triangle_pairs'] for p in results),**data['checks']}
    summary['passed']=summary['top_crossings']==summary['neighbor_crossings']==0 and summary['minimum_top_gap_mm']>0 and summary['minimum_neighbor_gap_mm']>0 and summary['maxRootDrift']<1e-6 and summary['maxBoundsOverflow']<1e-6 and summary['maxSyncError']<1e-5
    (HERE/'unity-outer-clearance-validation.json').write_text(json.dumps({'summary':summary,'method':'Exact plane separation over clipped XY-overlap triangles from imported Unity BakeMesh world positions; 40 discrete pose tests, including independent upper phases and pulses.','poses':results},indent=2))
    print(json.dumps(summary,indent=2),flush=True)
    raise SystemExit(0 if summary['passed'] else 1)
