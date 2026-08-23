---
name: allocation-budget-audit
description: >
  Audit of allocation regressions in critical (hot path) methods. Checks that the
  agent did not add allocations to latency-sensitive code, and that every
  hot path method has a paired allocation test.
---

# Allocation Budget Audit — Skill

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


> Optional interaction convention (agent-specific): some agents mark an active
> skill with an emoji in their status stack (e.g., `💸` for this skill, prefixed
> with `♻️` on re-read). The skill is fully usable without it.

## Purpose and Non-Goals

You are a performance engineer. Your task is to catch allocation regressions in
methods marked as hot paths before they reach production. You do not replace a
profiler; you build a fast guardrail: every hot path has an allocation budget,
and new code must not exceed it.

Portable core:
- Every method on a critical path must have a recorded allocation budget and a test that enforces it.
- New code must not exceed the budget without explicit rationale and repeated measurement.
- Measurements must be repeatable: environment, runtime, GC mode, and regression threshold are fixed.

Non-goals: full profiling sessions, latency/throughput load testing (see
performance-audit), optimization work without a measured regression.

## Applicability and Exclusions

Requires adaptation:
- Hot path marker: `[HotPath]`, `// HOTPATH`, a registry, etc.
- Measurement mechanism: `GC.GetAllocatedBytesForCurrentThread`, BenchmarkDotNet, custom harness.
- Regression threshold: baseline + N%, fixed limit, percentile.
- Language / runtime: .NET, JVM, Go, native, etc.

Adaptation for project:
- **No hot path methods** → Won't do, document it.
- **Hot path attribute / marker exists** → every marked method must have an
  allocation test or equivalent guardrail.
- **Critical path without a marker** → introduce a hot path marker and inventory it.
- **Runtime without per-thread allocation API** → use available API carefully and document measurement error.

Not applicable when:
- The project has no latency-sensitive paths.
- Stable measurements are impossible (e.g., shared CI runners with high variance).

## Required Inputs

- Repo access: hot path markers, allocation tests, and the diff under review.
- Recorded baseline budgets and the regression threshold (or a way to compute a baseline).
- A stable measurement environment: runtime, GC mode, hardware/OS fixed.

## Procedure

### 1. Hot path inventory
- [ ] All methods with a hot path marker are found (via reflection, static analysis, or registry).
- [ ] Every hot path has an allocation test or equivalent guardrail.
- [ ] Hot path marker is not used for helper / rare operations.

### 2. Allocation budget
- [ ] A baseline allocation budget is recorded for every hot path.
- [ ] Regression threshold is defined (e.g., baseline + 10% or a project-specific limit).
- [ ] Tests use warmup + multiple iterations for stability.
- [ ] Tests run in CI on relevant hardware / OS.

### 3. Regressions
- [ ] Methods exceeding the budget are reviewed manually.
- [ ] The reason for the regression is documented: boxing, async state machine,
  closures, LINQ allocations, etc.
- [ ] The fix is confirmed by a repeated measurement.

### 4. Compile-time guardrail
- [ ] If the project has a custom analyzer for hot paths (Roslyn, linter, AST analysis), it catches forbidden patterns at compile time.
- [ ] The analyzer has its own unit tests.

### Project-specific examples

> The examples below illustrate application in a .NET stack. Replace with your runtime and tools.

Example: .NET + `[HotPath]` + `GC.GetAllocatedBytesForCurrentThread`

- **Marker:** `[HotPath]` attribute.
- **Test:** `{MethodName}_AllocationBudget`.
- **Analyzer:** `HotPathAnalyzer` catches `new` / `async` / boxing inside `[HotPath]` at compile time.
- **Analyzer tests:** see `tests/patterns/AnalyzerTests.cs` in the methodology repository.

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/Infrastructure/EntityQueryService.cs:88`
2. **Code quote:** 3–5 lines showing the added allocation
3. **Baseline/current values:** baseline 1024 bytes, current 1150 bytes
4. **Reason:** boxing in LINQ, async state machine, closure, etc.
5. **Measurement details:** iteration count, GC mode, OS/runtime

**NEVER report:**
- “The method is slow” without measurements
- “Needs optimization” without a concrete bottleneck
- Problems not confirmed by a repeatable measurement

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

Severity describes impact and urgency:

| Severity | Meaning |
|----------|---------|
| **BLOCKER** | Change/release must not proceed; immediate action required |
| **CRITICAL** | High impact; fix in the current iteration |
| **MAJOR** | Degradation or defect; schedule the fix |
| **MINOR** | Improvement; backlog |

Skill-specific calibration:
- **BLOCKER** — budget exceeded by more than 50% on a critical hot path.
- **CRITICAL** — budget exceeded by 10–50% on a hot path.
- **MAJOR** — missing allocation test for a hot path method.
- **MINOR** — fluctuation within measurement noise.

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

Skill-specific calibration:
- **CONFIRMED** — repeated measurement on the same hardware / OS / runtime shows a
  stable budget regression.
- **NEEDS_REVIEW** — only one measurement, or the delta is within 5–10%. Needs rerun.

## Outputs and Downstream Consumer

Report format:

```markdown
## Allocation Budget Audit — {date}

### Summary
| Method | Baseline (bytes) | Current (bytes) | Delta | Status |
|--------|------------------|-----------------|-------|--------|
| GetHotPathData | 1024 | 1150 | +12% | FAIL |
| GetDayTimeline | 512 | 510 | -0.4% | OK |

### Regressions
- [ ] [CONFIRMED] `{File}:{Line}` — `{Method}`: +{N}% because of {reason} → {fix}

### Missing budget tests
- [ ] [CONFIRMED] `{Method}` does not have an allocation test
```

Consumers:
- **Input from:** Load Tests, Performance Audit, compile-time guardrail for hot paths.
- **Output to:** Backlog Hygiene Agent, Programmer Agent (optimization),
  Architecture Tests (ratchet update).

## Trigger or Schedule

Runs when a change touches hot path-marked methods or their allocation tests,
and as a gate before releasing latency-sensitive features.

## Limitations and Expected False Positives

- Allocation deltas within measurement noise (±5%) are investigation signals, not defects.
- Shared CI runners with high variance produce unstable measurements — rerun before reporting.
- Catches managed allocations only; latency regressions from I/O, locks, or
  contention are out of scope.
