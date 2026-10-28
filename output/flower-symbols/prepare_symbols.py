"""Package the approved symbol set; no generated redraw of the six existing icons.

Run from the Unity project root. New icon artwork is produced by image_gen;
Pillow here only extracts tiles, resamples coverage, and packs shader channels.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/flower-symbols'
ASSETS = ROOT / 'Assets/HK_Signature/Deepavali/PetalPrototype'
DEST = ASSETS / 'Symbols/Initial State'
LABELS = ['Lotus', 'Sunburst', 'Star', 'Sprout', 'Diya', 'People and Heart',
          'Gift Box', 'Stacked Coins', 'Center Mandala']
for directory in (DEST / 'Icons', DEST / 'Petal Masks'):
    directory.mkdir(parents=True, exist_ok=True)

existing_path = ASSETS / 'Center/Center Icons.png'
existing = Image.open(existing_path).convert('RGB')
assert existing.size == (1536, 1024)
new = Image.open(OUT / 'gift-coins-mandala-generated.png').convert('RGB')
assert new.width == 3 * new.height, 'Expected three square cells in one row'
icons = []
for index in range(6):
    x, y = (index % 3) * 512, (index // 3) * 512
    icons.append(existing.crop((x, y, x + 512, y + 512)))

for index in range(3):
    # Normalize only the newly generated glyphs to the existing set's visual size.
    tile = new.crop((index * new.height, 0, (index + 1) * new.height, new.height))
    coverage = tile.getchannel('R')
    bounds = coverage.point(lambda value: 255 if value > 32 else 0).getbbox()
    assert bounds is not None
    glyph = coverage.crop(bounds)
    target = 360 if index < 2 else 390
    scale = target / max(glyph.size)
    glyph = glyph.resize((round(glyph.width * scale), round(glyph.height * scale)), Image.Resampling.LANCZOS)
    icon = Image.new('L', (512, 512), 0)
    icon.paste(glyph, ((512 - glyph.width) // 2, (512 - glyph.height) // 2))
    icons.append(Image.merge('RGB', (icon, icon, icon)))

report = {'phase_source_sha256': hashlib.sha256(existing_path.read_bytes()).hexdigest(),
          'existing_icons_pixel_identical': [], 'symbols': []}
for index, (label, icon) in enumerate(zip(LABELS, icons)):
    icon.save(DEST / 'Icons' / f'{index:02d}-{label}.png')
    if index < 6:
        x, y = (index % 3) * 512, (index // 3) * 512
        same = ImageChops.difference(icon, existing.crop((x, y, x + 512, y + 512))).getbbox() is None
        assert same
        report['existing_icons_pixel_identical'].append(same)
    bounds = icon.getchannel('R').point(lambda value: 255 if value > 32 else 0).getbbox()
    assert bounds and min(bounds[:2]) >= 32 and max(bounds[2:]) <= 480
    report['symbols'].append({'id': index, 'label': label, 'bounds': bounds})
    if index == 8:
        continue
    # ArtworkUV covers 0.182 x 0.263 m, independent of texture aspect ratio.
    # This physical square preserves icon proportions on the original inner petal.
    width, height = 224, 480
    physical_size = 0.146
    glyph_width = round(width * physical_size / 0.182)
    glyph_height = round(height * physical_size / 0.263)
    glyph = icon.getchannel('R').resize((glyph_width, glyph_height), Image.Resampling.LANCZOS)
    coverage = Image.new('L', (width, height), 0)
    # PIL y is downward; Unity ArtworkUV v is upward, root=0 and tip=1.
    coverage.paste(glyph, (round((width - glyph_width) / 2), round(height * (1 - 0.62) - glyph_height / 2)))
    mask = Image.merge('RGBA', (coverage, Image.new('L', (width, height), 255),
                              Image.new('L', (width, height), 222), Image.new('L', (width, height), 255)))
    assert mask.getchannel('A').getextrema() == (255, 255)
    mask.save(DEST / 'Petal Masks' / f'{index:02d}-{label}.png')

# Contact sheet for reviewing shape, independent of scene palette and bloom.
board = Image.new('RGB', (1560, 1610), '#111018')
draw = ImageDraw.Draw(board)
font_path = 'C:/Windows/Fonts/segoeui.ttf'
title_font = ImageFont.truetype(font_path, 40)
label_font = ImageFont.truetype(font_path, 26)
note_font = ImageFont.truetype(font_path, 20)
draw.text((52, 28), 'Flower symbols', font=title_font, fill='#f2d9a4')
draw.text((54, 84), 'Eight side symbols + a separate center mandala', font=label_font, fill='#b8afc4')
for index, (label, icon) in enumerate(zip(LABELS, icons)):
    x, y = 30 + (index % 3) * 510, 140 + (index // 3) * 480
    draw.rounded_rectangle((x, y, x + 480, y + 450), radius=16, fill='#191522', outline='#3b3048', width=1)
    mask = icon.getchannel('R').resize((385, 385), Image.Resampling.LANCZOS)
    board.paste(Image.new('RGB', mask.size, '#f7d99c'), (x + 48, y + 2), mask)
    draw.text((x + 24, y + 382), f'{index + 1:02d}  {label}', font=label_font, fill='#f5ebd6')
    note = 'Existing phase artwork' if index < 6 else ('New side symbol' if index < 8 else 'New center artwork')
    draw.text((x + 24, y + 418), note, font=note_font, fill='#aaa0b7')
board.save(OUT / 'symbol-review.png')
report['petal_mask_size'] = [224, 480]
report['petal_icon_physical_square_m'] = 0.146
report['petal_icon_center_uv'] = [0.5, 0.62]
report['petal_mask_channels'] = {'R': 'coverage', 'G': 255, 'B': 222, 'A': 255}
(OUT / 'source-validation.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'icons': len(icons), 'petal_masks': 8, 'reused_exactly': sum(report['existing_icons_pixel_identical']), 'output': str(DEST)}))
