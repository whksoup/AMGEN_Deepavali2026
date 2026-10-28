"""Rootward medallions and inset gold filigree, authored in each mesh's planar UV.

Reuses the approved glyph sources. No geometry, palettes or sequence timing change.
"""
from pathlib import Path
import collections
import json
import math
import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
BASE = ROOT / 'Assets/HK_Signature/Deepavali/PetalPrototype'
DEST = BASE / 'Symbols/Rootward Inner/Decorated Masks'
DEST.mkdir(parents=True, exist_ok=True)
source = json.loads((ROOT / 'output/refined-flower-integration/source-manifest.json').read_text())
W, H, SS = 448, 640, 3
labels = ['Sprout', 'Diya', 'Star', 'Stacked Coins', 'People and Heart', 'Gift Box', 'Sunburst', 'Lotus']
source_ids = [3, 4, 2, 7, 5, 6, 1, 0]
fills = ['#B51F4B', '#B7770A', '#264BB2', '#47871D', '#C51683', '#12748F', '#918011', '#6A239C']
centers = [.46, .46, .45, .44, .45, .43, .45, .47]
sheet = Image.new('RGB', (1600, 1100), '#17131c')
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 18)
manifest = []

def polygon(mesh):
    points = []
    lookup = {}
    remap = []
    for uv in mesh['uv']:
        key = tuple(np.round(uv, 5))
        if key not in lookup:
            lookup[key] = len(points)
            points.append(uv)
        remap.append(lookup[key])
    triangles = np.array(remap)[np.array(mesh['triangles']).reshape(-1, 3)]
    counts = collections.Counter(tuple(sorted((a, b))) for t in triangles
                                 for a, b in zip(t, np.roll(t, -1)) if a != b)
    return np.array(points), [e for e, n in counts.items() if n == 1]

def to_mask(values):
    return Image.fromarray(np.uint8(np.clip(values, 0, 1) * 255)).resize((W, H), Image.Resampling.LANCZOS)

for i, mesh in enumerate(source['meshes'][:8]):
    width, length = np.array(mesh['max'])[:2] - np.array(mesh['min'])[:2]
    points, edges = polygon(mesh)
    points *= [width, length]
    x, y = np.meshgrid((np.arange(W * SS) + .5) / (W * SS) * width,
                       (1 - (np.arange(H * SS) + .5) / (H * SS)) * length)
    cy = centers[i] * length
    crossings = []
    dist2 = np.full(x.shape, np.inf)
    inside = np.zeros(x.shape, dtype=bool)
    for ia, ib in edges:
        a, b = points[ia], points[ib]
        dx, dy = b - a
        t = np.clip(((x - a[0]) * dx + (y - a[1]) * dy) / (dx * dx + dy * dy), 0, 1)
        dist2 = np.minimum(dist2, (x - a[0] - t * dx)**2 + (y - a[1] - t * dy)**2)
        if abs(dy) > 1e-12:
            inside ^= ((a[1] > y) != (b[1] > y)) & (x < dx * (y - a[1]) / dy + a[0])
            if min(a[1], b[1]) <= cy <= max(a[1], b[1]):
                crossings.append(a[0] + (cy - a[1]) * dx / dy)
    cx = (min(crossings) + max(crossings)) / 2
    local_width = max(crossings) - min(crossings)
    radius = min(.052, local_width * .295)
    dotted = radius + .0052
    dist = np.sqrt(dist2)
    radial = np.hypot(x - cx, y - cy)
    frame = np.abs(radial - radius) < .00043
    angle = np.arctan2(y - cy, x - cx)
    nearest = np.round(angle / (2 * np.pi / 44)) * (2 * np.pi / 44)
    frame |= np.hypot(x - cx - dotted * np.cos(nearest), y - cy - dotted * np.sin(nearest)) < .00105
    # A strong inset gold leaf outline plus a separate hairline toward the circle.
    # The geometric edge glow remains the existing material's independent effect.
    main_width = .0009 + .0011 * np.clip((y / length - .69) / .23, 0, 1)
    frame |= np.abs(dist - .0090) < main_width
    frame |= np.abs(dist - .0165) < .00048
    # Small botanical accent in the space opened up toward the free tip.
    tip_y = max(cy + dotted + .018, length * .83)
    tip_x = width * (-mesh['min'][0] / width)
    for offset, rx, ry in [(0, .0023, .0058), (-.0052, .0015, .0033), (.0052, .0015, .0033)]:
        frame |= np.abs(x - tip_x - offset) / rx + np.abs(y - tip_y + (0 if offset == 0 else .002)) / ry < 1
    frame &= inside & (dist > .0045)

    glyph = Image.open(BASE / f'Symbols/Initial State/Icons/{source_ids[i]:02d}-{labels[i]}.png').getchannel('R')
    glyph = glyph.crop(glyph.point(lambda p: 255 if p > 32 else 0).getbbox())
    # Upright from the installed camera, preserving each approved symbol identity.
    canvas = Image.new('L', (max(glyph.size), max(glyph.size)), 0)
    canvas.paste(glyph, ((canvas.width - glyph.width) // 2, (canvas.height - glyph.height) // 2))
    canvas = canvas.rotate(i * 45, Image.Resampling.BICUBIC, expand=True, fillcolor=0)
    physical_span = radius * 1.50 * canvas.width / max(glyph.size)
    if i == 5:
        physical_span *= .94  # Keep the diagonal gift corners clear of the circular frame.
    glyph = canvas.resize((round(W * physical_span / width), round(H * physical_span / length)), Image.Resampling.LANCZOS)
    icon = Image.new('L', (W, H), 0)
    icon.paste(glyph, (round(W * cx / width - glyph.width / 2), round(H * (1 - cy / length) - glyph.height / 2)))
    # The glyph stays within the medallion even for a rotated square icon.
    icon = ImageChops.multiply(icon, to_mask(radial < radius - .0035))
    mask = ImageChops.lighter(to_mask(frame), icon)
    mask = ImageChops.multiply(mask, to_mask(inside))
    Image.merge('RGBA', (mask, Image.new('L', (W, H), 255), Image.new('L', (W, H), 222), Image.new('L', (W, H), 255))).save(DEST / f'{i:02d}-{labels[i]}.png')

    preview = Image.new('RGB', (W, H), '#17131c')
    preview.paste(Image.new('RGB', (W, H), fills[i]), (0, 0), to_mask(inside))
    preview.paste(Image.new('RGB', (W, H), '#F8CF78'), (0, 0), mask)
    # Display at the mesh's physical aspect ratio, not the rectangular atlas tile.
    preview = preview.resize((round(430 * width / length), 430), Image.Resampling.LANCZOS)
    sx, sy = i % 4 * 400 + (400 - preview.width) // 2, i // 4 * 550 + 15
    sheet.paste(preview, (sx, sy))
    draw.text((i % 4 * 400 + 35, sy + 448), f'{i+1:02d}  {labels[i]}', font=font, fill='#f6d497')
    draw.text((i % 4 * 400 + 35, sy + 477), f'Rootward shift {(0.57-centers[i])*length*1000:.1f} mm', font=font, fill='#c2af9c')
    manifest.append(dict(index=i, name=mesh['name'], label=labels[i], width=float(width), length=float(length),
                         center_uv=[float(cx / width), centers[i]], old_center_v=.57,
                         rootward_shift_mm=float((.57-centers[i])*length*1000), radius_m=float(radius),
                         dotted_radius_m=float(dotted), dot_count=44,
                         icon_region=[float(cx/width), centers[i], float((radius-.0035)/length), .003]))

sheet.save(HERE / 'artwork-review.png')
(HERE / 'layout.json').write_text(json.dumps(manifest, indent=2) + '\n')
print('Built 8 individual physical-UV masks; rootward shifts:', [round(m['rootward_shift_mm'], 1) for m in manifest])
