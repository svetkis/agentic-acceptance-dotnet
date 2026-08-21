# Skeptical AI Engineering Control Pyramid

> **Legacy / visual metaphor.** This document is the extended illustration of the talk
> "AI is confident. I am not". The canonical artifact classifier is the
> **Engineering Assurance Levels** model in [`README.md`](README.md#how-it-works).
> Mapping of legacy names to levels:
>
> | Pyramid (legacy) | Engineering Assurance Level |
> |------------------|-----------------------------|
> | Layer 0. Agent instructions (AGENTS.md + Decision Guards) | Control Foundation |
> | 1.1 Compiler + types | 1. Change Checks |
> | 1.4 Pre-commit code review | 2. Behavior Checks (gate before PR) |
> | 1.2 Architecture + Ratchet | 2. Behavior Checks |
> | 1.3 Tests | 2. Behavior Checks |
> | 1.5 Smoke | 3. System Checks |
> | 2.1 E2E / MCP | 3. System Checks |
> | 2.3 Load (NBomber) | 3. System Checks |
> | 2.2 Audits | 4. Reality Checks |
> | Outer loop (human) | Engineering Governance (process, not a level) |
> | Artifact grooming | Control Maintenance (process, not a level) |

> **Skeptical AI Engineering (SAE)** — methodology from the talk "AI is confident. I am not".
> Feedback speed determines the cost of an error. The earlier you find it, the cheaper it is to fix.
> AI makes it possible to add control layers that used to be too expensive: audits, load testing, a second review.
> The compiler catches things in seconds. Smoke in minutes. Anything longer is the acceptance cycle. Human judgment is the outer loop.

---

<a name="layer-0"></a>
## Layer 0. Agent Instructions (AGENTS.md + Decision Guards)

The agent reads these before writing code. This is not a verification layer — it is a development layer.

- `rules/AGENTS_TEMPLATE.md` — hierarchical instructions: root + per-module
- `PERF-###` / `DB-###` / `AUD-###` — numbered decisions in code that the agent must not "clean up"
- An architecture test checks the uniqueness of decision IDs

---

<a name="layer-1"></a>
## Layer 1. Development Cycle / Fast Feedback

Everything that runs in seconds and minutes — while the developer (or agent) is still in context. Fixing here is cheapest.

<a name="layer-1-compiler"></a>
### 1.1 Compiler + Types (~seconds)

**Trap:** Agent returns `string` instead of `DateTime`, changes a DTO, frontend never finds out.

**Fix:** Static typing — fastest and cheapest feedback loop.

#### Backend
- `dotnet build` — Nullable reference types, record-based DTO immutability
- **BannedApiAnalyzers (RS0030)** — forbidden APIs (`DateTime.Now`, `FindAsync`) caught at build time, not in tests
- Agent cannot return `null` unchecked
- **Strongly Typed IDs** — `BookingId` instead of `Guid`, `CustomerId` instead of `string`. The compiler catches wrong-ID substitution before tests run. See `examples/DemoProject/src/DemoProject.Domain/BookingId.cs`
- **Roslyn-first for C# guardrails** — source-level C# rules should live in analyzers when they need language meaning, types or symbols. `SAE001` catches `Guid Id` in Domain entities, `SAE002` catches `Guid somethingId` parameters right in the IDE, before `dotnet build`. See `examples/DemoProject/src/DemoProject.Analyzers/`
- **[HotPath] guardrails (SAE003/004/005)** — catches `new`, `async` state machine and boxing in `[HotPath]` methods right in the IDE. Performance degradation is prevented before commit, not by a profiler in production. See `examples/DemoProject/src/DemoProject.Analyzers/HotPathAnalyzer.cs`
- **Complexity analyzers** — `SonarAnalyzer.CSharp` (`S3776` cognitive / `S1541` cyclomatic) prevents the agent from quietly turning a method into a knot of branches. For new projects — `error` with thresholds 15/10; for legacy — baseline + ratchet. See `tests/patterns/ComplexityRatchetTest.cs`
- **Analyzer tests** — custom Roslyn analyzers have positive/negative unit tests, so a Roslyn update does not silently break guardrails. See `tests/patterns/AnalyzerTests.cs`

#### Frontend
- `tsc --noEmit` (strict mode) + `noUnusedLocals`
- Auto-generated types from OpenAPI snapshot — backend changes DTO, frontend fails to compile

**Pattern:** `tests/patterns/SnapshotTest.cs`

---

<a name="layer-1-architecture"></a>
### 1.2 Architecture + Ratchet (~10 seconds)

**Trap:** Agent adds `using Infrastructure` in Application, deletes types/tests, or uses `.FindAsync()` in read-path.

**Fix:** 25+ automated checks running in seconds.

| Category | Checks | Example |
|----------|--------|---------|
| Layers (Clean Architecture) | 10 | Domain does not depend on Infrastructure |
| Naming | 3 | Job classes end with `Job` |
| Structure and anti-patterns | 12 | Forbid `.FindAsync()` and `.Include()` in read-path; forbid duplicated business logic; forbid cyclic dependencies; entity leak guard; forbid raw Guid/string/int for identifiers in Domain |
| Complexity | 1 | Number of methods violating `S3776`/`S1541` must not grow (baseline + ratchet) |
| Allocation budget | 1 | Every `[HotPath]` method has a `{MethodName}_AllocationBudget` test; allocations stay within baseline + 10%. Green: [`examples/DemoProject/tests/DemoProject.Tests/AllocationBudgetTests.cs`](examples/DemoProject/tests/DemoProject.Tests/AllocationBudgetTests.cs). Red: [`examples/DemoProject/traps-src/DemoProject.Traps/AllocationBudgetHotspot.cs`](examples/DemoProject/traps-src/DemoProject.Traps/AllocationBudgetHotspot.cs) + [`tests/DemoProject.Traps.Tests/AllocationBudgetTests.cs`](examples/DemoProject/tests/DemoProject.Traps.Tests/AllocationBudgetTests.cs) — `new List<int>` in a hot path exceeds the budget |
| Performance | 1 | Number of public types and tests must not decrease |
| Test inventory | 1 | Number of tests must not decrease (protection against "0 tests ran") |

### Failing demo

See the live example of broken guardrails: [`examples/DemoProject/TRAPS.md`](examples/DemoProject/TRAPS.md) — 7 intentionally broken tests with `IType.Explanation` and ArchUnitNET. Run `dotnet run --project tests/DemoProject.Traps.Tests` to see what a failure looks like for each violated rule.

### Roslyn-first, regex-last

For C# source code, the default choice is a **Roslyn analyzer**, because it understands syntax trees, semantic model, types and symbols. Regex is text matching: it catches comments, dead code and formatting, but does not understand C#.

Use tools by meaning:

| Task | Preferred guardrail |
|------|---------------------|
| Forbidden API / raw primitive ID / hot-path allocation | Roslyn analyzer (Layer 1.1) |
| Dependencies between layers, slices, cycles | NetArchTest / ArchUnitNET (Layer 1.2) |
| Unique `PERF-###`, `.csproj`, `.yml`, markdown, package manifests | Regex / parser over artifacts |
| Urgent spike before the rule stabilizes | Temporary regex with TODO to promote to Roslyn |

Example of a rule that should be a Roslyn analyzer:

```csharp
// SAE006: forbid FindAsync() in read-path, but allow it in write-path via attribute/namespace.
// The analyzer checks InvocationExpression + semantic symbol, not the string ".FindAsync(".
```

Example of a rule where regex is still appropriate:

```csharp
// Check the uniqueness of Decision Guards in comments, markdown and config files.
var ids = ExtractDecisionIds(rootDir, @"(PERF|DB|AUD)-\d{3}");
```

### Whitelist with staleness check

Whitelist for exceptions (write-path) self-validates: if a file from the whitelist no longer contains the pattern — the test fails.

**Patterns:** `tests/patterns/ArchitectureRules.cs`, `tests/patterns/RatchetTest.cs`, `tests/patterns/DependencyDriftTest.cs`, `tests/patterns/EntityLeakTest.cs`, `tests/patterns/StronglyTypedIds.cs`, `tests/patterns/ArchUnitNetSliceTest.cs`, `tests/patterns/ComplexityRatchetTest.cs`, `tests/patterns/AllocationBudgetTest.cs`

---

<a name="layer-1-tests"></a>
### 1.3 Tests (TUnit + `dotnet run`) (~30 seconds)

**Trap:** CI is green because `0 tests ran`, exit code 0. Code merges unchecked for two weeks.

**Fix:**
- **TUnit** + `dotnet run --project` (no `dotnet test`)
- **BUG-regression convention:** every bug-fix = file `BUG###_DescriptiveName.cs`
- **OpenAPI snapshot test:** backend changes DTO → snapshot fails → frontend is not forgotten
- **Characterization tests:** pinning behavior of critical algorithms ("this is how it works now")

**Artifacts:** `tests/conventions/TUnit_Guide.md`, `tests/conventions/BUG_TEMPLATE.cs`

---

<a name="layer-1-code-review"></a>
### 1.4 Pre-commit code review by agent (~2 minutes)

**Trap:** Agent wrote XSS in `returnUrl`, forgot `await`, leaked internal ClientId.

**Fix:** Before commit, a **separate agent** (not the one that wrote the code) reviews the diff.

From practice: 8 review commits with findings:
- XSS in query params
- Forgotten `await` on notification send (silent failure)
- Leaked internal ClientId in API response
- Constant-time hash not used (timing attack)

**Checklist:** when reviewing a commit with `fix:` — check for `BUG*Tests.cs`.

**Artifact:** `templates/skills/code-review/`

---

<a name="layer-1-smoke"></a>
### 1.5 Smoke tests (~5 minutes)

**Trap:** Agent broke the critical path (auth, payment, booking), but all unit tests are green because each component works in isolation.

**Fix:** Automated run of 10 critical scenarios — quick check that main flows are not broken. This is not full E2E; it's speed: if smoke is on fire, we don't proceed.

---

### Synthesis: Layer 1 (development cycle)

```
          ┌─────────────────────┐
          │   Smoke tests       │  ← 1.5: 10 critical scenarios (~5 min)
          ├─────────────────────┤
          │   Pre-commit        │  ← 1.4: Second agent reads staged diff (~2 min)
          │   code review       │
          ├─────────────────────┤
          │   TUnit             │  ← 1.3: BUG-regression, snapshot (~30 sec)
          │   + dotnet run      │
          ├─────────────────────┤
          │   NetArchTest       │  ← 1.2: Layers, ratchet (~10 sec)
          ├─────────────────────┤
          │   Compiler          │  ← 1.1: dotnet build, tsc (~seconds)
          │   + Snapshot        │
          ├─────────────────────┤
          │   AGENTS.md         │  ← Layer 0: Instructions before code
          │  + Decision Guards  │
          └─────────────────────┘
```

---

<a name="layer-2"></a>
## Layer 2. Acceptance Cycle

> Anything that runs before release or on trigger — not part of the daily feedback loop, but full validation that the system holds together as a whole.

<a name="layer-2-e2e"></a>
### 2.1 E2E MCP with full scenarios (~15–30 minutes)

**Trap:** Agent does not see that shift mode shows all days as "Day Off" due to stale cache.

**Fix:** 20+ MCP tools: Telegram, VK, browser, API. The agent exercises the app itself through full user scenarios.

| Bug | How it was found |
|-----|------------------|
| Stale cache (22 days in prod!) | Agent saw empty schedule on screenshot |
| Dashboard date reset | Agent "clicked" through dashboard |
| Self-booking bypass | Agent tried to book themselves |

**Key point:** E2E found stale cache that slipped through:
- Compiler ✅ (code is syntactically correct)
- Unit tests ✅ (mock cache)
- Code review ✅ (caching diff looked correct)

**Pattern:** `docs/traps/testing.md#silent-breakdown`

---

<a name="layer-2-audits"></a>
### 2.2 Audits (~1–2 hours)

This is not a "feedback level" — it is a **repeatable audit procedure**. Without skills, the assurance review becomes chaotic manual browsing.

- `templates/skills/security-audit/`, `templates/skills/dba-audit/` etc. — narrow audit roles with `CHECKLIST.md`
- Run in batches — cross-pollination of findings between security and UX, performance and DB schema
- Each skill = an agent role you can run anytime with the same result

| Audit | Artifact |
|-------|----------|
| Security | `templates/skills/security-audit/` |
| DBA | `templates/skills/dba-audit/` |
| Performance | `templates/skills/performance-audit/` |
| i18n | `templates/skills/i18n-audit/` |
| API Design | `templates/skills/api-design-audit/` |
| Tech Debt | `templates/skills/tech-debt-audit/` |
| Test Coverage | `templates/skills/test-audit/` |
| UX Flow | `templates/skills/ux-audit/` |
| Simplicity | `templates/skills/simplicity-audit/` |
| Version | `templates/skills/version-audit/` |
| Complexity | `templates/skills/complexity-audit/` |
| Allocation Budget | `templates/skills/allocation-budget-audit/` |
| Spellcheck | `templates/skills/spellcheck-audit/` |
| Release Readiness | `templates/skills/release-readiness-audit/` |
| Mutation Testing | `templates/skills/mutation-audit/` |
| Analyzer Tests | `templates/skills/analyzer-tests-audit/` |
| Business Risk / Cross-Layer | `templates/skills/business-risk-audit/` |

**Schedule:** once per sprint or ad-hoc when you smell danger.

---

<a name="layer-2-load"></a>
### 2.3 Load (NBomber) (~minutes)

**Trap:** Agent shows P50 = 6ms, but Max = 4400ms. User hits tail latency.

**Fix:** NBomber — read + write mix, spike, concurrent booking. Runs before release or when degradation is suspected.

Scenarios: read + write mix, spike, concurrent booking.

Addendum: **allocation budget tests** for `[HotPath]` methods catch allocation regressions before the load lab — cheaper and faster than a profiler in production.

**Patterns:** `tests/patterns/LoadTest.cs`, `tests/patterns/AllocationBudgetTest.cs`

---

### Synthesis: Layer 2 (acceptance cycle)

```
          ┌─────────────────────┐
          │   Load              │  ← 2.3: NBomber, read+write mix
          │   (NBomber)         │
          ├─────────────────────┤
          │   Audits            │  ← 2.2: Batch audit roles
          │   (batch)           │
          ├─────────────────────┤
          │   E2E MCP           │  ← 2.1: Full scenarios, real hands
          │   (full scenarios)  │
          └─────────────────────┘
```

---

<a name="outer-loop"></a>
## Outer Loop / Human Judgment

> Anything that requires a business decision, product judgment, or final human validation.

This is not a pyramid layer in the classic sense — it's the last line of defense that cannot be automated.

- **Business decisions** — does the implementation match strategy, does it break contracts
- **Product decisions** — UX, behavior, edge cases that cannot be formalized
- **Final human validation** — production usage, real users, real data

Anything that got here passed Layer 1 (fast feedback) + Layer 2 (acceptance cycle).

---

## Overall synthesis: pyramid + outer loop

```
          ┌─────────────────────┐
          │   Outer loop        │  ← Human, business, product
          │   (Human judgment)  │
          ├─────────────────────┤
          │   Acceptance cycle  │  ← Layer 2: E2E, audits, load
          │   (Acceptance)      │
          ├─────────────────────┤
          │   Development cycle │  ← Layer 1: Compiler → Smoke
          │   (Fast feedback)   │
          ├─────────────────────┤
          │   AGENTS.md         │  ← Layer 0: Rules before code
          │  + Decision Guards  │
          └─────────────────────┘
```

---

## Effectiveness metrics

> **Evidence note.** Data classification below: **observed case** — a single project
> of the author (talk material), git history of fix commits over ~6 months,
> denominator ~450 commits. The numbers are a single observation, not a reproducible
> measurement or a benchmark; do not extrapolate to your project. Cost figures in the
> ROI tables are **estimates** (expert judgment), marked `~`.

### What each layer caught (from git history, observed case)

| Cycle | Layer | Bugs found | % of fixes | Examples |
|-------|-------|-----------|------------|----------|
| **Inner** (Layer 1) | Compiler + types | ~0 commits | — | Catches before commit, invisible in git |
| **Inner** (Layer 1) | Arch tests + Ratchet | ~0 commits | — | Catches before commit, invisible in git |
| **Inner** (Layer 1) | Unit/integration | 6 commits | 1.3% | Falling tests after refactoring |
| **Inner** (Layer 1) | Code review | 8 commits | 1.8% | XSS, await, data leak |
| **Inner** (Layer 1) | Smoke | ~0 commits | — | Prevents critical-path regression before merge |
| **Acceptance** (Layer 2) | E2E MCP | 9 commits | 2.0% | UI flow, stale cache, self-booking |
| **Acceptance** (Layer 2) | Audits | 19 commits | 4.2% | Security, i18n, UX, perf |
| **Acceptance** (Layer 2) | Load | ~0 commits | — | Prevents degradation before production |
| **Outer** | Human judgment | ~78 commits | 17% | Business logic, edge cases |
| — | Gray zone | ~331 commits | 74% | Unknown who found it |

### The invisible layer paradox

Compiler, arch tests, and smoke are the **most effective** layers, but in git they show 0 commits. They prevent bugs **before** code leaves the workstation.

### ROI by layer

| Layer | Setup cost | Maintenance cost | Break-even |
|-------|-----------|------------------|------------|
| Compiler + types | 0 (built-in) | 0 | Instant |
| Arch tests + Ratchet | ~2 days | ~1 hour/month | First week |
| Unit tests | ~2 weeks | ~30 min/feature | First month |
| Code review | ~0 (AGENTS.md rule) | ~2 min/commit | First XSS |
| Smoke | ~1 day | ~15 min/session | First broken critical path |
| E2E MCP | ~3 days | ~1 hour/platform | After stale cache (22 days in prod) |
| Audits | ~0 (prompts) | ~2 hours/audit | First batch |
| Load (NBomber) | ~1 day | ~30 min/scenario | First silent breakdown under load |
| Human judgment | — | ~hours-days | Impossible to measure |

---

## Grooming loop — Artifact maintenance

> The inner loop catches bugs in code. Acceptance catches systemic holes. The outer loop covers human judgment.
> But agent artifacts rot too: AGENTS.md drifts from code, Auto Memory
> accumulates duplicates, backlog turns into a graveyard. This is the third loop.

```
          ┌─────────────────────┐
          │   backlog-hygiene   │  ← Clean dead specs and priority drift
          ├─────────────────────┤
          │   doc-hygiene       │  ← AGENTS.md consistency and cross-agent docs
          ├─────────────────────┤
          │   memory-hygiene    │  ← Auto Memory: duplicates, hierarchical drift
          └─────────────────────┘
```

### Why separate

Grooming is not a pyramid layer or an audit. It does not give feedback on code
during development. It prevents **meta-information decay** that the whole
pyramid depends on.

| Artifact | What rots | Consequences |
|----------|-----------|--------------|
| **Auto Memory** | Duplicates, stale notes, drift from AGENTS.md | Agent makes decisions based on garbage |
| **AGENTS.md** | Contradictions between levels, code drift | Guardrails lie or become powerless |
| **Backlog** | Orphaned specs, stale tasks, priority drift | `task-compliance` checks diff against dead requirements |

### Grooming skills

| Skill | What it cleans | Frequency |
|-------|---------------|-----------|
| `templates/skills/memory-hygiene/` | Auto Memory: duplicates, hierarchical drift, stale refs | Once per sprint or on agent change |
| `templates/skills/doc-hygiene/` | AGENTS.md: hierarchy consistency, code drift, cross-agent docs | Once per sprint or after module refactoring |
| `templates/skills/backlog-hygiene/` | Backlog: stale, orphaned, duplicates, priority drift | Once per sprint |

### The invisible decay paradox

Guardrail decay is invisible in git, not flagged by CI, and not caught by tests.
It hits suddenly: the agent "forgets" a rule because AGENTS.md
is out of sync, or makes an architectural decision based on a stale note from
Auto Memory. Grooming is the only defense against this.

### ROI

| Skill | Cost | Break-even |
|-------|------|------------|
| memory-hygiene | ~30 min/sprint | First architectural bug from stale memory |
| doc-hygiene | ~1 hour/sprint | First time agent broke code due to outdated AGENTS.md |
| backlog-hygiene | ~30 min/sprint | First false positive `task-compliance` on a dead spec |

---

## Evolution: how the system grows

```
January:   compiler + types → unit tests → smoke
  ↓ architecture bugs
February:  + arch tests → + code review
  ↓ UI bugs
March:     + E2E MCP (acceptance) → + characterization tests
  ↓ write-path degradation
April:     + audits (in batches) → + NBomber → + ratchet tests
```

Every new layer is a reaction to a bug class that previous layers missed.

### Principle: Guardrails must be justified by risk

> A guardrail is justified by a real incident, a credible threat model, a regulatory
> requirement, or a documented high-impact failure scenario.

Every guardrail — Roslyn analyzer, architecture check, test, artifact regex or linter rule — must answer: **"What specific risk does this cover?"** "A real bug" is the strongest answer, but not the only valid one: proactive security and compliance controls are justified by a threat model or a regulatory requirement before the first incident.

Zero triggers is **not sufficient grounds** for removal. When reviewing a guardrail, weigh risk severity, likelihood, maintenance cost, false-positive rate, and the presence of compensating controls. A guardrail covering a rare high-impact risk (security, data loss) may never fire and still be justified. A removal candidate: low impact + high maintenance cost + existing compensating checks.

## 4 rules for Monday

1. **Every reproducible bug-fix = `BUG###_` test.** For configuration, documentation, operational, and process defects, another regression control is allowed (a check, a runbook, an ADR) — with an explicit explanation of why an automated test does not apply.
2. **Every PR = `dotnet run --project` tests + code review by agent + smoke.**
3. **Every sprint = acceptance cycle (E2E + audits + NBomber) before release.** An agent does not see systemic holes — a scoped audit role does.
4. **Every sprint = groom artifacts.** Memory-hygiene, doc-hygiene, backlog-hygiene — agent artifacts rot too.
