"""Author geometric medallion/rim coverage in physical mesh coordinates.

The approved symbol PNGs are reused, resampled and oriented for the eight petals.
All ornament goes into one coverage channel; color and edge light are runtime data.
"""
from pathlib import Path
import json
import math
import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/flower-initial-state'
BASE = ROOT / 'Assets/HK_Signature/Deepavali/PetalPrototype'
DEST = BASE / 'Symbols/Initial State/Decorated Masks'
DEST.mkdir(parents=True, exist_ok=True)
W, H, SS = 448, 640, 3
PW, PH = 0.182, 0.263
CX, CY = PW / 2, PH * 0.57
outline = np.array(json.loads((ROOT / 'output/flower-petal-texture/uv-outline.json').read_text())) * [PW, PH]
x, y = np.meshgrid((np.arange(W * SS) + 0.5) / (W * SS) * PW,
                   (1 - (np.arange(H * SS) + 0.5) / (H * SS)) * PH)
dist2 = np.full(x.shape, np.inf)
inside = np.zeros(x.shape, dtype=bool)
for a, b in zip(outline, np.roll(outline, -1, axis=0)):
    dx, dy = b - a
    t = np.clip(((x-a[0])*dx + (y-a[1])*dy) / (dx*dx + dy*dy), 0, 1)
    dist2 = np.minimum(dist2, (x-a[0]-t*dx)**2 + (y-a[1]-t*dy)**2)
    if abs(dy) > 1e-12:
        inside ^= ((a[1] > y) != (b[1] > y)) & (x < dx*(y-a[1])/dy + a[0])
dist = np.sqrt(dist2)
radial = np.sqrt((x-CX)**2 + (y-CY)**2)
coverage = np.abs(radial - 0.052) < 0.00052
# Dotted circle surrounding the fine medallion line.
angle = np.arctan2(y-CY, x-CX)
nearest = np.round(angle / (2 * np.pi / 48)) * (2 * np.pi / 48)
dot_dist = np.sqrt((x-CX-0.059*np.cos(nearest))**2 + (y-CY-0.059*np.sin(nearest))**2)
coverage |= dot_dist < 0.00115
# Two gold contours, separated from the geometric glow edge at a 3 mm inset.
# The outer ornament widens toward the pointed tip and lower shoulder.
half_width = 0.0010 + 0.0013 * np.clip((y/PH - 0.65)/0.30, 0, 1)
coverage |= np.abs(dist-0.0105) < half_width
coverage |= np.abs(dist-0.0155) < 0.00040

def diamond(px, py, rx, ry):
    return np.abs(x-px)/rx + np.abs(y-py)/ry < 1

for v in [0.27, 0.875]:
    coverage |= diamond(CX, PH*v, 0.0025, 0.006)
    coverage |= diamond(CX-0.006, PH*v-0.003, 0.0018, 0.0035)
    coverage |= diamond(CX+0.006, PH*v-0.003, 0.0018, 0.0035)
    coverage |= (x-CX)**2 + (y-PH*(v+0.032))**2 < 0.0010**2
for side in [-1, 1]:
    coverage |= diamond(CX + side*0.073, PH*0.51, 0.0032, 0.0015)
coverage &= inside & (dist > 0.0065)
frame = Image.fromarray((coverage*255).astype('uint8')).resize((W,H), Image.Resampling.LANCZOS)
frame.save(OUT / 'medallion-frame.png')
shape = Image.fromarray((inside*255).astype('uint8')).resize((W,H), Image.Resampling.LANCZOS)

# Clockwise, beginning at the top. All icons read upright from the review camera.
labels = ['Sprout', 'Diya', 'Star', 'Stacked Coins', 'People and Heart', 'Gift Box', 'Sunburst', 'Lotus']
source_ids = [3,4,2,7,5,6,1,0]
fills = ['#B51F4B','#B7770A','#264BB2','#47871D','#C51683','#12748F','#918011','#6A239C']
manifest = []
sheet = Image.new('RGB', (1600,1000), '#14101a')
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 19)
for i, (label, sid, fill) in enumerate(zip(labels, source_ids, fills)):
    icon = Image.open(BASE / f'Symbols/Initial State/Icons/{sid:02d}-{label}.png').getchannel('R')
    bounds = icon.point(lambda value: 255 if value > 32 else 0).getbbox()
    glyph = icon.crop(bounds)
    icon = Image.new('L',(512,512),0)
    icon.paste(glyph,((512-glyph.width)//2,(512-glyph.height)//2))
    # Rotating inside a larger square avoids clipping the diagonal compass/lotus.
    icon = icon.rotate(i*45, Image.Resampling.BICUBIC, expand=True, fillcolor=0)
    span = 0.114 * icon.width / 512
    glyph = icon.resize((round(W*span/PW), round(H*span/PH)), Image.Resampling.LANCZOS)
    mask = Image.new('L', (W,H), 0)
    mask.paste(glyph, (round(W/2-glyph.width/2), round(H*(1-CY/PH)-glyph.height/2)))
    mask = ImageChops.lighter(frame, mask)
    Image.merge('RGBA', (mask, Image.new('L',(W,H),255), Image.new('L',(W,H),222), Image.new('L',(W,H),255))).save(DEST / f'{i:02d}-{label}.png')
    preview = Image.new('RGB',(W,H), '#14101a')
    preview.paste(Image.new('RGB',(W,H),fill), (0,0), shape)
    preview.paste(Image.new('RGB',(W,H),'#F8CF78'), (0,0), ImageChops.multiply(mask,shape))
    preview = preview.resize((300,434),Image.Resampling.LANCZOS)
    sx, sy = (i%4)*400+50, (i//4)*500+15
    sheet.paste(preview,(sx,sy))
    draw.text((sx,sy+448),f'{i+1:02d}  {label}',font=font,fill='#f6d497')
    manifest.append({'petal':i,'symbol':label,'source_id':sid,'fill':fill,'glyph_rotation_degrees':i*45})
sheet.save(OUT / 'pattern-review.png')
# Seven-icon center atlas: original phase IDs are preserved, mandala appended at 6.
center = Image.new('RGB',(2048,1024),'black')
for i in range(7):
    sid = i if i < 6 else 8
    label = ['Lotus','Sunburst','Star','Sprout','Diya','People and Heart','Center Mandala'][i]
    center.paste(Image.open(BASE / f'Symbols/Initial State/Icons/{sid:02d}-{label}.png').convert('RGB'), ((i%4)*512,(i//4)*512))
center.save(BASE / 'Symbols/Initial State/Center Phases and Mandala.png')
(OUT/'layout.json').write_text(json.dumps({'petals':manifest,'gold':'#F8CF78','edge':'#FFD35A','center_fill':'#30133F','tile_size':[W,H], 'circle_radius_m':0.052,'dotted_radius_m':0.059,'dot_count':48},indent=2)+'\n')
print('Built 8 decorated masks and 7-icon center atlas.')
