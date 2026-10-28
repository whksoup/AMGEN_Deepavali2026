HAND GUIDANCE APPEARANCE
2026-10-29

Implementation
--------------
Changed only this runtime source:
Assets/HK_Signature/Deepavali/UI/HandHereInteraction.cs

Original source snapshot:
output/hand-guidance-appearance/before/HandHereInteraction.cs
Original SHA-256: 2D3CD37D34338AAC610FEFEC94422ABF9AA4EA80DD53B88E0EB3080CCDF4A7D1

The prompt now assembles whenever it is instantiated/enabled in Play Mode or
ResetPrompt() is called, including resetting an already active prompt. It drives
the existing shader's _Dissolve from 1 to 0 over disintegrateDuration (currently
0.85 seconds in the prefabs), using unscaled time. That reverses the existing
fragment trajectories and dissolve pattern. The loading ring stays idle during
appearance. The shader, graphic mesh, materials and prefab serialization did not
need changes; disintegrateDuration controls both assembly and disintegration.

APIs and integration
--------------------
IsAppearing: new read-only property, true until assembly finishes. The session
must check this before accumulating its own dwell timer. While true, keep that
timer/progress at zero; pointer continuity and fresh-entry bookkeeping can still
run. SetDwellProgress() ignores updates during appearance or dismissal, but that
alone cannot stop a caller's timer or phase from advancing. The parent chat owns
the session integration; no session source was edited here.

ResetPrompt(): activates the object and restarts assembly from fully dissolved.
Clears dwell progress, any current dismissal and any pending dismissal request.
Resetting while active, appearing, loading or disintegrating starts a fresh run.

CancelAndHide(): clears all animation state and disables the object immediately.
Disabling the GameObject, component or parent also clears state and releases the
runtime material. Re-enabling creates a fresh material and replays assembly.
Neither cancellation nor appearance completion emits either despawn event.

Dismiss(): retains loading then disintegration. BeginDisintegration(): retains
the already-loaded path, skipping the loading duration. A request received while
appearing is accepted once and starts after assembly completes, with a fresh
dismissal clock. IsDismissing is true as soon as that request is accepted, so it
can overlap IsAppearing while waiting. Repeated requests do not restart or
replace it. ResetPrompt(), CancelAndHide() and disable cancel the waiting request.

Pointer click behavior still respects externallyDriven and the left button.
Circular hit testing and BlocksScreenPoint() remain active during assembly as
well as loading/disintegration. Completed dismissal still disables the object
and invokes onDespawned and Despawned once. Runtime materials remain per-instance,
including the existing stencil-material state update.

Validation and remaining checks
-------------------------------
Offline: reviewed the source diff, lifecycle/queued-dismissal transitions,
existing call sites, and the shader's dependence on _Dissolve for both fragment
position and alpha. Checked the patch for whitespace errors against the backup.
No compiler, AssetDatabase, Unity MCP mutation, Editor/Play Mode control, scene
or prefab save was invoked. No scene, session, restoration, settings or debug
scripts were edited. Compilation and rendered behavior remain for the parent
chat to validate in its shared Editor.

Suggested Play Mode checks after parent compilation:
1. Instantiate a prompt; toggle its GameObject, component and parent off/on.
   Each enable should begin at _Dissolve=1, finish at 0, and clear IsAppearing
   after disintegrateDuration. Repeat with timeScale=0 (unscaled animation).
2. Confirm golden fragments converge into the badge with the dotted ring idle.
   Then confirm external dwell still fills the dotted ring clockwise, and the
   existing disintegration still looks unchanged. Check a masked canvas too.
3. At early/mid/late appearance, call ResetPrompt() repeatedly and CancelAndHide().
   No stale completion, unexpected reactivation or despawn event should occur.
   Repeat reset/hide during loading and disintegration.
4. With externallyDriven=false, click during appearance and after it finishes.
   Early clicks wait for assembly, then run loading/disintegration exactly once;
   repeated clicks do not change timing. With externallyDriven=true, clicks do
   nothing. An early BeginDisintegration() waits, then skips loading as before.
5. Cancel or reset an early queued dismissal and confirm it never runs later.
   Verify each completed dismissal emits each existing despawn event once.
6. Run two prompt copies; their timing and materials must remain independent.
   Confirm the badge blocks petal input during assembly and releases it on hide.
7. After the parent's IsAppearing guard, hold a pointer over opening and Shimmer
   respawn prompts during assembly: session dwell must start only when ready.

The older output/hand-guidance/validate-interaction.cs assumes re-enable is
instantly solid. That assertion must be updated to expect assembly, or wait for
IsAppearing=false, before using that older manual validation routine.
