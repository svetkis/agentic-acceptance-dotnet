# Architecture Tests in .NET — Catching Violations Before Review

> 25+ tests in 4 files — catch violations BEFORE code review.  
> **Failing demo:** [`examples/DemoProject/TRAPS.md`](../../examples/DemoProject/TRAPS.md) — intentionally broken code with 7 failing tests demonstrating guardrails in action.

## Tools

| Approach | What it checks | Library |
|----------|----------------|---------|
| Roslyn analyzers | C# semantic rules: API calls, types, symbols, attributes | Microsoft.CodeAnalysis |
| Assembly reflection | Dependencies, naming, inheritance | NetArchTest |
| Artifact scanning | Markdown/config/manifests where no C# semantic model is needed | Regex / structured parser |
| Type counting | Number of public types and tests does not decrease | Reflection |

---

## 1. Layered Architecture (Clean Architecture)

**File:** `ArchitectureTests.cs`

| Test | Rule |
|------|------|
| `Domain_ShouldNotDependOn_Application` | Domain does not know about Application |
| `Domain_ShouldNotDependOn_Infrastructure` | Domain does not know about Infrastructure |
| `Application_ShouldNotDependOn_Infrastructure` | Application does not depend on Infrastructure |
| `Api_ShouldNotDependOn_Infrastructure` | Api depends only via DI |

**Why:** One `using MyApp.Infrastructure` in Application — and the dependency leaks. The test breaks instantly.

---

## 2. Naming Conventions

**File:** `ArchitectureTests.Naming.cs`

| Test | Rule |
|------|------|
| `Interfaces_ShouldStartWith_I` | Interfaces in Domain and Application start with `I` |
| `Jobs_ShouldEndWith_Job` | Background jobs end with `Job` |
| `Dtos_ShouldBeRecords` | DTOs are records only (immutability) |

**Why:** Uniformity = codebase search. `grep *Job` finds all jobs.

---

## 3. Structural Rules: Roslyn First

**File:** `ArchitectureTests.Structure.cs`

### EF Anti-patterns

For C# code, the preferred path is a Roslyn analyzer. It checks the syntax tree and semantic model, so it can distinguish a real call from a comment, an extension method from a similar string, and read-path from write-path by namespace/attribute/symbol.

```csharp
// SAE006 rule idea:
// InvocationExpression -> symbol.Name == "FindAsync"
// + containing namespace/type/attribute determines read-path vs write-path.
```

| Rule | Guardrail |
|------|-----------|
| Forbid `FindAsync()` in read-path | Roslyn analyzer: real method invocation, read/write context by namespace/attribute |
| Forbid `.Include()` in QueryService | Roslyn analyzer: invocation chain + query service type |
| Exceptions for write-path | Explicit attribute/namespace/decision ID instead of a text whitelist |

### Cache Safety

| Test | Rule |
|------|------|
| `CacheSet_ShouldAlwaysSpecifySize` | Every `cache.Set()` must specify size (SizeLimit on MemoryCache) |

### Decision Tracking

| Test | Rule |
|------|------|
| `PerfAndDbDecisions_ShouldHaveUniqueIds` | Optimization IDs (`PERF-###`, `DB-###`) are unique across the codebase |

---

## 4. Architectural Inventory Control

**File:** `ArchitectureTests.Ratchet.cs`

| Test | Rule |
|------|------|
| `PublicTypeCount_ShouldNotDecrease` | Number of public types in Application >= baseline. Cannot silently remove a service or DTO |
| `TestCount_ShouldNotDecrease` | Number of tests >= baseline. Catches "0 tests ran" |

**Why:** The agent likes to "clean up code" and remove "unused" services. Reflection by namespace catches this instantly.

---

## 5. Strongly Typed Identifiers

**File:** `StronglyTypedIds.cs`

| Test | Rule |
|------|------|
| `DomainEntities_ShouldNotUseRawPrimitivesForIds` | Properties `*Id` in Domain entities cannot have type `Guid`, `string`, `int`, `long`. Only types ending in `Id` (e.g. `BookingId`) are allowed |
| `StronglyTypedIdUsage_ShouldNotDecrease` | Ratchet: number of strongly typed IDs in Domain >= baseline |

**Why:** By habit the agent uses `Guid` for all identifiers. This opens the door to substituting `ClientId` into a method expecting `AgentId`. The architecture test forces creating a separate type for each entity — the compiler does the rest.

**Demo use-case:**
- **Compiler (Change Checks):** the IDE underlines `GetAgent(clientId)` in red because type `ClientId` does not convert to `AgentId`.
- **Architecture tests (Behavior Checks):** a failed pipeline with `DomainEntities_ShouldNotUseRawPrimitivesForIds` forces the developer (and agent) to create `BookingId` instead of `Guid`.

**Template:** [tests/patterns/StronglyTypedIds.cs](../../tests/patterns/StronglyTypedIds.cs)  
**Working example:** `examples/DemoProject/tests/DemoProject.Tests/StronglyTypedIds.cs`

---

## 6. Domain Type Immutability (eNhancedEdition)

**File:** `ArchitectureRules.cs`

| Test | Rule |
|------|------|
| `DomainTypes_ShouldBeImmutableExternally` | Types in Domain with public access modifier must not have mutable state (public fields/setters). Enums are excluded. |

**Why:** The agent adds `public string Status { get; set; }` to a value object "because it's easier to update". This breaks Domain invariants.

**Limitation:** In eNhancedEdition 1.4.5 `BeImmutableExternally` catches **public fields**, but auto-properties (`{ get; set; }`) may not be detected. For precise mutable property checking — use Roslyn analyzers.

**Working example:** `examples/DemoProject/tests/DemoProject.Tests/ArchitectureRules.cs` (file name)  
**NOTE:** `HaveSourceFilePathMatchingNamespace` in eNhancedEdition 1.4.5 may behave inconsistently depending on project structure. Use `HaveSourceFileNameMatchingName` when needed.

---

## 7. File and Namespace Conventions (eNhancedEdition)

**File:** `ArchitectureRules.cs`

| Test | Rule |
|------|------|
| `Types_ShouldHaveSourceFileNameMatchingName` | The `.cs` file name must match the type name (except nested types) |
| `Types_ShouldResideInMatchingFilePath` | The file path must correspond to the namespace |

**Why:** During refactoring the agent renames a class but forgets to rename the file. File search breaks, namespace drifts from folder.

**Working example:** `examples/DemoProject/tests/DemoProject.Tests/ArchitectureRules.cs`

---

## 8. Slices — Cross-Module Dependencies (eNhancedEdition)

**File:** `ArchitectureTests.Slices.cs`

| Test | Rule |
|------|------|
| `Features_ShouldNotDependOn_EachOther` | Modules (slices) must not directly depend on each other |

```csharp
var result = Types.InAssembly(typeof(Program).Assembly)
    .Slice()
    .ByNamespacePrefix("MyApp.Features")
    .Should()
    .NotHaveDependenciesBetweenSlices()
    .GetResult();
```

**Why:** In a modular monolith the agent adds `using Features.Orders` to `Features.Payments` "for a single DTO". The slice test catches this instantly.

**Working example:** see [`docs/ONBOARDING.md`](../../docs/ONBOARDING.md) §"Step 5. Slices"

---

## 9. Cyclic Dependencies Between Slices (ArchUnitNET)

**File:** `ArchUnitNetSliceTest.cs`

| Test | Rule |
|------|------|
| `Modules_ShouldBeFreeOfCycles` | Slices (modules/features) must not have cyclic dependencies |

```csharp
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

private static readonly Architecture Architecture = new ArchLoader()
    .LoadAssemblies(typeof(Program).Assembly)
    .Build();

IArchRule rule = SliceRuleDefinition.Slices()
    .Matching("MyApp.Modules.(*)..")
    .Should()
    .BeFreeOfCycles();

rule.Check(Architecture);
```

**Why:** In a modular monolith the agent adds an integration event or a call via Mediator, creating an implicit cycle `Orders → Payments → Shipping → Orders`. NetArchTest `NotHaveDependenciesBetweenSlices` forbids **any** dependencies between slices (zero-tolerance). ArchUnitNET allows a directed acyclic graph (DAG), but catches only cycles.

| Tool | Approach | When to use |
|------|----------|-------------|
| NetArchTest.eNhancedEdition | `NotHaveDependenciesBetweenSlices` — zero-tolerance | Modules must be fully isolated; any `using` into a neighbour feature is an error |
| ArchUnitNET | `BeFreeOfCycles` — DAG validation | Modules may depend on each other hierarchically, but closed cycles are forbidden |

**Working example:** `examples/DemoProject/tests/DemoProject.Traps.Tests/ArchUnitNetSliceTest.cs`

---

## 10. IType.Explanation — Failure Diagnostics (eNhancedEdition)

Unlike original NetArchTest 1.3.2, eNhancedEdition provides a **reason** for each failing type:

```csharp
private static string FormatFailingTypes(NetArchTest.Rules.TestResult result)
{
    if (result.IsSuccessful)
        return string.Empty;

    var lines = result.FailingTypes
        .Select(t => $"- {t.FullName}: {t.Explanation}")
        .ToList();

    return "Failing types:\n" + string.Join("\n", lines);
}
```

Use in Assert:
```csharp
await Assert.That(result.IsSuccessful).IsTrue()
    .Because(FormatFailingTypes(result));
```

**Why:** When a test fails with "IsSuccessful = False", it's unclear who is at fault. With `IType.Explanation` you see: `MyService depends on Infrastructure`.

**Working example:** `examples/DemoProject/tests/DemoProject.Tests/ArchitectureRules.cs`

---

## 11. Roslyn Analyzers as the Default for C#

**File:** `DemoProject.Analyzers/StronglyTypedIdAnalyzer.cs`

For source-level C# guardrails, the default choice is a Roslyn analyzer. Regex searches strings; Roslyn understands C#: syntax tree, semantic model, types, symbols, attributes, and real invocations. The difference is feedback loop time:

| Approach | Feedback time | Trigger |
|----------|--------------|---------|
| Regex scanning over `.cs` | ~10 seconds | `dotnet run --project` (Behavior Checks), temporary fallback only |
| Roslyn analyzer | ~0.5 seconds | Typing in IDE / `dotnet build` (Change Checks) |

**Example:** `SAE001` catches `public Guid Id { get; init; }` in Domain entities before compilation — the IDE shows a red squiggle. `SAE002` catches `void DoSomething(Guid orderId)` — a raw Guid in a parameter.

**When to use Roslyn:** when the rule looks at C# code and depends on language meaning: method calls, types, attributes, namespaces, inheritance, generic constraints, nullable, allocation patterns.

**When not Roslyn:** if you check assembly dependency graphs, use NetArchTest / ArchUnitNET. If you check markdown, `.csproj`, `.yml`, lock files, or Decision Guard ID uniqueness, use regex or a structured parser.

**Working example:** `examples/DemoProject/src/DemoProject.Analyzers/`

**Example 2 — Performance:** `SAE003` catches `new` in `[HotPath]` methods, `SAE004` catches `async` state machines, `SAE005` catches boxing (explicit cast struct → interface/reference). The `[HotPath]` attribute is applied consciously by the developer; the analyzer prevents forgetting about allocations.

**Working example:** `examples/DemoProject/src/DemoProject.Analyzers/HotPathAnalyzer.cs`

Project hookup:

```xml
<ProjectReference Include="..\DemoProject.Analyzers\DemoProject.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

**Minimal agent process for a new analyzer:**

1. State the bug class: which specific agent bug this catches.
2. Write 2-3 tiny code examples: should trigger / should not trigger.
3. Create a `DiagnosticDescriptor` with an `SAE###` ID.
4. Register a syntax or operation action.
5. Use `SemanticModel` when the rule depends on types or symbols.
6. Hook the analyzer with `OutputItemType="Analyzer"`.
7. Set severity: error for safety/correctness, warning for performance guidance.

> **Repository rule:** regex over `.cs` is only a temporary spike or fallback. If a C#
> rule has stabilized and should protect the team permanently, promote it to a Roslyn analyzer.


---

## Whitelist with Staleness Check

Whitelist for exceptions (write-path) is itself checked: if a file from the whitelist no longer contains the pattern — the test fails. The agent cannot "clean up" code and leave a dead entry.

```csharp
var whitelist = new[]
{
    "BookingCommandService.cs:2 calls: write-path uses FindAsync for update",
    "MasterProfileService.cs:1 call: write-path entity load"
};
```

---

## How to Add a New Test (Template)

```csharp
// Reflection: type checking
[Test]
public async Task MyRule_ShouldBeEnforced()
{
    var result = Types.InAssembly(DomainAssembly)
        .That().ResideInNamespace("MyApp.Domain")
        .Should().BeSealed()
        .GetResult();

    await Assert.That(result.IsSuccessful).IsTrue();
}

// For source-level C# anti-patterns, write a Roslyn analyzer first.
// Keep regex for markdown/config/manifests or temporary spikes.
```

---

## Pattern

See `tests/patterns/ArchitectureRules.cs`
