# Happy Deepavali finale evidence

Integrated on 29 October 2026 (Asia/Singapore) in Unity 6000.0.55f1.

- [Rendered greeting over the selected rangoli](greeting-over-rangoli.png)
- [Runtime report: 29 passing checks](runtime-validation.json), produced by [validate-finale.cs](validate-finale.cs)
- [Saved settings and failure report: 11 passing checks](saved-and-failure-validation.json), produced by [validate-saved-and-failure.cs](validate-saved-and-failure.cs)
- [One-time saved scene setup](wire-scene.cs)
- [Supplied package README](package-README.md), retained as reference material; integrated ownership differs from its standalone click-to-toggle example.

Run the validation snippets with Unity Pipeline `eval_file` in Play Mode of the integrated scene, one at a time. The finale harness forces a selected red result and then uses actual video frames and controller transitions. It does not retest the opening dwell, painting or random wheel selection. First round retains the 15-second scroll hold and 4-second greeting hold, and deliberately expires inactivity during scroll opening. Subsequent rounds shorten holds for reset/disable/error checks. It restores all temporary live settings, touch/mouse routes and run-in-background state and removes its Editor update callback.

The runtime harness passed before and again after explicitly applying VP8 transcoding/Keep Alpha and reopening the saved scene. The report and screenshot represent the final imported configuration. The settings/failure snippet round-trips 7.25 seconds through the asset and restores 4 seconds in `finally`; it also forces the no-progress deadline. The deliberate error and timeout tests log `injected validation error` and `Happy Deepavali playback timed out`; these are expected fault-injection logs, not observed media failures.

Initial compilation remained pending and the first recompile RPC timed out; Unity subsequently completed compilation without errors and processed asset imports. The supplied editor postprocessor caused a texture reimport pass. No generated Library/Temp files were manually edited. The scene save adds only the greeting hierarchy/bindings on top of the prior presentation work; generated whitespace on added YAML lines was cleaned.

Physical sensor, target wall performance/display acceptance, build/launcher deployment and approved logo artwork were not tested or supplied. The shared scroll prefab and study scenes were not changed.
