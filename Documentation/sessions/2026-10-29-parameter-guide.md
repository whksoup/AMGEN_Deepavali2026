# Session: human-readable parameter and tooltip guide

- ID: `2026-10-29-parameter-guide` (Asia/Singapore)
- Status: Complete
- Request: document the impact/effect of each parameter for humans.
- Owner: primary agent; no delegation.
- Scratchpad: not needed for this bounded documentation-only change.

## Outcome and changed files

- `Documentation/parameter-guide.md`: all 17 M-menu sliders, mouse preview, camera buttons, save/load and status readouts. Includes source-checked defaults/ranges, increasing/decreasing effects, application timing, parameter interactions and explicitly illustrative tuning examples.
- `Documentation/README.md`, `Documentation/scenes.md`: links to the guide.
- `Documentation/sessions/README.md` and this summary: discoverable session record.

Existing runtime, scene, settings and earlier documentation changes were preserved. No application tooltip popups or runtime changes were requested or added.

## Decisions

Use the main scene's M-menu as the scope of “each parameter.” Keep the guide in the repository alongside operator documentation, with the exact control labels. Distinguish normal slider ranges from Inspector limits and saved defaults from live values. Explain captured durations, all-four-lamp gating, idle interruption and logo fade sharing the scroll hold.

## Verification

- Newly performed: inspected menu/settings/controller/presentation/scroll/inactivity/input/restoration source, saved settings and main-scene bindings; checked guide coverage against every slider label and saved parameter; local link/path and whitespace checks.
- Previous presentation implementation tests remain historical evidence; not rerun here.
- Unity, device and build checks were not run because only documentation changed. No Unity state was changed or queried in this session.

## Documentation and handoff

The new [guide](../parameter-guide.md) is linked from the canonical index and scene operations page. Architecture and validation policy need no change because implementation, ownership and test requirements are unchanged.

None pending within this request. Future runtime hover tooltips, if wanted, can use this guide's copy. Existing deferred logo artwork/greeting and hardware acceptance work remain separate.
