# Documentation and handoff workflow

Keep current behavior, session history and temporary working state distinct. Use the existing `Documentation/` casing and standard `README.md`/`AGENTS.md` filenames; do not create a parallel lowercase documentation tree.

## At the start

Read [AGENTS.md](../AGENTS.md), the [documentation index](README.md), and the newest relevant [session summary](sessions/README.md). Verify important claims against current source, scene/prefab overrides and saved assets. Check the working diff so existing user/agent changes are not mistaken for your own.

Choose a session ID such as `2026-10-29-1430-touch-input`. Use Asia/Singapore timestamps and add a short unique suffix if sessions overlap. One task/session has one summary, updated across continuations. Copy [the summary template](sessions/TEMPLATE.md) when useful; omit irrelevant fields.

For sustained work or a handoff, create `Documentation/scratchpads/<session-id>.md` using [the scratchpad template](scratchpads/TEMPLATE.md). Link it from the session summary (an in-progress summary is fine) and add a row to the session index. Small changes that fit one uninterrupted pass do not need a scratchpad.

## During work

Checkpoint findings that another agent would otherwise have to rediscover: relevant paths, confirmed facts, decisions and their rationale, failed approaches, current Unity scene/Play Mode state if observed, running operations, temporary settings that need restoration, and the precise next action. Label hypotheses and unverified statements.

When multiple agents have already been authorized, use separate scratchpads or clearly owned sections. State file ownership and overlapping dependencies before edits. The coordinating agent consolidates durable findings and the final summary. Do not use a shared scratchpad as a substitute for explicit handoff or as permission to start more agents.

## After a meaningful change

Update only the documentation affected by the result:

| Change | Update |
| --- | --- |
| Scene role, launch/preview controls, deployment selection | [Scenes](scenes.md), and root README if the entry point changed |
| State flow, subsystem ownership, API/input contract, settings, mapping or rendering | [Architecture](architecture.md) |
| New verification, changed checks, hardware/build limits | [Validation](validation.md); link task-specific evidence under `output/<work-item>/` |
| New documentation entry point or changed overall status | [Documentation index](README.md) |
| Agent workflow itself | This file, [AGENTS.md](../AGENTS.md), and the [skill](../.agents/skills/deepavali-documentation/SKILL.md) as needed; keep procedural detail here |

Preserve original plans and reports. Add a dated status pointer when a historical document can mislead readers, rather than rewriting the record as if its author knew later changes. Do not copy full logs or screenshots into prose; link the evidence.

Complete the session summary with the request, actual changed paths, decisions, verification and pending work. If a canonical page needs no update, say why in the summary. Mark tests as newly run, historical or not run; record actual failures and limits. Avoid secrets and machine-specific transient IDs unless needed for a live handoff.

## Close or hand off

1. Promote confirmed, durable scratchpad findings into the relevant docs and summary. Carry unresolved questions into the summary's pending-work section.
2. Check local links and referenced paths, inspect `git diff --check` and the scoped diff/status. Verify settings against saved assets, not just C# defaults. Use the relevant checks in [validation](validation.md).
3. Update the [session index](sessions/README.md) with status and summary link, newest first. Close the scratchpad with a link to the summary, or mark it active with a concrete next action if handing off unfinished work. Keep closed notes as history; later sessions should not treat them as current state.
4. Report the result and verification limits to the user. Documentation requirements do not require a commit, deployment or external message unless requested.

For a read-only investigation with no project changes, a summary is optional unless requested or needed for a handoff. For every session that changes project files, include a session summary, including documentation-only work.
