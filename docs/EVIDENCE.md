# Evidence & ROI of the Control Levels

> Quantitative material behind the Engineering Assurance Levels model
> ([README.md](../README.md#how-it-works)). This is the only place in the repository
> where effectiveness metrics and cost estimates are kept.
>
> **Evidence note.** Data classification below: **observed case** — a single project
> of the author, git history of fix commits over ~6 months,
> denominator ~450 commits. The numbers are a single observation, not a reproducible
> measurement or a benchmark; do not extrapolate to your project. Cost figures in the
> ROI tables are **estimates** (expert judgment), marked `~`.

---

## What each level caught (from git history, observed case)

| Level | Control | Bugs found | % of fixes | Examples |
|-------|---------|-----------|------------|----------|
| Change Checks | Compiler + types | ~0 commits | — | Catches before commit, invisible in git |
| Behavior Checks | Arch tests + Ratchet | ~0 commits | — | Catches before commit, invisible in git |
| Behavior Checks | Unit/integration | 6 commits | 1.3% | Falling tests after refactoring |
| Behavior Checks | Code review | 8 commits | 1.8% | XSS, await, data leak |
| System Checks | Smoke | ~0 commits | — | Prevents critical-path regression before merge |
| System Checks | E2E MCP | 9 commits | 2.0% | UI flow, stale cache, self-booking |
| Reality Checks | Audits | 19 commits | 4.2% | Security, i18n, UX, perf |
| System Checks | Load | ~0 commits | — | Prevents degradation before production |
| Engineering Governance | Human judgment | ~78 commits | 17% | Business logic, edge cases |
| — | Gray zone | ~331 commits | 74% | Unknown who found it |

### The invisible layer paradox

Compiler, arch tests, and smoke are the **most effective** controls, but in git they
show 0 commits. They prevent bugs **before** code leaves the workstation.

---

## ROI by control

| Control | Setup cost | Maintenance cost | Break-even |
|---------|-----------|------------------|------------|
| Compiler + types | 0 (built-in) | 0 | Instant |
| Arch tests + Ratchet | ~2 days | ~1 hour/month | First week |
| Unit tests | ~2 weeks | ~30 min/feature | First month |
| Code review | ~0 (AGENTS.md rule) | ~2 min/commit | First XSS |
| Smoke | ~1 day | ~15 min/session | First broken critical path |
| E2E MCP | ~3 days | ~1 hour/platform | After stale cache (22 days in prod) |
| Audits | ~0 (prompts) | ~2 hours/audit | First batch |
| Load (NBomber) | ~1 day | ~30 min/scenario | First silent breakdown under load |
| Human judgment | — | ~hours-days | Impossible to measure |

### Grooming ROI (Control Maintenance)

| Skill | Cost | Break-even |
|-------|------|------------|
| memory-hygiene | ~30 min/sprint | First architectural bug from stale memory |
| doc-hygiene | ~1 hour/sprint | First time agent broke code due to outdated AGENTS.md |
| backlog-hygiene | ~30 min/sprint | First false positive `task-compliance` on a dead spec |

### The invisible decay paradox

Guardrail decay is invisible in git, not flagged by CI, and not caught by tests.
It hits suddenly: the agent "forgets" a rule because AGENTS.md is out of sync, or
makes an architectural decision based on a stale note from Auto Memory. Grooming
(`memory-hygiene`, `doc-hygiene`, `backlog-hygiene`) is the only defense against this.

---

## Evolution: how the control set grew (observed case)

```
January:   compiler + types → unit tests → smoke
  ↓ architecture bugs
February:  + arch tests → + code review
  ↓ UI bugs
March:     + E2E MCP (acceptance) → + characterization tests
  ↓ write-path degradation
April:     + audits (in batches) → + NBomber → + ratchet tests
```

Every new control is a reaction to a bug class that previous controls missed.

---

## Principle: guardrails must be justified by risk

> A guardrail is justified by a real incident, a credible threat model, a regulatory
> requirement, or a documented high-impact failure scenario.

Every guardrail — Roslyn analyzer, architecture check, test, artifact regex or linter
rule — must answer: **"What specific risk does this cover?"** "A real bug" is the
strongest answer, but not the only valid one: proactive security and compliance
controls are justified by a threat model or a regulatory requirement before the
first incident.

Zero triggers is **not sufficient grounds** for removal. When reviewing a guardrail,
weigh risk severity, likelihood, maintenance cost, false-positive rate, and the
presence of compensating controls. A guardrail covering a rare high-impact risk
(security, data loss) may never fire and still be justified. A removal candidate:
low impact + high maintenance cost + existing compensating checks.
