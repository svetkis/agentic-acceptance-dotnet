# E2E Testing via MCP — the Agent Tests the System Itself

> Level 3 (System Checks). Catches "works in code, broken in reality"
> bugs before a human tester sees them.

## The trap

An agent can write code that compiles, passes unit tests, and passes its own
review — and still break a user flow end-to-end: the booking button saves
nothing, the Telegram bot answers with last week's menu, a stale cache serves
the canceled slot. None of the Level 1–2 controls see this, because the break
lives in the interaction between deployed components, not in any single file.

## The pattern

Point a coding agent at an already deployed stand (test environment) through
MCP tools and let it execute basic user scenarios:

1. **Give the agent access, not screenshots.** MCP server for the browser
   (Playwright/Chrome MCP), API client, or the bot's admin interface.
2. **List the flows that matter** — the ones whose breakage you would learn
   about from an angry customer ("book a slot", "cancel", "pay"). Do not cover
   everything.
3. **Ask for evidence, not verdicts.** The agent must quote the actual state
   after the action: the HTTP response, the database row, the bot's reply —
   "looks fine" is not a result.
4. **Fail on mismatch.** A scenario report becomes a bug ticket with
   reproduction steps; the agent may fix it and re-run the scenario.

Example instruction to the agent:

```text
Stand: https://test.slotik.local (deployed in CI, seed data: master #42, 3 free slots)
Run these scenarios and attach evidence:
1. Book the first free slot as client A → show the booking row in the DB
   (via the admin MCP tool) and confirm the slot is blocked for client B.
2. Cancel the booking → confirm the slot is bookable again.
3. Reload the schedule page twice → confirm the second load serves the
   updated state, not the pre-booking cache.
```

## What it caught (observed case)

9 fixes in ~6 months: broken UI flow, a stale cache serving canceled slots,
self-booking (a master's own slot could be booked by the same master). All
three were invisible to unit tests and code review.

## What it is NOT

- **Not a replacement for smoke tests.** Smoke checks one deterministic
  critical path on every merge; E2E MCP is broader, agent-driven, and run
  before involving a human tester or on demand.
- **Not deterministic.** Same agent, same stand, different runs can explore
  differently — treat it as a cheap exploratory pass, not a regression gate.
- **Not a load check.** Concurrency and latency belong to
  [LoadTest](../../tests/patterns/LoadTest.cs).

## Where it lives in the model

Engineering Assurance Levels → **System Checks** (Level 3), next to smoke and
load. Related trap: [agent-behavior.md](../traps/agent-behavior.md) §1.
