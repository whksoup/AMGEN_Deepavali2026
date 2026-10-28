"""Develop the supplied guide into eight staged masks; never writes active assets."""
from pathlib import Path
import collections
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from guide_curves import CONTOURS, CX, CY, RADIUS, TIP, ROOT as GUIDE_ROOT, outer_half_width

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
DEST = HERE / 'staged/Decorated Masks'
DEST.mkdir(parents=True, exist_ok=True)
source = json.loads((ROOT/'output/refined-flower-integration/source-manifest.json').read_text())
layout = json.loads((HERE/'before/layout.json').read_text())
W,H,SS = 448,640,3
fills = ['#B51F4B','#B7770A','#264BB2','#47871D','#C51683','#12748F','#918011','#6A239C']
sheet = Image.new('RGB',(1600,1100),'#17131c')
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',19)
report=[]

def smooth_mapping(query, knots, values):
    """Monotone cubic mapping keeps the supplied curves smooth at circle anchors."""
    knots=np.array(knots);values=np.array(values)
    h=np.diff(knots);delta=np.diff(values)/h
    slope=np.empty(len(values));slope[0]=delta[0];slope[-1]=delta[-1]
    w1=2*h[1:]+h[:-1];w2=h[1:]+2*h[:-1]
    slope[1:-1]=(w1+w2)/(w1/delta[:-1]+w2/delta[1:])
    query=np.clip(query,knots[0],knots[-1])
    k=np.clip(np.searchsorted(knots,query)-1,0,len(knots)-2)
    t=(query-knots[k])/h[k]
    return (2*t**3-3*t*t+1)*values[k]+(t**3-2*t*t+t)*h[k]*slope[k]+(-2*t**3+3*t*t)*values[k+1]+(t**3-t*t)*h[k]*slope[k+1]

def perimeter(mesh):
    points=[]; lookup={}; remap=[]
    for p in mesh['uv']:
        key=tuple(np.round(p,5))
        if key not in lookup: lookup[key]=len(points);points.append(p)
        remap.append(lookup[key])
    triangles=np.array(remap)[np.array(mesh['triangles']).reshape(-1,3)]
    count=collections.Counter(tuple(sorted((a,b))) for t in triangles for a,b in zip(t,np.roll(t,-1)) if a!=b)
    adjacency=collections.defaultdict(list)
    for (a,b),n in count.items():
        if n==1: adjacency[a].append(b);adjacency[b].append(a)
    start=next(iter(adjacency)); previous=-1; current=start; chain=[]
    while True:
        chain.append(current)
        nxt=next(n for n in adjacency[current] if n!=previous)
        previous,current=current,nxt
        if current==start: break
        assert len(chain)<=len(points)
    return np.array(points)[chain]

for i,(mesh,item) in enumerate(zip(source['meshes'][:8],layout)):
    width,length=item['width'],item['length']
    cx,cy=np.array(item['center_uv'])*[width,length]
    radius=item['radius_m']; dotted=item['dotted_radius_m']
    boundary=perimeter(mesh)*[width,length]
    def limits(ys):
        lo=np.full(np.shape(ys),np.inf);hi=np.full(np.shape(ys),-np.inf)
        for a,b in zip(boundary,np.roll(boundary,-1,axis=0)):
            if abs(b[1]-a[1])<1e-10:continue
            mask=(ys>=min(a[1],b[1]))&(ys<=max(a[1],b[1]))
            xx=a[0]+(ys-a[1])/(b[1]-a[1])*(b[0]-a[0])
            lo[mask]=np.minimum(lo[mask],xx[mask]);hi[mask]=np.maximum(hi[mask],xx[mask])
        return lo,hi
    def map_curve(curve):
        sy=curve[:,1]
        yy=smooth_mapping(sy,[TIP,CY-RADIUS,CY,CY+RADIUS,GUIDE_ROOT],
                          [length,cy+radius,cy,cy-radius,0])
        yy=np.clip(yy,.0003,length-.0003)
        lo,hi=limits(yy)
        q=(curve[:,0]-CX)/np.maximum(outer_half_width(sy),1)
        xx=(lo+hi)/2+q*(hi-lo)/2
        return np.column_stack([xx,yy])
    def pixels(points):
        return [tuple(p) for p in np.column_stack([points[:,0]/width*W*SS,(1-points[:,1]/length)*H*SS])]
    def line(mask,points,stroke):
        ImageDraw.Draw(mask).line(pixels(points),fill=255,width=max(1,round(stroke*.5*(W*SS/width+H*SS/length))),joint='curve')

    gold=Image.new('L',(W*SS,H*SS),0)
    curves=[map_curve(c) for c in CONTOURS]
    # The two supplied contours close independently below the medallion.
    # Their staggered tips create the pointed crown shown in the source guide.
    line(gold,curves[0],.00165)
    line(gold,curves[1],.00085)
    # Taper the upper outer crown with a small warm-gold leaf accent.
    crown_y=(curves[0][:,1].max()+curves[1][:,1].max())/2
    lo,hi=limits(np.array([crown_y])); crown_x=float((lo[0]+hi[0])/2)
    ry=min(.0050,(curves[0][:,1].max()-curves[1][:,1].max())*.20)
    rx=min(.0022,float(hi[0]-lo[0])*.09)
    pen=ImageDraw.Draw(gold)
    diamond=np.array([[crown_x,crown_y+ry],[crown_x+rx,crown_y],[crown_x,crown_y-ry],[crown_x-rx,crown_y]])
    pen.polygon(pixels(diamond),fill=255)
    for sign in [-1,1]:
        leaf_x=crown_x+sign*rx*2.6; leaf_y=crown_y-ry*.35
        leaf=np.array([[leaf_x-sign*rx*.55,leaf_y+ry*.48],[leaf_x+sign*rx*.6,leaf_y],
                       [leaf_x+sign*rx*.45,leaf_y-ry*.35],[leaf_x-sign*rx*.55,leaf_y+ry*.05]])
        pen.polygon(pixels(leaf),fill=255)

    shape=Image.new('L',gold.size,0);ImageDraw.Draw(shape).polygon(pixels(boundary),fill=255)
    gold=np.array(gold.resize((W,H),Image.Resampling.LANCZOS))
    shape=np.array(shape.resize((W,H),Image.Resampling.LANCZOS))
    x,y=np.meshgrid((np.arange(W)+.5)/W*width,(1-(np.arange(H)+.5)/H)*length)
    radial=np.hypot(x-cx,y-cy)
    protected=radial<=dotted+.0025
    old=np.array(Image.open(HERE/f'before/Rootward Inner/Decorated Masks/{i:02d}-{item["label"]}.png'))
    gold=np.uint8(gold.astype(float)*shape/255)
    gold[protected]=old[:,:,0][protected]
    rgba=old.copy();rgba[:,:,0]=gold
    Image.fromarray(rgba).save(DEST/f'{i:02d}-{item["label"]}.png')
    preview=Image.new('RGB',(W,H),'#17131c')
    preview.paste(Image.new('RGB',(W,H),fills[i]),(0,0),Image.fromarray(shape))
    preview.paste(Image.new('RGB',(W,H),'#F8CF78'),(0,0),Image.fromarray(gold))
    preview=preview.resize((round(430*width/length),430),Image.Resampling.LANCZOS)
    sx,sy=i%4*400+(400-preview.width)//2,i//4*550+15
    sheet.paste(preview,(sx,sy));draw.text((i%4*400+35,sy+448),f'{i+1:02d}  {item["label"]}',font=font,fill='#f6d497')
    draw.text((i%4*400+35,sy+480),'Nested gold teardrop contours',font=font,fill='#baa797')
    changed=np.any(old!=rgba,axis=2)
    distance=min(float(np.min(np.hypot(c[:,0]-cx,c[:,1]-cy))) for c in curves)
    report.append(dict(index=i,name=item['name'],label=item['label'],glyph_and_medallion_unchanged=bool(np.array_equal(old[protected],rgba[protected])),
                       changed_pixels=int(changed.sum()),guide_circle_clearance_mm=(distance-dotted)*1000,
                       outer_crown_v=float(curves[0][:,1].max()/length),inner_crown_v=float(curves[1][:,1].max()/length)))

sheet.save(HERE/'artwork-review.png')
(HERE/'pattern-validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
