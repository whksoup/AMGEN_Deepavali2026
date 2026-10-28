"""Read the supplied vector geometry; its viewBox does not contain the drawing."""
from pathlib import Path
import re
import math
import xml.etree.ElementTree as ET
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
root = ET.parse(HERE / 'patternBasics.svg').getroot()
paths = [e.attrib['d'] for e in root.iter() if e.tag.endswith('path')]
circle = next(e for e in root.iter() if e.tag.endswith('circle'))
CX, CY, RADIUS = (float(circle.attrib[k]) for k in ('cx', 'cy', 'r'))

def sample(path, count=180):
    tokens = re.findall(r'[MLCA]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?', path)
    result = []
    cursor = 0
    while cursor < len(tokens):
        command = tokens[cursor]; cursor += 1
        size = {'M': 2, 'L': 2, 'C': 6, 'A': 7}[command]
        values = np.array(list(map(float, tokens[cursor:cursor+size])))
        cursor += size
        if command == 'M':
            point = values; result.append(point)
        elif command == 'L':
            result.extend(np.linspace(point, values, 24)[1:]); point = values
        elif command == 'C':
            p1, p2, end = values.reshape(3, 2)
            t = np.linspace(0, 1, count)[1:, None]
            result.extend((1-t)**3*point + 3*(1-t)**2*t*p1 + 3*(1-t)*t*t*p2 + t**3*end)
            point = end
        else:
            rx, ry, rotation, large, sweep, ex, ey = values
            assert abs(rx-ry) < .001 and rotation == 0 and large == 0
            end = np.array([ex, ey]); chord = end-point; distance = np.linalg.norm(chord)
            midpoint = (point+end)/2; normal = np.array([-chord[1],chord[0]])/distance
            offset = math.sqrt(max(0, rx*rx-distance*distance/4))
            for center in (midpoint+normal*offset, midpoint-normal*offset):
                a = math.atan2(point[1]-center[1],point[0]-center[0])
                b = math.atan2(end[1]-center[1],end[0]-center[0])
                delta = (b-a) % (2*math.pi) if sweep else -((a-b) % (2*math.pi))
                if abs(delta) <= math.pi:
                    angles = np.linspace(a,a+delta,40)[1:]
                    result.extend(center + rx*np.column_stack([np.cos(angles),np.sin(angles)]))
                    break
            point = end
    return np.array(result)

def mirror(points):
    reflected = points.copy(); reflected[:,0] = 2*CX-reflected[:,0]
    return reflected

LEFT_CURVES = [sample(d) for d in paths[:2]]
CONTOURS = [np.vstack([p, mirror(p)[::-1]]) for p in LEFT_CURVES]
outer_top = sample(paths[2])
outer_left = np.vstack([sample(paths[3])[::-1], outer_top[:np.argmin(outer_top[:,1])+1]])
outer_left = outer_left[np.argsort(outer_left[:,1])]
OUTER = np.vstack([outer_left, mirror(outer_left)[::-1]])
TIP = float(OUTER[:,1].min())
ROOT = 3370.0

def outer_half_width(ys):
    return CX-np.interp(ys, outer_left[:,1], outer_left[:,0])

if __name__ == '__main__':
    image = Image.new('RGB',(900,1200),'#19151e'); draw = ImageDraw.Draw(image)
    scale = .135; origin = np.array([450,100])
    def pixels(points):
        return [tuple(p) for p in (points-[CX,TIP])*scale+origin]
    draw.polygon(pixels(OUTER),fill='#332538',outline='#75616c',width=3)
    for points, color, width in zip(CONTOURS,['#d9a54a','#fff0ad'],[5,3]):
        draw.line(pixels(points),fill=color,width=width,joint='curve')
    center = (np.array([CX,CY])-[CX,TIP])*scale+origin
    r=RADIUS*scale
    draw.ellipse((center[0]-r,center[1]-r,center[0]+r,center[1]+r),outline='#d6d7df',width=3)
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',24)
    draw.text((45,30),'Supplied guide - complete curves mirrored',font=font,fill='#f7e5c1')
    draw.text((45,1130),'Gold contours / white medallion / gray leaf boundary',font=font,fill='#b9a9b6')
    image.save(HERE/'guide-review.png')
