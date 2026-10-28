Flower animation sequence - 28 October 2026

Scene: Assets/Scenes/Deepavali_LightSequence.unity
Open the scene and press Play. It starts in Sleeping.
The original palette review and two-petal lighting prototype scenes are preserved.

Stages:
1. Sleeping: all flower emission is off. Painted artwork remains dimly visible.
2. Ambient: soft warm light pools drift independently across inner and outer
   petals. Dark gaps remain. The center rim is off.
3. Roulette: the tip portions of the inner-petal edges and their icons light
   clockwise, sharing the same crossfade rather than switching on/off.
   It accelerates and decelerates to a random stop, holds
   there, then reveals that petal's assigned color. Outer-petal emission and the
   center rim remain off throughout the spin and winner hold.
4. Result: the existing outward palette transition reveals the selected full
   flower color. Full ornament lighting and the center rim are enabled here.

Eight equal-probability outcomes, clockwise from the top:
1 Gold; 2 Pink; 3 Purple; 4 Green; 5 Blue; 6 Orange; 7 Cyan; 8 Red.
Cyan and Red have separate inner/outer tones and matching center colors. They
reuse the opening center mandala; the original six palette icons are unchanged.

Preview controls:
- Sleep resets all state, pending palette animation and lighting.
- Ambient enters the indefinite ambient animation.
- Spin chooses once and runs the complete spin/hold/result sequence.
- Play full sequence cycles Sleep -> Ambient -> Roulette -> Result -> Sleep.
- Eight color buttons directly preview results.
- Ambient brightness, drift speed and dark gaps are adjustable independently.
- Roulette tip brightness, duration and full revolutions are adjustable. A running
  spin snapshots its duration/turn count so edits affect the next spin.
- Tip reach down edge (%) adjusts how far down from the tip the edge light extends
  live during a spin (5-100%, default 35%). Icon brightness has a separate slider.
  Both values are included in Save timing and lighting defaults.
- Save timing and lighting defaults persists the current sliders to the preset
  during Editor Play Mode. Unsaved changes last for the current play session.

Preset: Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Light Sequence/Flower Sequence.asset
The preset Inspector also exposes demo sleep/ambient/result durations, winner
hold time, warmth and outcome order. Default demo timing: 2 seconds sleeping,
6 seconds ambient, 8-second spin with 4 full turns plus the target offset,
1.2-second winner hold, 8-second result.

Architecture:
- FlowerLightSequence owns stage timing, random selection and emission policy.
  Public Sleep, WakeAmbient, StartRoulette and PreviewResult methods are available
  for future interaction wiring. StageChanged and ResultSelected are local C# events.
- FlowerPaletteController remains the sole material owner; stage multipliers
  default to 1, preserving other scenes and existing palette behavior.
- Ambient reuses the two geometric lighting maps with independent per-petal seeds.
- Roulette uses monotonic smoothstep travel with zero start/end velocity. Its
  integer winner is chosen once with Random.Range(0,8), independently of timing.
- Tip glow uses an adjustable eased mask (default last 35% of petal length),
  restricted to the edge. Whole-edge brightness and inward wash are zero.
  Inner glyph coverage is isolated inside its opening medallion, excluding the
  ring/dots/border, and added before the same stage multiplier as the tip.
  Neighboring tips use complementary eased lerps, including the 8-to-1 handoff.
  There is a 0.25-second entry fade and a 0.3-second fade out at the end of the
  winner hold. The petal body no longer brightens abruptly during a handoff.
- Result disables spatial prototype lighting and uses the existing palette system.
- Full-flower spill-light proxies are disabled before Result. Background diya
  flames and scene lights retain their existing behavior; 'unlit' refers to the flower.
- Three copied palette assets preserve all original seven entries (Base + six
  colors) and append Cyan/Red. Serialized enum IDs for earlier colors are unchanged.
- The preview uses manual/demo controls. Sensor input, blessing text and the main
  application's production session flow are not part of this scene prototype.

Validation: validation.json records the latest checks: stage emission masks,
ambient spatial/temporal variation, clockwise traversal, a stable random winner,
all eight mathematical paths accelerating/decelerating to their exact targets,
all eight result palettes across all 23 petals and center, one result event per
spin, continuous tip crossfades including wraparound, entry/exit fades,
reset/disable cleanup, automatic four-stage playback, and shader compilation.
Screenshots 01 through 07 show sleeping, ambient, roulette, hold and result looks.
