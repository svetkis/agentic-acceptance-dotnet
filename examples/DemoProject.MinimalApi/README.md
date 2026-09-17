# DemoProject.MinimalApi

> Single-project ASP.NET Core Minimal API with guardrails.
> For projects **without Clean Architecture** — shows which architecture tests remain relevant.

## Stack

- .NET 10
- ASP.NET Core Minimal API
- Single-project (no Domain / Application / Infrastructure layers)
- TUnit + `dotnet run --project`
- NetArchTest.eNhancedEdition

## What's Inside

```
src/DemoProject.MinimalApi/
├── Program.cs
├── Domain/
│   ├── Order.cs              # Record with init-only properties
│   └── Payment.cs
├── Features/
│   ├── Orders/
│   │   ├── OrderEndpoints.cs # Minimal API endpoint mapping
│   │   └── OrderService.cs   # Business logic
│   └── Payments/
│       ├── PaymentEndpoints.cs
│       └── PaymentService.cs

tests/DemoProject.MinimalApi.Tests/
├── ArchitectureRules.cs      # Naming, banned APIs (DateTime.Now), CancellationToken
├── RatchetTests.cs           # Public types and tests did not decrease
```

## How to Run

```bash
cd examples/DemoProject.MinimalApi
dotnet build
dotnet run --project tests/DemoProject.MinimalApi.Tests
```

## How It Differs from DemoProject (Clean Architecture)

| Aspect | DemoProject (Clean) | DemoProject.MinimalApi (No Layers) |
|--------|---------------------|------------------------------------|
| Projects | 4 (Domain, Application, Infrastructure, Tests) | 1 + Tests |
| Arch tests | Layer dependencies | Naming conventions, banned APIs |
| EF Core | Yes | No (in-memory list) |
| Goal | Show guardrails for CA | Show guardrails for single-project MVP |
