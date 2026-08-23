---
name: doc-hygiene
description: >
  Grooming of hierarchical project documentation. Checks consistency of
  AGENTS.md, README, docs/ and correspondence to actual code.
  Catches dead rules and guardrail bloat.
---

# Doc Hygiene Agent

> **Repo-internal / for methodology archive.** This skill is intended for internal self-audit of the `dotnet-ai-guardrails` repository. It checks the integrity of its own ecosystem (`docs/agents/`, `rules/AGENTS_TEMPLATE.md`, `templates/skills/`, `examples/`). The methodological core (hierarchy consistency, code drift, dead rules, fact checking) can be adapted to another project, but the concrete paths and artifacts are specific to this repository.

## Purpose and Non-Goals

- Persona: documentation grooming agent. Checks that hierarchical guardrails do not contradict each other and match the code.
- Catches dead rules (no enforcement), internal contradictions, guardrail bloat, and drift between docs and code.
- **Key rule:** AGENTS.md is the single source of truth for architectural guardrails. Everything else (docs/agents/, templates/skills/) is derived. If it diverges — update the derived docs, not the root. A dead rule is worse than no rule: if there is no enforcement, remove it or add a guardrail.
- **Non-goals:** writing new documentation; changing the guardrail rules themselves (recommendations only); reviewing code correctness.

## Applicability and Exclusions

- Applies to repositories with hierarchical agent documentation (`AGENTS.md` at root and in subfolders) plus `docs/`, `README`, and examples.
- Concrete paths below are specific to `dotnet-ai-guardrails`; adapt the file list when applying to another project.
- Not applicable: repositories without agent-facing rule files, or where docs are generated from a single source.

## Required Inputs

- Read access to the full repository (docs, rules, templates, examples, tests).
- Ability to search the codebase for enforcement of each rule (tests, analyzers, CI config).
- Optional: git history to determine rule age for the `dead-rule` (> 90 days without enforcement) check.

## Scope

- `AGENTS.md` (root and subfolders)
- `README.md` (plus `README.ru.md` as the only localized mirror)
- `GLOSSARY.md`
- `docs/README.md`
- `docs/ONBOARDING.md`
- `docs/agents/*.md`
- `docs/solutions/*.md`
- `examples/README.md`
- `CONTRIBUTING.md`
- `CHANGELOG.md`

## Anti-patterns

| Problem | Why it's bad |
|---------|-------------|
| Root AGENTS.md contradicts module one | Agent doesn't know which rule to follow |
| AGENTS.md requires X, but no guardrail for X in code | Rule is a dead letter |
| docs/agents/FRONTIER-AGENTS.md describes non-existent pipeline | Onboarding of a new agent starts with a lie |
| README is outdated relative to stack | Human developer loses trust |
| **AGENTS.md bloat** | Agent stops reading the file entirely due to size |
| **Dead rules** | Rule exists, but no enforcement (test, compiler, CI) |
| **Internal contradictions** | Two rules in one AGENTS.md contradict each other |

## Procedure

### Phase 1: Hierarchy Consistency
1. Root `AGENTS.md` → `rules/AGENTS_TEMPLATE.md` — no conflicts?
2. `src/{Module}/AGENTS.md` — do not contradict root?
3. Deep overrides: does deeper AGENTS.md override shallower one?
   Check that override is intentional and documented.

### Phase 1a: Internal Contradictions

// TRAP: Agent adds a rule to AGENTS.md without noticing it contradicts existing §2.1.
// GUARDRAIL: Every MUST / FORBIDDEN is cross-checked with other rules in the same file.

- Find rule pairs where one requires X and another forbids X
- Example: "All services must have interfaces" vs "Minimal API — static classes, no interfaces"
- Mark conflicts as `internal-contradiction`, require resolution

### Phase 2: Code Drift
1. `AGENTS.md` forbids `.FindAsync()` in read-path → is there a regex test?
2. `AGENTS.md` requires `BUG###_` tests → is there a convention in `tests/`?
3. Do mentioned modules/skills exist in `templates/skills/`, `tests/`?
4. Are Decision Guards (`PERF-###`) from AGENTS.md present in code?

### Phase 2a: Rule Vitality

// TRAP: AGENTS.md forbids "raw SQL without comment", but no test/analyzer enforces it. Agents quickly learn the rule is dead.
// GUARDRAIL: Every MUST/FORBIDDEN has enforcement: compiler, test, linter, or CI.

- For each MUST / FORBIDDEN find enforcement (test, compiler, linter, CI)
- Rules without enforcement > 90 days — mark `dead-rule`
- Recommend: either add guardrail or remove the rule

### Phase 3: Cross-Agent Docs
1. Is `docs/agents/FRONTIER-AGENTS.md` current relative to `AGENTS.md`?
2. No divergence in pipeline description between `docs/agents/FRONTIER-AGENTS.md` and `docs/agents/STEP-BY-STEP-AGENTS.md`?
3. Do all agents describe the same stack/versions?

### Phase 4: README, GLOSSARY, ONBOARDING & CHANGELOG
1. Does `README.md` contain current build commands?
2. Does `CHANGELOG.md` cover the latest release?
3. Does `GLOSSARY.md` define its terms consistently and link to existing files?
4. Does `docs/ONBOARDING.md` reference existing skills, patterns, and agent docs?
5. Localization is single-mirror: `README.ru.md` is the only non-English doc; links from it resolve (they may target the English main docs as the single source).
7. No links to deleted sections/skills?

### Phase 5: Size Budget

// TRAP: AGENTS.md grows to 500 lines. Agent reads the beginning, skips the middle, breaks on the end.
// GUARDRAIL: Hard or soft budget on size + suggestion to split.

- Count lines in root `AGENTS.md`
- If > 150 lines — warning (`approaching budget`)
- If > 200 lines — mark `size-budget-exceeded`, suggest splitting into module-specific files
- Count module-level AGENTS.md: if > 80 lines — suggest refactoring

## Evidence Requirements

Every finding MUST include:
1. **Exact file and section/line:** `AGENTS.md §2.1` or `docs/agents/FRONTIER-AGENTS.md:42`
2. **Quoted fragment** of the contradicting / dead / outdated content
3. **Counter-evidence:** the conflicting file:line, or the missing enforcement (no test/analyzer/CI found by an explicit search)
4. **Recommended action:** update the derived doc, add a guardrail, or remove the rule

**NEVER report:** "docs are inconsistent" without both quoted sides; "rule is dead" without an explicit enforcement search.

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

Categories: `hierarchy`, `internal-contradiction`, `code-drift`, `dead-rule`,
`cross-agent`, `readme`, `size-budget`.

## Severity and Confidence

| Severity | Meaning |
|----------|---------|
| **BLOCKER** | Change/release must not proceed; immediate action required |
| **CRITICAL** | High impact; fix in the current iteration |
| **MAJOR** | Degradation or defect; schedule the fix |
| **MINOR** | Improvement; backlog |

Project-specific mapping:
- **BLOCKER** — root AGENTS.md contradicts a module one, or documents a pipeline/guardrail that does not exist (agent onboarding starts with a lie).
- **CRITICAL** — internal contradiction within one rule file; dead rule on a safety-critical guardrail.
- **MAJOR** — code drift (rule without enforcement, non-critical), outdated build commands, broken links to deleted sections.
- **MINOR** — size-budget warnings, glossary/CHANGELOG staleness, link hygiene.

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

**CONFIRMED** — both quoted sides exist, or an enforcement search returned nothing.
**NEEDS_REVIEW** — apparent contradiction that may be an intentional, documented override.

## Outputs and Downstream Consumer

### Phase 6: Report

```markdown
## Doc Hygiene Report

### Hierarchy
- [ ] `src/Payment/AGENTS.md` overrides "Dapper" to "EF Core" — documented?

### Internal Contradictions
- [ ] Root guardrail §X requires A, §Y forbids A

### Code Drift
- [ ] Guardrail requires X, but corresponding guardrail test / analyzer not found

### Dead Rules
- [ ] Guardrail forbids X — no enforcement found (> 90 days)

### Cross-Agent
- [ ] Agent documentation references deleted skill / artifact

### README
- [ ] Build command outdated

### Size Budget
- [ ] Root guardrail — 230 lines, exceeds budget of 200
```

**Output:** `.backlog/doc-hygiene-{date}.md`.
**Consumer:** repository maintainer / Backlog Hygiene Agent — findings become backlog items to update docs, add guardrails, or remove dead rules.

## Trigger or Schedule

- On schedule (e.g., monthly) for the full documentation surface.
- After changes to `AGENTS.md`, `rules/`, `templates/skills/`, `docs/agents/`, or a release (CHANGELOG check).

## Limitations and Expected False Positives

- Apparent contradictions may be intentional, documented overrides (deeper AGENTS.md wins) — mark **NEEDS_REVIEW** unless the override is undocumented.
- "No enforcement found" depends on search coverage; enforcement may live in CI or external tooling — verify before declaring a rule dead.
- Size budgets are heuristics; a long but well-structured AGENTS.md may be acceptable.

> Optional interaction convention (agent-specific): some agents add `📝` to their starter-character stack while this skill is active. Not required — the skill is fully usable without emoji.
