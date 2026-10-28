HISTORICAL INVESTIGATION
The user later authorized symmetric designs for all eight inner petals,
richer ornament, and stiffer base-state motion. That implementation is in
../inner-petal-regularized/README.txt. This folder retains pre-change evidence.

INNER PETAL SHAPE INVESTIGATION — 29 OCTOBER 2026

FINDING
Yes, the video-fit process overfit uncertain visible borders into the mesh.
The four questioned petals really are asymmetric in their untextured local
XY silhouettes. This is not an illusion created solely by the latest gold
pattern. However, two things need separating: broad directional lean and
small local bends. Some lean is already visible in both supplied references;
the evidence does not establish a perfectly symmetric original 3-D petal.

Gold and purple are the clearest cases of a body displaced sideways relative
to the existing root-to-tip axis. Green and cyan have milder displacement,
with extra local curvature changes on one side. The pattern fitting copies
those width and centerline variations into the decorative contours, making
the mesh irregularities more conspicuous.

REVIEW IMAGES
01-current-silhouettes.png — all eight meshes, untextured, equal scale,
root-to-tip axes and width midlines. Removes camera, shingle and artwork.
02-shape-options.png — current, conservative fairing A, balanced design B.
03-fairing-detail.png — larger A overlay, including the previous narrow roots
and the 40%-52% transition region of the approved broad-root correction.
04-reference-evidence.png — raw enlarged video pixels, original image,
and an additional oblique video frame. Crops are not perspective corrected.
05-ring-comparison.png — original saved placements with each candidate.
These are planar diagrams, not Unity or Blender renders.

MAPPING AND MEASUREMENTS
The manifest confirms mesh order and identity. Symbol labels are confirmed
by output/inner-petal-artwork/layout.json; the manifest does not contain
those symbol labels itself. Directions are the source layout directions.

Index  Mesh              Label / colour       Direction  Body offset
1      REF_TopPetal_02    Diya / gold          NE         27.7 mm / 18.5%
3      REF_TopPetal_04    Stacked Coins / green SE        13.7 mm /  7.8%
5      REF_TopPetal_06    Gift Box / cyan       SW         13.1 mm /  7.5%
7      REF_TopPetal_08    Lotus / purple       NW         25.9 mm / 17.0%

Index is zero-based. Offset means the maximum absolute midpoint between
left and right sides over 40%-90% of length, divided by full bounding width
for the percentage. It measures departure from the existing root-to-tip
axis, not a noise amplitude or an error against a known original model.
The other four petals have offsets of 3.6-7.5 mm (about 2%-4%).
All millimetres are prototype model units, not fabrication dimensions.
Reference scale was inferred from a 242 mm center-disc diameter.

CAUSES AND THEIR RELATIVE IMPORTANCE
1. Full fitting to uncertain traced contours is the primary cause.
   video-proportion-review/build_round.py sets the third-round blend to 1.0.
   Every inner profile is driven by hand-picked gold borders from the
   near-overhead frame, interpolated with a cubic Hermite/Catmull-style
   construction and sampled into a 56-row grid. This imports the reference
   render's shape, visible border choice and registration assumptions.
   The other camera views check projections of that same inferred geometry;
   they do not independently recover the hidden outline of each petal.

2. The original fitting metric rewarded matching the trace, not clean shape.
   Historical mean outline error improved from 1.32 pixels in round 2 to
   0.93 pixels in round 3. Both are smaller than the declared +/-3-pixel
   manual outline uncertainty (about +/-4.8 mm). That extra numerical fit
   cannot establish that local irregularities are real design features.
   These scores are historical, from before the approved root correction.

3. Profile interpolation contributes small bends, not the large lean.
   In the untouched upper part, current side coordinates differ from
   straight interpolation of the raw observation polygon by at most
   2.3-2.8 mm for these four petals. This is below the declared trace
   uncertainty and far smaller than the gold/purple midpoint offsets.
   Thus replacing interpolation alone cannot make the four leaves balanced.
   Raw tracing and the chosen axis already carry most of that asymmetry.

4. The broad-root join is a secondary, localized contributor.
   revise_roots.py replaces the lower profile with a broad trapezoid and
   blends to the old outline between 40% and 52% length. Direct numeric
   comparison confirms EVERY vertex above 52% is unchanged for all eight
   petals. The visible small indentation on gold's left flank and purple's
   right flank lies around this transition. It joins a centered base to
   an already displaced body. This does not invalidate the approved wide,
   flat bases; the transition just needs deliberate curve control.

5. Perspective, occlusion and shingling do not explain the planar result.
   Occluded lower borders were previously mistaken for narrow roots; this
   is documented and was corrected. The recorded near-overhead view is
   approximately 89 degrees, and camera calibration is an approximation.
   The original image and low-angle recording also have perspective and
   overlapping decorative rims. Neither is geometric ground truth.
   The implemented 0.45-degree shingle changes height, z=tan(angle)*x,
   and does not generate these XY wiggles. Unequal tip lengths/directions,
   shallow layer height and overlapping placement are deliberate decisions.
   The references support directional character; exact bilateral symmetry
   cannot be proved from them.

6. The new pattern reveals/amplifies, rather than creates, the mesh issue.
   inner-petal-pattern-guides/build_patterns.py maps guide x from the local
   boundary midpoint and half-width at each y. A midpoint bend or width
   bump therefore appears in the nested gold lines as well. This explains
   why waviness became particularly apparent in the latest artwork review.
   The independently derived planar silhouettes show the problem without it.

RECOMMENDED CORRECTION
Start with explicit curve fairing, not another exact video trace. Preserve
the approved wide, flat roots, current tips, lengths, rig placements and
overall arrangement. Fair the root-to-shoulder join and the upper sides
with a small number of controlled curves. Judge the untextured profile
first, then the complete ring, then the gold ornament.

A is the conservative first review candidate. It removes small contour
kinks while retaining the existing directional character. In this study
the lower 22/56 = 39.3% of each profile is exactly preserved, as are the
root and tip positions and overall length. Each side above that point is
a single degree-6 Bezier with constrained root and tip tangents. Maximum
lateral changes for gold/green/cyan/purple are 7.4/6.6/4.3/2.8 mm, or about
4.6/4.1/2.6/1.7 reference pixels. RMS changes over all side samples are
1.7/2.1/1.3/0.8 mm. Some maximum changes exceed the tracing uncertainty;
this is intentional design fairing, not a claim of recovered truth.
Maximum width changes are +0.04/-4.55/-3.55/+0.31 mm respectively.

A will NOT remove the obvious gold/purple lopsidedness. If the concern is
the broad asymmetric silhouette itself, use B as the design direction:
smooth, mirrored leaves with the existing individual lengths, base widths,
tip locations and target maximum widths. B deliberately recenters the
slanted root flanks and body, so it changes more of the approved profile
than A. It is a clear comparison, not a claim that the reference was exactly
symmetric. A restrained amount of asymmetry can be designed back in after
the balanced silhouette is accepted, instead of inheriting noisy borders.

After a shape is selected for implementation, use a separate source copy,
retain the planar lower surface and shallow shingle, and rebuild geometry-
dependent UV fitting, border lighting and body maps together. Refit the gold
guide from the clean contour. Recheck medallion clearance, picking, bounds,
the motion envelope and inter-petal/center clearance before replacing the
active FBX. The existing clearance report does not validate these proposals.
No shape implementation is performed by this investigation.

REPRODUCTION AND SCOPE
python output/inner-petal-shape-investigation/investigate.py
Dependencies: NumPy and Pillow. The script reads the manifest, previous
manifest, saved reference images and symbol layout. It writes only into
this investigation folder. It never loads Blender/Unity, runs build_round,
exports an FBX or modifies production assets. profile-studies.json stores
the current and both proposed 2-D profiles; measurements.json contains
definitions, measurements, checks and protected-file SHA-256 hashes.

The current Blender source hash matches the integration manifest. Hashes
of that source, the production FBX and the source manifest remain identical
before/after generation. Other chats may independently edit other project
assets; this investigation makes no editor or active-asset changes.
