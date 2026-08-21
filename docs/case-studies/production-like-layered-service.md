# Case Study: Production-Like — Layered Service with Reality Checks

> **Evidence class:** *illustrative composite* — the control set and green/trap
> behavior are reproducible in this repository (`examples/DemoProject/`,
> `examples/DemoProject/TRAPS.md`, 33 tests); the incident narrative is a composite
> of the trap catalog (`docs/traps/`) and talk material, not a single measured
> project. Costs are *estimates*, marked `~`.

## Context

A production-like booking service: Clean Architecture (Domain, Application,
Infrastructure), EF Core + PostgreSQL, public API consumed by a mobile app.
Two developers + AI agents writing most of the code; releases twice a month;
the service handles PII (customer names, phones).

## Risk Profile

| Risk | Likelihood | Impact | Driver |
|------|-----------|--------|--------|
| Agent breaks layer boundary (Domain → Infrastructure) | High | High | Fast agent edits across projects |
| PII leak into logs/responses | Medium | High | Sensitive data + generated logging |
| N+1 / missing index under growth | Medium | High | Agent writes queries "by eye" |
| Public contract drift (DTO change breaks the app) | Medium | Medium | External consumer |
| Latency regression on write path | Medium | Medium | Mixed read/write load |
| Complexity growth (unjustified patterns) | High | Low | Agent "best practices" |

## Selected Controls

| Control | Level | Why selected |
|---------|-------|--------------|
| `AGENTS.md` + Decision Guards (`PERF-###`) | Control Foundation | Steer agent; stop "cleanup" of deliberate deviations |
| Banned APIs + custom Roslyn analyzers (`SAE001`–`SAE005`) | 1. Change Checks | Defects caught in IDE, before CI |
| Pre-commit code-review skill | 1. Change Checks | Diff-level review before commit |
| Architecture tests (NetArchTest) + ratchets | 2. Behavior Checks | Layer boundaries, silent deletions |
| Snapshot tests (JSON contracts) | 2. Behavior Checks | External consumer |
| Allocation budget tests (`[HotPath]`) | 2. Behavior Checks | Measured hot path exists |
| NBomber load test (read/write mix, Max latency gate) | 3. System Checks | `tests/patterns/LoadTest.cs` |
| Security audit (PII focus) | 4. Reality Checks | Sensitive data in the risk profile |
| DBA audit (migrations, indexes) | 4. Reality Checks | EF Core + growing tables |
| Simplicity audit | 4. Reality Checks | Complexity risk rated High |
| `memory-hygiene` / `doc-hygiene` / `backlog-hygiene` | Control Maintenance | Agent artifacts drift |

## Rejected and Removed Controls

| Control | Disposition | Why |
|---------|-------------|-----|
| E2E via Telegram/browser MCP | Rejected | No bot/channel surface in this service |
| i18n audit | Rejected | Single-locale product |
| bot-audit | Rejected | Not a bot |
| mutation-audit (Stryker) | Deferred | Cost ~hours/run; scheduled only before release |
| `NuGetAuditSuppress` for transitive CVE | **Removed** | Root cause fixed by upgrading NBomber 5.8.0 → 6.5.0 (MessagePack dropped) instead of suppressing indefinitely |
| Guardrail "index for every WHERE" | **Removed** | False-positive generator; replaced by hot-path EXPLAIN evidence rule (see `dba-audit`, `performance-audit`) |

## Findings (reproducible + composite)

- **Reproducible here:** all 33 tests green in `examples/DemoProject/`;
  every trap in `examples/DemoProject/TRAPS.md` is caught (CI job "Traps —
  Guardrails Must Catch").
- **Composite (from `docs/traps/`):** stale cache survived compiler + unit
  tests + review and was caught only by system-level exercise
  (`docs/traps/testing.md#silent-breakdown`); P50 looked fine while Max latency
  degraded (`docs/traps/runtime.md#p50-vs-max`).

## False Positives Observed

- Security audit flagged a public webhook as unprotected — NEEDS_REVIEW
  (protected by secret token; documented as Decision Guard).
- Simplicity audit flagged an interface with one implementation — turned out
  to be a Port for a planned second adapter; NEEDS_REVIEW, no action.
- Ratchet baselines bump on legitimate deletions (~2 min each).

## Costs (estimates)

| Item | Setup | Maintenance |
|------|-------|-------------|
| Control Foundation (`AGENTS.md`, Decision Guards) | ~1 day | ~1 h/month |
| Change Checks (analyzers, review skill) | ~2 days | ~2 min/commit |
| Behavior Checks (arch, ratchet, snapshot, allocation) | ~3 days | baselines ~1 h/month |
| System Checks (NBomber) | ~1 day | ~30 min/scenario |
| Reality Checks (3 audits) | ~0 (skills) | ~2 h/audit, ~3 per quarter |
| Control Maintenance (3 hygiene skills) | ~0 | ~2 h/sprint |

## Takeaway

Controls were selected from the risk profile, not copied wholesale: three
audit skills were rejected outright, one suppression was removed by fixing the
root cause, and one heuristic rule was deleted as a false-positive generator.
The dominant cost is not setup but maintenance — ratchet baselines and
NEEDS_REVIEW triage — which is why Control Maintenance is staffed explicitly.
