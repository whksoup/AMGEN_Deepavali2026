# Presentation controls evidence — 29 October 2026

Unity 6000.0.55f1, `Assets/Scenes/Deepavali_PetalWheelSequence.unity`.

- [Runtime report](runtime-validation.json): 68 passing checks, no captured errors, phase timeline. [Harness](validate-presentation.cs) runs via Unity `eval_file` in Play Mode. First round uses simulated viewport input with the native adapter temporarily disabled; later rounds force results to test lifecycle behavior. Real frames and VideoPlayers run throughout. Test tuning: pitch -15 degrees, speed 8 slots/s, cruise 8 s, slowdown 6 s, hold 3 s, inactivity 180 s until the short inactivity cleanup check. The harness restores settings and input afterward.
- [Camera/settings report](settings-camera-validation.json): 8 passing checks via Editor eval; set -15/8/8/6, saved session asset, changed live values, applied saved values, checked disk/shared wheel asset and restored the original asset. Disable/zero restore the authored view; repeated apply does not drift.
- [Menu report](menu-validation.json): 3 passing checks using a temporary virtual Input System keyboard; M opens/closes and the panel blocks restoration input. Synthetic keyboard and test input setting restored. Initial generic simulate-key tool attempts did not visibly deliver input; the virtual-device check replaced them.
- [Fresh scene reload report](reload-validation.json): 6 passing checks for saved defaults, logo reference/hidden opening, native touch, clean scene, base prompt and absence of synthetic validation devices.
- [Debug menu](debug-menu.png), [pitched spin](pitched-spin.png), [logo below scroll](logo-below-scroll.png): newly captured rendered frames, visually inspected. Spin/logo captures show the temporary -15-degree camera offset; final saved pitch is zero. The rectangle is a placeholder, not approved logo artwork.

Compilation status completed with zero errors after automatic import. First explicit compile call overlapped that reload and returned HTTP 400; the successful completion was checked separately. Saved scene reloaded and default parameters checked after test cleanup. Shared wheel settings and scroll prefab are unchanged.

No build, physical sensor, target-wall calibration or performance checks. Other scene studies were not rerun; they do not contain the integrated session controller and their assets were unchanged. [Session summary](../../Documentation/sessions/2026-10-29-presentation-controls.md) records implementation scope and handoff.
