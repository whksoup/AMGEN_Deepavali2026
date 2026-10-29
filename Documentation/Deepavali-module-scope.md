# AMGEN Deepavali module scope and integration

> Historical scope note. Status pointer added 29 October 2026: the integrated session, restoration, wheel, scroll and touch adapter now exist. Read [current architecture](architecture.md), [scene operation](scenes.md) and [validation limits](validation.md) for delivered behavior. Implementation-status statements below describe the earlier investigation, not the current checkout; original requirements and open product questions are retained.

Updated 27 October 2026. Planning note; no gameplay or host configuration changes have been made.

## Assignment and scope boundary

Produce an AMGEN Deepavali module within the existing LED wall application. The experience scope and interactions have already been defined. The developer's stated assignment is to code the interaction using supplied assets, rather than design a new experience.

The supplied SOW is reference material for this assignment, not authorization to implement every item immediately. It also requests visual asset creation, animation production, editable art sources and an animated walkthrough. These are broader than the stated coding assignment. Confirm who owns missing artwork, animation authoring and demo production before estimating or accepting those deliverables. Supplied artwork does not necessarily include finished Unity animations.

The SOW calls this an update to a previous Deepavali experience. Obtain that Unity source project/scene, supplied slides and latest approved feedback before deciding whether this is modification or new implementation. Their locations have not yet been established.

## Required experience from the SOW

1. Idle/attract: festive animation, participation instructions and guidance on where to stand.
2. Start: hand over a centre target initiates a short countdown.
3. Restore colour: hovering over highlighted rangoli regions progressively restores colour; restored regions remain coloured throughout the round.
4. Reveal: completion or timeout always produces the full-colour rangoli, followed by prominent Happy Deepavali animation and subtle Amgen branding.
5. Lucky screen: illuminate petals clockwise, then reveal one randomly selected blessing for this session.
6. Photo moment: show “Capture your festive blessing!” and leave enough time to read and photograph the result.
7. Reset: return automatically to the module's idle state, ready for another participant.

Approved blessing text in the supplied SOW:

- MAY PROSPERITY SHINE YOUR WAY!
- LET JOY BLOOM!
- HERE’S TO BRIGHT BEGINNINGS!
- MAY NEW POSSIBILITIES BLOOM!
- MAY PEACE AND LIGHT BE WITH YOU!
- MAY LOVE AND TOGETHERNESS SHINE!

The SOW leaves gameplay duration (30 or 45 seconds), greeting/photo duration, detection distance and participant-abandonment behaviour to testing. Start dwell/countdown duration, region dwell thresholds and interruption behaviour also need concrete values. Keep these adjustable rather than embedding them throughout code. The photo prompt does not establish a requirement for an in-app camera, image capture or photo storage.

## What the supplied reference actually contains

The Hari Raya directory contains 11 FBX files, 45 PNGs, seven materials, two Unity asset files, a font, five MP3s, one WAV, metadata files and a colour note. No .unity scenes, .cs scripts, .prefab files, standalone .anim clips or .controller files were found.

This is an art/audio reference package rather than a working Unity module example. FBX files may contain embedded animation; the inventory alone cannot establish whether they do. It also says nothing about completion of the separate Deepavali assets, whose location is still needed. Do not import the reference package merely to learn the host integration.

For each Deepavali asset, check: editable source, final exported format, transparent background if needed, resolution, separate interactable regions, grey/coloured states or masks, pivots, and any supplied animation clips or rendered sequences. A single flattened rangoli image requires additional masking or segmentation work to restore regions independently.

## How this host application works

A Unity scene is a collection of objects, visuals and attached behaviour scripts. A prefab is a reusable object template. In this project, a module would most naturally be another scene in the existing application, subject to confirmation by its maintainer.

The current Game scene already demonstrates a complete interaction module. It uses a Canvas for interface layout, C# scripts for behaviour, DOTween for transitions, and shared application services for navigation and audio. It is a useful structural reference; its antibody gameplay logic is unrelated to rangoli restoration.

### Access

Startup is the first enabled build scene. LaunchSceneController manages scene transitions and keeps itself alive using DontDestroyOnLoad. Its TriggerChangeScene(string) method starts a fade, loads the requested scene and fades back in. AudioManager also persists between scenes.

Proposed integration: create a Deepavali scene, add it to the build scene list and wire the agreed launcher entry to the host transition method. Confirm whether it needs a new menu tile, replaces a seasonal module, starts automatically, or participates in screensaver rotation. Those are product decisions, not established by the SOW.

Do not assume adding an item to LaunchSceneController.sceneList creates a working menu entry: the inspected script declares that list but does not use it to generate the menu. Check the actual launcher prefab, scene instance overrides and button callbacks when integrating. The code also contains direct SceneManager.LoadScene paths, so navigation is not completely centralized.

### Reset and exit are different operations

Session reset should clear restored-region flags, timers, countdown progress, selected blessing and transient effects, then return to Deepavali idle without leaving the scene.

Module exit should use the agreed host navigation, such as returning to Startup. LaunchSceneController currently calls LoadSceneAsync without an additive mode; this replaces the previous scene. Ordinary scene objects are destroyed, while objects explicitly marked DontDestroyOnLoad survive. Avoid making Deepavali objects persistent without a specific need.

Cleanup still needs to stop module-owned tweens, unsubscribe from shared/static input events, release temporary resources and stop or change module music through the persistent audio service. Scene replacement is not a guarantee that every retained reference, resource or external device connection has been cleaned up. Test repeated entry and exit.

Seasonal removal from the application is a separate deployment decision: remove/disable its navigation entry and, if required, exclude its scene from a later build. The existing code does not establish a downloadable plug-in or hot-install system.

### Host idle timeout needs coordination

Startup serializes timeToSwitch = 30 seconds and timeToCloseLauncher = 8 seconds. Its screen transition duration is 1 second. LaunchSceneController advances its idle timer and can move to a screensaver after inactivity. It exempts the Video Player ScreenSaver scene and active playback in VideoPlayer, but has no Deepavali-specific exemption.

This can interrupt a hands-off reveal or photo moment. Agree how the host temporarily suspends its idle transition during a Deepavali session, then restores normal idle behaviour on completion or exit. Track abandonment separately if required. A module's 30/45-second gameplay timer and the host's 30-second inactivity timer are different clocks.

### Input and wall layout

The existing UI uses Input System EnhancedTouch; the Game interaction uses legacy Input.GetTouch(0). Project settings enable both input systems. This does not verify how the Optituo sensor delivers input or whether hovering is represented as an active touch, pointer motion or a vendor-specific signal.

Obtain the sensor integration details and test continuous stationary detection, multiple hands, loss of tracking, coordinate mapping and reliable standing distance on the installation. The existing TouchAreaButton defaults to firing immediately and should not be assumed to implement the requested dwell/countdown behaviour unchanged.

Game's Canvas reference size is 2560 x 1200 with width-based scaling. Project player defaults are 1024 x 768 in fullscreen-window mode. Confirm the deployed LED wall's actual desktop resolution, scaling, active display and sensor calibration; the Canvas size alone does not specify the physical output.

## Likely implementation work

| Work | Concrete result |
| --- | --- |
| Handover and asset audit | Approved flow, previous Deepavali source, actual asset inventory, sensor test access and agreed integration entry/exit |
| Scene assembly | Camera/Canvas, rangoli regions, centre target, instructions, greeting, petals and blessing layout |
| Session controller | Explicit Idle, Countdown, Restore, Reveal, Blessing, Photo and Reset states; controlled transition and input eligibility in each |
| Interaction | Sensor/pointer mapping, centre dwell, region highlighting, progress feedback and persistent restored colours |
| Completion | Early-completion and timeout paths converge on the same guaranteed full-colour reveal |
| Animation and audio integration | Trigger supplied clips or author agreed fades, scales, particles and clockwise petal sequence; synchronize transitions |
| Host integration | Launcher route, build inclusion, shared audio, inactivity coordination and reliable exit cleanup |
| Validation and delivery | Hardware checks, repeat-session testing, packaged source/assets, working integrated build and walkthrough if assigned |

For a first implementation, use a small controller that owns the current state and timings, plus a component for each interactive region. Make visual changes follow state transitions. Keep sensor-specific conversion separate from region logic so the flow can also be tested using a mouse. This is a proposed structure, not an existing implementation.

Start with one vertical slice: launch Deepavali through Startup, hold over one target, restore one region, force a full reveal, reset and exit. Validate that on the actual sensor before expanding to all regions and polishing animation. This exposes integration problems early.

## Questions to resolve with the project owner

1. Where are the previous Deepavali Unity source, latest slides and actual Deepavali asset package?
2. Is the work limited to interaction coding and integration? Who supplies missing animation, editable art and the walkthrough?
3. How should visitors access Deepavali: menu tile, seasonal replacement, automatic launch or screensaver rotation?
4. Should session completion stay in Deepavali idle? What exits the module, and what happens when a participant leaves?
5. What input does Optituo expose, who maintains/calibrates it, and when is hardware testing available?
6. Are rangoli sections and petals already separated and animated? What are the accepted timings and dwell rules?
7. Who owns host changes, the final Windows build, installation, acceptance and rollback? What wall resolution and target machine must be supported?

Do not commit to a firm estimate until the previous implementation, asset readiness and sensor access are known. Coding an interaction over ready assets differs materially from building missing artwork, animations or a sensor adapter.

## Acceptance checks

- A stationary hand starts the countdown reliably; leaving the target follows the agreed cancellation rule.
- Restored sections stay coloured and do not register completion twice.
- Completion and timeout each reveal the entire rangoli exactly once.
- One approved blessing is selected per session and remains stable during the photo period.
- Host inactivity does not cut off the greeting or photo period.
- Reset restores every section, timer and effect to a clean initial state.
- Leaving and reopening the module does not retain old audio, callbacks, effects or session state.
- Repeated unattended sessions and sensor loss recover according to the agreed rules.
- The integrated build works at actual LED wall resolution and calibrated detection distance.

## Evidence and limits

### Approved deck and mouse prototype clarification

The user supplied `C:/Users/whkso/Downloads/20261001_SERIAL CO_DECK_PRJ2515-Amgen LED Festive Refresh.pptx` as the approved flow and confirmed that the prototype will use a mouse instead of a hand. Slides 3–6 describe a 45-second experience: centre target, countdown, highlighted regions restored by hovering, full-colour reveal, and clockwise petal illumination leading to a randomized blessing. Slides 5–8 describe six blessing themes with colours, symbols and messages.

There are unresolved differences between this approved deck and the later supplied SOW. Slide 4 permits restored regions to fade back to desaturated; the SOW explicitly requires restored colours to remain. The deck specifies 45 seconds; the SOW leaves 30 versus 45 seconds for testing. Slides 7–8 contain older blessing text and a different photo prompt. Approval of the flow alone does not establish which conflicting details supersede the other document. Confirm these before final behaviour and copy are locked.

Architecture assessment: the host scene navigation, Canvas UI, DOTween animation library, shared audio and input packages are present. A dedicated Deepavali session controller, region restoration logic, sequential highlighting, clockwise petal sequence, blessing/theme selection and reset handling still need implementation or recovery from the previous Deepavali source. Animation tooling exists; finished Deepavali animations have not been verified.

For mouse prototyping, feed pointer position and a presence flag into the region logic. Recommended default: movement positions the simulated hand and dwelling over a region activates it without clicking. Provide a separate way to simulate hand absence, such as a debug toggle. An optional held-mouse-button mode can make presence explicit. Use the same region logic later with sensor positions and tracking presence. This avoids coupling the experience to mouse clicks or a particular sensor protocol.

TouchAreaButton enables TouchSimulation, but it begins tracking on touch-down inside its own rectangle, uses a movement tolerance and does not start a new hold when an already-active pointer enters a different region. It therefore is not a drop-in implementation of the approved hover flow. MouseVisualizer only draws pointer feedback; it does not implement interaction. Host inactivity must also be coordinated explicitly rather than assuming mouse hover resets its touchscreen press listener.

Mouse testing can validate sequencing, dwell feedback, region selection, timeout, full reveal, theme selection and repeated reset. It cannot validate Optituo tracking stability, standing distance, coordinate calibration or multiple simultaneous hands.

- Scope: `C:/Users/whkso/Downloads/20261027 Hekai SOW for AMGEN Deepavali (1).txt` and the user's coding-only description.
- Reference package: `F:/Freelance_Work/amgen-LEDWall/M-A03 Hari Raya -20261027T082709Z-1-001/M-A03 Hari Raya`.
- Host implementation: `Assets/Scripts/Game/LaunchSceneController.cs`, `AudioManager.cs`, `SceneButton.cs`, `BackToStartButton.cs`, `TouchAreaButton.cs`, `Assets/Prefabs/Launcher.prefab`, and `Assets/Scenes/Startup.unity`.
- Project input configuration: `ProjectSettings/ProjectSettings.asset`; Game scene configuration was inspected through Unity MCP earlier in this chat.
- Deepavali artwork/animation readiness and sensor behaviour have not been verified. This note is an implementation outline, not a tested module or final quotation.
