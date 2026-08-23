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
