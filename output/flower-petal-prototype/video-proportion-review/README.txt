LATEST HANDOFF: inner roots corrected and Unity integration implemented. See
../../refined-flower-integration/README.txt. The merge and round records below
are historical; the corrected source retains the merged 14-leaf outer ring.

LATEST EDIT - TOP OUTER CLUSTER MERGED

The current ../Deepavali_InnerPetal_VideoFit.blend has 14 outer leaves.
OuterPetal_01, OuterPetal_02 and OuterPetal_16 were combined into one larger
single-tip north leaf, retaining OuterPetal_01 and its three-bone rig.
Its width is 267.06 mm, up from the old central leaf's 137.96 mm; length
and tip height remain unchanged. The other leaves and center are unchanged.
The former side leaves and their rigs were removed from the active sculpt.
before-top-merge.blend preserves the prior 16-leaf version.

top-merged-view-15.png, top-merged-view-5.png and top-merged-view-9.png show
the change in the existing overhead, oblique and low-angle cameras.
merge_top_outer.py reproduces the edit once from the unmerged review file.
top-merge-validation.json records geometry, weight, anchoring and unchanged
mesh checks. Unity replacement remains deferred.

The three-round records below are historical. To rebuild those versions,
start from before-top-merge.blend or before-video-fit.blend, rather than
running build_round.py directly on the merged 14-leaf scene.

VIDEO PROPORTION REVIEW - THREE BUILD / JUDGE ROUNDS (BEFORE TOP MERGE)

Final file: ../Deepavali_InnerPetal_VideoFit.blend
Current sculpt: 8 individually fitted inner petals and 16 outer petals.
Reference: user recording 2026-10-28 21-57-14.mp4. Original Image 1 remains
the artwork reference; the video supplies the additional angles.

ROUND 1 BUILD: Fit per-position inner tip radius, width and direction; correct
outer count to 16; reduce the exaggerated layer height.
ROUND 1 JUDGE: Tips align; straight-sided diagonal bodies do not. Proceed to
individual shoulder / neck contour fitting.

ROUND 2 BUILD: Blend 80% toward traced individual contours, narrow outer
bodies 6%, reduce shingle to 0.8 degrees.
ROUND 2 JUDGE: Much closer silhouettes; remaining upper-diagonal neck and
lower-left shoulder errors. Outer spacing is still mechanically uniform.

ROUND 3 BUILD: Complete contour fit; individual outer spacing and widths;
0.45 degree shallow shingle. Fit 14 readable outer silhouette landmarks and
infer two partly occluded far-side tips to preserve the confirmed count 16.
ROUND 3 JUDGE: Retain as the review candidate. See numerical evidence below.

MEASUREMENTS (inner visible-silhouette overlap with hand-marked video borders)
Round    Overhead       Three main views    Mean outline error, source pixels
1        77.6%          76.7%               4.56
2        93.5%          89.9%               1.32
3        96.4%          92.0%               0.93

These are fitting scores against approximate manual observations, not an
accuracy certificate for the unseen source geometry. Borders carry about
3 pixels of selection uncertainty. The low-angle view uses manually matched
symbol centers after automatic temporal tracking was rejected. Cameras are
approximate and use separate fitted focal lengths to minimize registration error.

Five saved review cameras:
REVIEW_Video_15: near overhead, about 89 degrees, 27.46 s.
REVIEW_Video_5: oblique, about 37 degrees, 9.33 s (default).
REVIEW_Video_9: low angle, about 20 degrees, 16.58 s.
REVIEW_Video_2 and REVIEW_Video_0: additional check views.

INNER POSITION DIMENSIONS (prototype units; center disc diameter = 242 mm)
Position    Root-to-tip length    Maximum body width    Tip radius    Direction
N            231.02 mm           157.84 mm        313.02 mm       0.89 deg
NE           235.97 mm           143.43 mm        317.97 mm      38.61 deg
E            271.33 mm           182.46 mm        353.33 mm      89.48 deg
SE           287.38 mm           177.23 mm        369.38 mm     141.03 deg
S            282.67 mm           181.87 mm        364.67 mm     181.01 deg
SW           314.23 mm           174.95 mm        396.23 mm     222.52 deg
W            271.69 mm           175.12 mm        353.69 mm     272.61 deg
NW           234.23 mm           148.39 mm        316.23 mm     323.71 deg

VALIDATION / HANDOFF
24 petal meshes: no loose vertices, degenerate faces, inverted faces or
nonmanifold interior edges. All weights normalize, with at most 2 influences.
Three bones per petal remain accessible. Four sampled poses keep root drift
below 0.000001 mm and all 16 outer driver chains evaluate correctly.
Total petal triangles: 19,136; this is a sculpt review mesh, not a device benchmark.
Center diameter is unchanged; its relief is reduced for the low-angle match.
UV channel names remain ArtworkUV and PetalCoord_Width_Length, with rebuilt coordinates.

No Unity Assets or FBX files were changed. The later integration must update
bind poses, 16-petal motion mapping, artwork UVs, picking, renderer bounds and
layer clearance under the actual runtime motion settings.

FILES / REBUILD
round-1.blend, round-2.blend, round-3.blend preserve each completed build.
round-N-build.json and round-N-judge.json separate build parameters and judgment.
round-N-comparison.jpg pairs the recording and sculpt at three angles.
round-N-overlay-V.png uses cyan = reference, magenta = sculpt.
Frozen reference-measurements.json holds the accepted pixel observations and cameras.
build_round.py runs inside Blender with ROUND = 1, 2 or 3; always rebuilds from
sculpt-baseline.json, so dimensions do not accumulate between rounds.
judge_round.py N runs in normal Python/OpenCV and does not modify Blender.
validate_final.py runs inside Blender. before-video-fit.blend is the original backup.
measure_reference.py and register_video_views.py are reference preparation tools;
do not rerun them over the accepted observations without reviewing their output.

