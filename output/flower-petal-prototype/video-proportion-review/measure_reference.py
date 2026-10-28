"""Measurements from the supplied recording, with a planar multi-view fit.

Hand-picked gold outer borders are approximate pixel observations, not CAD truth.
The center disc fixes scale at the prototype's 242 mm diameter.
"""
import cv2, json, math
import numpy as np
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

OUT=Path(__file__).resolve().parent
CENTER=np.array([651.,346.])
SCALE=.242/150
INNER={
'N': [[654,152],[644,174],[620,190],[609,212],[607,239],[616,263],[625,277],[653,284],[681,279],[693,258],[704,229],[701,210],[688,190],[664,174]],
'NE': [[774,192],[778,214],[790,236],[792,254],[785,275],[768,295],[743,305],[718,299],[702,284],[696,267],[702,247],[710,228],[729,210],[751,205]],
'E': [[870,344],[850,336],[838,313],[821,297],[798,288],[772,292],[750,304],[731,324],[727,348],[735,374],[759,394],[785,401],[810,395],[832,382],[849,358]],
'SE': [[795,524],[791,500],[802,470],[798,441],[786,412],[766,396],[744,388],[722,392],[699,408],[689,439],[692,465],[709,491],[731,506],[758,510],[781,512]],
'S': [[647,572],[631,552],[611,535],[598,515],[588,490],[591,462],[603,437],[622,421],[644,416],[663,421],[685,444],[697,469],[700,492],[691,518],[676,540],[660,553]],
'SW': [[485,527],[488,501],[485,475],[492,445],[504,416],[522,396],[546,388],[566,390],[583,404],[596,420],[602,442],[596,465],[583,488],[558,507],[532,512],[507,513]],
'W': [[432,336],[452,326],[466,306],[485,291],[507,284],[531,284],[550,290],[566,305],[574,324],[575,345],[569,365],[551,381],[527,389],[504,391],[482,384],[464,370],[452,351]],
'NW': [[535,188],[556,201],[578,203],[594,217],[605,239],[603,259],[592,276],[573,290],[550,295],[533,287],[519,269],[514,249],[518,225],[526,209]],
}
# Readable silhouette landmarks, clockwise; some far-side lobes are occluded.
# These 14 landmarks are NOT a petal count. User confirms 16 outer petals.
OUTER_TIPS=[[652,100],[789,130],[883,202],[926,344],[901,465],[820,559],[751,592],[649,612],[548,584],[472,549],[397,465],[369,330],[422,191],[537,130]]

def world(points):
    points=np.asarray(points,dtype=float)
    return np.stack([(points[:,0]-CENTER[0])*SCALE,(CENTER[1]-points[:,1])*SCALE],axis=1)

data={'source':'2026-10-28 21-57-14.mp4','anchor':'reference-15-027.5s.png','center_pixel':CENTER.tolist(),
      'meters_per_pixel':SCALE,'manual_outline_uncertainty_px':3,'inner':{},'outer_tips_pixel':OUTER_TIPS,
      'outer_tips_world':world(OUTER_TIPS).tolist(),'outer_petal_count':16,'views':{}}
for name, points in INNER.items():
    p=world(points); tip=p[0]; angle=math.atan2(tip[0],tip[1]); d=tip/np.linalg.norm(tip); r=np.array([d[1],-d[0]])
    xx=p@r; yy=p@d
    data['inner'][name]={'pixels':points,'world':p.tolist(),'tip_radius_m':float(np.linalg.norm(tip)),
                          'angle_degrees':math.degrees(angle)%360,'width_m':float(xx.max()-xx.min()),
                          'local_x':xx.tolist(),'local_radial_y':yy.tolist()}

anchor=cv2.imread(str(OUT/data['anchor']))
sift=cv2.SIFT_create(nfeatures=7000)
mask=np.zeros(anchor.shape[:2],np.uint8); cv2.circle(mask,(651,346),280,255,-1); mask[:135]=0
k0,d0=sift.detectAndCompute(anchor,mask)
Hworld=np.array([[1/SCALE,0,CENTER[0]],[0,-1/SCALE,CENTER[1]],[0,0,1.]])
cx,cy=653.5,391.
for index in [15,5,9,2,0]:
    file=next(OUT.glob(f'reference-{index:02d}-*.png'))
    image=cv2.imread(str(file)); k1,d1=sift.detectAndCompute(image,None)
    matches=cv2.BFMatcher().knnMatch(d0,d1,k=2)
    good=[m for m,n in matches if m.distance<.70*n.distance]
    a=np.float32([k0[m.queryIdx].pt for m in good]); b=np.float32([k1[m.trainIdx].pt for m in good])
    H,inliers=cv2.findHomography(a,b,cv2.RANSAC,3)
    hw=H@Hworld; hw/=hw[2,2]
    # With a fixed viewport principal point, orthogonal plane axes constrain f.
    q=hw.copy(); q[0]-=cx*q[2]; q[1]-=cy*q[2]
    aa=q[0,0]*q[0,1]+q[1,0]*q[1,1]; bb=q[2,0]*q[2,1]
    cc=q[0,0]**2+q[1,0]**2-q[0,1]**2-q[1,1]**2; dd=q[2,0]**2-q[2,1]**2
    f2=-(aa*bb+cc*dd)/(bb*bb+dd*dd) if bb*bb+dd*dd>1e-10 else 0
    errs=np.linalg.norm(cv2.perspectiveTransform(a[:,None,:],H)[:,0]-b,axis=1)
    data['views'][str(index)]={'file':file.name,'H_anchor_to_frame':H.tolist(),'H_world_to_frame':hw.tolist(),
                              'feature_inliers':int(inliers.sum()),'median_registration_error_px':float(np.median(errs[inliers[:,0]>0])),
                              'focal_estimate_px':math.sqrt(f2) if f2>0 else None}

# Use one focal length for all views; projective source imperfections remain measurable.
focals=[v['focal_estimate_px'] for k,v in data['views'].items() if k not in ['15','0'] and v['focal_estimate_px']]
f=float(np.median(focals))
K=np.array([[f,0,cx],[0,f,cy],[0,0,1.]])
for key,v in data['views'].items():
    h=np.array(v['H_world_to_frame']); q=np.linalg.inv(K)@h
    scale=2/(np.linalg.norm(q[:,0])+np.linalg.norm(q[:,1])); r1=q[:,0]*scale; r2=q[:,1]*scale
    R=np.column_stack([r1,r2,np.cross(r1,r2)]); u,_,vh=np.linalg.svd(R); R=u@vh
    t=q[:,2]*scale
    if np.linalg.det(R)<0:R[:,-1]*=-1
    C=-R.T@t
    v.update({'camera_R_cv':R.tolist(),'camera_t_cv':t.tolist(),'camera_position_world':C.tolist(),
              'elevation_degrees':math.degrees(math.asin(abs(C[2])/np.linalg.norm(C))),
              'focal_px':f,'viewport':[211,99,1096,683]})

(OUT/'reference-measurements.json').write_text(json.dumps(data,indent=2))
im=Image.fromarray(cv2.cvtColor(anchor,cv2.COLOR_BGR2RGB)); draw=ImageDraw.Draw(im); font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',19)
for n,p in INNER.items():
    draw.line([tuple(x) for x in p+[p[0]]],fill='#00ffff',width=2)
    draw.text(tuple(np.mean(p,axis=0).astype(int)),n,font=font,fill='white',stroke_width=2,stroke_fill='black')
for i,p in enumerate(OUTER_TIPS):
    x,y=p;draw.ellipse((x-4,y-4,x+4,y+4),fill='#ffdf00');draw.text((x+6,y),str(i+1),font=font,fill='#ffdf00',stroke_width=2,stroke_fill='black')
draw.ellipse((576,271,726,421),outline='#00ff80',width=2)
im.save(OUT/'reference-landmarks.png')
print(json.dumps({'inner':{k:{x:v[x] for x in ['angle_degrees','tip_radius_m','width_m']} for k,v in data['inner'].items()},
                  'views':{k:{x:v[x] for x in ['feature_inliers','median_registration_error_px','focal_estimate_px','elevation_degrees']} for k,v in data['views'].items()},'focal':f},indent=2))
