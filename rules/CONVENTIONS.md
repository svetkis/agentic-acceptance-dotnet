# Project Conventions

> ⚠️ **TEMPLATE** — Adapt to your stack. Replace `[ADAPT]` with your conventions.

## Test Naming

### Regression tests (mandatory)
```csharp
// Format: BUG{number}_{ShortDescription}
// [ADAPT] — replace [Test] with your framework attribute
[Test]
public async Task BUG055_NewMaster_ShouldGenerateSlots()
{
    // Arrange: reproduce the bug
    // Act
    // Assert: bug no longer reproduces
}
```

### Ratchet tests
```csharp
// Format: {Metric}_ShouldNotDecrease
[Test]
public void PublicTypeCount_ShouldNotDecrease()
{
    // Ensure the agent did not delete public types during refactoring
}
```

### Architecture tests
```csharp
// Format: {RuleName}_Should{Satisfy}
[Test]
public void FindAsync_ShouldNotBeUsedInReadPath()
{
    // NetArchTest rule
}
```

### Complexity ratchet
```csharp
// Format: {Metric}_ShouldNotIncrease
[Test]
public void SonarComplexityViolations_ShouldNotIncrease()
{
    // Baseline + ratchet for S3776/S1541
}
```

### Allocation budget
```csharp
// Format: {HotPathMethod}_AllocationBudget
[Test]
public void GetAvailableSlots_AllocationBudget()
{
    // GC.GetAllocatedBytesForCurrentThread vs baseline + 10%
}
```

### Spellcheck / Release readiness / Mutation / Analyzer tests
```csharp
// Format: {Guard}_Should{Condition}
[Test]
public void CSpell_ShouldNotFindNewMisspellings() { }

[Test]
public void HealthEndpoint_ShouldBeHealthy() { }

[Test]
public void StrykerMutationScore_ShouldMeetBaseline() { }

[Test]
public void StrongTypedIdAnalyzer_FlagsPrimitiveIdInDomainEntity() { }
```

> Reference templates in this repository:
> - [`tests/conventions/BUG_TEMPLATE.cs`](../tests/conventions/BUG_TEMPLATE.cs) — regression test format
> - [`tests/conventions/TUnit_Guide.md`](../tests/conventions/TUnit_Guide.md) — TUnit conventions

## Post-change Workflow

```
code → dotnet build → tests ([ADAPT]: use `dotnet run --project` for TUnit; for other frameworks, document the command explicitly) → docs → commit
```

## Determinism in Tests

- Serialization inside tests/snapshots/golden masters must use a **fixed invariant
  format**: `value.ToString("HH:mm", CultureInfo.InvariantCulture)`, not
  culture-sensitive standard formats (`"t"`, `"d"`, `"g"`). A golden master recorded
  on Windows will not reproduce on a Linux CI runner otherwise.
- Random inputs: seeded `Random` only. Time: `TimeProvider` fake or fixed constants.
- See the trap live: `tests/patterns/CharacterizationTest.cs`.

## Code Review by Agent

Before commit, **always** run a separate agent for review:
- Diff compliance with specs
- Scope creep detection
- Regression risk assessment

## CI Guardrails

- `dotnet build` must fail on warning as error
- `[ADAPT]` — tests must actually run (not 0 ran)
- `[ADAPT]` — contract checks (OpenAPI snapshot / DTO snapshot / etc.)
- `[ADAPT]` — load metrics must not degrade (if applicable)
- `[ADAPT]` — complexity violations must not grow (baseline + ratchet)
- `[ADAPT]` — allocation budget tests for `[HotPath]` methods
- `[ADAPT]` — spellcheck for markdown / public API (if applicable)
- `[ADAPT]` — mutation testing before release (if applicable)
- `[ADAPT]` — analyzer tests for custom Roslyn analyzers (if applicable)

## Decision Guard IDs

- `PERF-###` — performance decisions
- `DB-###` — database/schema decisions
- `AUD-###` — audit decisions
- `COMPLEXITY-###` — complexity threshold deviations
- `SPELL-###` — intentional misspellings / non-dictionary terms
- `MUTATION-###` — mutation testing exceptions
