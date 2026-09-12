# Analyzer diagnostics (DemoProject.Analyzers)

Custom Roslyn analyzers live in `examples/DemoProject/src/DemoProject.Analyzers/`.
They are compile-time guardrails (Change Checks level).

| ID | Severity | Meaning | Example |
|----|----------|---------|---------|
| **SAE001** | Error | Domain entity uses a primitive ID (`long`, `Guid`, `string`, `int`) instead of a strongly typed ID | `public long Id { get; set; }` |
| **SAE002** | Error | Domain method exposes a primitive ID parameter | `void Load(Guid bookingId)` |
| **SAE003** | Warning | `[HotPath]` method contains a `new` allocation | `new byte[1024]` in hot path |
| **SAE004** | Warning | `[HotPath]` method is `async` (state-machine allocation) | `async Task<int> ProcessAsync()` in hot path |
| **SAE005** | Warning | `[HotPath]` method contains boxing | `(object)value` in hot path |
| **SAE006** | Warning | Test method has no assertion or verification | `public void Test() { var x = 1; }` |
| **SAE007** | Warning | Test method asserts only null / not-null | `Assert.That(x).IsNotNull()` as the only check |
| **SAE008** | Warning | Test assertion can be bypassed on the successful path | `if (flag) Assert.That(x).IsEqualTo(1);` with no else |
| **SAE009** | Warning | Test assertion is tautological | `Assert.That(x).IsEqualTo(x)` or `Assert.True(true)` |
| **SAE010** | Error | DbContext (or derived type) used outside the Infrastructure layer | `new AppDbContext()` in a controller |
| **SAE011** | Error | Domain entity referenced from the API layer | `public Booking Get()` in a controller |
| **SAE012** | Warning | `[Query]` method mutates state (member assignment or a SaveChanges/Add/Remove/Update call) | `booking.Status = ...` inside a `[Query]` method |

## SAE010-SAE012: Layer violations at compile time

Architectural violations used to surface only in NetArchTest tests — minutes after the
agent finished the task. BannedApiAnalyzers does not help: it is a flat blacklist and
knows nothing about layers. SAE010-SAE012 close this gap in the IDE / at `dotnet build`:

- **SAE010** — a DbContext-like type (named `DbContext` or from `Microsoft.EntityFrameworkCore`)
  created, declared, or injected outside `*.Infrastructure`.
- **SAE011** — a type from `*.Domain` referenced from `*.Api` / `*.Controllers` / `*.Endpoints`.
  The API contract must use DTOs, not domain entities.
- **SAE012** — a method marked `[Query]` (`DemoProject.Domain.QueryAttribute`) that assigns to
  a member or calls a persistence mutation (`SaveChanges`, `Add`, `Remove`, `Update`, ...).
  The read path must be side-effect free (CQRS).

Namespace-suffix matching is a deliberate simplification for the demo — adapt the layer
names to your project structure. See also
[`docs/solutions/architecture-tests.md`](../../docs/solutions/architecture-tests.md).

## SAE006-SAE009: Self-Checking Tests

SAE006-SAE009 detect **non-validating tests**: tests that are discovered,
executed, and green while proving nothing about the behavior promised by their
name. See [`docs/traps/testing.md#non-validating-tests`](../../docs/traps/testing.md#non-validating-tests)
and [`docs/SELF-CHECKING-TESTS-WORKSTREAM.md`](../../docs/SELF-CHECKING-TESTS-WORKSTREAM.md).

- A test must be **self-checking** (automatic pass/fail), have **assertion
  reachability** (no green path bypasses the assertions), and be **fault
  sensitive** (fail when the promised behavior breaks).
- SAE006-SAE009 focus on reachability: zero-assert, null-only, bypassed, and
  tautological assertions.
- Fault sensitivity is checked by deliberate fault injection or mutation
  testing (see `MutationGuardTest` / `mutation-audit` skill).

## When to suppress

Suppress only with an explicit rationale and a comment linking to a Decision
Guard or ADR. Never suppress SAE006-SAE009 because "the test is good enough" —
that is exactly the trap.
