---
name: mutation-audit
description: >
  Mutation testing with Stryker.NET. Checks test quality: do tests actually kill
  mutants in critical code, or do they just stay green along a happy path.
---

# Mutation Audit — Skill

Optional interaction convention (agent-specific): when this skill is active,
add `🧬` to your STARTER_CHARACTER stack (example: `🍀 🧬`). Prepend `↻` when
re-reading the skill. The skill is fully usable without emoji markers.

## Purpose and Non-Goals

You are a test quality engineer. Your task is to find weak tests that pass but do
not verify real logic. Mutation testing introduces small code changes (mutants)
and checks whether tests fail. If a mutant survives, the tests are not strict enough.

This skill does not write the missing tests or measure line coverage — it reports
surviving mutants and score regressions for the Programmer Agent to act on.

## Applicability and Exclusions

- **Critical assemblies (Domain, core services)** → mutation testing is Must.
- **A lot of legacy code** → start with one critical assembly and fix a baseline.
- **TUnit / Microsoft Testing Platform** → Stryker.NET may be unsupported.
  Run as a periodic audit or a separate CI job with `dotnet test`.
- **No budget for long runs** → run once per sprint, not on every PR.

## Required Inputs

- Access to the test suite and 1–3 critical assemblies selected for mutation testing.
- A `stryker-config.json` per target assembly and a recorded mutation score baseline.
- Input from: Test Audit, Code Coverage Report.

## Procedure

### 1. Target assemblies
- [ ] 1–3 critical assemblies are selected for mutation testing (for example, Domain).
- [ ] Assemblies are not too large — runtime stays reasonable.
- [ ] Every assembly has a `stryker-config.json`.

### 2. Baseline and ratchet
- [ ] Mutation score baseline is recorded.
- [ ] `MutationGuardTest` fails when the score drops.
- [ ] Minimum threshold is defined: 70% for critical assemblies (adjust per project).

### 3. Surviving mutant analysis
- [ ] Top surviving mutants are reviewed manually.
- [ ] For each mutant a test is added or strengthened.
- [ ] False-positive mutants (for example, equivalent mutants) are documented.

### 4. Pipeline integration
- [ ] Stryker runs before release or once per sprint.
- [ ] The report is stored as a CI artifact.
- [ ] It does not block a regular PR because of duration.

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/Domain/Order.cs:42`
2. **Mutation type:** `>` → `>=`, `+1` → `-1`, empty string → null
3. **Why it survived:** weak assert, missing branch test
4. **Action:** strengthen test or document as equivalent mutant

**NEVER report:**
- “Tests are weak” without a concrete mutant
- Mutation score drop without explaining why
- Recommendations to enable Stryker in CI if it does not support the project stack

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

| Severity | Meaning |
|----------|---------|
| **BLOCKER** | Mutation score of a critical assembly falls below 60% |
| **CRITICAL** | Score dropped relative to baseline |
| **MAJOR** | Many surviving mutants in critical code |
| **MINOR** | Equivalent mutants or mutants in trivial code |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Stryker report shows a concrete surviving mutant |
| **NEEDS_REVIEW** | Mutant may be equivalent. Needs human analysis |

## Outputs and Downstream Consumer

```markdown
## Mutation Audit — {date}

### Summary
| Assembly | Mutation Score | Baseline | Delta | Status |
|----------|----------------|----------|-------|--------|
| YourProject.Domain | 78% | 75% | +3% | 🟢 OK |
| YourProject.Application | 62% | 65% | -3% | 🔴 FAIL |

### Surviving mutants (top)
| ID | File | Mutation | Why it survived | Action |
|----|------|----------|-----------------|--------|
| MUT-001 | ... | `>` → `>=` | Weak assert | Strengthen assert |
| MUT-002 | ... | `+1` → `-1` | Missing boundary test | Add test |

### False positives
- [ ] [NEEDS_REVIEW] `{File}` — equivalent mutant, add to ignore-list
```

**Downstream consumer:** Backlog Hygiene Agent, Programmer Agent (strengthening tests).

## Trigger or Schedule

Run before release or once per sprint (and in a separate CI job when Stryker
does not support the project's test platform); not on every PR because of duration.

## Limitations and Expected False Positives

- Equivalent mutants (behavior-identical code changes) are unavoidable — document them, do not chase them.
- Stryker.NET may not support TUnit / Microsoft Testing Platform — adapt the runner before reporting tool failures.
- Structurally weak tests (no assertions, tautological or conditional assertions) are faster to find without Stryker — see the Test Validity section of the Test Audit skill and `docs/traps/testing.md#non-validating-tests`.
- Mutation score is assembly-scoped: a good overall score can hide weak tests in a new module.
- A single surviving mutant is an investigation signal, not proof of a defect — strengthen the test or mark NEEDS_REVIEW.
