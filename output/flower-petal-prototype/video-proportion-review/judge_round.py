"""Read-only judge: compare a completed Blender build to video observations."""
import cv2,json,sys,numpy as np
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parent
round_number=int(sys.argv[1]);reference=json.loads((OUT/'reference-measurements.json').read_text());build=json.loads((OUT/f'round-{round_number}-build.json').read_text())
names=['N','NE','E','SE','S','SW','W','NW'];objects=['InnerPetal_01']+[f'REF_TopPetal_{i:02d}' for i in range(2,9)]
report={'round':round_number,'phase':'judge','outer_count':build['outer_count'],'views':{},'measurement_note':'Hand-marked borders, approximate planar registration; silhouette fit is not proof of exact 3D geometry.'}
for key in ['15','5','9','2','0']:
    v=reference['views'][key];H=np.array(v['H_anchor_to_frame']);result={}
    center=np.array(reference['center_pixel']);circle=np.array([center+75*np.array([np.cos(t),np.sin(t)]) for t in np.linspace(0,2*np.pi,180)],np.float32)
    circle=cv2.perspectiveTransform(circle[:,None,:],H)[:,0]
    exclusion=np.zeros((720,1280),np.uint8);cv2.fillPoly(exclusion,[np.round(circle).astype(np.int32)],1)
    im=Image.open(OUT/v['file']).convert('RGB');draw=ImageDraw.Draw(im)
    for name,obj in zip(names,objects):
        actual=np.array(build['projected_outlines'][key][obj],np.float32)
        target=cv2.perspectiveTransform(np.array(reference['inner'][name]['pixels'],np.float32)[:,None,:],H)[:,0]
        ma=np.zeros((720,1280),np.uint8);mb=ma.copy()
        cv2.fillPoly(ma,[np.round(actual).astype(np.int32)],1);cv2.fillPoly(mb,[np.round(target).astype(np.int32)],1)
        ma[exclusion>0]=0;mb[exclusion>0]=0
        iou=np.count_nonzero(ma&mb)/max(1,np.count_nonzero(ma|mb))
        ca=cv2.morphologyEx(ma,cv2.MORPH_GRADIENT,np.ones((3,3),np.uint8));cb=cv2.morphologyEx(mb,cv2.MORPH_GRADIENT,np.ones((3,3),np.uint8))
        da=cv2.distanceTransform(1-ca,cv2.DIST_L2,3);db=cv2.distanceTransform(1-cb,cv2.DIST_L2,3)
        contour_error=float((db[ca>0].mean()+da[cb>0].mean())*.5)
        result[name]={'visible_outline_iou':float(iou),'mean_outline_error_px':contour_error,
                      'observed_tip_distance_px':float(np.linalg.norm(actual-target[0],axis=1).min())}
        for mask,color in [(mb,'#00ffff'),(ma,'#ff65cf')]:
            contours,_=cv2.findContours(mask,cv2.RETR_EXTERNAL,cv2.CHAIN_APPROX_SIMPLE)
            for contour in contours:
                pts=[tuple(p) for p in contour[:,0]]
                if len(pts)>2:draw.line(pts+[pts[0]],fill=color,width=2)
    im.crop((211,99,1096,683)).save(OUT/f'round-{round_number}-overlay-{key}.png')
    report['views'][key]={'reference_file':v['file'],'camera_elevation_degrees':v['elevation_degrees'],
                         'camera_plane_fit_rms_px':v['camera_plane_fit_rms_px'],'petals':result,
                         'mean_iou':float(np.mean([r['visible_outline_iou'] for r in result.values()])),
                         'mean_outline_error_px':float(np.mean([r['mean_outline_error_px'] for r in result.values()]))}
report['mean_iou_three_angles']=float(np.mean([report['views'][k]['mean_iou'] for k in ['15','5','9']]))
report['mean_outline_error_three_angles_px']=float(np.mean([report['views'][k]['mean_outline_error_px'] for k in ['15','5','9']]))
(OUT/f'round-{round_number}-judge.json').write_text(json.dumps(report,indent=2))
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',25);small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',18)
sheet=Image.new('RGB',(1440,1585),'#17171d');dr=ImageDraw.Draw(sheet)
dr.text((24,16),f'ROUND {round_number}  /  BUILD → JUDGE',font=font,fill='#f5eddd')
dr.text((24,53),'RECORDING',font=small,fill='#c8b890');dr.text((744,53),'BLENDER SCULPT',font=small,fill='#c8b890')
for row,key in enumerate(['15','5','9']):
    y=85+row*490;v=reference['views'][key]
    a=Image.open(OUT/v['file']).crop((211,99,1096,683));b=Image.open(OUT/f'round-{round_number}-view-{key}.png')
    for i,im in enumerate([a,b]):im=im.resize((708,467),Image.Resampling.LANCZOS);sheet.paste(im,(i*720+6,y))
    dr.text((18,y+468),f"{v['elevation_degrees']:.0f}° elevation  |  silhouette overlap {report['views'][key]['mean_iou']:.1%}",font=small,fill='#e2d7c4')
dr.text((24,1560),'16 outer petals. Approximate video measurements; plain materials for shape review.',font=small,fill='#aeadbb')
sheet.save(OUT/f'round-{round_number}-comparison.jpg',quality=94)
print(json.dumps({'round':round_number,'three_view_iou':report['mean_iou_three_angles'],'mean_outline_error_px':report['mean_outline_error_three_angles_px'],
                  'views':{k:round(v['mean_iou'],4) for k,v in report['views'].items()},'overhead_per_petal':report['views']['15']['petals']},indent=2))
