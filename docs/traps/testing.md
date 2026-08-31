# Testing Traps

> How a green test suite can still prove nothing.


## False Safety
### Scenario

The agent updates TUnit or changes `.csproj`. As a result, `dotnet test` silently outputs:

```
Build succeeded.
Test run finished: 0 tests ran
```

Exit code: 0. CI is green. Code gets merged.

### Why This Is Dangerous

For two weeks the team thinks everything is checked. In reality:
- A new bug is not caught
- Regression goes through
- The agent broke the runner settings

### Root Causes

- TUnit + .NET 10 + MTP: `dotnet test` doesn't always correctly run TUnit
- The agent removed `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`
- The agent renamed the test project, but CI still points to the old path

### Solution

1. **`dotnet run --project`** instead of `dotnet test`
2. **Verify script** — `ci/scripts/run-and-verify-tests.sh` parses output and checks that count > 0
3. **CI guardrail** — a separate step that fails if "0 tests ran"

### Related Traps

- [non-validating-tests](testing.md#non-validating-tests) — the test-level instance of
  false safety: the test runs and is green, but its assertions cannot fail when
  the promised behavior breaks. Runner-level verification (this trap) does not
  detect it — use assertion reachability analysis and fault-injection checks.

### Pattern

See `tests/conventions/TUnit_Guide.md` and `ci/github-actions/safe-ci.yml`


## Non-Validating Tests
> **Status:** in force — wired into `rules/AGENTS_TEMPLATE.md` (Tests), Test Audit and Mutation Audit skills. Part of the [Self-Checking Tests workstream](../SELF-CHECKING-TESTS-WORKSTREAM.md) (SV-006 analyzer blockers still open).

A test can be discovered, executed, and green while proving nothing about the
behavior named by the test.

### Terminology

Three distinct properties (do not merge them):

| Term | Origin | Meaning |
|------|--------|---------|
| **Self-Checking Test** | industry-standard (xUnit Test Patterns, Meszaros) | The test determines pass/fail automatically, without manual interpretation of results |
| **Assertion Reachability** | methodology-specific | No successful execution path bypasses the assertions |
| **Fault Sensitivity** | borrowed (mutation testing) | The test fails when a relevant defect is present (mutation, or the original bug) |

Self-checking is the baseline this trap assumes. The trap itself lives in the
other two: assertions exist but are unreachable on the green path, or reachable
but insensitive to the defect.

### The trap

AI agents are good at producing tests that look structurally complete:

```text
Arrange → Act → Assert → green build
```

The presence of an assertion is not enough. A test is useful only if a relevant
defect makes it fail. Non-validating tests preserve the appearance of coverage
while allowing broken behavior through CI.

### Common forms

| Pattern | Why it stays green |
|---------|--------------------|
| Zero-assert test | Nothing is verified; execution without exception passes |
| `IsNotNull()` as the only check | Any non-null wrong result passes |
| Assertion inside a conditional branch | The branch may never execute |
| Tautological assertion (`x == x`, `expect(true)`) | Cannot fail by construction |
| Negative-only fixture without positive control | Proves absence of one defect, not presence of behavior |
| Mock-of-mock | The test verifies mock wiring, not system behavior |
| Test name promises more than assertions check | Readers trust the name; the gap is invisible |
| `waitForTimeout` instead of condition wait (frontend) | Timing races pass on fast machines, and body-only checks miss behavior |

### Why agents produce them

Observed mechanism (no mind-reading required):

- The task says "add tests" but does not state what must be verified. The
  formal completion criteria — file exists, test discovered, run green — are
  satisfiable without behavior checks.
- Review diffs show `Assert.` calls; whether the assertion is reachable and
  sensitive to the defect is not visible without control-flow analysis or
  fault injection, so reviews pass.

### Guardrails

1. **Constitution rule** (`rules/AGENTS_TEMPLATE.md`): tests must be
   self-checking with assertion reachability and fault sensitivity — a test
   must fail when the behavior promised by its name is broken. Zero-assert,
   `IsNotNull()`-only, bypassed, tautological, and negative-only tests are
   forbidden unless the weaker check *is* the contract and the reason is
   documented.
2. **Compile-time analyzers** (`examples/DemoProject/src/DemoProject.Analyzers/`):
   SAE006-SAE009 detect non-validating tests directly in the IDE / build:
   zero-assert, null-only, bypassed, tautological. See
   `tests/conventions/AnalyzerDiagnostics.md` for the full diagnostic catalog.
3. **Deliberate fault injection:** for critical behavior, break the production
   code locally and confirm the test fails. If it doesn't — the test is dead.
4. **Mutation testing** (risk-trigger / release): mutation score on critical
   assemblies measures fault sensitivity of the whole suite. See
   `templates/skills/mutation-audit/`.
5. **Test audit checklist** (`templates/skills/test-audit/`): scan for the
   forms above; each hit is an investigation signal, not an automatic defect.
6. **Review anchor:** in code review, check that assertions verify observable
   postconditions (state, output, effects) — not merely execution or object
   existence.

### Relation to other traps

- [false-safety](testing.md#false-safety) — green CI ≠ working code; non-validating
  tests are the test-level instance of that trap.
- [over-engineering](agent-behavior.md#over-engineering) — the opposite failure: test fixtures
  so complex that nobody notices they verify mocks, not behavior.


## Silent Breakdown
### Scenario

The agent optimizes read queries for performance. Adds `.AsNoTracking()` to all queries indiscriminately, without understanding the difference between read-path and write-path.

```csharp
// Agent optimized "ticket list"
var tickets = await dbContext.Tickets
    .AsNoTracking()  // ✅ OK here — pure read
    .ToListAsync();

// But then copied the same pattern into a command
var ticket = await dbContext.Tickets
    .AsNoTracking()  // ❌ HORROR! Change tracking disabled
    .FirstAsync(t => t.Id == id);

ticket.Resolve();     // Changing status
await dbContext.SaveChangesAsync();  // Silently not saving! 0 rows affected
```

### Why InMemory Tests Swallow It

The EF Core InMemory provider **does not emulate change tracking**. `SaveChanges()` always "succeeds", even with `AsNoTracking`.

### Consequences

- CI is green
- Unit tests pass
- In production, write fails for 21 hours
- A bug without an exception is the most expensive one

### Solution

1. **AGENTS.md** — explicit rule: `AsNoTracking` only in read-path with `.Select()`
2. **NBomber** — run read + write mix under load. $Max$ write latency spikes or failed requests appear
3. **Integration tests** — only on a real DB (TestContainers), no InMemory for logic

### Pattern

See `tests/patterns/LoadTest.cs`

## Examples-Only Tests

### Scenario

The agent implements phone (or any user-input) parsing and writes example-based tests:

```csharp
[Test]
public void Normalize_Works()
{
    Assert.That(Normalize("+7 905 123-45-67")).IsEqualTo("+79051234567");
}
```

Green. Merged. Then a user enters `8 (905) 123-45-67`, a bot sends ` 9051234567`,
and account linking silently breaks — the suite stays green the whole time.

### Why This Is Dangerous

- Example tests only check inputs someone thought of; humans and agents pick happy paths
- The agent generating tests from the implementation copies the cases the code already handles
- Boundary and cross-layer cases (double normalization, leading 8, junk separators) go unprobed

### Root Causes

- Tests written after the implementation (by the same agent) mirror it instead of constraining it
- No invariant was ever stated: "output is +digits for ALL inputs", "normalization is idempotent"

### Solution

Replace (or supplement) examples with **property-based tests** (`tests/patterns/PropertyBasedTest.cs`):

1. A generator produces hundreds of realistic inputs (digits mixed with separators)
2. The assertion checks an invariant that must hold for every input:
   structural (format), idempotence, round-trip, or bounds
3. Failure output contains the exact generated input — the repro is free

### Pattern

See `tests/patterns/PropertyBasedTest.cs`

### Related Traps

- [non-validating-tests](testing.md#non-validating-tests) — examples-only is the cousin:
  the test executes but constrains almost nothing
