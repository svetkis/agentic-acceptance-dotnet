---
name: load-test-ops
description: >
  Operational skill for running and comparing load tests (NBomber or
  equivalent): environment preflight, run order, scenario realism, thresholds,
  history archiving, and trend comparison against previous runs.
---

# Load Test Ops — Skill

Optional interaction convention (agent-specific): when this skill is active, add
`⚡` to your STARTER_CHARACTER stack. The skill is fully usable without emoji markers.

## Purpose and Non-Goals

You are a load test operator. Your task is to produce **reproducible** load test
runs and trustworthy comparisons with history — not to author new load test
scenarios (see the perf-test-authoring skill) and not to audit code for
performance smells (see the performance-audit skill).

The classic failure this skill prevents: a load run on a contended machine, in
Debug mode, with dirty database state, compared against last month's numbers —
producing a "regression" or "improvement" that is pure noise.

## Applicability and Exclusions

- **Applies to:** HTTP/API load tests (NBomber, k6 and similar), recurring
  performance validation before releases, regression tracking over time.
- **Excludes:** micro-benchmarks of individual methods (perf-test-authoring
  skill), static code analysis of hot paths (performance-audit skill), and
  production incidents.

## Required Inputs

- The load test project and its scenario list.
- Infrastructure preflight facts: container runtime status (if Testcontainers is
  used), no other heavy process running, build green in **Release**.
- The history archive: summary table (CSV or equivalent) + archived reports per
  scenario.

## Procedure

### 1. Pre-run checklist
- [ ] Container runtime (Docker/dockerd) is running if the fixture needs it.
- [ ] No other heavy load/build process on the machine (resource contention).
- [ ] Fixture configuration is current (test credentials, disabled rate
  limiting/inbound security stubs as the fixture defines).
- [ ] Build is green: `dotnet build` (run tests with `dotnet run --project`, per
  repo conventions).

### 2. Run order
- [ ] Always run the **baseline/validation scenario first** — it validates the
  fixture and warms the seed before expensive scenarios.
- [ ] Escalation scenarios (`stress`, `spike`) run last and are optional.
- [ ] Runs longer than ~5 minutes execute in the background.

### 3. Scenario realism — report flows correctly
Distinguish two classes and never mix them in one conclusion:
- **Realistic flows** (actual user journeys) — the only valid source for
  latency/RPS reporting.
- **Synthetic contention/stress scenarios** (many actors on the same resource,
  ramp beyond normal load) — validate correctness under extreme concurrency.
  Report **error rate and correctness outcomes** (e.g. exactly 1 success / N
  conflicts), not p95/p99.

### 4. State and sequential runs
- [ ] Read-only scenarios may share one seeded container/database.
- [ ] **Mutating scenarios** (onboarding, booking, cancel, and any write path)
  change state; running them sequentially in one container produces
  data-dependent failures that look like performance errors. Reset/reseed the
  database between mutating scenarios.
- [ ] Seeding large datasets is expensive — keep a restorable snapshot (e.g. a
  SQL dump) instead of reseeding every time.

### 5. Thresholds
The load program (not the operator) should enforce:
- [ ] Error rate < 1% for every normal scenario.
- [ ] Read paths: strict latency budgets (e.g. p95 < 800 ms, p99 < 1500 ms —
  calibrate per project).
- [ ] Write paths: relaxed budgets (e.g. p95 < 2000 ms, p99 < 3000 ms).
- [ ] Stress/spike scenarios skip latency checks but keep the error-rate budget.

### 6. Archive and compare
- [ ] Current-run tool output (e.g. `reports/`) is **temporary working output** —
  treat it as disposable.
- [ ] Every run is archived: report + raw log copied to a persistent history
  folder (`history/runs/<scenario>/<timestamp>/`), plus a row appended to a
  rolling summary (CSV) with timestamp, scenario, git branch/commit, duration,
  ok/fail counts, RPS, percentiles, thresholds status.
- [ ] The history folder is committed after each load-test session (it is the
  only copy that survives cleanups).
- [ ] Comparison rules — compare **matching scenarios only**:
  - same RPS but higher latency → read-path regression suspect;
  - higher error rate → breaking point shifted;
  - lower ok count → throughput regression.

## Evidence Requirements

Every conclusion MUST include:
1. **Scenario name and class** (realistic flow vs synthetic contention).
2. **Run metadata:** date, git commit, configuration (Release, seeded state).
3. **Numbers:** RPS, ok/fail, p95/p99 (realistic flows) or error rate +
   correctness outcome (contention).
4. **The comparison row** from the history summary when claiming a trend.

**NEVER report:** a regression verdict from a single run without a comparable
historical row; stress-scenario percentiles presented as normal latency; runs
performed in Debug mode or under a loaded machine.

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
| **BLOCKER** | Error-rate budget exceeded on a realistic flow before release |
| **CRITICAL** | p95/p99 budget exceeded on a realistic flow; throughput regression vs comparable history |
| **MAJOR** | Correctness failures under contention (lost updates, double-writes); history archive not committed after the session |
| **MINOR** | Flaky scenario, non-comparable run metadata |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Reproduced on ≥2 comparable runs with matching metadata |
| **NEEDS_REVIEW** | Single run, contended machine, or non-comparable baseline |

## Outputs and Downstream Consumer

```markdown
## Load Test Results — {date} @ {git commit}

### Summary
| Scenario | Class | RPS | p95, ms | p99, ms | Errors, % | Status |
|----------|-------|----:|--------:|--------:|----------:|--------|
| baseline | realistic | 18.2 | 210 | 480 | 0.0 | 🟢 PASS |
| stress | contention | 610 | — | — | 0.4 | 🟢 PASS |

### Comparison with {previous date}
- baseline p95: 180 → 210 ms (+17%) — within noise, monitor
- booking ok count: 10 400 → 8 900 (-14%) — 🔴 throughput regression

### Regression risk: low | medium | high
```

**Downstream consumer:** Programmer Agent (investigate regressions), Release
Readiness Audit (uses thresholds status), Human (release go/no-go).

## Trigger or Schedule

Before release, after hot-path or infrastructure changes, and on suspicion of a
performance regression. Not on every PR (duration).

## Limitations and Expected False Positives

- Machine jitter: ±10–20% latency drift between runs is normal on shared
  hardware; demand reproduction before declaring a regression.
- Test-environment latencies are not production latencies — budgets calibrated
  for the test rig do not transfer to production numbers.
- Contention scenarios are expected to degrade latency by design; only error
  rate and correctness are meaningful there.
- A green load run does not prove the absence of regressions in paths no
  scenario covers.
