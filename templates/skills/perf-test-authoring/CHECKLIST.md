# Performance Test Authoring — Checklist

## Fixture
- [ ] Real database container per test class (no in-memory EF provider)
- [ ] Migrations run; full infrastructure provider built
- [ ] Nondeterministic externals replaced (time provider, gateways)
- [ ] Seed data production-like and deterministic (fixed seed)

## Seed Data
- [ ] Domain preconditions hold (schedules, time zones, working hours)
- [ ] Weekends / non-working hours avoided where relevant
- [ ] No unique-index collisions
- [ ] Typed IDs constructed explicitly
- [ ] Denormalized columns populated

## Test Class
- [ ] `[NotInParallel]` (or stack equivalent)
- [ ] One shared fixture per class
- [ ] Test exercises exactly one operation type

## Budgets
- [ ] Suffix declares the budget (`_LatencyAndSqlBudget`, `_AllocationBudget`)
- [ ] At least one budget assertion per test (latency, SQL count, allocations)
- [ ] Baselines from real calibration runs — not guesses
- [ ] Tolerance applied: latency +20%, allocations +10%
- [ ] Re-run without calibration flag and confirm green

## Runs
- [ ] Release mode only (`dotnet run -c Release --project`)
- [ ] Quiet machine (no parallel heavy processes)
- [ ] No budget raised without an investigation trail

## Report
- [ ] Budget vs measured per test
- [ ] Calibration values with infrastructure noted
- [ ] Recommended baselines (tolerance applied)
- [ ] Findings follow the finding schema
