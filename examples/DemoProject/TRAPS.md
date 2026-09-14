# DemoProject.Traps — Failing Demo for Agentic Acceptance

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
| `DemoProject.Traps.PublicApi/` | Public helper never declared in the contract; shipped method renamed; another deleted | PublicApiAnalyzers — RS0016/RS0017 build errors |

## Usage

1. Run the tests — you will see 7 failures (4 architecture with `IType.Explanation`, 1 with ArchUnitNET, 1 allocation budget, 1 complexity ratchet).
2. "Fix" a trap (remove the violation) — the test turns green.
3. Use it for team onboarding: "this is what a guardrail catches when an agent breaks the architecture".

## Build-Time Trap (PublicApi)

The surface-leak trap is a **build-time** guardrail, so its demo is a failing *build*, not a failing test —
it lives outside `DemoProject.sln` on purpose. Run it:

```bash
dotnet build traps-src/DemoProject.Traps.PublicApi
```

Expected: build failure with `error RS0016` (public symbols that leaked out undeclared) and
`error RS0017` (declared symbols that were renamed or deleted). The green counterpart is
`src/DemoProject.Domain/` — the same wiring with a fully declared surface (`PublicAPI.Shipped.txt`).
The `traps-guardrails` CI job verifies both directions.
