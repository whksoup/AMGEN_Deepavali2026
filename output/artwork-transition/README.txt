Per-petal root-to-tip artwork transitions
========================================

Open Assets/Scenes/Deepavali_PaletteTest.unity and press Play.
Tap a petal, then Next artwork; All petals lets the same control affect the flower.
The incoming artwork spreads outward from the root of each selected petal as a
curved radial front. Restoring the original ornament uses the same direction.
All interior ornament and its pigment detail are replaced; the border remains.

Per-petal control
-----------------
The inner-layer prefab has PetalArtworkController on each petal wrapper. The
current two-layer review scene has 23 bound controls (8 inner, 15 outer).
Select that component in the Inspector to choose Artwork, Reveal duration and
Reveal softness. Edit Mode sets the starting appearance. Play Mode changes
animate. Duration defaults to 1.2 seconds; softness is a 0.04 half-width fraction
of petal length. Timing edits affect the next reveal, preserving one in progress.
The Inspector reads/writes the flower's matching Petal Appearances entry, so
there is only one authored/requested value per petal.

Application code (namespace Amgen.Deepavali):

    petalArtwork.SetArtwork(1);           // reveal Diamond sample
    petalArtwork.SetArtwork(3);           // queue Rosette if a reveal is active
    petalArtwork.RestoreOriginal();       // reveal original ornament
    petalArtwork.NextArtwork();           // cycle from the latest request
    petalArtwork.SetArtworkImmediate(0);  // immediately Blank; cancel this queue

These void methods can be bound to UnityEvents. TrySetArtwork(int) and
TrySetArtworkImmediate(int) return whether a request was accepted. -1 is original;
0..Count-1 are IDs from the flower's shared FlowerSymbolSet. CurrentArtwork is
the last completed artwork. RequestedArtwork is the latest choice. Progress,
IsTransitioning and HasPendingArtwork expose this petal's reveal status.

For code that already owns the flower and a renderer/index:

    int index = flower.GetPetalIndex(renderer);
    flower.TransitionSymbol(index, symbolId); // normal animated change
    flower.SetSymbol(index, symbolId);        // explicit immediate reset

Each petal finishes its active reveal and then starts only its latest queued
choice. Selecting the active destination clears an obsolete pending choice.
Selecting a settled destination does not restart it. Different petals may
start at different times, run at different speeds, and display different icons
and palettes. Palette waves, brightness adjustments and bone pulses run
independently. SetState resets palettes only; SetSymbol resets one artwork only.
Disabling/re-enabling the flower applies the latest requested appearance
immediately and recreates owned materials; interrupted reveals are not resumed.

Implementation
--------------
FlowerPaletteController remains the sole material owner. Artwork state and its
preallocated per-petal clocks are in FlowerPaletteController.Artwork.cs.
PetalArtworkController is a renderer-bound facade and never edits materials.
FlowerAppearancePreview routes artwork controls through those components.

The shader blends source and target atlas entries at a curved radial front.
UV0 is mapped to the imported mesh's XY plane relative to root (0,0), normalized
by petal length. The radial endpoint covers the rest-space rectangle. Coordinates
stay attached to the petal during curl, sway, rotation, or whole-flower motion.
Original artwork is an explicit source/target mode, so replacement, symbol
changes, Blank, and restoring Original all use the same reveal. Palette colors
are evaluated independently for both artworks. Emission continues to use fill.

Textures, geometry, and runtime material instances are reused. Atlas samples
keep the existing safe mip cap and compressed-alpha erasure correction. Pixels
fully before/after the reveal sample only their visible artwork; the soft band
may sample both entries. UV derivatives are evaluated before per-pixel branches.

Validation
----------
validation.json checks per-petal wiring, independent clocks/durations, palette
and brightness isolation, latest-request queue behavior, immediate reset,
restoration, Inspector changes, invalid IDs, stable materials, and zero managed
allocations across 256 updates with all 23 artwork transitions active.
GPU pixel tests compare the rendered old/new regions against their endpoints
for Original->Blank, Blank->Rosette, Diamond->Ring, and Rosette->Original at
25/50/75% progress. All tested pixels outside the soft band match exactly within
one 8-bit level. Shader compilation and SRP Batcher checks pass.
The earlier end-to-end appearance regression was rerun successfully, including
28 symbol/palette renders, compressed-mask erasure, bloom, multi-touch selection,
bone motion, pinned roots, palette queuing, and material cleanup.
That regression ran before the shared scene expanded from eight to 23 petals.
The artwork-specific validation and live-validation.json use the current 23-petal
layout, including both layers, simultaneous palette/motion, and lifecycle checks.

artwork-reveal.gif shows replacement and restoration with different palettes
and symbols per petal. frames/ contains direct Unity renders used for the GIF.
Android native shader/build validation and hardware profiling remain unverified.
