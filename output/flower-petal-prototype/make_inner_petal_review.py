"""Assemble the original reference and two Blender renders for sculpt review."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
REFERENCE = Path(r'C:\Users\whkso\AppData\Local\Temp\codex-clipboard-a6c89c74-eb32-47f9-ace0-e1ab28a5e234.png')
source_copy = OUT/'inner-petal-reference-image1.png'
if not source_copy.exists():
    source_copy.write_bytes(REFERENCE.read_bytes())
canvas = Image.new('RGB', (1800, 650), '#15151b')
draw = ImageDraw.Draw(canvas)
font_path = 'C:/Windows/Fonts/segoeui.ttf'
title = ImageFont.truetype(font_path, 26)
small = ImageFont.truetype(font_path, 19)
draw.text((30, 22), 'INNER PETAL SCULPT  /  Image 1 proportion study', font=title, fill='#faf5eb')
panels = [
    ('IMAGE 1  /  Ground truth', source_copy, (405, 251), 1.08),
    ('PREVIOUS  /  Same review camera', OUT/'inner-petal-before-image1-angle.png', (560, 393), .61),
    ('PETAL2  /  Refined sculpt', OUT/'inner-petal-image1-angle.png', (560, 393), .61),
]
for i, (label, path, center, scale) in enumerate(panels):
    x = 600*i
    draw.text((x+24, 72), label, font=small, fill='#d8c49f')
    src = Image.open(path).convert('RGB')
    if i == 0:
        src = src.crop((160, 85, 650, 446))
        center = (center[0]-160, center[1]-85)
    # Registration uses the center disc as a scale landmark; reference is approximate.
    src = src.resize((round(src.width*scale), round(src.height*scale)), Image.Resampling.LANCZOS)
    panel = Image.new('RGB', (580, 464), '#15151b')
    panel.paste(src, (round(290-center[0]*scale), round(218-center[1]*scale)))
    canvas.paste(panel, (x+10, 112))
    draw.line((x+599, 108, x+599, 576), fill='#41414a', width=1)
draw.text((28, 591), 'Petal body: 263 → 220 mm long   |   182 → 163 mm wide   |   Camera: 50° elevation', font=small, fill='#f2e9db')
draw.text((28, 621), 'Shape review only. Plain materials; original outer layer retained. Unity replacement remains a separate phase.', font=small, fill='#b4b1bd')
canvas.save(OUT/'inner-petal-image1-comparison.png')
print(OUT/'inner-petal-image1-comparison.png')
