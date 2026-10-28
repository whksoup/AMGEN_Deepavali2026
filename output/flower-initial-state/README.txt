INITIAL INNER FLOWER APPEARANCE
2026-10-28

Open Assets/Scenes/Deepavali_PaletteTest.unity and press Play.
The scene and Deepavali Peacock Flower prefab start with eight decorated inner
petals and the center mandala. Use Initial state to restore that appearance.
Select All petals and a palette to transition to the existing phase appearance.
Individual petal and center requests still operate independently.

Clockwise from the top: Sprout, Diya, Star, Stacked Coins, People and Heart,
Gift Box, Sunburst, Lotus. The six phase symbols reuse the existing center art.
The gift, coins, and mandala come from the approved first-stage symbol assets.
Each inner petal has a distinct fill, a circular symbol frame, 48 rim dots,
two inset contours with a widening outer stroke, and small diamond ornaments.
All initial ornament uses #F8CF78 gold; the separate edge light uses #FFD35A.
Palette values and symbol placement are recorded in layout.json.

Runtime integration
- FlowerPaletteController.Initial.cs configures the opening atlas and reset.
- An explicit per-petal useStartupArtwork flag limits these masks to the inner
  eight petals. The outer peacock startup artwork retains its own configuration.
- Startup art, colors, and edge hue use the existing radial palette blend.
  There is no second transition clock. Explicit palette requests consume the
  opening state, including Base requested while the public state is already Base.
- Normal artwork selection and the latest queued palette request still work.
- The original six center icon IDs remain unchanged; mandala is appended as ID 6.
- Normal phases retain their authored patterns, colors, and fill-colored edges.
- Disabling and enabling the component does not replay the initial appearance.
  Only Initial state / ResetToInitialState deliberately restores it.

Assets are in Assets/HK_Signature/Deepavali/PetalPrototype/Symbols/Initial State/
and Palettes/Initial Inner/. Decorated Initial Petals is a padded 2048-square
atlas; Center Phases and Mandala is a 2048x1024 atlas. Mobile import overrides
are supplied. The original standalone symbol assets remain available.

Review and validation
- initial-inner-pc.png: focused inner petals and center with PC render settings.
- initial-flower-pc.png: full flower including the coordinated outer artwork.
- initial-inner.png and initial-flower.png: Mobile render-pipeline previews.
- transition-00/45/100.png: opening, wave midpoint, and completed Blue phase.
- validation.json: all 24 runtime checks passed, including all seven phases,
  single-petal/center changes, queued transitions, reset, and shader diagnostics.
- pixel-wave-validation.json: 1,100,596 pixels outside the wave band matched
  their opening/destination reference exactly (zero mismatches).
- No runtime or shader errors were reported. Native Android/device validation
  remains outstanding; these captures and checks ran in the Unity Editor.

Reproduction
build_patterns.py authors the geometric masks, center atlas, and pattern review.
setup_initial.cs imports/configures those assets and applies them to the current
scene and Peacock prefab. setup_review_button.cs installs the Initial state UI.
Run the C# files through the Unity eval_file tool. Runtime validation and capture
scripts require Play mode. Captures temporarily isolate/pose renderers and restore
their scene settings afterward. The before/ folder records pre-integration sources.
