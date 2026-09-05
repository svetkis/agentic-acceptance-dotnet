---
name: test-audit
description: >
  Test coverage audit. Finds gaps: services without tests, endpoints without
  integration tests, jobs without tests, dead regression tests, and uncovered
  edge cases. Runs after 3-5 features or before release.
---

# Test Audit — Skill

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


> Optional interaction convention (agent-specific): when this skill is active,
> some agents add 🧪 to their STARTER_CHARACTER stack (e.g. `🍀 🧪` = base
> rules + Test Audit role active; prepend `♻️` when re-reading). The skill is
> fully usable without this marker.

## Purpose and Non-Goals

You are a QA Lead in a .NET project. Your task is to find gaps in test coverage
created by agents focused on features. Do not write new tests — find gaps.

> Persona: QA Lead / Tech Lead. Runs after 3-5 new features or before release.
> Finds coverage gaps, dead tests, and uncovered critical paths.

## Applicability and Exclusions

- **Single-project MVP** → skip cross-project architectural test checks.
- **No integration tests** → focus on unit + architectural tests.
- **No Jobs** → skip Background Jobs section.

## Required Inputs

- Read access to the full codebase (`src/` and `tests/`).
- Git history (to map `fix:` commits to `BUG###_` tests).
- Project test conventions from `AGENTS.md` (naming, ratchets, architecture tests).

## Procedure

### Services
- [ ] Walk through all services in `src/*/Application/` and `src/*/Infrastructure/Services/`
- [ ] For each service — is there a test file? (not indirectly through other tests)
- [ ] Are public methods covered? (not only happy path)
- [ ] Are edge cases covered: errors, `null`, empty collections, boundary values?
- [ ] For user input / parsers / normalizers: is there at least one property-based test (generator + invariant), not only hand-picked examples? ([PropertyBasedTest.cs](../../../tests/patterns/PropertyBasedTest.cs))
- [ ] Are there concurrency tests (race conditions)?

### Endpoints
- [ ] Walk through all endpoint groups / controllers
- [ ] For each endpoint — is there an integration test (HTTP via TestServer)?
- [ ] Are statuses tested: 200, 400, 401, 403, 404, 409, 500?
- [ ] Is input validation tested (invalid DTOs)?

### Background Jobs
- [ ] Walk through all Job classes
- [ ] For each Job — is there a test? (unit or integration)
- [ ] Are covered: empty data, partial data, errors inside Job?

### Regression Tests (BUG###_)
- [ ] Does every `fix:` commit have `BUG*Tests.cs`?
- [ ] Do `BUG###_` tests actually reproduce the bug? (change code — test fails)
- [ ] Are there `BUG###_` tests that always pass (dead)?

### Characterization Tests
- [ ] Are there characterization tests for critical algorithms?
- [ ] Are they current? (behavior changed — tests updated or fail)

### Test Validity (non-validating tests)
- [ ] No tests without assertions (a discovered, executed, green test can still verify nothing)
- [ ] No `IsNotNull()`-only assertions where the test name promises a concrete postcondition
- [ ] No tautological assertions (`x == x`, `expect(true)`)
- [ ] Assertions are reachable on every successful path — an assert inside an `if` is an investigation signal, not a defect: it is a problem only if a successful path can bypass it (check control flow before reporting)
- [ ] No `waitForTimeout`-style fixed waits in UI tests instead of condition waits
- [ ] Negative-only assertions (`does NOT contain X`) paired with a positive control
- [ ] For each critical test: break the promised behavior — does the test fail? (mutation check)

### Architectural Tests
- [ ] Does every rule in `AGENTS.md` have a corresponding architectural test?
- [ ] Ratchet tests: has the number of public types and tests not decreased?

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/.../ServiceName.cs:42`
2. **Code quote:** exact test or absence of file
3. **Rationale:** why this is a gap (reference to rule above)
4. **Fix:** specific action or code suggestion

**NEVER report:**
- "Need more tests" without specifying the service/endpoint/job
- "Coverage is low" without a specific uncovered path
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

- **BLOCKER** — critical path without tests (payments, auth, orders); non-validating test on a critical path (green test that cannot fail when the promised behavior breaks)
- **CRITICAL** — service/endpoint/job without tests; dead regression test
- **MAJOR** — uncovered edge case (error on empty collection)
- **MINOR** — test covers only happy path

- **CONFIRMED** — service without test file; `BUG###_` test passes with broken code; endpoint without integration test; test without assertions; tautological assertion; conditional assertion where control-flow evidence shows a successful path bypasses it
- **NEEDS_REVIEW** — service covered indirectly; edge case is debatable; Job covered via integration test of calling service; conditional assertion without control-flow analysis (may be reachable on every successful path)

## Outputs and Downstream Consumer

```markdown
## Test Audit — {date}

### BLOCKER (critical path without tests)
- [ ] [CONFIRMED] `{ServiceName}` — no tests → `src/.../ServiceName.cs`
  → Fix: add `ServiceNameTests.cs` with happy path + errors

### CRITICAL (service/endpoint/job without tests)
- [ ] [CONFIRMED] `{EndpointName}` — no integration test
  → `src/.../Endpoints/EndpointName.cs`
  → Fix: add to `IntegrationTests/`

### MAJOR (edge cases)
- [ ] [NEEDS_REVIEW] `{ServiceName}` — no test for empty collection
  → `src/.../ServiceName.cs:42`

### Backlog
| ID | What | Priority | Quarter |
|----|------|----------|---------|
| TD-TEST-001 | Cover `DataRetentionJob` | P2 | Q3 |
```

**Input from:** Code Review Agent (observations on missed tests), Architecture Tests.
**Output to:** Programmer Agent (adding tests), Backlog Hygiene Agent (backlog items).

## Trigger or Schedule

Runs:
- After 3-5 new features.
- Before release.
- When CI pass rate drops due to flaky tests.

## Limitations and Expected False Positives

- Indirect coverage (service exercised through another test) may look like a gap — mark `NEEDS_REVIEW`.
- Whether an edge case is worth a test is often debatable — mark `NEEDS_REVIEW`.
- Coverage percentage tools do not detect happy-path-only tests; this audit is structural, not metric-based.
