# Load Test Ops — Checklist

## Pre-Run
- [ ] Container runtime running (Testcontainers fixtures)
- [ ] No other heavy load/build process on the machine
- [ ] Fixture config current (credentials, disabled rate limiting/security stubs)
- [ ] Build green; run via `dotnet run --project` in **Release**

## Run Order
- [ ] Baseline/validation scenario first (fixture validation + seed warm-up)
- [ ] Realistic flows before stress/spike
- [ ] Long runs (> 5 min) in background

## Scenario Classification
- [ ] Realistic flows identified (latency/RPS reporting source)
- [ ] Synthetic contention scenarios identified (error rate + correctness only)
- [ ] No mixing of the two classes in one conclusion

## Database State
- [ ] Read-only scenarios may share the seeded container
- [ ] Mutating scenarios reseed/reset the database between runs
- [ ] Restorable seed snapshot used for expensive datasets

## Thresholds
- [ ] Error rate < 1% enforced for normal scenarios
- [ ] Read/write latency budgets calibrated for the project
- [ ] Stress/spike exempt from latency checks, not from error-rate budget

## Archive
- [ ] Report + log copied to `history/runs/<scenario>/<timestamp>/`
- [ ] Summary row appended (timestamp, scenario, commit, duration, ok/fail, RPS, percentiles)
- [ ] History folder committed after the session

## Comparison
- [ ] Only matching scenarios compared
- [ ] Comparable metadata (mode, machine, seeded state)
- [ ] Trends backed by ≥ 2 runs or explicitly marked NEEDS_REVIEW

## Report
- [ ] Combined table sorted by RPS
- [ ] Expected failures marked explicitly
- [ ] Regression risk verdict with evidence
