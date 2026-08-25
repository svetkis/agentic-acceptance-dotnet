# Skill Contract

> Normative contract for every **installable** skill in `templates/skills/`.
> Verified automatically by `ci/scripts/check-skills.sh` (runs locally and in the
> `repo-quality` CI job of `.github/workflows/demo-project-ci.yml`).

## Required Files

- `SKILL.md` — instructions for the agent
- `CHECKLIST.md` — the portable checklist

`templates/skills/skeptical-ai-bootstrap/` is a **support-template bundle, not an
installable skill**: it ships templates (inventory, decision guards, report formats).
Its installable counterpart lives in `.agents/skills/skeptical-ai-bootstrap/`.
When either half changes, check the other in the same PR: the installable skill links
into the bundle via relative paths, and nothing else keeps them coherent.

## Required Sections of SKILL.md

1. **YAML frontmatter** — `name`, `description`.
2. **Purpose and non-goals** — what the skill does and explicitly what it does not do.
3. **Applicability and exclusions** — stacks/architectures it applies to, and where it does not.
4. **Required inputs** — what the agent needs before running (repo access, diff, config).
5. **Procedure** — the rules/checklist the agent executes.
6. **Evidence requirements** — what proof every finding must carry.
7. **Finding schema** — the canonical finding format (below).
8. **Severity and confidence definitions** — from the canonical scales (below).
9. **Outputs and downstream consumer** — report format and who acts on it.
10. **Trigger or schedule** — when the skill runs.
11. **Limitations and expected false positives** — known blind spots.

Sections may reuse existing content and be concise, but all eleven must be present.

## Checklist Item Marking

`CHECKLIST.md` items use a fixed marking; agents must not invent their own:

- `[ ]` — to be checked (the default before adaptation)
- `[-]` — not applicable: struck out during adaptation (see `ADAPTATION.md`),
  ideally with a short reason on the same line
- `[x]` — checked

## Finding Schema

```text
ID
Severity: BLOCKER | CRITICAL | MAJOR | MINOR
Confidence: CONFIRMED | NEEDS_REVIEW
Category / Control
Evidence: file:line, command output, trace or reproduction
Impact
Recommended action
Owner / disposition
```

## Severity and Confidence

Severity describes **impact and urgency**. `Caching`, `Performance`, `Authorization`
are categories, not severities.

| Severity | Meaning |
|----------|---------|
| **BLOCKER** | Change/release must not proceed; immediate action required |
| **CRITICAL** | High impact; fix in the current iteration |
| **MAJOR** | Degradation or defect; schedule the fix |
| **MINOR** | Improvement; backlog |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

Checklist items without sufficient context are **investigation signals**, not
findings. Examples: `SELECT *`, an interface with one implementation, CQRS,
`Task.WhenAll` on a small collection, or a missing index on an individual `WHERE`
do not prove a defect by themselves.

## Interaction Conventions (optional)

Emoji context markers (e.g. `🔍` for review) are an **optional interaction
convention** of specific agents, not part of the portable skill core. A skill must
be fully usable without them.
