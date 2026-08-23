---
name: perf-test-authoring
description: >
  Authoring skill for performance tests of hot paths: test design, deterministic
  fixtures with real infrastructure, latency/SQL/allocation budgets,
  calibration and baseline policy.
---

# Performance Test Authoring — Skill

Optional interaction convention (agent-specific): when this skill is active, add
`⚙️` to your STARTER_CHARACTER stack. The skill is fully usable without emoji markers.

## Purpose and Non-Goals

You are a performance test author. Your task is to write reproducible,
infrastructure-aware micro-benchmarks over real hot paths, with budgets that
fail when performance regresses.

Non-goals: running whole load suites (load-test-ops skill), static code review
of hot paths (performance-audit skill), or proving business correctness (Test
Audit skill).

## Applicability and Exclusions

- **Applies to:** service-layer hot paths (read paths, write paths, polling,
  cache-stampede scenarios), allocation-sensitive code, query-count-sensitive
  operations.
- **Excludes:** one-off investigations (a quick console measurement is fine),
  cold paths where latency does not matter, and comparisons across different
  hardware.

## Required Inputs

- The hot path under test and its domain preconditions (working hours, slot
  availability — whatever the operation requires to succeed).
- A real database/infrastructure fixture (Testcontainers or equivalent).
- The project's budget assertion helpers (latency, SQL command count,
  allocations) or a plan to add them.

## Procedure

### 1. Fixture rules
The shared fixture must:
- [ ] Start **one real database container per test class** — never an in-memory
  EF provider (it does not reflect real query/lock behavior).
- [ ] Run all migrations and build the full infrastructure service provider.
- [ ] Replace nondeterministic externals with deterministic fakes (time
  provider, messenger/gateways).
- [ ] Seed **production-like, deterministic** data (fixed random seed).

Seed-data checklist:
- [ ] Domain preconditions actually hold (schedule covers target slots, time
  zones respected, weekends avoided).
- [ ] No unique-index collisions in generated data.
- [ ] Typed IDs constructed explicitly, not cast from raw values.
- [ ] Denormalized columns populated if the code depends on them.

### 2. Test-class rules
- [ ] Test classes do **not** run in parallel (`[NotInParallel]` in TUnit or
  equivalent) — keeps command counters meaningful and avoids DbContext
  contention.
- [ ] One shared fixture per class, not per test.

### 3. Test design
Each test exercises exactly **one operation type**, with a suffix that declares
what it budgets:

| Suffix | Measures | Assertion |
|---|---|---|
| `_LatencyAndSqlBudget` | Mean/p99 latency + SQL command count | Latency + command-count asserts |
| `_AllocationBudget` | Allocated bytes | Allocation assert |

Pattern (adapt helpers to your stack):

```csharp
// latency
var result = await LatencyTracker.MeasureAsync(() => operation());
LatencyAssert.WithinBudget(name, result, baselineMeanMs: 12.0, baselineP99Ms: 35.0);

// SQL commands
using var tracker = fixture.TrackSqlCommands();
await operation();
tracker.AssertMaxCommands(3, name);

// allocations
var bytes = await AllocationTracker.MeasureAsync(() => operation());
AllocationAssert.WithinBudget(name, bytes, baselineBytes: 24_000);
```

### 4. Calibration and baseline policy
- [ ] Baselines come from **real calibration runs** on the target
  infrastructure (calibration mode emits measured values), never from guesses.
- [ ] Tolerance applied: latency **+20%**, allocations **+10%** — to absorb
  machine jitter.
- [ ] After calibration, re-run without the calibration flag and confirm green.
- [ ] **Never raise a budget to silence a failing test** — investigate the code
  first; raising is the last resort and must be justified in the report.

### 5. Run rules
- [ ] Always **Release** mode (`dotnet run -c Release --project ...`). Debug
  builds distort latency and allocations and break comparability with history.
- [ ] No other heavy process on the machine during runs.

## Evidence Requirements

Every finding/budget change MUST include:
1. **Test and operation:** `HotPathPerfTests.GetWeekAsync_LatencyAndSqlBudget`
2. **Measured values:** mean/p99, SQL count, bytes — before and after
3. **Baseline provenance:** calibration date and infrastructure
4. **Action:** fix code, recalibrate (with reason), or document equivalent noise

**NEVER report:** budgets set without calibration; "feels slower" without
numbers; a budget raise without an investigation trail.

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
| **BLOCKER** | Budget raised without investigation of a suspected real regression |
| **CRITICAL** | Hot-path budget exceeded after a code change (release-blocking until explained) |
| **MAJOR** | New hot path shipped without a budget test; fixture uses in-memory provider |
| **MINOR** | Flaky budget near tolerance edge; missing tolerance on a new baseline |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Reproduced in Release on quiet machine, comparable metadata |
| **NEEDS_REVIEW** | Single run, contended machine, or baseline from different infrastructure |

## Outputs and Downstream Consumer

```markdown
## Perf Test Authoring — {action} — {date}

### Tests
| Test | Budget | Measured | Status |
|------|--------|----------|--------|
| X_LatencyAndSqlBudget | 12/35 ms, 3 SQL | 9/28 ms, 3 SQL | 🟢 |
| Y_AllocationBudget | 24 000 B | 31 000 B | 🔴 +29% |

### Calibration
- CALIBRATE_LATENCY <name>: mean X ms, p99 Y ms
- CALIBRATE_ALLOCATION <name>: N bytes

### Recommended baselines (tolerance applied)
- <name>: mean = X * 1.2, p99 = Y * 1.2
```

**Downstream consumer:** Programmer Agent (fix regressions), Load Test Ops
(micro-budgets feed scenario expectations), CI (budget tests run as normal
tests via `dotnet run --project`).

## Trigger or Schedule

When adding or changing a hot path, recalibrating after infrastructure changes,
refactoring perf tests, or investigating a flaky perf test.

## Limitations and Expected False Positives

- Micro-benchmarks measure the test rig; absolute values do not transfer
  between machines — only budget-relative results on one rig are meaningful.
- JIT/GC noise can push a measurement past tolerance occasionally; demand a
  reproduced failure before treating it as a regression.
- SQL-count budgets are database-agnostic but latency budgets are not —
  recalibrate when the database engine/version changes.
- A passing budget test proves the operation stayed within budget, not that the
  operation is correct (that is the Test Audit skill's job).
