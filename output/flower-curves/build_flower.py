"""Deterministic, editable Bezier reconstruction of the supplied flower reference.
Run with Python + Pillow. No raster tracing, external assets or font dependencies.
"""
from pathlib import Path
from math import sin, cos, pi
import json
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
CENTER = (500, 500)
K = 4 * (2**0.5 - 1) / 3

# Each template is mirror symmetric about its vertical axis. Coordinates are
# baked into each exported path for predictable curve import in DCC software.
OUTER = [
    ('M', 0, -100),
    ('C', -46, -137, -101, -210, -101, -272),
    ('C', -101, -315, -77, -342, -49, -369),
    ('C', -25, -393, -9, -416, 0, -446),
    ('C', 9, -416, 25, -393, 49, -369),
    ('C', 77, -342, 101, -315, 101, -272),
    ('C', 101, -210, 46, -137, 0, -100), ('Z',)
]
INNER = [
    ('M', 0, -82),
    ('C', -39, -110, -91, -155, -91, -214),
    ('C', -91, -259, -66, -280, -38, -300),
    ('C', -19, -313, -7, -326, 0, -345),
    ('C', 7, -326, 19, -313, 38, -300),
    ('C', 66, -280, 91, -259, 91, -214),
    ('C', 91, -155, 39, -110, 0, -82), ('Z',)
]

def placed(template, angle=0):
    a = angle*pi/180
    out = []
    for op, *nums in template:
        values = []
        for x, y in zip(nums[::2], nums[1::2]):
            values += [500 + x*cos(a) - y*sin(a), 500 + x*sin(a) + y*cos(a)]
        out.append((op, *values))
    return out

def circle(radius, reverse=False):
    r, k = radius, radius*K
    commands = [('M',r,0), ('C',r,k,k,r,0,r), ('C',-k,r,-r,k,-r,0),
                ('C',-r,-k,-k,-r,0,-r), ('C',k,-r,r,-k,r,0), ('Z',)]
    if reverse:
        commands = [(op, *[v if i%2==0 else -v for i,v in enumerate(values)])
                    for op,*values in commands]
    return placed(commands)

def path_data(commands):
    return ' '.join(op + (' ' + ' '.join(f'{n:.5f}'.rstrip('0').rstrip('.') for n in nums) if nums else '')
                    for op,*nums in commands)

outer_colors = ['#b84a12','#55205e','#492363','#971246','#492363','#55205e']*2
inner_colors = ['#b71353','#b97613','#283b82','#14665c','#a3116c','#155c83','#7e6519','#50316d']
layers = [
    dict(id='outer-petals', label='01 Outer petals - 12 x 30 degrees',
         objects=[dict(id=f'outer-petal-{i+1:02}', commands=placed(OUTER,i*30), color=outer_colors[i]) for i in range(12)]),
    dict(id='inner-petals', label='02 Inner petals - 8 x 45 degrees',
         objects=[dict(id=f'inner-petal-{i+1:02}', commands=placed(INNER,i*45), color=inner_colors[i]) for i in range(8)]),
    dict(id='center-disc', label='03 Center disc',
         objects=[dict(id='center-disc', commands=circle(121), color='#2d163f')]),
    dict(id='outer-glow-ring', label='04 Orange circumference - closed annulus',
         objects=[dict(id='outer-glow-ring', commands=circle(480)+circle(474,True), color='#ff9e2c')]),
]

def svg(selected, filename, outline=False):
    root = ET.Element('svg', {'xmlns':'http://www.w3.org/2000/svg',
        'xmlns:inkscape':'http://www.inkscape.org/namespaces/inkscape',
        'width':'1000mm','height':'1000mm','viewBox':'0 0 1000 1000'})
    # Canonical namespace used by Inkscape for named layers.
    root.set('xmlns:inkscape','http://www.inkscape.org/namespaces/inkscape')
    ET.SubElement(root,'title').text = 'Symmetric top-down flower - editable closed Bezier curves'
    ET.SubElement(root,'desc').text = ('Reference-based reconstruction: 12 outer petals, 8 inner petals, '
        'center disc, orange annulus. All coordinates baked; no transforms, clipping, filters or raster images. '
        'Petals continue under the center disc. Nominal ring outside diameter: 960 mm; rescale as needed.')
    for layer in selected:
        g = ET.SubElement(root,'g',{'id':'layer-'+layer['id'], 'inkscape:groupmode':'layer', 'inkscape:label':layer['label']})
        for obj in layer['objects']:
            ET.SubElement(g,'path',{'id':obj['id'], 'd':path_data(obj['commands']),
                'fill':'none' if outline else obj['color'], 'fill-rule':'evenodd',
                'stroke':'#20202b' if outline else 'none', **({'stroke-width':'1.6'} if outline else {})})
    ET.indent(root)
    ET.ElementTree(root).write(OUT/filename, encoding='utf-8', xml_declaration=True)

svg(layers, 'flower-top-down-layered.svg')
svg(layers, 'flower-top-down-curves.svg', outline=True)
for i,layer in enumerate(layers,1):
    svg([layer], f'{i:02}-{layer["id"]}.svg')
# A single circular spline, convenient for beveling as an emissive tube.
svg([dict(id='ring-centerline',label='Ring centerline - bevel this curve',objects=[
    dict(id='ring-centerline',commands=circle(477),color='#ff9e2c')])], 'ring-centerline.svg', outline=True)

def flatten(commands):
    contours, points, current = [], [], None
    for op,*n in commands:
        if op == 'M':
            points = [(n[0],n[1])]; current = points[0]
        elif op == 'C':
            p0 = current; p1,p2,p3 = (n[0],n[1]),(n[2],n[3]),(n[4],n[5])
            for j in range(1,33):
                t=j/32; u=1-t
                points.append(tuple(u**3*p0[k]+3*u*u*t*p1[k]+3*u*t*t*p2[k]+t**3*p3[k] for k in (0,1)))
            current=p3
        elif op=='Z':
            contours.append(points); points=[]
    return contours

S=2
def render(draw, selected, origin, size, bg, stroke=True):
    ox,oy=origin
    def px(p): return ((ox+p[0]*size/1000)*S,(oy+p[1]*size/1000)*S)
    for layer in selected:
        for obj in layer['objects']:
            if layer['id']=='outer-glow-ring':
                bounds=[px((20,20)),px((980,980))]
                draw.ellipse(bounds,outline=obj['color'],width=max(1,round(size/1000*6*S)))
                continue
            for index,contour in enumerate(flatten(obj['commands'])):
                points=[px(p) for p in contour]
                draw.polygon(points,fill=obj['color'] if index==0 else bg)
                if stroke and layer['id']!='outer-glow-ring':
                    draw.line(points+[points[0]],fill='#edb851',width=max(1,round(size/1000*2.4*S)),joint='curve')

# Presentation only: mesh geometry itself contains no decorative strokes.
bg='#16121e'
im=Image.new('RGB',(1460*S,1030*S),bg); d=ImageDraw.Draw(im)
font_path='C:/Windows/Fonts/segoeui.ttf'
def font(size): return ImageFont.truetype(font_path,round(size*S))
def label(x,y,text,size=20,color='#eee8e0'):
    d.text((x*S,y*S),text,font=font(size),fill=color)
label(48,32,'FLOWER / TOP-DOWN CURVES',32)
label(48,81,'Symmetric reconstruction from the supplied perspective reference',18,'#bdb3c5')
render(d,layers,(45,123),850,bg)
label(52,989,'12 outer petals  /  8 inner petals  /  separate center and ring',18,'#bdb3c5')
for i,(layer,x,y) in enumerate(zip(layers,[970,1220,970,1220],[177,177,551,551])):
    render(d,[layer],(x-25,y),250,bg)
    label(x-10,y+266,['01 / OUTER','02 / INNER','03 / CENTER','04 / RING'][i],18)
    label(x-10,y+296,['12 closed curves','8 closed curves','1 closed curve','1 annular path'][i],15,'#bdb3c5')
label(975,922,'Closed cubic Bezier paths',19)
label(975,953,'Common origin and scale',17,'#bdb3c5')
im.resize((1460,1030),Image.Resampling.LANCZOS).save(OUT/'flower-preview.png')

metadata = {'source':'codex-clipboard-35544284-8947-447e-89e8-6938c935bec1.png',
 'method':'Symmetry-constrained manual vector reconstruction, not calibrated inverse projection',
 'viewBox':[0,0,1000,1000], 'center':[500,500], 'nominal_units':'mm',
 'outer_petals':{'count':12,'rotation_step_degrees':30,'tip_radius':446},
 'inner_petals':{'count':8,'rotation_step_degrees':45,'tip_radius':345},
 'center_radius':121,'ring':{'outer_radius':480,'inner_radius':474,'centerline_radius':477},
 'geometry':'22 individually named closed paths; ring has two oppositely wound closed subpaths',
 'excluded':'Icons, dotted borders, surface textures, lamps, sparks, arrowheads and raster glow',
 'depth_order_bottom_to_top':['outer-glow-ring (independent)','outer-petals','inner-petals','center-disc']}
(OUT/'geometry.json').write_text(json.dumps(metadata,indent=2)+'\n',encoding='utf-8')

# Export checks: closed paths, expected counts, valid XML, baked geometry.
ns={'s':'http://www.w3.org/2000/svg'}
root=ET.parse(OUT/'flower-top-down-layered.svg').getroot()
paths=root.findall('.//s:path',ns)
assert len(paths)==22
assert len({p.get('id') for p in paths})==22
assert all(p.get('d').endswith('Z') for p in paths)
assert all('transform' not in p.attrib for p in paths)
assert paths[-1].get('d').count('Z')==2
for f in OUT.glob('*.svg'): ET.parse(f)
print(f'Wrote and validated 7 SVG files and preview in {OUT}')
