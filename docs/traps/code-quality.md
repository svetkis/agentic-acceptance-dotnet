# Code Quality Traps

> How codebase structure degrades one small diff at a time.


## Code Duplication
### Scenario

The agent implements a new feature without noticing that similar logic already exists:

- Copies validation from one service to another
- Duplicates discount/fee calculation in an API controller
- Inserts an order status check into 3 different handlers

```csharp
// Agent: "I need to check that the order is confirmed"
// Service 1:
if (order.Status == BookingStatus.Confirmed && order.Total > 0)

// Service 2 (copied):
if (order.Status == BookingStatus.Confirmed && order.Total > 0)

// Service 3 (copied):
if (order.Status == BookingStatus.Confirmed && order.Total > 0)
```

### Consequences

- Fixing a bug in one place does not fix it in others
- The agent changes logic in one service but is unaware of the duplicates
- Compliance rules diverge (in one place `>= 100`, in another `> 100`)
- Refactoring regressions — "I fixed it" (but only in one place)

### Why the agent duplicates

- The agent looks at the diff, not the whole codebase
- Context window is insufficient to search for similar fragments
- "Vibe coding" — faster to copy than to find a shared abstraction

### Why automated tests are not enough

Regex scanning only catches **literal** duplication. Reality is worse:

```csharp
// Service 1
if (order.Status == BookingStatus.Confirmed)

// Service 2
if (order.IsConfirmed())

// Service 3
order.EnsureConfirmed();
```

This is the same business rule, but the automated test **will not see it**.

### Solution

#### 1. Automated test: Literal Duplication Guard
Regex scanning for literal copying. Catches agent copy-paste. See `tests/patterns/DuplicationGuardTest.cs`.

#### 2. Code Review: Semantic Duplication in diff
The reviewer agent checks: if validation/calculation was added in the PR — does similar logic already exist in other services? See `templates/skills/code-review/CHECKLIST.md`.

#### 3. Tech Debt Audit: Semantic Duplication across codebase
An agent (and regex) cannot recognize **semantic** duplication across the entire codebase. This is the task of the Tech Lead persona-audit, run once per sprint. See `templates/skills/tech-debt-audit/SKILL.md`.

Audit checklist:
- [ ] New validation/status calculation — does similar logic exist in other services?
- [ ] Can the rule be extracted into a Domain Service / Value Object?
- [ ] Is there an existing `BR-###` (numbered business rule) that covers this case?

#### 4. Numbered business rules `BR-###`
Like `PERF-###` / `DB-###`, but for business logic:

```csharp
// BR-001: An order is considered confirmed only at Confirmed status and non-zero total
public static class BookingRules
{
    public static bool IsConfirmed(this Booking b) =>
        b.Status == BookingStatus.Confirmed && b.Total > 0;
}
```

If an agent encounters `BR-###` in code — it must use the existing method rather than writing a new check.

#### 5. Domain Services
Move business rules into the domain; prohibit hardcoding in Application/API.

### Pattern

See `tests/patterns/DuplicationGuardTest.cs`


## Dependency Drift
> **TL;DR:** Code duplication and dependency drift are different metrics. The diff looks harmless — +1 `using`, +1 `#include`, +1 `ProjectReference` — but breaks the topology of the dependency graph.

| | Duplication | Dependency Drift |
|---|---|---|
| **What** | Same logic in multiple places | Layer A starts depending on layer B, which depends on A (or on C, which depends on A) |
| **In review** | Obvious: "we already wrote this in file X" | Non-obvious: "+1 using in 9 files — so what?" |
| **Consequences** | Bugs when logic diverges | Inability to test layers in isolation, cascading rebuilds |
| **AI agent** | Repeats logic without seeing existing code | Adds a "convenient" using without checking the architecture graph |

---

### Scenario

The agent performs a "cosmetic" refactoring or adds a feature:

```csharp
// Diff: just one line
+ using DemoProject.Infrastructure.Helpers;
```

```cpp
// Diff: just one #include
+ #include "Acts/TrackFinding/Seed.hpp"
```

In code review the diff looks benign. But that single line closes a cycle in the dependency graph.

#### Real-world case: CERN ACTS

A colleague performed a "cosmetic" refactoring in the ACTS project (CERN). Added **+1 `#include` in 70 files**. In review the diff looked like a trivial header cleanup. The result — **40 new circular dependencies**. Compiler passed, tests passed, but the architecture was broken: full rebuild time grew from 12 to 47 minutes, parallel module compilation became impossible.

```cpp
// Before: A → B → C (acyclic)
// After:  A → B → C → A (cycle)
// Cause: added #include <C.hpp> in A "for just one constant"
```

#### Types of drift

**Type 1: "Convenient using" — gradual penetration**
```
Need to log a metric
    ↓
+ using Infrastructure.Telemetry;
    ↓
After a month: 5 endpoints use Telemetry directly
    ↓
Api layer depends on Infrastructure
```

**Type 2: "DTO in wrong layer" — semantic cycle**
```
Infrastructure.Payments.YooKassaPaymentDto
    ↓
Used in Api.Endpoints
    ↓
Api depends on Infrastructure not through interface, but through concrete type
```

**Type 3: "Refactoring for cleanliness" — CERN effect**
```
"Let's extract a common header" / "Let's generalize a base class"
    ↓
+1 include/using in N files
    ↓
Each file now sees more types
    ↓
After a week: 40 circular dependencies
```

**Type 4: "Test added too late"**
```
Graph drifts for months
    ↓
Architecture test added
    ↓
Test immediately red
    ↓
"But the code works!" — yes, but the architecture is broken
```

### Consequences

- **Layers stop being independent.** Domain depends on Application, Application on Infrastructure, Infrastructure on Domain.
- **Parallel compilation breaks.** Build becomes sequential, CI time grows.
- **Tests stop isolating.** A unit test drags in half the codebase because of the cycle.
- **Entity leak — hidden cycle.** Application returns `Booking` (Entity) instead of DTO. This is not a cycle in `.csproj`, but a semantic cycle: Application pulls ORM logic, lazy loading, navigation properties — and testing the layer in isolation becomes impossible.
- **Agent amplifies drift.** Seeing that "others already do this", it adds more usings to the same layer.

### Why the agent doesn't see it

- The agent looks at the **diff**, not the dependency graph.
- Context window is insufficient to analyze the entire `#include` or `using` graph.
- The compiler **does not fail** — the code is syntactically correct. The agent thinks everything is fine.
- "Vibe coding" — faster to add a using than to extract a shared constant into a separate header/namespace.

### Why automated tests are not enough

NetArchTest checks dependencies **between assemblies**, but:
- In C++ there are no assemblies — there is an `#include` graph.
- In .NET a cycle can be **within a single assembly** between namespaces/types (Entity leak).
- `dotnet build` catches cycles between projects, but not between namespaces inside one project.

### Solution

#### 1. Automated test: Cycle Detection Guard
Parse `.csproj` (or `#include` / `using`) and search for cycles in the graph. See `tests/patterns/DependencyDriftTest.cs`.

```csharp
// Parse all .csproj → build ProjectReference graph → DFS for cycles
var cycles = FindCycles(graph);
Assert.That(cycles).IsEmpty();
```

#### 2. Architectural test: Layer Boundaries
NetArchTest catches real assembly dependencies between layers, while a Roslyn analyzer fits C# source-level rules such as forbidden `using` statements, API calls and attributes. See `tests/patterns/ArchitectureRules.cs` and `docs/solutions/roslyn-analyzers.md`.

**Rule:** add architecture tests **in the very first commit** with layered architecture. A test added after 3 months and immediately failing does not mean the test is bad. It means the graph has been drifting unnoticed.

#### 3. Code Review: Import/Using diff analysis
The reviewer agent checks: if new `using` or `#include` statements were added in the PR, do they close a cycle?

Review checklist:
- [ ] New `using` / `#include` — is there an inverse dependency in the target module?
- [ ] Can a shared constant/interface be extracted into a separate header/file?
- [ ] If the agent adds the **same using in 3+ files of the same layer** — this is a signal: the type lives in the wrong layer, an abstraction is needed.
- [ ] Did a cycle appear in the assembly graph (`.csproj`) or header graph (`#include`)?

#### 4. Tech Debt Audit: Dependency graph
Once per sprint the tech lead builds the dependency graph and compares it with `ARCHITECTURE-INVENTORY.md`. See `templates/skills/tech-debt-audit/SKILL.md`.

Audit metrics:
- Number of "illegal" `using` statements per layer (e.g. `grep -r "using .*Infrastructure" src/Api/`)
- NetArchTest score: passing / failing tests
- Entity leak count: interfaces returning Entity instead of DTO
- Lifetime of TODOs in architectural tests

#### 5. Shared Kernel / Common Contracts
Extract constants and interfaces needed by both layers into a separate module that both depend on:

```
// Before: A → B → C → A (cycle)
// After:  A → Contracts ← B
//              ↓
//              C
```

#### 6. Compile-time guard: BannedApiAnalyzers
`Microsoft.CodeAnalysis.BannedApiAnalyzers` is a Roslyn analyzer from Microsoft. It reads `BannedSymbols.txt` and catches forbidden calls during `dotnet build` (error `RS0030`). The agent sees a red squiggle in the IDE **before commit**.

**Why it's better than a regex test for specific APIs:**
- Regex searches for the string `FindAsync(` in a file — catches comments and dead code
- BannedApiAnalyzers analyzes IL calls — catches only real usage
- No need to run `dotnet test` — the guard is embedded in compilation

**Project configuration:**

```xml
<!-- Directory.Build.props -->
<PackageReference Include="Microsoft.CodeAnalysis.BannedApiAnalyzers" Version="3.3.4">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<AdditionalFiles Include="$(MSBuildThisFileDirectory)BannedSymbols.txt" />
```

```txt
## BannedSymbols.txt
P:System.DateTime.Now;Use DateTime.UtcNow or TimeProvider instead. AGENT-GUARD: timezone bug
M:Microsoft.EntityFrameworkCore.DbSet`1.FindAsync(System.Object[]);Use read-optimized queries
```

```ini
## .editorconfig — make it an error, not a warning
dotnet_diagnostic.RS0030.severity = error
```

**Where to see it:** `examples/DemoProject/BannedSymbols.txt`, `examples/DemoProject/Directory.Build.props`

**Limitation:** BannedApiAnalyzers catches only specific APIs (methods, types, properties). It **does not** catch architectural layers ("Domain must not depend on Infrastructure") or measure coupling. For layers — NetArchTest, for coupling metrics — semantic tests.

#### 7. Rule after cosmetic refactoring
If a PR contains "moved header / extracted using / generalized import" and touches **>5 files** — a `grep` for new cross-layer dependencies is mandatory.

### Pattern

- `tests/patterns/DependencyDriftTest.cs` — circular dependencies + layer drift
- `tests/patterns/EntityLeakTest.cs` — Application interfaces returning Entity instead of DTO (ratchet)

#### Takeaways for the talk

1. **"The diff looks fine, but the architecture is broken" — that's about usings.** An AI agent does not see the dependency graph. It sees a local file. +1 using is +1 potential hole in layered architecture.
2. **Measurement reveals drift.** An architecture test added late and immediately failing is not a bad test. It means the graph has been drifting for months unnoticed.
3. **The guard should be compile-time.** Tests can be skipped. `dotnet build` with BannedApiAnalyzers — cannot.
4. **Entity leak is a hidden cycle.** Application → Entity does not look like a cycle in `.csproj`. But it is a semantic cycle that breaks layer isolation.
5. **If the agent adds the same using in 3+ files of the same layer — it's a pattern.** The type lives in the wrong layer. An abstraction is needed, not copied imports.
