# Skeptical AI Engineering Onboarding

> Step-by-step guide for implementing guardrails in an existing .NET project.  
> **Audience:** Tech Lead, CTO, Lead Developer.  
> **Format:** do it yourself or delegate to an agent using this document.
>
> **Control model:** Engineering Assurance Levels — see [README.md](../README.md#how-it-works).
> The steps below use legacy layer names (0, 1.1–2.3) from `PYRAMID.md` as
> references to concrete steps; mapping: Layer 0 → Control Foundation,
> 1.1/1.4 → Change Checks, 1.2/1.3 → Behavior Checks, 1.5/2.1/2.3 → System Checks,
> 2.2 → Periodic Assurance, outer loop → Engineering Governance,
> artifact grooming → Control Maintenance.

---

## How Long It Takes

| Mode | Time | What we implement | When to choose |
|------|------|-------------------|----------------|
| **Fast** | 1–2 days | Layer 0 (AGENTS.md) + Layer 1.1 (compiler) + Layer 1.2 (basic arch tests) | Pilot. Want to quickly check if the methodology works. |
| **Standard** | 2–4 weeks, incrementally | Layer 0 → Change Checks → Behavior Checks; then one sub-layer/audit per sprint | Main scenario. Most projects start here. |
| **High-assurance** | 1–2 months, incrementally | All levels + Engineering Governance + Control Maintenance | High-risk project (fintech, health, high-load). |

> **Do not try to implement everything in one day.** Guardrails work only if the team understands and supports them. Timelines above are calendar estimates; adoption is always incremental: one control at a time, verified working before the next.

---

## Prerequisites

- [ ] Access to `.sln` and all `.csproj`
- [ ] Understanding of current architecture (Clean / VSlice / Modular / Big Ball of Mud)
- [ ] Permission to change CI/CD and add files to the repository root
- [ ] Decision on which AI agent is used (Kimi / Claude / Cursor / Codex / multiple)
- [ ] 30 minutes to fill out [`ARCHITECTURE-INVENTORY.md`](../templates/skills/skeptical-ai-bootstrap/ARCHITECTURE-INVENTORY.md)

---

## Step-by-Step Plan

### Step 0. Capture Current Architecture

**Goal:** The agent (and you) must understand what they are working with, not guess.

1. Fill out [`ARCHITECTURE-INVENTORY.md`](../templates/skills/skeptical-ai-bootstrap/ARCHITECTURE-INVENTORY.md):
   - Draw a C4 Container diagram (4–6 blocks)
   - Fill in the Assembly Boundaries table
   - Identify 3–5 Critical Paths
   - Fill in the Technology Inventory
2. Save the file to `docs/ARCHITECTURE-INVENTORY.md` in your project.
3. If there are intentional deviations from standards — record them as `PERF-###` / `DB-###` / `ARCH-###` using the [`DECISION-GUARDS.md`](../templates/skills/skeptical-ai-bootstrap/DECISION-GUARDS.md) template.

**Output:** ground truth for all subsequent guardrails.

---

### Step 1. Assess Maturity

**Goal:** Understand what already exists and what needs to be built from scratch.

**Option A — via agent (recommended):**
1. Install the `skeptical-ai-bootstrap` skill into your project ([`INSTALL.md`](../templates/skills/skeptical-ai-bootstrap/INSTALL.md))
2. Run: `kimi run skeptical-ai-bootstrap` (or equivalent for your agent)
3. Get the report `.backlog/onboarding-{date}.md`

**Option B — manual assessment:**
1. Open [`PYRAMID.md`](../PYRAMID.md)
2. For each sub-layer (1.1→2.3) answer:
   - Is the principle followed? (Yes / Partially / No)
   - What is implemented now?
   - What needs to be added?
3. Record in `.backlog/onboarding-manual.md`

**Output:** backlog of tasks with Must / Should / Could priorities.

---

### Step 2. Adapt Artifacts to Your Stack

**Goal:** Strike out what does not apply BEFORE the first run.

1. Open [`templates/skills/ADAPTATION.md`](../templates/skills/ADAPTATION.md)
2. Find your stack in the table "If the project has… → skip…"
3. For each skill you plan to use:
   - Open `CHECKLIST.md`
   - Mark items `[-]` (N/A) or `[ ]` (we will check)
4. If >50% of a skill does not apply — do not adapt it, create a new one ([`NEW-SKILL-TEMPLATE.md`](../templates/skills/skeptical-ai-bootstrap/NEW-SKILL-TEMPLATE.md))

**Output:** adapted checklists that do not generate false positives.

---

### Step 3. Write the Constitution (Layer 0)

**Goal:** The agent reads the rules BEFORE code.

1. Copy [`rules/AGENTS_TEMPLATE.md`](../rules/AGENTS_TEMPLATE.md) to your project root
2. Edit for your stack:
   - Remove inapplicable rules (e.g., `[Authorize]` for Minimal API)
   - Add specific ones (e.g., "In our project `FindAsync()` is allowed only in `*CommandService.cs`")
   - Fix naming conventions
3. If the project is large — add `AGENTS.md` to subfolders (deeper instructions prevail)
4. Add `rules/CONVENTIONS.md` — test naming, workflow, CI guardrails

**Output:** `AGENTS.md` + `CONVENTIONS.md` in the project root that the agent reads.

---

### Step 4. Implement Layer 1.1. Compiler

**Goal:** The fastest feedback loop — the build fails.

1. In `Directory.Build.props` (or `.csproj` if single-project):
   ```xml
   <Nullable>enable</Nullable>
   <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
   ```
2. Add `.editorconfig` with severity=error for critical rules
3. For frontend (if any): `tsc --noEmit` in strict mode + generate types from OpenAPI
4. Check: does `dotnet build` fail on warnings?

**Readiness criterion:** `dotnet build` without warnings = green CI.

---

### Step 5. Implement Layer 1.2. Architecture

**Goal:** Automatic verification of layers and anti-patterns.

1. Install `NetArchTest.eNhancedEdition` in the test project (fork with fixed bugs and new features: Slices, Immutable rules, file path checks)
2. Copy [`tests/patterns/ArchitectureRules.cs`](../tests/patterns/ArchitectureRules.cs)
3. Adapt namespaces and assembly names to your project (use the table from Step 0)
4. Add `RatchetTest.cs` — baseline of public types and tests
5. **Modular Monolith / Vertical Slice:** use `Slice().ByNamespacePrefix(...).Should().NotHaveDependenciesBetweenSlices()` to check inter-module dependencies
6. If a rule looks at C# sources — prefer a Roslyn analyzer (see [`roslyn-analyzers.md`](solutions/roslyn-analyzers.md)); leave regex for config/markdown/manifests or temporary spikes
7. Run: `dotnet run --project tests/YourProject.Tests` — do tests pass?
8. See a live failing demo: [`examples/DemoProject.Traps/`](../examples/DemoProject.Traps/) — 7 intentionally broken guardrails with `IType.Explanation` and ArchUnitNET

**Readiness criterion:** New `using Infrastructure` in Application = red CI.

---

### Step 6. Implement Layer 1.3. Tests

**Goal:** Every change is covered by tests, and tests actually run.

1. **"0 tests ran" check:** copy [`ci/scripts/verify-tests.sh`](../ci/scripts/verify-tests.sh) into CI
2. **BUG-regression convention:** for every bug fix create `BUG###_DescriptiveName.cs` ([`BUG_TEMPLATE.cs`](../tests/conventions/BUG_TEMPLATE.cs))
3. **Snapshot test:** if there is an API — add an OpenAPI snapshot test ([`SnapshotTest.cs`](../tests/patterns/SnapshotTest.cs))
4. **Characterization tests:** capture behavior of critical algorithms (see [`ai-patterns.md`](solutions/ai-patterns.md))
5. If using TUnit — read [`TUnit_Guide.md`](../tests/conventions/TUnit_Guide.md)

**Readiness criterion:**
- CI fails if `0 tests ran`
- Every `fix:` commit has a `BUG*Tests.cs`
- Backend changed DTO → snapshot test fails

---

### Step 7. Implement Layer 1.4. Code Review by Agent

**Goal:** A second agent checks the first agent's code.

1. Copy [`templates/skills/code-review/`](../templates/skills/code-review/) to `.kimi/skills/code-review/` (or your agent's format)
2. Adapt `SKILL.md` to your stack (see Step 2)
3. Set up execution on every PR / before commit:
   - Kimi: `kimi run code-review --git-diff HEAD~1`
   - Claude: `/{command}` in chat
4. Check on 3–5 recent PRs — does the agent find real issues?

**Readiness criterion:** Code review agent catches at least 1 issue out of 5 PRs.

---

### Step 8. Implement Layer 1.5. Smoke Tests

**Goal:** Fast check that critical paths are not broken.

1. Identify 10 critical scenarios (see Critical Paths from Step 0)
2. Write automated smoke tests — at least 1 per path
3. Run smoke before every merge or in CI on every PR

**Readiness criterion:** Smoke fails if a critical path is broken (auth, payment, booking).

---

### Step 9. Implement Layer 2.1. E2E / MCP

**Goal:** The agent "touches" the application with real hands.

1. Identify available MCP tools (browser, Telegram, API, DB)
2. Write 3–5 E2E scenarios for critical paths (see Critical Paths from Step 0)
3. If there is a frontend — add visual checks (screenshots)
4. Run in CI nightly or before release

**Readiness criterion:** E2E finds a problem that unit tests do not catch (e.g., stale cache).

---

### Step 10. Implement Layer 2.2. Audits

**Goal:** Deep checks on schedule.

| Audit | Frequency | When to start |
|-------|-----------|---------------|
| Security audit | Once per sprint | After implementing Layer 1.3 |
| DBA audit | Once per sprint / on migrations | If using EF Core / Dapper |
| Performance audit | Before release | After architecture stabilizes |
| Complexity audit | Once per sprint | When methods start growing |
| Allocation budget audit | Before release / when hot paths change | If there are latency-sensitive paths |
| Spellcheck audit | Once per sprint | If there are public APIs / documentation |
| Release readiness audit | Before release / beta launch | Before going to production |
| Mutation audit | Once per sprint | If Stryker is compatible with test framework |
| Analyzer tests audit | When creating / updating Roslyn analyzers | If there are custom analyzers |
| Tech debt audit | Once per sprint | Immediately — catches duplication and dead code |
| Test audit | After 3–5 features | When new features appear without tests |

**How to implement:**
1. Copy `templates/skills/{audit}/` to `.kimi/skills/{audit}/`
2. Adapt `CHECKLIST.md` (Step 2)
3. Schedule in the team calendar (recurring meeting or CI-trigger)
4. For manual audit — use [`human-audit-bridge.md`](solutions/human-audit-bridge.md)

**Readiness criterion:** Every audit has been run at least once, results recorded.

---

### Step 11. Implement Layer 2.3. Load (NBomber)

**Goal:** Do not let the agent break production with load.

1. Install `NBomber` in the test project
2. Copy [`tests/patterns/LoadTest.cs`](../tests/patterns/LoadTest.cs)
3. Write a scenario: read + write mix for a critical path
4. Run before release or when degradation is suspected

**Readiness criterion:** NBomber shows tail latency (Max, P95), not just average.

---

### Step 12. Set Up the AI Agent

**Goal:** The agent knows how to work with your project.

1. Choose your agent:
   - **Kimi Code CLI** → [`docs/agents/KIMI.md`](agents/KIMI.md)
   - **Claude Code** → [`docs/agents/CLAUDE-CODE.md`](agents/CLAUDE-CODE.md)
   - **Cursor** → [`docs/agents/CURSOR.md`](agents/CURSOR.md)
   - **Codex** → [`docs/agents/CODEX.md`](agents/CODEX.md)
   - **OpenCode** → [`docs/agents/OPENCODE.md`](agents/OPENCODE.md)
2. Copy the configuration into your project
3. Make sure the agent sees `AGENTS.md` and skills

**Readiness criterion:** The agent generates code that passes architecture tests on the first try.

---

## Implementation Anti-Patterns (What NOT to Do)

| Anti-pattern | Why harmful | What to do instead |
|--------------|-------------|--------------------|
| **Big Bang** — implement all layers in one sprint | The team does not absorb, guardrails break and get disabled | One sub-layer per sprint, starting with 1.1 |
| **Copy-paste without adaptation** — copy all skills 1-to-1 | False positives overwhelm the team, checklists are ignored | Strike N/A before first run |
| **Agent only, no human review** | Agents hallucinate, miss context | Human audit once per sprint ([`human-audit-bridge.md`](solutions/human-audit-bridge.md)) |
| **AGENTS.md from another project** | Rules about another stack mislead the team | Write your own using [`rules/AGENTS_TEMPLATE.md`](../rules/AGENTS_TEMPLATE.md) as a template |
| **Architecture tests without inventory** | NetArchTest configured for non-existent assemblies | Step 0 first — fix boundaries |
| **"We are not ready for guardrails"** | Guardrails are needed exactly when an agent writes code | Start with Fast mode (1–2 days) |
| **Cloning DemoProject** | Agent creates `examples/DemoProject/` in the target repo, copying structure from `dotnet-ai-guardrails` | `examples/` is a demonstration of the methodology, not a template to copy. Do not create demo projects in the working repo. |

---

## Readiness Checklist: Is the Project Ready?

Go through this list after implementation. If everything is checked — guardrails work.

### Layer 0 + Layer 1.1–1.2 (Must have)
- [ ] `AGENTS.md` in the project root, team knows it exists
- [ ] `dotnet build` fails on warnings
- [ ] Architecture tests pass (NetArchTest or equivalent)
- [ ] `verify-tests.sh` checks that tests actually ran

### Layer 1.3–1.5 + Layer 2.1 (Should have)
- [ ] Regression tests cover all reproducible bug fixes (capability: no closed bug without a `BUG###_` test or a justified alternative)
- [ ] OpenAPI snapshot test (if there is an API) or equivalent contract test
- [ ] Agent code review is part of the PR process (built into every PR's workflow, not a one-off exercise)
- [ ] Smoke tests pass on every PR
- [ ] E2E ran at least once and found or confirmed a critical path

### Outer Loop (Could have)
- [ ] Security audit performed, findings recorded in backlog
- [ ] DBA audit performed, execution plans of new queries checked
- [ ] Tech debt audit performed, semantic duplication recorded

### Ecosystem
- [ ] Agent is configured and sees `AGENTS.md`
- [ ] Skills live in `.kimi/skills/` (or equivalent folder for another agent)
- [ ] CI runs architecture tests + verify-tests on every PR

---

## FAQ

**Q: We use .NET Framework 4.8. Is SAE applicable?**  
A: Yes, but adapt. Enable nullable per file (`#nullable enable`), replace NetArchTest with Roslyn analyzers, E2E — with integration tests via `HttpClient`.

**Q: We have a single-project MVP, no Clean Architecture. What should architecture tests check?**  
A: Not layers, but conventions: naming, forbidden API calls, ratchet on public types. See [`ADAPTATION.md`](../templates/skills/ADAPTATION.md).

**Q: The team resists — "this slows down development".**  
A: Start with Fast mode (1–2 days). Show how `AGENTS.md` prevents the agent from rewriting code. Guardrails save time, not waste it.

**Q: Can we implement without an AI agent (just for the team)?**  
A: Yes, but a significant part of the value (estimate) is protection FROM agents. Without an agent it is just good engineering practices.

**Q: How much does maintenance cost?**  
A: Layers 0 + 1.1–1.2 are "set and forget" (minimal maintenance). Audits — 1–2 hours per sprint. E2E — setup 1 day, then self-running.

---

## Onboarding Navigation

| Stuck at step | Where to go |
|---------------|-------------|
| I don't understand our architecture | [`ARCHITECTURE-INVENTORY.md`](../templates/skills/skeptical-ai-bootstrap/ARCHITECTURE-INVENTORY.md) |
| I don't know which skills to choose | [`ADAPTATION.md`](../templates/skills/ADAPTATION.md) |
| I don't know how to configure the agent | [`docs/agents/`](agents/) → choose yours |
| I don't understand how a layer works | [`PYRAMID.md`](../PYRAMID.md) |
| I want to perform an audit manually | [`human-audit-bridge.md`](solutions/human-audit-bridge.md) |
| The agent does not find skills | [`INSTALL.md`](../templates/skills/skeptical-ai-bootstrap/INSTALL.md) |
| Ready-made artifacts do not fit | [`NEW-SKILL-TEMPLATE.md`](../templates/skills/skeptical-ai-bootstrap/NEW-SKILL-TEMPLATE.md) + [`SKILL-ARCHITECTURE.md`](../templates/skills/skeptical-ai-bootstrap/SKILL-ARCHITECTURE.md) |

---

> **Principle:** SAE is not about perfection. It is about preventing the agent from breaking code faster than the team can fix it. Start small, add layers as the project grows.
