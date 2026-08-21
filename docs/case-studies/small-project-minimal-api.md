# Case Study: Small Project — Single-Project Minimal API

> **Evidence class:** *observed case* — reproducible in this repository
> (`examples/DemoProject.MinimalApi/`, 7 tests, green CI). Costs are *estimates*
> from the example's construction, marked `~`.

## Context

A small booking API: one project, two features (Orders, Payments), in-memory
storage, no EF Core, no Clean Architecture. The team is one developer + an AI
agent writing most of the code.

## Risk Profile

| Risk | Likelihood | Impact | Driver |
|------|-----------|--------|--------|
| Agent renames public API surface silently | High | Medium | No layer boundaries to catch drift |
| Agent uses banned API (`DateTime.Now`) | High | Medium | Time-zone bugs in booking logic |
| Agent duplicates business logic across features | Medium | Medium | Two parallel feature folders |
| Missing `CancellationToken` on async endpoints | Medium | Low | Agent boilerplate |
| Layer violation (Domain → Infrastructure) | N/A | — | No layers exist |

## Selected Controls

| Control | Level | Why selected |
|---------|-------|--------------|
| `AGENTS.md` with conventions | Control Foundation | Cheapest steering for the agent |
| Banned APIs analyzer (`DateTime.Now`) | 1. Change Checks | Compile-time, zero runtime cost |
| Naming + `CancellationToken` arch tests | 2. Behavior Checks | NetArchTest works without layers |
| Ratchet tests (public types, test count) | 2. Behavior Checks | Catches silent deletions |
| Duplication guard | 2. Behavior Checks | Direct response to the identified risk |
| TUnit + `dotnet run --project` | 2. Behavior Checks | Reproducible test run, `0 tests ran` guard |

## Rejected Controls (and why)

| Control | Why rejected |
|---------|--------------|
| Layer-dependency arch tests (Domain → Application → Infrastructure) | No layers — would produce 100% false positives |
| EF Core guard rules (AsNoTracking, Include, FindAsync) | No EF Core in the project |
| Allocation budget tests (`[HotPath]`) | No measured hot path; unjustified (no incident, no threat model) |
| NBomber load tests | In-memory store; no production-like load to reproduce |
| Snapshot/contract tests | No external consumers of the API yet |

## Findings (reproducible)

- All 7 guardrail tests pass on the green example — see
  `examples/DemoProject.MinimalApi/tests/DemoProject.MinimalApi.Tests/`.
- The trap counterpart (`examples/DemoProject/TRAPS.md`) demonstrates what each
  guard catches when the agent violates it (tests fail by design).

## False Positives Observed

- Ratchet on public types fires on any legitimate type removal — resolved by
  updating the baseline with a comment (expected behavior, ~2 min each time).
- Duplication guard matched a coincidental 3-line similarity once — marked
  NEEDS_REVIEW, no action.

## Costs (estimates)

| Item | Setup | Maintenance |
|------|-------|-------------|
| `AGENTS.md` adaptation | ~1 h | ~15 min/month |
| Arch + ratchet + duplication tests | ~2 h | baseline bumps, ~10 min/month |
| CI wiring (`run-and-verify-tests.sh`) | ~30 min | 0 |

## Takeaway

For a single-project MVP the entire Reality Checks level and most
System Checks are **unjustified**: no incident history, no sensitive data, no
production load. Control Foundation + a handful of Change/Behavior Checks cover
the actual risk profile at ~3 h of setup. Adopting the full artifact set would
be over-engineering — the exact anti-pattern the methodology warns about.
