---
name: complexity-audit
description: >
  Audit of cognitive and cyclomatic complexity. Finds methods that agents turned
  into unreadable nests of branches and records a baseline for gradual technical
  debt reduction.
---

# Complexity Audit — Skill

## Purpose and Non-Goals

- Find methods with high cognitive / cyclomatic complexity, identify top hotspots, and provide a refactoring plan.
- For new projects, verify that SonarAnalyzer catches violations at compile time. For legacy code, record a baseline and apply a "do not make it worse" ratchet.
- Persona: Staff engineer responsible for code readability and maintainability.
- **Non-goals:** rewriting the hotspots itself (output is a plan, not a refactor); functional correctness review; enforcing complexity rules without analyzer/metrics evidence.

## Applicability and Exclusions

- **New project** → enable `S3776` / `S1541` as `error` in `.editorconfig`.
  Cognitive threshold: 15, cyclomatic: 10. API / endpoint layer: 10 / 7.
- **Legacy with high complexity** → do not turn on `error` immediately. Create a
  baseline and ratchet: the number of violations must not grow.
- **No SonarAnalyzer** → use `Microsoft.CodeAnalysis.Metrics` or regular
  `dotnet build` with parsed warnings.
- **Frontend (TS/React)** → use `eslint-plugin-sonarjs` with similar thresholds.

## Required Inputs

- Read access to the repository and ability to run the build / analyzers (`dotnet build`, `SonarAnalyzer.CSharp`, or `Microsoft.CodeAnalysis.Metrics`).
- Existing `.editorconfig` / `Directory.Build.props` to verify compile-time guardrails.
- Existing `complexity-baseline.txt` (or equivalent ratchet file), if a baseline has already been recorded.
- Input from other agents is optional: Code Review Agent observations about complex methods, Architecture Tests, Simplicity Audit.

## Procedure

### 1. Compile-time guardrails (for new projects)
- [ ] `SonarAnalyzer.CSharp` is connected to all production assemblies.
- [ ] `S3776` (cognitive) is configured as `error` with threshold 15 (API layer: 10).
- [ ] `S1541` (cyclomatic) is configured as `error` with threshold 10 (API layer: 7).
- [ ] `TreatWarningsAsErrors=true` is enabled in `Directory.Build.props`.

### 2. Legacy / baseline ratchet
- [ ] Baseline audit is completed: the number of `S3776` / `S1541` violations is recorded.
- [ ] `complexity-baseline.txt` or an equivalent ratchet file exists.
- [ ] `ComplexityRatchetTest` fails if the number of violations exceeds baseline.
- [ ] Top 10 hotspots are documented with a refactoring plan.

### 3. Hotspot analysis
- [ ] Methods with cognitive complexity > 25 are reviewed manually.
- [ ] Methods with cyclomatic complexity > 15 are reviewed manually.
- [ ] Hotspot logic is not duplicated (cross-check with the code-review semantic-duplication checklist).
- [ ] Every hotspot has a complexity reason: business logic, missing abstraction,
  or over-engineering.

### 4. Decision Guards
- [ ] Every conscious deviation from complexity thresholds is documented as
  `COMPLEXITY-###` with explanation.
- [ ] `COMPLEXITY-###` entries are listed in `DECISION-GUARDS.md`.

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/Application/OrderService.cs:142`
2. **Code quote:** 3–7 lines showing nesting / branching
3. **Exact complexity value:** cognitive 28, cyclomatic 19
4. **Reasoning:** why this is a problem (readability, regression risk, review time)
5. **Concrete plan:** how to simplify it (extract method, early return, lookup table)

**NEVER report:**
- "The method is complex" without file, line, and value
- "Needs simplification" without a concrete next step
- Problems not confirmed by analyzer run or metrics

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
| **BLOCKER** | Change/release must not proceed; immediate action required |
| **CRITICAL** | High impact; fix in the current iteration |
| **MAJOR** | Degradation or defect; schedule the fix |
| **MINOR** | Improvement; backlog |

Project-specific mapping:
- **BLOCKER** — cognitive > 25 or cyclomatic > 15; the method cannot be reviewed safely.
- **CRITICAL** — cognitive 15–25; slows comprehension and increases bug risk.
- **MAJOR** — cognitive 10–15; technical debt to remove on next touch.
- **MINOR** — stylistic issue that does not change the complexity score.

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

Project-specific mapping: **CONFIRMED** — analyzer `S3776` / `S1541` produced a
concrete number, or `Microsoft.CodeAnalysis.Metrics` confirms the threshold breach.
**NEEDS_REVIEW** — manual complexity estimate; subjective differences are possible.

## Outputs and Downstream Consumer

```markdown
## Complexity Audit — {date}

### Summary
| Metric | Current | Baseline | Delta |
|--------|---------|----------|-------|
| S3776 (cognitive) violations | {N} | {Baseline} | +/- |
| S1541 (cyclomatic) violations | {N} | {Baseline} | +/- |
| Max cognitive complexity | {N} | {Baseline} | +/- |

### BLOCKER (cognitive > 25 or cyclomatic > 15)
- [ ] [CONFIRMED] `{File}:{Line}` — `{Method}` ({N}) → {refactoring plan}

### CRITICAL (cognitive 15–25)
- [ ] [CONFIRMED|NEEDS_REVIEW] `{File}:{Line}` — `{Method}` ({N}) → {plan}

### Simplification backlog
| ID | Method | Current Complexity | Target | Quarter |
|----|--------|--------------------|--------|---------|
| COMPLEXITY-001 | ... | 28 | 12 | Q3 |
```

**Output to:** Backlog Hygiene Agent, Programmer Agent (refactoring),
Doc Hygiene Agent (updating AGENTS.md thresholds).

## Trigger or Schedule

- On schedule (e.g., monthly) or after large agent-generated refactors.
- When Code Review Agent, Architecture Tests, or Simplicity Audit flag complex methods.
- When a baseline / ratchet file is created or updated.

## Limitations and Expected False Positives

- High complexity can be inherent to business logic (state machines, pricing rules) — such cases belong in `DECISION-GUARDS.md` as conscious deviations, not findings.
- Manual estimates without an analyzer/metrics run are **NEEDS_REVIEW** signals, not confirmed defects.
- Threshold breaches in generated or glue code (migrations, serialization) are usually noise.

> Optional interaction convention (agent-specific): some agents add `🧠` to their starter-character stack while this skill is active. Not required — the skill is fully usable without emoji.
