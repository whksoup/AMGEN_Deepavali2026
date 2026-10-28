Upper-petal shingling and steadier bases

Follow-up: output/flower-rustle restores a stronger tap bend and adds coordinated
neighbor movement. The pulse settings and tests below record the earlier state.

The shared Deepavali Inner Petal prefab now uses Models/Inner Petal Shingled.asset.
This is a copy of the original FBX petal with local height z += x * tan(6 degrees).
It gives each side a consistent over/under relationship with its neighbor while
preserving the top-down silhouette, both UV channels, triangle indices, weights,
bind poses, root position, and all existing artwork. Normals and tangents are
recalculated. Vertex count remains 191; there is no added runtime geometry work.
The imported FBX and Blender source remain unchanged.

Motion, in degrees for Base / Middle / Tip:
                    Before             After
Idle lift bias      1.5 / 2 / 2        0.3 / 1 / 2
Idle lift amplitude 1 / 1.4 / 1.8      0.15 / 0.6 / 1.8
Idle sway amplitude 0.8 / 1.2 / 1.5    0.05 / 0.3 / 1.5
Pulse lift          5 / 9 / 8          0.25 / 1.5 / 10

Frequency, per-petal phase offsets, pulse timing, and the existing outer-layer
follow logic are preserved. Both the Two Layer Flower and Peacock Flower inherit
the shared prefab changes. No decorative collar is added.

Validation
- 64 actual Unity BakeMesh snapshots: rest, long idle sampling, single pulses,
  alternating pulses, all-petal pulses, startup pulses, and opposing extrema.
- Zero upper-neighbor or upper/outer intersections in those sampled poses.
- Minimum upper-neighbor gap: 10.6995 mm. Minimum upper/outer gap: 4.8409 mm.
- No renderer-bounds overflow, bone translation, or bone scaling.
- Root numerical error below 0.000017 mm in snapshot checks.
- Live check: all 8 visible petal picks selected the intended petal and pulsed it.
  Tip travel was 16.57 to 21.37 mm; root drift was zero; no runtime errors recorded.
- Verified the outline, topology, UVs, weights and bind poses match the source.
- These are sampled animation checks, not a guarantee for arbitrary future
  parameter changes. Recheck clearance if mesh shape or motion limits change.

Files
apply_shingle.cs: deterministic mesh rebuild and prefab setup via Unity MCP
  eval_file in Edit Mode. Always rebuilds from the original FBX, never accumulates
  deformation. Use this again after intentionally changing the source FBX.
snapshot_poses.cs: capture pose-snapshots.json in Play Mode; restores pose state.
check_clearance.py: exact separation test; accepts raw or gzipped snapshots.
check_runtime.cs: Play Mode picking and motion check.
capture_pulse.cs: temporary alternating-pulse render; restores pose and camera.
clearance-validation.json and runtime-validation.json: recorded test results.
before.png / after-rest.png: same Edit Mode appearance before and after geometry.
after-idle.png / after-alternating-pulse.png: live initial artwork state.
before-inner-petal.prefab and before-scene.unity: pre-change backups. The saved
  scene matches the pre-change live scene backup exactly; changes are inherited
  from the shared prefab rather than added as scene overrides.
