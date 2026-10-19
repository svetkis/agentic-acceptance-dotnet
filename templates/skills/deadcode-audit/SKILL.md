---
name: deadcode-audit
description: >
  Reference-based dead code audit for .NET solutions: dead types/members (zero
  references anywhere), parasitic tests (code alive only through test projects),
  DI zombie registrations, and naming residue (V2/Old/Temp). Scanner-driven,
  evidence-first: every row is a candidate verified with rg before deletion;
  cleanup runs in waves until counts stabilize.
---

# Dead Code Audit — Skill

> **[ADAPT] Scanner and layout:** the exclusion catalog below names ASP.NET /
> EF Core / Quartz specifics and example namespace patterns — replace them with
> your project's reflection roots, source generators, and convention-dispatched
> catalogs before the first run. Paths assume a `src/` + `tests/` layout. See
> `ADAPTATION.md`.

Optional interaction convention (agent-specific): when this skill is active,
add `🧟` to your STARTER_CHARACTER stack (example: `🍀 🧟`). The skill is fully
usable without emoji markers.

## Purpose and Non-Goals

Agents leave residue: types and methods whose callers were deleted in another
commit, code kept alive only by tests written for a feature that no longer
ships, services registered in DI "just in case", `V2`/`Old` renames never
cleaned up. Your task: produce a **verified** list of dead code candidates and
drive their removal in waves — without breaking reflection, DI, or public
contracts.

The audit is a **candidate generator, not an auto-deleter**. Detection is
deliberately reference-based and syntax-only: it errs by **undercounting**
(same-name symbols mask each other), never by overcounting. Every row requires
the verification protocol below before deletion.

Non-goals:

- No semantic reachability analysis (no MSBuild workspace, no builds) — that is
  IDE tooling territory, too slow and too coupled for a scheduled audit.
- Not a duplication or abstraction-debt audit (that is `tech-debt-audit`;
  this skill links into it).
- Not an unused-NuGet/dependency audit (`version-audit`).
- Does not delete anything by itself — deletions happen in separate cleanup
  commits with evidence recorded.

## Applicability and Exclusions

- **Any C# solution**; most value in codebases where agents write or refactor
  code. Multi-project solutions get the parasitic-test and DI-zombie categories
  for free; single-project repos get A/B/F only (C/D/E need separate test
  projects / DI registrations — strike them out).
- **Requires a scanner** (see Procedure §1): a ~200-line syntax-only Roslyn
  console tool built once per repo. One-shot alternative for small repos:
  generate candidates with IDE "find usages" and run only the verification
  protocol.
- **Not applicable when:** the codebase is a prototype being actively
  rewritten (the diff is the audit), or a shipped **contract/library assembly**
  is in scope — its public API is alive by contract regardless of in-repo
  references (gate it via `public-api-surface.md`, exclude it here).

## Required Inputs

- Read access to the whole repo (source + tests + non-.cs files: markup,
  scripts, configs — they carry references the scanner cannot see).
- The exclusion catalog calibrated for this project (Procedure §1) — first run
  produces false positives; each one becomes a new exclusion.
- DI registration files locatable (`Program.cs`, `*DependencyInjection*.cs`,
  or project equivalent).
- `rg` (ripgrep) for the verification protocol.

## Procedure

### 1. Build/calibrate the scanner

A syntax-only Roslyn console tool (one `CSharpSyntaxTree.ParseText` pass per
`.cs` file, ~5 s for ~800 files, no build of the audited solution required).
If the repo has none, build it from this spec:

**Collect per file** (a `CSharpSyntaxWalker`):

- **Usages:** every `IdentifierName`/`GenericName` identifier, as a name →
  file-set index split three ways: `AppAll` (non-test files), `AppNoReg`
  (non-test files that are not DI-registration files — a file containing
  `Add|TryAdd(Singleton|Scoped|Transient|HostedService)`), `Tests` (test
  projects).
- **Type declarations** (class/struct/interface/enum/record/delegate): name,
  containing chain, accessibility, base-list type names, "generator-activated"
  flag (attribute from the project's source-generator list).
- **Member declarations** (methods/properties): name, containing type,
  accessibility, `isOverride`, "extension container" flag when the containing
  static class has `this`-parameter methods (its type name never appears at
  call sites — exempt the type, still analyze members).

**Classify:**

| Category | Rule | Confidence |
|----------|------|------------|
| **A. Dead types** | reportable type; name absent from `AppAll` **and** `Tests` (registration-file refs count as usage — E covers those) | candidate |
| **B. Dead members** | reportable member; name absent from `AppAll` and `Tests` | candidate |
| **C. Test-only types** | name absent from `AppAll`, present in `Tests` — parasitic test | candidate |
| **D. Test-only members** | ditto, member level | candidate |
| **E. DI zombies** | service type of an `Add*` registration absent from `AppNoReg`; impl-only references are the **normal** interface-DI shape — never flag on the impl | candidate |
| **E2. Framework-activated** | `AddHostedService` etc. — informational, never a finding | — |
| **F. Naming residue** | name matches `(V\d+|Old|Legacy|Temp|Backup|Deprecated)$` | hygiene |

**Exclusion catalog** (bake into the scanner; each entry = one false positive
you already met): reflection roots by base list (`IEntityTypeConfiguration<>`,
`IDesignTimeDbContextFactory<>`, EF `Migrations/`); framework-invoked members
(`IHostedService.StartAsync/StopAsync`, Quartz `IJob.Execute`,
`IHealthCheck.CheckHealthAsync`, `IEndpointFilter`/`IAsync*Filter.InvokeAsync`,
`Dispose*`/`Equals`/`GetHashCode`/`ToString`); reflection-populated properties —
config binding (`*Settings`/`*Options`) **and serializer DTOs** (JSON response
models: the deserializer "uses" every property by name, so member analysis there
is meaningless while type analysis still works); source-generator marker
attributes (project's list); convention-dispatched catalogs (namespaces where
members are resolved by name/[CallerMemberName] — localizers, bot message
catalogs); compiler-consumed projects (Roslyn analyzers); shipped contract
assemblies; `Program`.

**Attribute fallback:** `[Foo]` counts as a usage of `FooAttribute`.

**File → project attribution:** a file belongs to the *deepest* containing
`.csproj` directory. In flat layouts a repo-root csproj otherwise swallows the
sibling test/benchmark projects' files, misclassifies tests as src, and poisons
every category (observed: 49 "dead types" that were test classes).

**Catalog as config, not constants:** keep the exclusion catalog in a config
file read by the scanner — calibration must not require a recompile. When
generalizing a calibrated scanner, regression-check it: the generalized tool +
profile must reproduce the calibrated repo's counts exactly.

### 2. Scan and triage

- [ ] Run the scanner; write the report (Outputs section) with per-category
  tables and file:line locations.
- [ ] Triage order: **A/B first** (highest confidence), then C/D, then E.
  F rides along.

### 3. Verify every candidate (before any deletion)

- [ ] **rg whole-word over ALL files**, not only `.cs`:
  `rg -w "SymbolName"` — markup (`.razor`/`.xaml`/`.cshtml`), scripts, and
  configs reference symbols the scanner never sees. Operational entry points
  hide exactly there: jobs/commands referenced only from a deployment guide,
  ops script, or scheduler config are alive to operators and dead to the
  scanner. Deletable only when hits are the declaration itself plus true
  same-name collateral.
- [ ] **Reflection & dispatch:** dictionary/convention routing (bot handlers,
  mediators), `typeof(T)`/`GetService<T>()` resolution, factory lambdas. If any
  plausibly reaches the candidate — `NEEDS_REVIEW`, not deletable.
- [ ] **EF Core:** deleting the last `DbSet<T>`/`Set<T>()`/configuration
  reference drops the entity from the model — the next migration generates a
  `DropTable`. Check all three before touching a `DbSet`. Entity *properties*
  are schema, not code: zero code references does not mean a dead column —
  deleting one is a `DropColumn` migration decision.
- [ ] **Interface chains:** a dead interface member means its implementations
  are dead too — delete the interface member and every implementation in one
  commit, or the next wave just re-reports them.
- [ ] **Public contracts:** candidates in shipped contract assemblies are not
  deletable regardless of references — out of scope here.
- [ ] **Parasitic tests (C/D):** delete the src code and its tests **together**;
  a test-only member is not "coverage", it is a pin holding dead code upright.

### 4. Clean up in waves

- [ ] One wave = one cleanup commit, never mixed with feature work.
- [ ] After each wave: rerun the scan. Dead chains peel in layers — an
  interface kept alive only by its deleted implementation surfaces on the next
  run. Stop when A–D stop shrinking (not necessarily zero: calibrated
  exclusions remain).
- [ ] Optional ratchet: commit the summary-counts block; each run diffs it —
  new dead code introduced by agent work becomes a trend, not a surprise.

## Evidence Requirements

Every finding MUST include:

1. **Location:** `src/App/Services/FooService.cs:42` from the scan report.
2. **Verification evidence:** the exact `rg -w` command, its hit count, and
   where the hits are (declaration-only ⇒ deletable).
3. **Category-specific checks done:** dispatch/EF/interface-chain verdicts, not
   silence.
4. **For deletions performed:** the commit hash and the post-wave scan summary
   delta.

**NEVER report:**

- "X is unused" from the scanner alone — an unverified scanner row is a
  candidate, not a finding.
- A deletion performed without its `rg` evidence in the report.
- DI zombies based on the implementation type instead of the service type.

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
| **BLOCKER** | Deletions executed without verification evidence, or a wave commit mixed with feature work — the process that makes deletion safe was skipped |
| **CRITICAL** | Dead code that actively misleads changes: a dead `V2`/parallel implementation of a live contract that agents keep editing instead of the live one |
| **MAJOR** | Dead/test-only types, DI zombies — residue that taxes every future change and pins dead behavior with tests |
| **MINOR** | Dead members, naming residue |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Candidate passed the full verification protocol (`rg -w` output + dispatch/EF checks recorded in the report) |
| **NEEDS_REVIEW** | Scanner row not yet verified, or verification unresolved (possible convention dispatch, markup-only usage, contract assembly) |

## Outputs and Downstream Consumer

```markdown
## Dead Code Audit — {date}

| Category | Count | Change vs last run |
|---|---|---|
| A. Dead types | n | +2 |
| B. Dead members | n | -5 |
| C/D. Test-only | n | 0 |
| E. DI zombies | n | 0 |
| F. Naming residue | n | +1 |

### Verified deletable this wave (CONFIRMED)
- [ ] [CONFIRMED] `FooService` (A) — rg -w: 2 hits, both declarations → src/App/FooService.cs:10
### Needs human judgment (NEEDS_REVIEW)
- [ ] [NEEDS_REVIEW] `IBotCallback` (C) — dispatcher routes by convention; check before deleting

Wave commits: {hashes} · post-wave scan: A 0, B 3 (-12)
```

**Downstream consumer:** `tech-debt-audit` backlog (P3 cleanup items),
Programmer Agent (wave commits), `doc-hygiene` when deleted types appear in
docs/AGENTS.md.

## Trigger or Schedule

- Once per quarter or before quarterly planning (paired with
  `tech-debt-audit`, which links here for its dead-code section).
- After any feature removal, migration, or mass agent-driven refactor —
  residue concentrates there.
- Not per-PR: a whole-repo scan does not pay for itself on diffs.

## Limitations and Expected False Positives

- **Same-name masking:** usage counting is per name, solution-wide — an
  equally named member of another type keeps a candidate alive. Errors run
  towards undercounting; do not "fix" this by deleting faster.
- **Blind beyond `.cs`:** the scanner reads only C#; markup, scripts, and
  configs must be covered by the `rg -w` step (which is why it searches all
  files).
- **Not analyzed:** `protected` members, events, operators, positional-record
  properties — all undercount, never overcount.
- **First run is noisy:** every project's reflection roots and convention
  catalogs differ; expect to calibrate the exclusion catalog once. An exclusion
  without a comment naming the mechanism that activates the symbol is a bug in
  the catalog.
- **C/D nuance:** test-only does not always mean parasitic — a fixture/Helper
  misplaced in src (e.g., an `InMemory*Queue` consumed by web-app factories) is
  a move finding, not a delete finding.
- **E nuance:** service resolved only through `typeof(...)`/`GetService(Type)`
  or composition-root factories may be a false zombie — including
  composition-root *consumption* (a `NpgsqlDataSource` registered and used only
  inside the `AddDbContext` lambda) — that is why E rows stay `NEEDS_REVIEW`
  until the dispatch check.
- **F false positives:** version-ish suffixes can be domain terms — Telegram's
  `MarkdownV2` escape helpers are not a "V2 copy". Read the row; never
  regex-delete.
