"""Offline silhouette measurements and review diagrams. No editor or asset writes.

Run from any directory. Outputs are restricted to this script's directory.
The proposals are 2-D profile studies, not replacement production meshes.
"""
from pathlib import Path
import hashlib
import json
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
INTEGRATION = ROOT / 'output/refined-flower-integration'
VIDEO = ROOT / 'output/flower-petal-prototype/video-proportion-review'
SOURCE = json.loads((INTEGRATION / 'source-manifest.json').read_text())
BEFORE = json.loads((INTEGRATION / 'root-correction/before-source-manifest.json').read_text())
REF = json.loads((VIDEO / 'reference-measurements.json').read_text())
LAYOUT = json.loads((ROOT / 'output/inner-petal-artwork/layout.json').read_text())
SUSPECTS = [1, 3, 5, 7]
DIRECTIONS = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW']
COLORS = ['#b51f4b', '#b7770a', '#264bb2', '#47871d', '#c51683', '#12748f', '#918011', '#6a239c']
BG, PANEL, TEXT, MUTED = '#121820', '#1b2430', '#edf2f7', '#a8b6c8'
OLD, NEW, BALANCED = '#f5a36e', '#61ded0', '#b7b7ff'
FONT = 'C:/Windows/Fonts/segoeui.ttf'
MONO = 'C:/Windows/Fonts/consola.ttf'


def font(n=22, mono=False):
    return ImageFont.truetype(MONO if mono else FONT, n)


def label(draw, xy, text, size=22, color=TEXT):
    draw.text(xy, text, font=font(size), fill=color)


def save(im, name):
    im.save(HERE / name)


def get_profile(mesh):
    verts = np.asarray(mesh['vertices'])
    length = verts[:, 1].max()
    row = np.rint(verts[:, 1] / length * 56).astype(int)
    sides = np.array([[verts[row == k, 0].min(), verts[row == k, 0].max()] for k in range(57)])
    return length, np.arange(57) / 56, sides, row


def fair(t, sides):
    """Degree-6 Bezier per side; root and tip fixed, endpoint tangents constrained."""
    start, degree = 22, 6
    q = (t[start:] - t[start]) / (1 - t[start])
    basis = np.array([math.comb(degree, k) * (1-q)**(degree-k) * q**k for k in range(degree+1)]).T
    out = sides.copy()
    for side in range(2):
        p0 = sides[start, side]
        p1 = p0 + (sides[start, side] - sides[start-1, side]) * 56 * (1-t[start]) / degree
        tip_slope = np.dot(t[-4:-1]-1, sides[-4:-1, side]) / np.sum((t[-4:-1]-1)**2)
        p5 = -tip_slope * (1-t[start]) / degree
        residual = sides[start:, side] - basis[:, 0] * p0 - basis[:, 1] * p1 - basis[:, 5]*p5
        middle = np.linalg.lstsq(basis[:, 2:5], residual, rcond=None)[0]
        out[start:, side] = basis @ np.r_[p0, p1, middle, p5, 0.0]
    return out


def hermite(q, a, b, da, db, span):
    return (2*q**3-3*q*q+1)*a+(q**3-2*q*q+q)*span*da+(-2*q**3+3*q*q)*b+(q**3-q*q)*span*db


def balance(t, sides):
    """Deliberately designed symmetric comparison; base, length and width retained.

    Lower root is a broad straight trapezoid with no waist; shoulder at 54%.
    This changes the slanted root flanks, unlike the conservative fairing.
    """
    h0 = (sides[0, 1]-sides[0, 0])/2
    hm = (sides[:, 1].max()-sides[:, 0].min())/2
    join, shoulder = .36, .54
    slope = (hm-h0)/shoulder
    hj = h0+slope*join
    h = np.empty_like(t)
    low = t <= join
    mid = (t > join) & (t <= shoulder)
    high = t > shoulder
    h[low] = h0+slope*t[low]
    h[mid] = hermite((t[mid]-join)/(shoulder-join), hj, hm, slope, 0, shoulder-join)
    h[high] = hermite((t[high]-shoulder)/(1-shoulder), hm, 0, 0, -.35*hm/(1-shoulder), 1-shoulder)
    return np.column_stack([-h, h])


def polygon(t, sides, length):
    return np.vstack([np.column_stack([sides[:, 0], t*length]), np.column_stack([sides[::-1, 1], t[::-1]*length])])


def draw_shape(draw, t, sides, length, origin, scale, fill=None, outline=None, width=3):
    p = polygon(t, sides, length)
    pix = np.column_stack([origin[0]+p[:, 0]*scale, origin[1]-p[:, 1]*scale])
    pts = [tuple(a) for a in pix]
    if fill:
        draw.polygon(pts, fill=fill)
    if outline:
        draw.line(pts+[pts[0]], fill=outline, width=width, joint='curve')


def dashed(draw, a, b, fill, width=1, dash=8):
    a, b = np.asarray(a), np.asarray(b)
    distance = np.linalg.norm(b-a)
    for k in np.arange(0, distance, dash*2):
        draw.line([tuple(a+(b-a)*k/distance), tuple(a+(b-a)*min(k+dash, distance)/distance)], fill=fill, width=width)


def cross_raw(obs, ys):
    result = []
    for yy in ys:
        xs = []
        for a, b in zip(obs, np.roll(obs, -1, axis=0)):
            if min(a[1], b[1]) <= yy <= max(a[1], b[1]) and abs(a[1]-b[1]) > 1e-9:
                xs.append(a[0]+(b[0]-a[0])*(yy-a[1])/(b[1]-a[1]))
        result.append([min(xs), max(xs)] if xs else [float('nan')]*2)
    return np.array(result)


def hash_file(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


protected = [INTEGRATION/'source-manifest.json', ROOT/'output/flower-petal-prototype/Deepavali_InnerPetal_VideoFit.blend', ROOT/'Assets/HK_Signature/Deepavali/PetalPrototype/Models/Refined Flower/Deepavali_RefinedFlower.fbx']
hashes = {str(p.relative_to(ROOT)): hash_file(p) for p in protected}
records = []
profiles = []
for i, mesh in enumerate(SOURCE['meshes'][:8]):
    length, t, sides, row = get_profile(mesh)
    proposal = fair(t, sides)
    symmetric = balance(t, sides)
    width = sides[:, 1].max()-sides[:, 0].min()
    c = sides.mean(axis=1)
    region = (t >= .4) & (t <= .9)
    obs = np.column_stack([REF['inner'][DIRECTIONS[i]]['local_x'], REF['inner'][DIRECTIONS[i]]['local_radial_y']])
    raw = cross_raw(obs, .082+t*length)
    upper = (t >= .52) & (t < 1)
    interp_delta = (sides-raw)[upper]
    interp_delta = interp_delta[np.isfinite(interp_delta)]
    delta = proposal-sides
    def turns(x):
        # Discrete curvature signs, excluding root and final tip; diagnostic only.
        return [int(np.count_nonzero(np.diff(np.sign(np.diff(x[24:54, k], n=2))))) for k in range(2)]
    records.append(dict(index=i, name=mesh['name'], label=LAYOUT[i]['label'], direction=DIRECTIONS[i],
                        width_mm=float(width*1000), length_mm=float(length*1000), base_width_mm=float(np.diff(sides[0])[0]*1000),
                        max_body_midline_offset_mm=float(np.max(abs(c[region]))*1000),
                        max_body_midline_offset_percent_width=float(np.max(abs(c[region]))/width*100),
                        curvature_sign_changes_left_right=turns(sides), fair_curvature_sign_changes_left_right=turns(proposal),
                        cubic_vs_raw_polyline_max_mm=float(np.max(abs(interp_delta))*1000),
                        cubic_vs_raw_polyline_rms_mm=float(np.sqrt(np.mean(interp_delta**2))*1000),
                        fair_max_lateral_change_mm=float(np.max(abs(delta))*1000),
                        fair_max_lateral_change_reference_px=float(np.max(abs(delta))/REF['meters_per_pixel']),
                        fair_rms_lateral_change_mm=float(np.sqrt(np.mean(delta**2))*1000),
                        fair_width_mm=float(np.ptp(proposal)*1000),
                        fair_root_rows_0_22_exact=bool(np.array_equal(sides[:23], proposal[:23])),
                        fair_tip_exact=bool(np.array_equal(sides[-1], proposal[-1])),
                        upper_vertices_unchanged_by_previous_root_fix=bool(np.array_equal(np.array(mesh['vertices'])[t[row]>=.52],np.array(BEFORE['meshes'][i]['vertices'])[t[row]>=.52]))))
    profiles.append(dict(index=i, name=mesh['name'], length_m=length, t=t.tolist(), current=sides.tolist(),
                         proposal_A_fair=proposal.tolist(), proposal_B_balanced=symmetric.tolist()))
    assert np.all(proposal[:-1, 1] > proposal[:-1, 0])
    assert np.all(symmetric[:-1, 1] > symmetric[:-1, 0])
    assert records[-1]['fair_root_rows_0_22_exact'] and records[-1]['fair_tip_exact']

# All eight local planar silhouettes, equal scale and zero textures/projection.
im = Image.new('RGB', (1680, 1030), BG); draw = ImageDraw.Draw(im)
label(draw, (40, 22), 'The irregularity exists in the mesh', 34)
label(draw, (40, 72), 'Untextured local XY outlines at equal scale. Dashed line = root-to-tip axis; pale line = midpoint between sides.', 21, MUTED)
for i, p in enumerate(profiles):
    t, sides, L = np.array(p['t']), np.array(p['current']), p['length_m']
    x, y = i%4*420, i//4*445+125
    draw.rounded_rectangle((x+16,y,x+404,y+423),radius=12,fill=PANEL)
    scale, base = 1010, (x+207,y+329)
    draw_shape(draw,t,sides,L,base,scale,fill=COLORS[i],outline='#c2cad4',width=2)
    dashed(draw,(base[0],base[1]),(base[0],base[1]-L*scale),'#ffffff',1)
    points = list(zip(base[0]+sides.mean(axis=1)*scale, base[1]-t*L*scale))
    draw.line(points,fill='#f8e4bd',width=3)
    label(draw,(x+34,y+345),f'{i+1:02d}  {LAYOUT[i]["label"]}',24)
    label(draw,(x+34,y+381),f'Midline offset: {records[i]["max_body_midline_offset_mm"]:.1f} mm',20,MUTED)
save(im,'01-current-silhouettes.png')

# Separate profile proposals. No textures to bias the shape judgment.
im = Image.new('RGB',(1680,1170),BG);draw=ImageDraw.Draw(im)
label(draw,(40,22),'Two separate shape studies for the four questioned petals',33)
label(draw,(40,71),'A smooths the existing lean. B tests a deliberately balanced design. Neither is applied to the project.',22,MUTED)
for col,i in enumerate(SUSPECTS):
    x=col*420
    label(draw,(x+30,116),f'{LAYOUT[i]["label"]}  /  {DIRECTIONS[i]}',25)
    p=profiles[i]; t=np.array(p['t']);L=p['length_m']
    for r,(key,title) in enumerate([('current','Current mesh'),('proposal_A_fair','A  /  Conservative fairing'),('proposal_B_balanced','B  /  Balanced design')]):
        y=159+r*304
        draw.rounded_rectangle((x+15,y,x+405,y+292),radius=12,fill=PANEL)
        base=(x+208,y+263);scale=690
        draw_shape(draw,t,np.array(p[key]),L,base,scale,fill=COLORS[i])
        if r:
            draw_shape(draw,t,np.array(p['current']),L,base,scale,outline=OLD,width=2)
            draw_shape(draw,t,np.array(p[key]),L,base,scale,outline=NEW if r==1 else BALANCED,width=3)
        dashed(draw,(base[0],base[1]),(base[0],base[1]-L*scale),'#c1c9d4',1)
        label(draw,(x+30,y+10),title,20)
label(draw,(40,1091),'Orange = current. Teal = A. Lavender = B. All use the same root and tip positions and the same length.',21,MUTED)
label(draw,(40,1126),'A retains the lower 39.3% exactly. B retains broad base width, but recenters the slanted root flanks and body.',21,MUTED)
save(im,'02-shape-options.png')

# Detailed A overlay and the pre-root-correction contour to locate the splice.
im=Image.new('RGB',(1680,850),BG);draw=ImageDraw.Draw(im)
label(draw,(40,23),'Conservative fairing: inspect the shoulder and the root join',31)
label(draw,(40,70),'Orange = current; teal = A; gray = the old narrow-root video fit. Coordinates are model-local, in millimetres.',21,MUTED)
for col,i in enumerate(SUSPECTS):
    x=col*420;p=profiles[i];t=np.array(p['t']);L=p['length_m'];s=np.array(p['current']);a=np.array(p['proposal_A_fair'])
    label(draw,(x+28,119),LAYOUT[i]['label'],26)
    base=(x+190,655);scale=1560
    _,_,old,_=get_profile(BEFORE['meshes'][i])
    draw_shape(draw,t,s,L,base,scale,fill=PANEL,outline=OLD,width=3)
    draw_shape(draw,t,old,L,base,scale,outline='#5b6775',width=2)
    draw_shape(draw,t,a,L,base,scale,outline=NEW,width=3)
    for frac,colr in [(.4,'#617a90'),(.52,'#617a90')]:
        yy=base[1]-L*frac*scale
        dashed(draw,(x+23,yy),(x+390,yy),colr)
        label(draw,(x+28,yy-23),f'{frac:.0%}',17,MUTED)
    dashed(draw,(base[0],base[1]),(base[0],base[1]-L*scale),'#617a90')
    r=records[i]
    label(draw,(x+26,693),f'Max edge move: {r["fair_max_lateral_change_mm"]:.1f} mm',22)
    label(draw,(x+26,729),f'~{r["fair_max_lateral_change_reference_px"]:.1f} reference pixels',20,MUTED)
label(draw,(40,797),'The prior root fix blends between 40% and 52%; it leaves every vertex above 52% unchanged.',22,MUTED)
save(im,'03-fairing-detail.png')

# Raw reference evidence. Cropping only; no geometric correction of observations.
im=Image.new('RGB',(1680,1080),BG);draw=ImageDraw.Draw(im)
label(draw,(40,24),'The input is a rendered reconstruction, not a clean geometric outline',31)
label(draw,(40,71),'Raw pixels below: same four diagonal petals. Their decorative borders, overlaps and perspective are ambiguous.',21,MUTED)
anchor=Image.open(VIDEO/REF['anchor']).convert('RGB')
for col,i in enumerate(SUSPECTS):
    obs=np.array(REF['inner'][DIRECTIONS[i]]['pixels']);lo=obs.min(axis=0)-9;hi=obs.max(axis=0)+10
    crop=anchor.crop(tuple(np.r_[lo,hi])).resize((round((hi[0]-lo[0])*1.9),round((hi[1]-lo[1])*1.9)),Image.Resampling.NEAREST)
    sx=col*420+(420-crop.width)//2
    im.paste(crop,(sx,164));label(draw,(col*420+30,119),LAYOUT[i]['label'],25)
label(draw,(40,477),'Near-overhead frame at 27.5 s. Enlarged with nearest-neighbor sampling to expose the source resolution.',21,MUTED)
original=Image.open(ROOT/'output/flower-petal-prototype/inner-petal-reference-image1.png').convert('RGB')
orig=original.crop((170,105,635,417));orig.thumbnail((710,480))
im.paste(orig,(70,570));label(draw,(70,515),'Original supplied image (unrectified)',24)
oblique=Image.open(VIDEO/'reference-05-009.3s.png').convert('RGB').crop((280,236,1022,606))
oblique.thumbnail((800,420));im.paste(oblique,(825,570));label(draw,(825,515),'Video at 9.3 s (unrectified)',24)
label(draw,(40,1000),'Both references support broad teardrop forms and some directional lean. Neither resolves hidden roots or true bilateral symmetry.',21,MUTED)
label(draw,(40,1036),'Saved trace uncertainty: +/-3 pixels, equivalent to about +/-4.8 mm in the prototype scale.',21,MUTED)
save(im,'04-reference-evidence.png')

# Orthographic ring study, using original saved placement matrices. No fake 3-D shading.
im=Image.new('RGB',(1740,700),BG);draw=ImageDraw.Draw(im)
label(draw,(32,18),'Check the shape decision in the complete inner ring',30)
label(draw,(32,64),'True orthographic XY diagram; original placements; only the four questioned petals differ in A and B.',20,MUTED)
for pane,key in enumerate(['current','proposal_A_fair','proposal_B_balanced']):
    cx,cy=pane*580+290,399
    label(draw,(pane*580+32,112),['Current','A  /  Preserve the lean','B  /  Balanced silhouettes'][pane],25)
    for i,p in enumerate(profiles):
        use=key if i in SUSPECTS else 'current'
        poly=polygon(np.array(p['t']),np.array(p[use]),p['length_m'])
        mat=np.array(SOURCE['meshes'][i]['matrix'])
        world=np.c_[poly,np.zeros(len(poly)),np.ones(len(poly))]@mat.T
        pix=list(zip(cx+world[:,0]*615,cy-world[:,1]*615))
        draw.polygon(pix,fill=COLORS[i]);draw.line(pix+[pix[0]],fill='#a2afbc',width=2)
    disc=.121*615
    draw.ellipse((cx-disc,cy-disc,cx+disc,cy+disc),fill='#302d41',outline='#a2afbc',width=2)
label(draw,(32,660),'Profile studies only. Overlap drawing order is illustrative; clearance, motion, UVs and final lighting have not been revalidated.',20,MUTED)
save(im,'05-ring-comparison.png')

report = dict(
    scope='Offline investigation. No live editor access. Proposals are 2-D studies only.',
    mapping_source='source-manifest.json supplies ordered mesh identities; inner-petal-artwork/layout.json supplies symbol labels.',
    measurements=records,
    definitions=dict(max_body_midline_offset='max abs((left+right)/2) over 40%-90% length; relative to existing root-to-tip axis, not a proof of surface asymmetry or noise.',
                     curvature_sign_changes='Signs of second differences of side x at rows 24..53. Sensitive to sampling; not a pass/fail quality score.',
                     units='Prototype model millimetres, not physical fabrication dimensions; scale fixed by assumed 242 mm center diameter.',
                     fair='Degree-6 Bezier least-squares profile per side above row 22/56 with fixed initial position/tangent, fixed tip, and tip tangent fitted to last three non-tip rows. No production validation.',
                     balanced='Mirrored, piecewise cubic profile; shoulder at 54%; preserves base width and target maximum width, changes root flank lateral slope.'),
    uncertainty=dict(manual_reference_pixels=REF['manual_outline_uncertainty_px'], millimetres_per_reference_pixel=REF['meters_per_pixel']*1000),
    source_blend_hash_matches_manifest=hashes[str(protected[1].relative_to(ROOT))] == SOURCE['sha256'],
    protected_file_sha256_before=hashes,
    protected_file_sha256_after={str(p.relative_to(ROOT)): hash_file(p) for p in protected},
)
assert report['protected_file_sha256_before'] == report['protected_file_sha256_after']
(HERE/'measurements.json').write_text(json.dumps(report,indent=2)+'\n')
(HERE/'profile-studies.json').write_text(json.dumps(profiles,indent=2)+'\n')
print(json.dumps({'protected_files_unchanged':True,'source_blend_matches_manifest':report['source_blend_hash_matches_manifest'],'suspects':[records[i] for i in SUSPECTS]},indent=2))
