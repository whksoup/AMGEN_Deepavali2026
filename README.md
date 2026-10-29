# AMGEN Deepavali 2026

Unity project for the AMGEN interactive LED wall, including the Deepavali restoration, flower wheel and blessing-scroll experience.

## Open and preview

1. Open this repository folder in Unity Hub using **Unity 6000.0.55f1**. The project uses **URP 17.0.4** and **Input System 1.14.1**. Allow assets, packages and videos to import; source media is included directly without requiring Git LFS.
2. Open [Deepavali_PetalWheelSequence.unity](Assets/Scenes/Deepavali_PetalWheelSequence.unity) and press Play.
3. Native touch/sensor input is the saved default. For desktop testing, focus the Game view, press **M** and enable **Mouse preview (temporary)**.
4. Dwell over the center prompt, restore the image and all four lamps, then leave/re-enter the second prompt to spin. The selected flower color/icon and matching scroll share one of eight outcomes. The session resets after the scroll or on inactivity.

See [scene controls and settings](Documentation/scenes.md) for the full flow and the roles of all nine Deepavali scenes. `Deepavali_PaletteTest.unity` remains the direct appearance review scene; `Deepavali.unity` is an older placeholder.

**Deployment status:** Deepavali is not currently in the build scene list. Previous Editor/simulated-touch evidence does not establish physical sensor, target-wall performance or launcher/deployment readiness.

## Documentation

- [Documentation index](Documentation/README.md)
- [Architecture and session flow](Documentation/architecture.md)
- [Validation evidence and remaining checks](Documentation/validation.md)
- [Agent instructions](AGENTS.md) and [documentation workflow](Documentation/agent-workflow.md)
- [Session summaries](Documentation/sessions/README.md) and [in-session scratchpads](Documentation/scratchpads/README.md)
- [Project documentation skill](.agents/skills/deepavali-documentation/SKILL.md)

## Sources and implementation records

- [Deepavali assets and runtime code](Assets/HK_Signature/Deepavali): petal appearance/motion, restoration/session, background and hand guidance.
- [Scroll overlay and media](Assets/DiwaliScroll): eight video variants and playback component.
- [Refined flower integration](output/refined-flower-integration/README.txt): 8 inner petals and 14 outer leaves, fitted artwork/light maps, source and review evidence. Editable Blender source: `output/flower-petal-prototype/Deepavali_InnerPetal_VideoFit.blend`.
- [Restoration/session integration](output/restoration-session/README.txt), [scroll integration](output/diwali-scroll-integration/README.txt), [session polish](output/session-polish/README.txt), [sensor input](output/sensor-input/README.txt).
- [Original scope](Documentation/Deepavali-module-scope.md) and [scroll plan](Documentation/Deepavali-scroll-integration-plan.md): historical requirements and decisions, with links to current behavior.

The repository also contains the existing wall scenes and media. Unity caches and local Editor settings are regenerated locally.
