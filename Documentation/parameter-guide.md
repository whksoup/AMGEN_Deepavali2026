# Deepavali controls: parameter and tooltip guide

For people reviewing or tuning **Deepavali_PetalWheelSequence**. Press **M** in the focused Game view to open or close the menu. This guide covers every parameter in that menu, in menu order. Values were checked against the scripts and saved session settings on **29 October 2026**.

The descriptions below can also serve as tooltip copy. They document the existing controls; this change does not add hover popups to the application.

## Before adjusting anything

Changes are temporary until you press **Save to ScriptableObject** in the Unity Editor. That saves all session parameters together for subsequent Play sessions. **Load saved** reapplies the saved values; it does not mean “restore factory defaults” and does not restart the interaction. Closing the menu keeps your current adjustments.

The **Default** column below records the currently saved baseline, not a promise that your live value still matches it. **Slider range** describes the normal M-menu range. Values entered through the Inspector can differ; the menu expands its displayed range to accommodate an existing value. Opening the menu alone does not change it.

## Input and camera

| Control | Default / slider range | Tooltip and visible effect |
| --- | --- | --- |
| **Mouse preview (temporary)** | Off | Use the mouse position to try the interaction without a touchscreen. Hover over the hand prompt to dwell, then move across the image to restore it; a held mouse button is not required. Turn this off to return to native touch input. This switch is excluded from the session settings save. |
| **Pitch offset (degrees)** | **0°** / -45° to +20° | Change the viewing angle around the flower while keeping the camera's distance from its center. Negative values give a lower, more oblique view; positive values give a more overhead view in the current scene. Zero restores the authored angle. This is an offset, not the camera's absolute angle, and it does not rotate the screen-space scroll or logo. Large offsets can expose the edge of the ground. |
| **Pitch down -2** / **Pitch up +2** | 2° per click | Decrease or increase the pitch offset by two degrees, within the slider limits. Useful for comparing small perspective changes. |
| **Authored view** | Sets offset to 0° | Return to the original camera position and angle. This changes only the camera control; it does not reset other settings or the current interaction. |

Camera changes appear immediately and remain when the menu closes or a visitor's round ends. Disabling the session restores the authored camera transform.

## Spin animation

“Spin” means the moving light selection around the eight inner petals. These controls affect its pacing; they do not change the outcome mapping, remove symbols, or physically rotate the flower.

| Parameter | Default / slider range | Tooltip and visible effect |
| --- | --- | --- |
| **Speed (petal slots / second)** | **16** / 1–32 | Set the peak speed of the light chase. Lower values make each step easier to follow; higher values make the selection feel faster. Eight slots equal one circuit, so 16 is about two circuits per second at peak speed. This is not a duration control; lowering speed does not proportionally stretch the whole sequence. |
| **Cruise duration (seconds)** | **1.2 s** / 0–30 s | Set the minimum time spent chasing at peak speed before slowing down. Increase it to prolong the selection animation; decrease it for a quicker result. Zero removes the requested minimum cruise, but the wheel can still travel farther to line up with its selected outcome. |
| **Slowdown duration (seconds)** | **3.6 s** / 0.1–20 s | Set how long the light chase takes to decelerate and land. Higher values create a longer, gentler approach to the final petal; lower values make the landing more abrupt. This does not slow the later color reveal or scroll video. |

All three values are used when the **next spin starts**. Moving a slider during a spin does not retime the spin already in progress. These settings are saved with the session and do not edit the shared wheel preset used by preview scenes.

With the current wheel configuration, total spin time is approximately:

**0.9 s acceleration + cruise duration + slowdown duration + landing alignment**

Landing alignment can add less than one extra circuit: approximately **8 ÷ speed** seconds. At the saved defaults, the spin is roughly **5.7–6.2 s**. The entry pulse/settle before the spin and the result reveal afterward are additional time. **Current planned spin**, shown during spinning, reports the planned spin duration rather than the duration of the whole interaction. Optional windup/overshoot settings outside this menu can add time too.

## Restoration and session timing

| Parameter | Default / slider range | Tooltip and visible effect |
| --- | --- | --- |
| **Restoration brush radius (viewport height)** | **0.32** / 0.02–0.40 | Set the size of the soft area revealed around the visitor's pointer. Larger values cover more of the image with each movement; smaller values require more sweeping. This is a radius measured against the camera viewport's height: 0.32 means a radius of 32% of that height, with a soft edge. It changes future strokes, not areas already restored. |
| **Idle time before return (seconds)** | **30 s** / 1–180 s | Set how long an active session can go without meaningful visitor input before fading back to the opening. Higher values allow more time to watch animations or take a photo; lower values reset abandoned interactions sooner. Movement, new tracking contact and explicit input actions refresh the timer. A stationary pointer, tiny jitter, or an animation playing does not. The timer also runs during spin and reveal. It continues counting during scroll/greeting playback, but timeout return is deferred until that presentation finishes. |
| **Return fade duration (seconds)** | **2 s** / 0.1–8 s | Set the total duration of an inactivity return: half fades to black, the content resets at black, and half fades the opening back in. A larger value makes that return slower and softer; a smaller value makes it quicker. This does not change the normal scroll or greeting closing animation. |
| **Opening dwell (seconds)** | **0.65 s** / 0.1–4 s | Set how long the visitor must remain over the opening hand prompt to begin restoring. Longer dwell requires a more deliberate pause; shorter dwell responds sooner and can be easier to trigger accidentally. Counting starts only after the prompt finishes appearing. Leaving the target or losing pointer continuity cancels the dwell. |
| **Spin dwell (seconds)** | **0.15 s** / 0.05–3 s | Set how long the visitor must remain over the second hand prompt to start selection. Higher values require a longer deliberate pause; lower values trigger sooner. The pointer must first be observed outside the prompt and then enter it. Reducing dwell does not remove that fresh-entry requirement. |
| **Required restored coverage** | **0.80 (80%)** / 0.10–1.00 | Set the restored fraction needed before the full reveal can begin. Lower values require less painting; higher values require more. Coverage is measured in the configured completion area, not by counting petals. **All four lamps must also be lit**, even at a low threshold. A value of 1 requires the measured area to reach full coverage and can be difficult with a soft brush. |
| **Fallback colored hold (seconds)** | **10 s** / 0–60 s | Set how long the selected colored flower remains visible if the scroll is unavailable or playback fails. Higher values leave more time to see that fallback result; zero advances to the Happy Deepavali finale as soon as the fallback is processed. It has no effect on a normally playing scroll; inactivity return is deferred during the fallback and finale. |

Brush size and coverage threshold work together. A larger brush plus a lower threshold usually makes restoration complete sooner, but neither bypasses the four-lamp requirement. The opening invitation stays visible when already idle at the base; the timeout does not repeatedly fade an untouched opening screen.

## Center reveal and lighting

| Parameter | Default / slider range | Tooltip and visible effect |
| --- | --- | --- |
| **Center reveal radius** | **0.16** / 0.02–0.40 | Set the size of the initial restored patch at screen center when the opening prompt finishes dismissing. Larger values reveal more immediately; smaller values leave more for the visitor to paint. Like the brush, this radius is a fraction of viewport height. It is a one-time reveal at the start of restoration, not the brush used for later strokes. |
| **Center light intensity** | **0.60** / 0–2 | Set the strength of the dedicated center light during restoration and the final reveal. Increase it for a brighter center; decrease it for a subtler center, or use zero for no contribution from this light. This does not set the brightness of all petals, lamps, scroll artwork or the logo. Appearance also depends on the scene's materials and exposure. |
| **Center light fade (seconds)** | **0.60 s** / 0.05–4 s | Set how gradually the center light rises during restoration and falls away when that phase ends. Larger values soften and lengthen the transition; smaller values make it faster. This controls the light's transition, not how quickly the restoration mask clears. |

## Result scroll and logo

| Parameter | Default / slider range | Tooltip and visible effect |
| --- | --- | --- |
| **Scroll delay after result (seconds)** | **2 s** / 0–15 s | Set the pause between the flower finishing its selected color/symbol reveal and the scroll starting to open. Increase it to give the flower result more time alone; decrease it to show the greeting sooner. Zero removes the deliberate pause, but playback still waits for the video to be ready. This timer does not start at the instant the wheel lands. |
| **Fully open scroll hold (seconds)** | **15 s** / 0–60 s | Set how long the greeting remains fully open before closing starts. Higher values provide more reading/photo time; lower values shorten the ending. Opening and closing videos are additional time. Zero starts closing as soon as the fully-open state is processed. The scroll/greeting presentation defers inactivity return until it completes. |
| **Logo fade after scroll opens (seconds)** | **1 s** / 0.1–5 s | Set how long the logo placeholder takes to fade from transparent to opaque after the scroll has finished opening. Higher values give a slower entrance; lower values make it appear sooner. This fade happens **inside** the fully-open hold and does not extend it. The placeholder hides when closing starts, so a hold shorter than the fade can prevent it becoming fully opaque. |

The scroll's opening and closing speeds come from its video clips; these menu controls do not change video playback speed. The placeholder remains a rectangle until approved logo artwork is assigned. Its position, size and sprite are scene Image/RectTransform properties, not session sliders. See [logo installation notes](scenes.md#presentation-controls-29-october-2026).

**Happy Deepavali hold (seconds)** defaults to **4 s**, with an M-menu range of **0–30 s**. It controls the fully written title after scroll closing, excluding about 5.6 s opening and 1.4 s closing. It updates live during the greeting hold and is saved with session settings.

## When an adjustment takes effect

| Controls | Timing |
| --- | --- |
| Mouse preview, camera pitch, center light intensity/fade | Live. A light change is visible only while that light contributes to the scene. |
| Brush radius, dwell durations, coverage threshold, fallback hold, idle timeout | Used by ongoing input/timer checks. A lower threshold or duration can complete an already-progressing action sooner. |
| Center reveal radius | Next time the opening prompt finishes dismissing; it does not resize the patch already painted. |
| Spin speed, cruise, slowdown | Next spin; the active spin keeps its planned motion. |
| Return fade duration | Next inactivity return; the active return keeps its captured duration. |
| Scroll delay / fully-open hold | Captured when the relevant delay/hold begins. Changes during that same phase apply on its next occurrence. |
| Logo fade duration | Live against time already elapsed in the hold. Changing it mid-fade can visibly jump the current opacity. |

## Buttons and status readouts

| Item | Meaning |
| --- | --- |
| **Save to ScriptableObject** | Editor-only button that writes all session tuning values to the assigned settings asset. Saves camera offset, spin parameters and session/scroll/logo timings together. Does not save mouse preview, logo artwork/layout, or shared wheel lighting settings. |
| **Load saved** | Replace live session parameters with the saved values. Does not restart the round or reset already-captured spin/phase durations. |
| **Phase** | The interaction's current stage, such as restoring, spinning, opening the scroll or holding the result. This is a status label, not a selector. |
| **Input** | Whether temporary mouse preview is active, native touch is waiting, or a sensor contact is being tracked. The contact number identifies the tracked contact; it is not a visitor count. |
| **Coverage / Candles** | Current measured restoration percentage and count of lit lamps out of four. Both must satisfy the completion gate. |
| **Inactive … / … seconds** | Time since qualifying activity versus the current return timeout. An open, untouched menu does not pause the timer. Using the controls counts as activity. |

## Practical tuning examples

- **Try a calmer, longer selection:** as an experiment, use speed **8**, cruise **8 s**, and slowdown **6 s**. The spin itself will be about **14.9–15.9 s**, excluding entry and result reveal. These are suggested trial values, not new saved defaults.
- **Watch the whole ending without touching the wall:** raise the idle timeout to cover entry, spin and color reveal from the last meaningful input. Once scroll delay begins, the scroll and Happy Deepavali finale finish without inactivity interruption. With the longer trial above, **60 s** is a useful starting point for review; video preparation or earlier inactivity can require more. The timeout is not restarted for each animation phase.
- **Give the logo time to settle before a photo:** keep the fully-open hold comfortably longer than the logo fade. A 15 s hold with a 1 s fade provides roughly 14 s at full opacity if nothing interrupts it.

Change one setting at a time, observe a complete round, then save the combination you want to keep. For perspective reviews, check all four lamp targets and the frame edges as well as the flower.

## Source of truth

Menu labels/ranges: [DeepavaliSessionDebugUI.cs](../Assets/HK_Signature/Deepavali/Restoration/Scripts/DeepavaliSessionDebugUI.cs). Saved baseline: [Deepavali Session Settings.asset](../Assets/HK_Signature/Deepavali/Restoration/Deepavali%20Session%20Settings.asset). Behavior: [session controller and partials](../Assets/HK_Signature/Deepavali/Restoration/Scripts), [PetalWheelPattern.cs](../Assets/HK_Signature/Deepavali/PetalPrototype/Scripts/PetalWheelPattern.cs). For technical ownership and flow, see [architecture](architecture.md).
