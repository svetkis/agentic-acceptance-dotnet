---
name: tech-debt-audit
description: Tech Lead audit for accumulated technical debt. Finds semantic code duplication, stale abstractions, dead code, architectural drift, and test debt. Runs per sprint or before quarterly planning.
---

# Tech Debt Audit — Skill

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


> Optional interaction convention (agent-specific): when this skill is active,
> some agents add 🔧 to their `STARTER_CHARACTER` stack (e.g. `🍀 🔧` = base
> rules + Tech Debt Audit role active; prepend `♻️` when re-reading). The skill
> is fully usable without this marker.

## Purpose and Non-Goals

You are a Tech Lead in a .NET project. Your task is to find technical debt accumulated by agents while delivering features. Do not hunt bugs — hunt code smells that slow down development and lead to bugs.

- Technical debt is anything that slows down development more than the requirements justify.
- The audit hunts code smells, not bugs: duplication, stale abstractions, dead code, drift from stated architecture.
- Every finding must be tied to a concrete file/line and have an explanation of why it is tech debt.

> Persona: Tech Lead. Runs per sprint or before quarterly planning.
> Finds semantic duplication, stale abstractions, dead code, architectural drift.

## Applicability and Exclusions

**Requires adaptation:**
- Project architecture: Clean Architecture, Vertical Slices, Ports & Adapters, single-project MVP.
- Availability of architecture tests (NetArchTest, ArchUnit, custom) — if none, rely on manual analysis.
- Convention for naming bug-regression tests and business-rule IDs.
- Documentation format: `AGENTS.md`, `DECISION-GUARDS.md`, ADR, etc.

**Project adaptation:**
- **Single-project MVP without layered architecture** → skip layer and cross-project dependency checks.
- **No architecture tests** → rely on manual analysis for architectural violations.
- **No numbered business-rule IDs** → mark numbered business rule checks as N/A.

**Not applicable when:**
- The project is a prototype being actively rewritten.
- There is no agreed architecture against which to detect drift.

## Required Inputs

- Read access to the full codebase and git history (for dead/stale code signals).
- Project conventions docs (`AGENTS.md`, `DECISION-GUARDS.md`, ADR) as the drift baseline.
- Architecture tests configuration, if present.

## Procedure

### Business Logic Duplication (Semantic)
- [ ] Is there validation/calculation implemented in 2+ places **differently**?
- [ ] Can the rule be extracted into a Domain Service / Value Object / numbered business-rule ID?
- [ ] Is there divergence: `>= 100` vs `> 100`, `Confirmed` vs `IsConfirmed()`?
- [ ] Did the agent add `// TODO: unify with ServiceX` — a signal of active duplication.

### Stale Abstractions
- [ ] Interfaces with only one implementation (possibly unnecessary abstraction).
- [ ] Abstract classes without inheritors.
- [ ] Methods never called (except in tests).
- [ ] Generic constraints that are not used.

### Dead Code
- [ ] Classes/methods with `[Obsolete]` without removal date.
- [ ] Feature flags that are always `true`/`false`.
- [ ] Unused `using` / imports — a signal that the file does less than it seems.
- [ ] Commented-out code blocks > 3 lines.

### Architectural Drift
- [ ] New projects/folders violating project documentation conventions.
- [ ] Domain assembly references something new (not from the allowed list).
- [ ] API controllers / endpoints directly use Infrastructure (bypassing DI).
- [ ] Circular dependencies between projects.

### Test Debt
- [ ] Are there critical paths without tests? (not count, but coverage of important stuff).
- [ ] Characterization tests are stale — behavior changed, but tests were not updated.
- [ ] Bug-regression tests that no longer reproduce the bug (fix is dead).
- [ ] Tests that test mocks, not real behavior.

### Documentation Drift
- [ ] Project documentation (`AGENTS.md`, `DECISION-GUARDS.md`, ADR) contradicts code.
- [ ] Decision Guards (`PERF-###`, `DB-###`, `BR-###`) reference deleted code.
- [ ] README is stale — startup instructions do not work.
- [ ] Comments `// HACK` / `// FIXME` without a backlog item.

### Project-specific examples

> The examples below illustrate application in a .NET stack. Replace with your stack and conventions.

**Example: .NET + Clean Architecture + NetArchTest**

- **Architectural drift:** Domain must not reference Infrastructure; checked by architecture tests.
- **Documentation drift:** `AGENTS.md` contradicts code; Decision Guards reference deleted code.
- **Bug-regression tests:** `BUG###_DescriptiveName` tests that no longer reproduce the bug.
- **Automated guardrails:** `DuplicationGuardTest`, architecture tests, ratchet tests.

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/Domain/OrderService.cs:42`
2. **Code quote:** 3-5 lines showing the problem
3. **Rationale:** why this is tech debt (reference to rule above)
4. **Fix:** specific action or code suggestion

**NEVER report:**
- "Code is bad" without specific file and line
- "Needs refactoring" without specifying what and why
- Problems you cannot confirm with code

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

- **BLOCKER** — security/stability at risk (circular dependency, dead code in hot path)
- **CRITICAL** — slows down development (duplication in 3+ places, architectural drift)
- **MAJOR** — debt for the future (unnecessary interface, stale comment)
- **MINOR** — inconvenience (commented-out code block)

- **CONFIRMED** — debt proven by evidence: dead code with no references traced (incl. reflection/DI), duplication with divergent implementations (`>= 100` vs `> 100`), circular dependency shown in the project graph.
- **NEEDS_REVIEW** — possible false positive (e.g., interface with one implementation — may be a Port). Requires human judgment.

Checklist items without evidence are **investigation signals**, not findings: an
interface with one implementation or a `// TODO` comment does not prove debt by itself.

## Outputs and Downstream Consumer

```markdown
## Tech Debt Audit — {date}

### BLOCKER (security/stability at risk)
- [ ] [CONFIRMED] {description} → {file:line}

### CRITICAL (slows down development)
- [ ] [CONFIRMED|NEEDS_REVIEW] {description} → {file:line}

### MAJOR (debt for the future)
- [ ] [CONFIRMED|NEEDS_REVIEW] {description} → {file:line}

### Backlog
| ID | Description | Priority | Owner | Quarter |
|----|-------------|----------|-------|---------|
| TD-001 | {description} | P1/P2/P3 | {role} | Q3 |
```

**Input from:** Code Review Agent (pattern observations), Architecture Tests, Programmer Agent (TODO comments).
**Output to:** Backlog Hygiene Agent (backlog addition), Programmer Agent (refactoring), Doc Hygiene Agent (project documentation update).

## Trigger or Schedule

Runs:
- Once per sprint (before planning the next one).
- Before quarterly planning (to estimate refactoring scope).
- When velocity drops without obvious reasons.

## Limitations and Expected False Positives

- An interface with one implementation may be a deliberate Port; a TODO may be intentional staging — mark these `NEEDS_REVIEW`.
- Without architecture tests, drift detection is manual and approximate.
- Dead-code detection misses reflection- and DI-based usage unless usage is traced.
