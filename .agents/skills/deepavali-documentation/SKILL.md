---
name: deepavali-documentation
description: Maintain this AMGEN Unity project's Deepavali scene and architecture documentation after project changes, and prepare session summaries and in-session agent handoffs. Use for implementation closeout, documentation updates or resuming a Deepavali session.
---

# Deepavali documentation

Keep the documentation consistent with the current checkout and leave enough verified context for the next agent. This is a repository-local skill: find the project root three directories above this skill directory and resolve all paths below from that root.

## Workflow

1. Read `AGENTS.md` and `Documentation/README.md`. Read `Documentation/agent-workflow.md` for the canonical maintenance and handoff procedure. Consult the newest relevant entry in `Documentation/sessions/README.md`; use its scratchpad when continuing live work.
2. Inspect the scoped diff and actual implementation. For Unity changes, include scene/prefab bindings, enable flags and saved ScriptableObjects, not just C# defaults. Check the currently referenced main scene before relying on historical output reports.
3. Update the affected canonical pages: `Documentation/scenes.md` for scene roles/controls/settings, `Documentation/architecture.md` for ownership/flow/contracts, and `Documentation/validation.md` for checks/evidence/limits. Update indexes/root README only when entry points or overall status change. Preserve historical plans and add status pointers when needed.
4. Create or update one `Documentation/sessions/<session-id>.md` using `Documentation/sessions/TEMPLATE.md`, and add/update its index row. Record actual changed paths, decisions, newly performed verification, historical evidence consulted, skipped checks and pending work. For a docs-only change, record static checks and why Unity was not run.
5. For ongoing work or information transfer, checkpoint `Documentation/scratchpads/<session-id>.md` using its template. Identify the owner, facts versus hypotheses, temporary state and next concrete action. Use distinct owned notes when multiple agents are already involved; this skill does not authorize delegation.
6. Before closeout, promote durable scratchpad findings into docs/summary and mark the pad closed with the summary link. Validate local links and referenced files, run `git diff --check`, and review the scoped status/diff. Leave unfinished work explicitly pending with an actionable next step.

## Evidence and scope

- Verify canonical statements against source and saved assets. A historical screenshot/report or generic package README can describe a different scene or an earlier configuration.
- Distinguish source inspection, fresh Editor checks, prior reports and physical hardware validation. Never claim an old test was rerun.
- Keep source paths and evidence links specific; do not duplicate long logs, store secrets, or invent device/build validation.
- Follow the user's request and existing authorization. Documentation upkeep does not require changing runtime behavior, running a deployment, committing, or messaging another chat.
- Keep detail in the canonical workflow/docs rather than expanding this skill with a second copy of the architecture.
