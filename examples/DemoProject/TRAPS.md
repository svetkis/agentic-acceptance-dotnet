# DemoProject.Traps — Failing Demo for Skeptical AI Engineering

> **Intentionally broken code.** Every test here fails, demonstrating a guardrail in action.
>
> **Canonical source of truth for the trap count.** Other documents must not duplicate the number — link here instead.

## Run

```bash
cd examples/DemoProject
dotnet run --project tests/DemoProject.Traps.Tests
```

## Traps

| File | Violation | Guardrail |
|------|-----------|-----------|
| `MutableState.cs` | `public int Counter;` in Domain | `BeImmutableExternally` |
| `DomainLeakingToInfra.cs` | `using System.Net.Http` in Domain | `NotHaveDependencyOnAny` |
| `PaymentService.cs` | `using Orders` from `Payments` | `NotHaveDependenciesBetweenSlices` |
| `Modules/` (Orders→Payments→Shipping→Orders) | Cyclic dependencies between modules | `ArchUnitNET.BeFreeOfCycles` |
| `RawGuidEntity.cs` | `Guid Id` instead of strongly typed ID | `NotHaveDependencyOnAny("System.Guid")` |
| `AllocationBudgetHotspot.cs` | `new List<int>` in a method with `[HotPath]` | `AllocationBudgetTests` (baseline + 10%) |
| `DemoProject.Analyzers` sources (complexity > 3) | Methods over the cyclomatic complexity threshold | `ComplexityRatchetTests` |

## Usage

1. Run the tests — you will see 7 failures (4 architecture with `IType.Explanation`, 1 with ArchUnitNET, 1 allocation budget, 1 complexity ratchet).
2. "Fix" a trap (remove the violation) — the test turns green.
3. Use it for team onboarding: "this is what a guardrail catches when an agent breaks the architecture".
