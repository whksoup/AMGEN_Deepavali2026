Deepavali result scroll integration
=================================

Main scene: Assets/Scenes/Deepavali_PetalWheelSequence.unity
Shared mapping: Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Light Sequence/Deepavali Outcomes.asset
Saved timing/session settings: Assets/HK_Signature/Deepavali/Restoration/Deepavali Session Settings.asset
Overlay prefab: Assets/DiwaliScroll/Prefabs/Diwali Scroll Canvas.prefab

Flow
----
The existing restoration and wheel sequence continue unchanged up to the result.
The flower finishes its color and symbol reveal, waits scrollDelayAfterResultSeconds
(default 2), plays the matching opening clip, holds fully open for scrollHoldSeconds
(default 15), closes, and calls the existing endSequence reset. The former 10-second
coloredHoldDuration is now used only if no scroll is assigned or playback fails.

Tune the public fields on Deepavali Session in Play Mode, then use Save to
ScriptableObject in its Inspector. Load saved settings restores the stored values.
The settings asset is loaded automatically on Play. Saving project assets is
Editor-only. The 15-second hold excludes the opening and closing clips.

Clockwise mapping
-----------------
Sprout / Red; Diya / Orange; Star / Blue; Stacked Coins / Green;
People and Heart / Pink; Gift Box / Cyan; Sunburst / Gold (Yellow scroll);
Lotus / Purple. The main flower's outcomeSet is the shared source of this mapping.
Existing palette enum values are preserved. The old style.outcomes array remains
available to legacy scenes without outcomeSet and is ignored by the main mapping.

Artwork
-------
Existing isolated icon sources supply the nine-tile result atlas (eight outcomes
plus the opening mandala). The result glyph is composited inside each inner
petal's existing fitted medallion using the same radial progress as the color.
Opening fills derive from their matching result palette, keeping their authored
ornament and edge settings. Outer artwork and all meshes remain unchanged.
No normal per-petal symbol assignments are overwritten during a round.

Playback
--------
Eight explicit named variants, three reusable VideoPlayers, preserved alpha,
first-frame handoff, unscaled timing, no participant click toggles or random
scroll selection. Preparation and playback stalls time out. Reset, disabling or
scene exit stops pending playback and clears the image. A playback failure keeps
the selected flower for the fallback hold, then resets; it never selects another
blessing. The host inactivity countdown is refreshed while the session is active.

Backups and evidence
--------------------
before-implementation/ contains the pre-change scene and affected source files.
package-audit.json identifies the supplied archive and original asset GUIDs.
scene-setup.json records the main-scene bindings and imported clip metadata.
playback-validation.json: 216 checks passed across all eight variants plus a repeated
Purple round with zero delay and zero hold. The default round measured 2.013 seconds
delay, 5.598 seconds opening, 15.011 seconds fully open and 3.414 seconds closing.
lifecycle-validation.json: 19 checks passed, including save-to-disk/reload of both
timings, input validation and cancellation during each of the four scroll phases.
failure-validation.json: 5 checks passed. Missing clips and a deliberately paused
player recover through the bounded fallback; a subsequent same-color round works.
The two logged errors in that report are expected fault injections.
mouse-cycle/session-validation.json: 31 checks passed through two complete sessions
using Input System mouse events, including both restoration gates, fresh-hover
requirements, the real wheel spin, full scroll timing and automatic reset.
These four reports contain 271 passing assertions and no unexpected errors.
Runtime scripts compile successfully and the changed petal shader reports no errors.
import-validation.json: all 24 clips use VP8 transcoding with alpha preserved.
result-*.png and scroll-*.png show each mapped result and its matching scroll.

Validation runs in Unity 6000.0.55f1 Play Mode. Keep Unity's Game view shown and
focused while running the supplied Editor test harnesses; an unfocused Editor
video subsystem can remain paused even while forced Editor ticks advance frames.
No deployment build or LED-wall performance/legibility test has been performed.
The project's existing build-scene list is unchanged and does not yet include
the main Deepavali scene; deployment selection remains a separate project step.
