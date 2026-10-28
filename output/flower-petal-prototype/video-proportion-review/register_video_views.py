import cv2,json,math
import numpy as np
from pathlib import Path
from PIL import Image,ImageDraw
OUT=Path(__file__).resolve().parent
d=json.loads((OUT/'reference-measurements.json').read_text())
cap=cv2.VideoCapture(r'C:\Users\whkso\Downloads\2026-10-28 21-57-14.mp4')
sift=cv2.SIFT_create(nfeatures=6500)
mask=np.zeros((720,1280),np.uint8);mask[140:620,280:1040]=255
def features(im):return sift.detectAndCompute(im,mask)
def match(ka,da,kb,db):
 pairs=cv2.BFMatcher().knnMatch(da,db,k=2);good=[a for a,b in pairs if a.distance<.72*b.distance]
 if len(good)<8:return None,0,999
 a=np.float32([ka[m.queryIdx].pt for m in good]);b=np.float32([kb[m.trainIdx].pt for m in good])
 H,ok=cv2.findHomography(a,b,cv2.USAC_MAGSAC,2.5)
 if H is None:return None,0,999
 err=np.linalg.norm(cv2.perspectiveTransform(a[:,None,:],H)[:,0]-b,axis=1)
 return H,int(ok.sum()),float(np.median(err[ok[:,0]>0]))
base=cv2.imread(str(OUT/d['anchor']));k0,z0=features(base)
lastk,lastz=k0,z0;previous=np.eye(3);records=[];selected={}
times={int(round((29*(i+.15)/16)*30)):str(i) for i in [15,9,5,2,0]}
anchor=max(times)
indices=sorted(set(list(range(anchor,-1,-3))+list(times)),reverse=True)
for frame in indices:
 cap.set(cv2.CAP_PROP_POS_FRAMES,frame);ok,im=cap.read()
 if not ok:continue
 k,z=features(im);direct,n_direct,e_direct=match(k0,z0,k,z)
 if n_direct>80:
  H=direct;n=n_direct;err=e_direct;kind='direct'
 else:
  relative,n,err=match(lastk,lastz,k,z)
  if relative is None or n<8:raise RuntimeError((frame,n))
  H=relative@previous;kind='chain'
 H/=H[2,2]
 records.append({'frame':frame,'matches':n,'error':err,'type':kind})
 if frame in times:selected[times[frame]]={'H':H.tolist(),'matches':n,'error':err,'type':kind}
 previous=H;lastk,lastz=k,z
cx,cy=653.5,391
scale=d['meters_per_pixel'];center=d['center_pixel'];baseH=np.array([[1/scale,0,center[0]],[0,-1/scale,center[1]],[0,0,1.]])
# Stable common lens; fitted camera view is further judged against projected contours.
f=850.;K=np.array([[f,0,cx],[0,f,cy],[0,0,1.]])
for key,s in selected.items():
 v=d['views'][key];H=np.array(s['H']);h=H@baseH;h/=h[2,2]
 # Calibrate pose to the dense target projection of world points via solvePnP.
 xy=np.array([[x,y] for x in [-.3,-.15,0,.15,.3] for y in [-.3,-.15,0,.15,.3]],np.float64)
 xyz=np.column_stack([xy,np.zeros(len(xy))])
 pts=cv2.perspectiveTransform(xy[:,None,:],h)[:,0]
 ok,rvec,tvec=cv2.solvePnP(xyz,pts,K,None,flags=cv2.SOLVEPNP_ITERATIVE)
 R,_=cv2.Rodrigues(rvec);t=tvec[:,0];C=-R.T@t
 reproj,_=cv2.projectPoints(xyz,rvec,tvec,K,None)
 v.update({'H_anchor_to_frame':H.tolist(),'H_world_to_frame':h.tolist(),'camera_R_cv':R.tolist(),'camera_t_cv':t.tolist(),
           'camera_position_world':C.tolist(),'focal_px':f,'registration':s,
           'camera_plane_fit_rms_px':float(np.sqrt(np.mean((reproj[:,0]-pts)**2))),
           'elevation_degrees':math.degrees(math.asin(abs(C[2])/np.linalg.norm(C)))})
 im=Image.open(OUT/v['file']).convert('RGB');draw=ImageDraw.Draw(im)
 for name,p in d['inner'].items():
  pts=cv2.perspectiveTransform(np.array(p['pixels'],np.float32)[:,None,:],H)[:,0]
  draw.line([tuple(p) for p in pts]+[tuple(pts[0])],fill='#00ffff',width=2)
 im.save(OUT/f'reference-tracked-{key}.png')
(OUT/'reference-measurements.json').write_text(json.dumps(d,indent=2))
(OUT/'tracking-log.json').write_text(json.dumps(records,indent=2))
print(json.dumps({k:{'registration':v['registration'],'elevation':v['elevation_degrees'],'camera_fit_rms':v['camera_plane_fit_rms_px']} for k,v in d['views'].items()},indent=2))
