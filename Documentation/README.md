# Deepavali documentation

Current implementation inspected **29 October 2026 (Asia/Singapore)**. Start with the integrated scene, `Assets/Scenes/Deepavali_PetalWheelSequence.unity`.

| Read | Purpose |
| --- | --- |
| [Scenes and running the experience](scenes.md) | Choose a scene, preview with mouse or sensor, locate settings |
| [Parameter and tooltip guide](parameter-guide.md) | Human-readable effects, defaults, ranges and timing for every M-menu control |
| [Architecture](architecture.md) | Session flow, input, restoration, flower/wheel, scroll, host boundaries |
| [Validation](validation.md) | Relevant checks, previous evidence, deployment limits |
| [Agent workflow](agent-workflow.md) | What to document after changes and how to hand off work |
| [Session summaries](sessions/README.md) | Dated decisions, changes, verification and pending work |
| [Scratchpads](scratchpads/README.md) | Temporary state and information transfer within a session |
| [Agent instructions](../AGENTS.md) | Repository-wide working conventions |
| [Documentation skill](../.agents/skills/deepavali-documentation/SKILL.md) | Reusable maintenance workflow |

## Current status

The integrated scene has center dwell, continuous screen restoration, four lamp gates, shimmer/wheel selection, eight synchronized outcomes, matching result videos, and inactivity/reset handling. Native touch is the saved input mode; press **M** for temporary mouse preview. The main flower has eight inner petals and fourteen outer leaves.

The M-menu also exposes session-local camera pitch and spin speed/cruise/slowdown controls, retaining current defaults. A screen-space rectangle fades in below the fully opened result scroll as the future AMGEN logo slot. A transparent Happy Deepavali finale now plays over the selected rangoli after scroll closing, before restoration resets. Approved logo artwork and symbol revisions remain deferred. See [presentation controls](scenes.md#presentation-controls-29-october-2026).

No Deepavali scene is currently included in [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset). Editor preview does not establish launcher/deployment readiness or actual sensor performance.

## Historical context

- [Module scope](Deepavali-module-scope.md): original requirements, host investigation and unresolved product questions. Its implementation-status statements predate the integrated session.
- [Scroll integration plan](Deepavali-scroll-integration-plan.md): design rationale and original integration evidence. Current inactivity behavior is documented in architecture.
- [Lighting report](../reports/Deepavali%20glow%20and%20lighting.md) and [research notes](../research_notes/Deepavali%20glow%20and%20lighting/): visual research.
- `output/<work-item>/`: implementation records, authored sources, screenshots, validation reports and recovery snapshots. Link to relevant evidence; do not treat every snapshot as current architecture.

Source and saved scene/prefab/settings data take precedence over stale descriptive notes. Requirements and delivered behavior are distinct: record differences instead of silently rewriting the original brief.
