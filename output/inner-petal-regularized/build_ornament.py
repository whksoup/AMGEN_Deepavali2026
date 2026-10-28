"""Deterministic vector ornament, rendered to the existing shader's RGBA mask.
Writes only staged files and review evidence. Mirrors decorative geometry;
preserves the eight established glyph source identities and palette roles.
"""
from pathlib import Path
import sys,json,math
import numpy as np
from PIL import Image,ImageDraw,ImageChops,ImageFont
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/'output/inner-petal-pattern-guides'))
from guide_curves import CONTOURS,CX,CY,RADIUS,TIP,ROOT as GUIDE_ROOT,outer_half_width
BASE=ROOT/'Assets/HK_Signature/Deepavali/PetalPrototype'
SOURCE=json.loads((HERE/'staged/source-manifest.json').read_text())
OLD=json.loads((HERE/'before/output/inner-petal-artwork/layout.json').read_text())
DEST=HERE/'staged/Decorated Masks';DEST.mkdir(exist_ok=True)
W=H=640;SS=2
COLORS=['#b51f4b','#b7770a','#264bb2','#47871d','#c51683','#12748f','#918011','#6a239c']
IDS=[3,4,2,7,5,6,1,0]
sheet=Image.new('RGB',(1760,1250),'#17131c');draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',21)
manifest=[]

def monotone(query,knots,values):
    knots=np.array(knots);values=np.array(values);h=np.diff(knots);delta=np.diff(values)/h
    slope=np.empty(len(values));slope[0]=delta[0];slope[-1]=delta[-1]
    w1=2*h[1:]+h[:-1];w2=h[1:]+2*h[:-1]
    slope[1:-1]=(w1+w2)/(w1/delta[:-1]+w2/delta[1:])
    query=np.clip(query,knots[0],knots[-1]);k=np.clip(np.searchsorted(knots,query)-1,0,len(knots)-2);t=(query-knots[k])/h[k]
    return (2*t**3-3*t*t+1)*values[k]+(t**3-2*t*t+t)*h[k]*slope[k]+(-2*t**3+3*t*t)*values[k+1]+(t**3-t*t)*h[k]*slope[k+1]

for i,mesh in enumerate(SOURCE['meshes'][:8]):
    v=np.array(mesh['vertices']);length=v[:,1].max();width=np.ptp(v[:,0]);rows=np.rint(v[:,1]/length*56).astype(int)
    ts=np.arange(57)/56;half=np.array([v[rows==j,0].max() for j in range(57)])
    cy=OLD[i]['center_uv'][1]*length;cx=0
    local=2*np.interp(cy/length,ts,half);radius=min(.052,local*.295);dotted=radius+.0052
    gold=Image.new('L',(W*SS,H*SS),0);pen=ImageDraw.Draw(gold)
    def px(points):
        p=np.asarray(points);return [tuple(a) for a in np.c_[(.5+p[:,0]/width)*W*SS,(1-p[:,1]/length)*H*SS]]
    def line(points,w=.00065,value=255):
        pen.line(px(points),fill=value,width=max(1,round(w*.5*(W*SS/width+H*SS/length))),joint='curve')
    def dot(x,y,r,value=255):
        p=px([[x-r,y+r],[x+r,y-r]]);pen.ellipse(tuple(p[0]+p[1]),fill=value)
    def bez(p0,p1,p2,p3):
        q=np.linspace(0,1,60)[:,None];return (1-q)**3*np.array(p0)+3*(1-q)**2*q*np.array(p1)+3*(1-q)*q*q*np.array(p2)+q**3*np.array(p3)
    def mapped(curve):
        sy=curve[:,1]
        yy=monotone(sy,[TIP,CY-RADIUS,CY,CY+RADIUS,GUIDE_ROOT],[length,cy+radius,cy,cy-radius,0])
        yy=np.clip(yy,.0003,length-.0003)
        q=(curve[:,0]-CX)/np.maximum(outer_half_width(sy),1)
        return np.c_[q*np.interp(yy/length,ts,half),yy]
    curves=[mapped(c) for c in CONTOURS]
    # Layered chased gold rim: broad lower-valued band with two bright rails.
    outer=curves[0]
    line(outer,.0033,155)
    line(outer,.0010,255)
    inset=outer.copy();inset[:,0]*=.958;inset[:,1]=cy+(inset[:,1]-cy)*.980
    line(inset,.00060,236)
    inner=curves[1];line(inner,.00068,235)
    # Continuous small beads inside the outer rim; equal arc spacing.
    bead=inset.copy();bead[:,0]*=.958;bead[:,1]=cy+(bead[:,1]-cy)*.982
    dist=np.r_[0,np.cumsum(np.linalg.norm(np.diff(bead,axis=0),axis=1))]
    for at in np.arange(.004,dist[-1],.0052):
        x=np.interp(at,dist,bead[:,0]);y=np.interp(at,dist,bead[:,1])
        if np.hypot(x,y-cy)>dotted+.004:dot(x,y,.00068,245)
    # Fine medallion hairlines and raised double-bead ring.
    angles=np.linspace(0,2*math.pi,450)
    for rr,w,val in [(radius,.00063,235),(radius+.0021,.00033,140),(dotted+.0025,.00042,195)]:
        line(np.c_[rr*np.cos(angles),cy+rr*np.sin(angles)],w,val)
    for k in range(48):
        a=2*math.pi*k/48
        dot(dotted*math.cos(a),cy+dotted*math.sin(a),.00102,255)
        b=a+math.pi/48;dot(dotted*math.cos(b),cy+dotted*math.sin(b),.00038,160)
    # Paired scrolls with almond leaves, in the open crown above the medallion.
    top_start=cy+dotted+.006;top_end=length*.885
    if top_end>top_start:
        height=top_end-top_start
        span=min(width*.13,np.interp((top_start+height*.5)/length,ts,half)*.53)
        for sign in [-1,1]:
            p=bez([0,top_start],[sign*span*1.3,top_start+height*.18],[sign*span,top_start+height*.66],[0,top_end])
            line(p,.00065,230)
            for fraction in [.28,.50,.70]:
                n=round(fraction*(len(p)-1));center=p[n];tip=center+np.array([sign*span*.34,height*.10])
                leaf=np.vstack([bez(center,center+[sign*span*.36,-height*.015],tip+[sign*span*.10,-height*.03],tip),bez(tip,tip+[-sign*span*.20,-height*.035],center+[-sign*span*.12,height*.03],center)])
                line(leaf,.00058,245)
            # Small spiral at the lower end of each scroll.
            aa=np.linspace(.1,math.pi*2.3,130);rr=np.linspace(span*.32,.00035,len(aa))
            spiral=np.c_[sign*(span*.42+rr*np.cos(aa)),top_start+height*.16+rr*np.sin(aa)]
            line(spiral,.00053,200)
        jewel_y=min(length*.915,top_end+.004)
        for off,rx,ry in [(0,.0025,.006),(-.005,.0013,.003),(.005,.0013,.003)]:
            yy=jewel_y-(.002 if off else 0)
            p=np.array([[off,yy+ry],[off+rx,yy],[off,yy-ry],[off-rx,yy],[off,yy+ry]])
            line(p,.00070,255)
    # Lower paired stems leave the medallion and glyph uncluttered.
    low_end=max(.012,cy-dotted-.012)
    for sign in [-1,1]:
        stem=bez([0,.026],[sign*width*.13,.052],[sign*width*.19,low_end-.008],[sign*width*.10,low_end])
        line(stem,.00062,205)
        for f in [.40,.64,.82]:
            c=stem[round(f*59)];end=c+[sign*.005,.008]
            leaf=np.vstack([bez(c,c+[sign*.007,.001],end+[sign*.002,-.002],end),bez(end,end+[-sign*.006,-.001],c+[-sign*.001,.004],c)])
            line(leaf,.00055,220)
    # Small axis jewel beneath the circle, matching the reference's tip accents.
    yy=max(.032,cy-dotted-.008)
    line([[0,yy+.004],[.002,yy],[0,yy-.004],[-.002,yy],[0,yy+.004]],.0007,255)
    # Mirror the decorative coverage before compositing the intentionally oriented icon.
    gold=ImageChops.lighter(gold,gold.transpose(Image.Transpose.FLIP_LEFT_RIGHT))
    x,y=np.meshgrid((np.arange(W*SS)+.5)/(W*SS)*width-width/2,(1-(np.arange(H*SS)+.5)/(H*SS))*length)
    inside=abs(x)<=np.interp(y/length,ts,half)
    protected=np.hypot(x,y-cy)<radius-.0015
    g=np.array(gold);g[protected]=0;g[~inside]=0
    gold=Image.fromarray(g).resize((W,H),Image.Resampling.LANCZOS)
    glyph=Image.open(BASE/f'Symbols/Initial State/Icons/{IDS[i]:02d}-{OLD[i]["label"]}.png').getchannel('R')
    glyph=glyph.crop(glyph.point(lambda p:255 if p>32 else 0).getbbox())
    canvas=Image.new('L',(max(glyph.size),)*2,0);canvas.paste(glyph,((canvas.width-glyph.width)//2,(canvas.height-glyph.height)//2))
    angle=math.degrees(math.atan2(mesh['matrix'][0][1],mesh['matrix'][1][1]))%360
    canvas=canvas.rotate(angle,Image.Resampling.BICUBIC,expand=True,fillcolor=0)
    span=radius*1.50*canvas.width/max(glyph.size)*(.94 if i==5 else 1)
    glyph=canvas.resize((round(W*span/width),round(H*span/length)),Image.Resampling.LANCZOS)
    icon=Image.new('L',(W,H),0);icon.paste(glyph,(round(W*.5-glyph.width/2),round(H*(1-cy/length)-glyph.height/2)))
    mask=ImageChops.lighter(gold,icon)
    silhouette=Image.fromarray(np.uint8(inside)*255).resize((W,H),Image.Resampling.LANCZOS)
    mask=ImageChops.multiply(mask,silhouette)
    Image.merge('RGBA',(mask,Image.new('L',(W,H),255),Image.new('L',(W,H),222),Image.new('L',(W,H),255))).save(DEST/f'{i:02d}-{OLD[i]["label"]}.png')
    preview=Image.new('RGB',(W,H),'#17131c');preview.paste(Image.new('RGB',(W,H),COLORS[i]),(0,0),silhouette)
    preview.paste(Image.new('RGB',(W,H),'#f8cf78'),(0,0),mask)
    preview=preview.resize((round(490*width/length),490),Image.Resampling.LANCZOS)
    sx=i%4*440+(440-preview.width)//2;sy=i//4*610+25
    sheet.paste(preview,(sx,sy));draw.text((i%4*440+30,sy+508),f'{i+1:02d}  {OLD[i]["label"]}',font=font,fill='#f6d497')
    draw.text((i%4*440+30,sy+544),'Symmetric gold filigree + beadwork',font=font,fill='#baaa98')
    manifest.append(dict(index=i,name=mesh['name'],label=OLD[i]['label'],width=float(width),length=float(length),center_uv=[.5,OLD[i]['center_uv'][1]],radius_m=float(radius),dotted_radius_m=float(dotted),dot_count=48,icon_region=[.5,OLD[i]['center_uv'][1],float((radius-.0035)/length),.003],glyph_rotation_degrees=angle))
sheet.save(HERE/'artwork-review.png')
(HERE/'staged/layout.json').write_text(json.dumps(manifest,indent=2))
print('Staged eight symmetric ornament masks, 640x640 each.')
