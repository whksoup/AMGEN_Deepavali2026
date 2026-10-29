# Deepavali validation

This page indexes existing evidence and defines checks for future changes. Documentation setup on **29 October 2026** inspected source/assets and historical reports; it did not rerun Unity or test hardware.

## Choose checks for the change

| Area changed | Relevant verification |
| --- | --- |
| Documentation only | Resolve local links/paths, compare facts with source/saved assets, inspect scoped diff and run `git diff --check`. Validate skill frontmatter if changed. |
| Runtime code / scene wiring | Let Unity compile in 6000.0.55f1; inspect Console and missing references. Run the affected main-scene flow and any study scene sharing the changed behavior. |
| Input / dwell | Native or simulated touch: stationary dwell, single-contact ownership, handover, release/cancel/device loss, viewport/display filtering, fresh-entry gate, M-menu blocking, mouse preview and return to sensor mode. |
| Restoration / lamp / rendering | Partial strokes, persistent coverage, all-four-lamps-plus-coverage gate, uninterrupted shimmer, camera aspect/offset, completion and reset; inspect actual rendered frames. |
| Wheel / outcomes / artwork | Verify clockwise renderer mapping and eight outcome mappings; color, icon and scroll agree; reveal finishes before delay; reset restores initial appearance. Check shader/material errors and visual output. |
| Scroll / timings | Exact variant, alpha, opening/fully-open hold/closing, failure fallback, cancellation during each scroll phase, repeated rounds. Keep Game view visible/focused for playback. |
| Inactivity / lifecycle | Stationary/jitter input does not refresh; real movement/acquisition does. Timeout during affected phases, reset at black, no stale playback/prompt callback, host ownership release on disable, repeated scene entry/exit. |
| Saved settings | Save in Editor, reload asset/re-enter Play, confirm intended values and restore temporary test settings. Mouse preview remains temporary. |
| Presentation controls / logo | Pitch orbit and authored-view reset, input/lamp gates at changed pitch, next-spin tuning without changing the active plan/shared preset, settings round-trip, logo hidden through opening and smoothly visible only after Shown; closing, playback fallback, reset, disable and inactivity cleanup. Review rendered placement below the scroll. |
| Happy Deepavali finale | Scroll is Hidden before greeting; selected rangoli remains visible; alpha and logo placement; writing/idle hold/hiding; expired inactivity cannot skip finale; reset, disable, error/stall cleanup and repeated rounds; saved hold/media settings after reload. |
| Deployment | Explicit build-scene/launcher setup, target display and resolution, physical sensor calibration/contact loss, media alpha/performance, unattended repeated sessions and host entry/exit on target hardware. |

Use the smallest checks that cover the changed behavior. A historical passing report is context, not proof that a new revision passes. Record environment, exact checks/commands, results and evidence paths in the session summary. If Unity/device checks cannot be run, state that and the unverified behavior.

## Existing evidence

| Evidence | Recorded scope / limitations |
| --- | --- |
| [Happy Deepavali finale record](../output/happy-deepavali/README.md), [runtime report](../output/happy-deepavali/runtime-validation.json), [saved/failure report](../output/happy-deepavali/saved-and-failure-validation.json) | Newly run 29 October 2026: 29 actual-video checks after saved scene reload/final import settings, and 11 reference/import/settings/fallback/watchdog checks. Forced result entry; rendered alpha overlay inspected. Full restoration input, physical wall and deployment not rerun. |
| [Presentation controls record](../output/presentation-controls/README.md), [runtime report](../output/presentation-controls/runtime-validation.json) | Newly run 29 October 2026: 68 passing checks, real frames/VideoPlayers with simulated viewport input; 8 camera/settings checks and 3 virtual-keyboard M-menu checks. Includes a 15.8 s spin and four-lamp restoration at -15 degree pitch offset. Approved logo, native device and target-wall acceptance remain pending. |
| [Restoration/session record](../output/restoration-session/README.txt) | Integration, mask/lifecycle checks and standalone prototype regressions. Some setup snapshots predate later shimmer/input changes. |
| [Scroll record](../output/diwali-scroll-integration/README.txt), [summary](../output/diwali-scroll-integration/validation-summary.json) | 271 recorded passing checks across playback, lifecycle, injected failures and mouse cycles. Includes eight variants and settings persistence. Target device was not tested. |
| [Session polish record](../output/session-polish/README.txt) | 90 recorded passing checks: flow, phase interruption, hand lifecycle, idle input and host ownership. Includes an actual 30-second inactivity wait in that run. |
| [Sensor record](../output/sensor-input/README.txt), [touch report](../output/sensor-input/touch-validation.json) | 26 recorded passing checks through simulated Input System touch events. Not a physical sensor acceptance test. The first-run report preserves an earlier failed fresh-entry assumption. |
| [Hand appearance record](../output/hand-guidance-appearance/README.txt) | Prompt appearance/dismissal lifecycle and visual evidence. |
| [Refined flower record](../output/refined-flower-integration/README.txt) | Mesh/artwork integration, sources, backups and visual comparisons. |

The `validate-*.cs` files in these output folders are task-specific Editor harness snippets, not an automatically discovered Unity Test Runner suite. Inspect their setup and execution requirements before reuse; do not move them into `Assets/` or execute scene-mutating setup scripts merely to read evidence. Preserve initial failures and later corrections distinctly.

## Outstanding acceptance work

- Physical installation: native sensor behavior, calibration, multi-contact handling, target resolution/display, continuous unattended runs and performance remain unverified by these reports.
- The current build list excludes Deepavali. A directly opened Editor scene is not an integrated deployment build.
- The original scope/deck and current implementation differ on timing and restoration flow. Current code uses activity-driven reset, not a fixed 45-second restoration countdown. Confirm the desired acceptance behavior when that work is requested.
- Inactivity still applies through wheel/reveal, but scroll delay, scroll/fallback and Happy Deepavali now defer timeout until their bounded presentation finishes. Check the complete visitor/photo experience on the wall.
