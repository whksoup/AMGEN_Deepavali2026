"""Refit existing authored artwork and rebuild physical outline lighting coordinates."""
import json, pathlib, collections
import numpy as np
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[2]
HERE=pathlib.Path(__file__).resolve().parent
OUT=HERE/'staged/Refitted Maps'
OUT.mkdir(parents=True,exist_ok=True)
source=json.loads((HERE/'staged/source-manifest.json').read_text())
old=json.loads((ROOT/'output/refined-flower-integration/old-uv-profiles.json').read_text())
N=512
v,u=np.mgrid[0:N,0:N].astype(float); u=(u+.5)/N; v=(v+.5)/N
def boundary(uv,tris):
    uv=np.asarray(uv); canonical={}; remap=[]; pts=[]
    for p in uv:
        key=tuple(np.round(p,5))
        if key not in canonical: canonical[key]=len(pts);pts.append(p)
        remap.append(canonical[key])
    tris=np.array(remap)[np.asarray(tris).reshape(-1,3)]
    edges=collections.Counter(tuple(sorted((a,b))) for t in tris for a,b in zip(t,np.roll(t,-1)) if a!=b)
    pts=np.array(pts); edges=[k for k,n in edges.items() if n==1]
    return pts,edges
def widths(pts,edges):
    yy=(np.arange(N)+.5)/N; lo=np.ones(N);hi=np.zeros(N)
    for a,b in edges:
        a,b=pts[a],pts[b]
        if abs(a[1]-b[1])<1e-8: continue
        mask=(yy>=min(a[1],b[1]))&(yy<=max(a[1],b[1]))
        x=a[0]+(yy-a[1])/(b[1]-a[1])*(b[0]-a[0]);lo[mask]=np.minimum(lo[mask],x[mask]);hi[mask]=np.maximum(hi[mask],x[mask])
    return lo[:,None],hi[:,None]
oldbounds=[widths(*boundary(m['uv'],m['triangles'])) for m in old]
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def save(name,a):
    Image.fromarray(np.uint8(np.clip(a,0,1)*255+.5)).transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(OUT/name)
report=[]
for index,m in enumerate(source['meshes'][:8]):
    name=m['name']; pts,edges=boundary(m['uv'],m['triangles']);lo,hi=widths(pts,edges)
    oldlo,oldhi=oldbounds[0 if index<8 else 1]
    fitu=np.clip(oldlo+(u-lo)/np.maximum(hi-lo,1e-5)*(oldhi-oldlo),0,1)
    def encode(q):
        h=np.floor(q*255)/255;return h,q*255-np.floor(q*255)
    uh,ul=encode(fitu);vh,vl=encode(v)
    save(name+' Fit.png',np.stack([uh,ul,vh,vl],-1))
    width=m['max'][0]-m['min'][0];length=m['max'][1]-m['min'][1];metric=np.array([width/length,1])
    # Trace both sides from the lowest boundary vertex to the single tip.
    adj=collections.defaultdict(list)
    for a,b in edges: adj[a].append(b);adj[b].append(a)
    assert all(len(n)==2 for n in adj.values())
    start=min(adj,key=lambda k:pts[k,1]);tip=max(adj,key=lambda k:pts[k,1]);segments=[]
    for side,first in enumerate(adj[start]):
        chain=[start];prev=start;cur=first
        while cur!=tip:
            chain.append(cur);nxt=next(n for n in adj[cur] if n!=prev);prev,cur=cur,nxt
            assert len(chain)<len(pts)+1
        chain.append(tip);xy=pts[chain]*metric;dist=np.linalg.norm(np.diff(xy,axis=0),axis=1);path=np.r_[0,np.cumsum(dist)]/sum(dist)
        segments.extend((a,b,p,q,side) for a,b,p,q in zip(xy[:-1],xy[1:],path[:-1],path[1:]))
    dist=np.full((N,N),np.inf);path=np.zeros((N,N));side=np.zeros((N,N));px=u*metric[0];py=v
    for a,b,p,q,s in segments:
        ab=b-a;t=np.clip(((px-a[0])*ab[0]+(py-a[1])*ab[1])/np.dot(ab,ab),0,1)
        d=(px-a[0]-ab[0]*t)**2+(py-a[1]-ab[1]*t)**2;mask=d<dist
        dist[mask]=d[mask];path[mask]=(p+(q-p)*t)[mask];side[mask]=s
    dist=np.sqrt(dist);noise=np.clip(.5+.25*np.sin(path*53+side*7+index*1.7)+.18*np.sin(path*97+index),0,1)
    arrival=np.clip(.05+.82*path+.13*np.clip(dist/.08,0,1),0,1)
    save(name+' Light.png',np.stack([np.clip(dist/.15,0,1),path,noise,arrival],-1))
    d=dist*length;shoulder=1-smooth(.004,.070,d);rootshade=.42+.58*smooth(.07,.62,v);cross=.42+.58*smooth(.06,.94,u)
    basin=1-smooth(.18,.88,np.sqrt(((u-.48)*2.1)**2+((v-.57)*1.9)**2))
    shade=(.18+.67*shoulder)*rootshade*cross*(1-.38*basin)
    recess=(1-smooth(.005,.018,np.abs(d-.013)))*smooth(.001,.005,d)
    ao=(1-.34*recess)*(.55+.45*smooth(.08,.48,v))
    save(name+' Body.png',np.stack([shade,ao,np.zeros_like(u),np.ones_like(u)],-1))
    report.append(dict(name=name,width=width,length=length,continuous_pattern=True,boundary_edges=len(edges),fitted_pixels=int(np.sum((u>=lo)&(u<=hi)))))
(HERE/'artwork-refit.json').write_text(json.dumps(report,indent=2))
print(f'Refitted {len(report)} continuous artwork maps; regenerated {len(report)} body and lighting maps.')
