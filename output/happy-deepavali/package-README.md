# Happy Deepavali title (Unity)

2560 × 1118, 60 fps, VP8 WebM with a transparent background. "Happy Deepavali" is written in gold cursive by a glowing pen, then the logo fades in and rises into place underneath. A shine sweep and twinkles loop while it's shown, and hiding fades the logo, then the title, to nothing.

| Clip | Length | Plays |
|---|---|---|
| `Videos/happy_deepavali_spawn.webm` | 5.6 s | once: title is written, glow builds |
| `Videos/happy_deepavali_idle.webm` | 4.0 s | looped while shown: shine sweep, twinkles (seamless) |
| `Videos/happy_deepavali_hide.webm` | 1.4 s | once: fades out; last frame is empty |

## Setup

1. Drag `HappyDeepavali.unitypackage` into the Project window and press **Import**. The videos are set to Transcode → VP8 with **Keep Alpha** automatically. Needs the **Unity UI** package (installed by default in new projects). It is independent of the Diwali Scroll package; both can be in one project.
2. **GameObject → UI → Happy Deepavali Title** (also **Tools → Happy Deepavali → Add Title To Scene**). This adds a canvas scaled from 2560 × 1118 with the title and a **Logo** child.
3. Press Play and left-click: the first click writes the title, the next fades it out.

If the title shows a black background, run **Tools → Happy Deepavali → Reapply Video Import Settings**.

## Logo

The videos contain only the title. The logo is the separate **Logo** Raw Image, faded in and out in sync with the clips. For now it shows `Logo/logo_placeholder.png` (the AMGEN text). To use the real logo:

1. Drop `logo.png` into `Assets/HappyDeepavali/Logo/` (transparency is set up automatically).
2. Drag it onto the Logo's **Texture**.
3. Set **Width / Height** in its Rect Transform to size it (2560 × 1118 reference pixels; it stays centred where the placeholder is).

Logo timing and rise distance are on the **Happy Deepavali Overlay** component.

## Scripting

```csharp
title.Show();     // write the title (only when hidden)
title.Hide();     // fade out (only when shown; queued if clicked while writing)
title.Toggle();
title.Spawned      += () => Debug.Log("title written");
title.HideFinished += () => Debug.Log("title gone");
```

Works with both the old Input Manager and the new Input System package.
