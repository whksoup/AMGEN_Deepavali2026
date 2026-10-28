# Diwali Scroll overlay (Unity)

2560 × 1118, 60 fps, VP8 WebM with a transparent background, in eight colour variants:

| Variant | Scroll | Text | Heading |
|---|---|---|---|
| Orange | orange, gold border | purple | MAY PROSPERITY SHINE YOUR WAY! |
| Blue | royal blue, teal border | orange | LET JOY BLOOM! |
| Green | green, gold border | orange | HERE’S TO BRIGHT BEGINNINGS! |
| Red | red, gold border | dark blue | MAY NEW POSSIBILITIES BLOOM! |
| Yellow | yellow, vermilion border | dark blue | MAY PEACE AND LIGHT BE WITH YOU! |
| Purple | deep purple, gold border | yellow | MAY LOVE AND TOGETHERNESS SHINE! |
| Pink | pink, gold border | dark blue | MAY JOY LIGHT UP YOUR HOME! |
| Cyan | cyan, gold border | deep purple | MAY GOOD FORTUNE FIND YOU! |

Each variant is three clips in `Videos/`, named `diwali_<variant>_<clip>.webm`:

| Clip | Length | Plays |
|---|---|---|
| `spawn` | 5.6 s | once: roll rises from below, unrolls bottom → top, diyas appear and light |
| `idle` | 4.0 s | looped while shown: flames flicker, petals sway, stars twinkle (seamless) |
| `hide` | 3.4 s | once: diyas go out with smoke, scroll rolls back down and drops off screen; last frame is empty |

The clips chain without a seam: the last spawn frame flows into idle frame 0, and the idle loop's last frame flows into hide frame 0.

## Setup

1. Drag `DiwaliScroll.unitypackage` into the Project window and press **Import**. The videos are set to Transcode → VP8 with **Keep Alpha** automatically; the first import takes a few minutes while Unity transcodes all 24 clips. Needs the **Unity UI** package (installed by default in new projects).
2. **GameObject → UI → Diwali Scroll Overlay** (also under **Tools → Diwali Scroll**). This adds a top-most overlay canvas with all eight variants assigned.
3. Press Play and left-click: the first click shows a scroll, the next click hides it. Each show picks a different variant at random.

If you imported an earlier version of this package, delete the `Assets/DiwaliScroll` folder and any overlay already in your scene first, then import this one and add the overlay again.

If the scroll shows a black background, run **Tools → Diwali Scroll → Reapply Video Import Settings**.

## Scripting

```csharp
overlay.Show();          // roll in the next variant (only when hidden)
overlay.Show("Blue");    // or a specific one, by name or index
overlay.Hide();          // roll away (only when shown; queued if clicked mid-spawn)
overlay.Toggle();
overlay.CurrentVariant   // index into overlay.variants of the scroll on screen
overlay.Spawned      += () => Debug.Log("scroll open");
overlay.HideFinished += () => Debug.Log("scroll gone");
```

Inspector options: *Variants* (remove or reorder entries to change which scrolls can appear), *Variant Order* (Random never shows the same scroll twice in a row; In Order cycles through the list), *Show On Start*, *Toggle On Mouse Click* (turn it off and call `Show`/`Hide` yourself), *Ignore Clicks Over UI*, *Queue Hide During Spawn*, *Wait For Idle Loop End* (hides only at the end of the 4-second loop so the flames never jump), *Preserve Aspect*.

Works with both the old Input Manager and the new Input System package.
