"""Technical texture data derived from the existing UV triangles; no mesh edits."""
import json
from collections import Counter, defaultdict
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
data = json.loads((ROOT / 'petal-uv.json').read_text())
uv = np.array(data['uv'], dtype=np.float64)
triangles = np.array(data['triangles']).reshape(-1, 3)
edges = Counter(tuple(sorted((int(a), int(b)))) for t in triangles for a, b in zip(t, np.roll(t, -1)))
boundary = [e for e, count in edges.items() if count == 1]
adj = defaultdict(list)
for a, b in boundary:
    adj[a].append(b)
    adj[b].append(a)
ordered = [min(adj, key=lambda i: uv[i, 1])]
previous = None
while True:
    current = ordered[-1]
    nxt = next(i for i in adj[current] if i != previous)
    if nxt == ordered[0]:
        break
    ordered.append(nxt)
    previous = current
outline = uv[ordered]
(ROOT / 'uv-outline.json').write_text(json.dumps(outline.tolist()))

# Portrait canvas reflects the petal's 0.182 m x 0.263 m planar UV mapping.
w, h = 1024, 1536
template = Image.new('RGB', (w, h), '#331020')
draw = ImageDraw.Draw(template)
polygon = [(float(u * (w-1)), float((1-v) * (h-1))) for u, v in outline]
draw.polygon(polygon, fill='#8f2245')
draw.line(polygon + [polygon[0]], fill='#ffdc99', width=3)
template.save(ROOT / 'petal-uv-art-guide.png')

# A 2 mm light ribbon, 3 mm inset, with subpixel antialiasing in physical units.
w, h = 512, 1024
x, y = np.meshgrid((np.arange(w)+0.5)/w * 0.182, (1-(np.arange(h)+0.5)/h) * 0.263)
distance2 = np.full((h, w), np.inf)
inside = np.zeros((h, w), dtype=bool)
physical = outline * np.array([0.182, 0.263])
for a, b in zip(physical, np.roll(physical, -1, axis=0)):
    dx, dy = b-a
    t = np.clip(((x-a[0])*dx + (y-a[1])*dy)/(dx*dx+dy*dy), 0, 1)
    distance2 = np.minimum(distance2, (x-a[0]-t*dx)**2 + (y-a[1]-t*dy)**2)
    if abs(dy) > 1e-12:
        inside ^= ((a[1] > y) != (b[1] > y)) & (x < dx*(y-a[1])/dy+a[0])
distance = np.sqrt(distance2)
t = np.clip((np.abs(distance - 0.003) - 0.00075) / 0.0005, 0, 1)
mask = (255 * (1-t*t*(3-2*t)) * inside).round().astype(np.uint8)
Image.fromarray(mask).convert('RGB').save(ROOT / 'Petal_EdgeEmission.png')
print(json.dumps({'uvBoundaryVertices': len(outline), 'maskSize': [w,h], 'litPixels': int(np.count_nonzero(mask)), 'meshChanged': False}))
