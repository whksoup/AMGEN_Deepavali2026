HAND GUIDANCE

Drag Hand Guidance Canvas.prefab into a scene. It binds to Main Camera automatically.
For more markers, duplicate its Hand Here child or add Hand Here.prefab beneath it.
Position each marker with RectTransform anchors/anchored position. Size defaults to
220 x 220 at 1080 reference height. Non-square rectangles preserve a circular badge.
One example is saved in Deepavali_LightSequence, at viewport anchor (0.82, 0.5).

Click/tap the circular badge to fill the dotted ring clockwise (0.65 seconds),
then disintegrate the badge into shader-driven golden fragments (0.85 seconds).
HandHereInteraction exposes both durations, Dismiss(), and an On Despawned event.
The object disables itself after the animation; re-enable it to reset and reuse.
Repeated clicks during the sequence are ignored. Each copy has its own runtime
material. A UI EventSystem with an input module is required (already in the scene).

Hand Guidance Gold.mat is a UI-compatible HDR emission material, matching the
flower's warm gold glow. It is separate from the lit petal shader, which is not
a Canvas shader. Gold Emission controls tint/brightness; Dark Backing controls
the solid background. The graphic exposes stroke width, dot count, ring brightness
and dot brightness. No bitmap artwork or font dependency is required.

Keep the Canvas in Screen Space - Camera and use an HDR camera with URP bloom
enabled to retain the glow. Screen Space - Overlay is drawn after bloom.
The canvas uses height-based scaling so marker diameter stays consistent on the
wide LED wall. The canvas includes a GraphicRaycaster. Hit testing follows the
disc, excluding the transparent corners. The petal input router ignores presses
inside active badges, including during loading/dissolve. Outside taps work normally.
HandHereInteraction is the temporary despawn driver for the experience flow.

Verification captures and aspect checks: output/hand-guidance/
