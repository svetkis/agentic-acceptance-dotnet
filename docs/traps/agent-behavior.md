# Agent Behavior Traps

> How agents fail when left unsupervised: loops, scope blindness, hallucinated scale, silent cleanups, stale training data.


## Agent Circles
### Scenario

The agent enters a fix → fix → fix (or fix → revert) loop when an optimization or refactoring touches too many subsystems. The agent sees a local problem, fixes it, but creates a new one — and so on in circles. Sometimes the only way out is to revert the entire change.

#### Examples from Practice

**1. NoTracking default — 21 hours in production**
```
perf: QueryTrackingBehavior.NoTracking globally (p99: 2727→209ms, 13x!)
  → fix: AsTracking() in 5 write-methods (patch)
  → db: remove NoTracking default, manual AsNoTracking (proper fix)
```

**2. Tailwind 3→4 — 5 fixes in 20 hours, then full revert**
```
upgrade Tailwind CSS 3→4
  → fix: outline-none, bg-opacity, ring-offset breaking
  → fix: CSS reset cascade layers
  → fix: Docker rolldown musl binding
  → REVERT: CSS reset fundamentally incompatible
```

**3. .Include() → .Select() — 11 files refactored, 46 files fixed**
```
refactor: replace Include/FirstOrDefaultAsync with Select projections (11 files)
  → fix: "Fixed all entity pass-throughs" (46 files!)
  → 4 additional perf commits with follow-ups
```

### Why the Agent Enters the Loop

1. **Doesn't see blast radius** — optimizes service A, not knowing that service B depends on A's side effect
2. **Tests give false confidence** — InMemory DB, isolated mocks, no cross-service tests
3. **Fixes symptom, not cause** — `AsTracking()` in 5 methods instead of reverting global NoTracking
4. **Visual bugs are invisible** — `tsc --noEmit` passes, but layout is broken
5. **Each fix creates an illusion of progress** — "one more commit and it's done"

### Signs of Entering the Loop

- Second fix-commit for the same problem
- Fix touches files that weren't in the original change
- Commit message contains "still", "another", "properly", "actually"

### Solution

#### When to Interrupt

| Signal | Action |
|--------|--------|
| 2nd fix-commit for the same problem | Review blast radius |
| 3rd fix-commit | Consider revert |
| Fix touches 4x more files than the original | **Definitely revert** |

#### Prevention

1. **E2E testing after perf commits** — catches most loops of this class (stale cache, layout breaks; *estimate from observed cases, not a measured rate*)
2. **Integration tests instead of mocks** — for cross-service interactions
3. **Rule: after an agent's perf commit — manual audit of write-paths**
4. **Ratchet tests** — prevent removal of critical attributes during refactoring

### Pattern

See `tests/patterns/RatchetTest.cs` and `tests/patterns/ArchitectureRules.cs`


## Context Blindness
### Scenario

The agent sees only the files it is editing. The context window is limited, and the agent:
- Does not see that the new endpoint is not covered by authorization
- Does not see that changing the DTO will break 3 other services
- Does not see that a "simple optimization" broke the Telegram integration

```csharp
// Agent added a new field to Response DTO
public record TicketDto(
    int Id,
    string Title,
    string InternalNotes  // ❌ Oops, this private field leaked into the API
);
```

### Why This Is a Systemic Problem

The agent cannot hold the entire codebase in its head. It optimizes locally, but the consequences are global.

### Solution

1. **E2E MCP** — make the agent poke the system itself. Telegram bot, API client — real scenarios
2. **Batch audits** — create narrow personas:
   - **Security** — sees data leaks that the agent missed
   - **DBA** — sees N+1 and missing indexes
   - **UX** — sees unclear errors and strange texts
3. **Scheduled runs** — audits don't wait for PR, they systematically search for holes

### Result

19 bugs found by shifting focus. The agent didn't see them because it was looking at functionality. The auditors were looking at risks.

### Pattern

See `templates/skills/security-audit/`, `templates/skills/dba-audit/`, `templates/skills/api-design-audit/`, `templates/skills/bot-audit/`


## Over-Engineering
### Scenario

The agent implements a feature but builds an architectural cathedral instead of a simple solution:

- Creates `IValidationStrategy<T>` with 3 implementations to validate 2 fields
- Introduces CQRS + Read Model + Projection for an order list with 5 columns
- Wraps `HttpClient` in 4 layers for an external API call: `Factory` → `Provider` → `Service` → `Manager`
- Creates `BaseEntity<TId>` with 8 generic constraints for a `User` with `Id`, `Name`, `Email`

```csharp
// Agent: "Need to get order by id"
// Before (simple):
var order = await db.Orders.FindAsync(id);

// After (architectural cathedral):
var query = new GetOrderByIdQuery(id);
var handler = _mediator.Send(query);
var result = await _pipelineBehavior.Handle(handler, CancellationToken.None);
var dto = _mapper.Map<OrderResponseDto>(result.Value);
```

### Consequences

- **Reading time:** a junior developer spends a day to understand how `FindAsync` works
- **Debugging time:** a validation bug hides behind 3 interfaces and 2 factories
- **Compilation time:** generic nesting slows down IntelliSense and build
- **Testing time:** to check `a + b > 0`, you need to mock 5 dependencies
- **AI degradation:** the next agent, seeing "beautiful" code, adds yet another abstraction layer

### Why Agents Over-Engineer

- **Training data:** training corpora contain more "correct" Clean Architecture examples than simple scripts
- **Pattern recognition:** the agent sees `Order` → automatically generates `IOrderRepository`, `OrderService`, `OrderManager`
- **Hallucination of scale:** the agent doesn't know the project has 10 users and introduces Event Sourcing "for growth"
- **Lack of context:** the agent doesn't see that the neighboring feature was done in 5 lines and does its own in 500

### Why Automated Tests Are Not Enough

You can count interfaces or generic nesting depth, but **complexity is semantics, not syntax**:

```csharp
// An automated test won't understand this is overkill:
public interface IBookingValidationStrategy<TRequest, TResult, TContext>
    where TRequest : class, IRequest<TResult>
    where TResult : class
    where TContext : ValidationContext<TRequest>
{
    Task<TResult> ValidateAsync(TRequest request, TContext context, CancellationToken ct);
}
```

The test will say "many generic parameters" but won't explain why they exist.

### Solution

#### 1. Simplicity Audit — Persona Auditor
An agent runs once per sprint with a simplicity checklist. See `templates/skills/simplicity-audit/SKILL.md`.

Checklist:
- [ ] Interface with one implementation — can it be replaced with a class?
- [ ] CQRS/Event Sourcing — is there a requirement for read/write separation or audit?
- [ ] Generic pipeline — how many parameters? Can it be collapsed?
- [ ] DTO nesting — how many levels? Does the client use all fields?
- [ ] async void — does any exist? Replace with Task?
- [ ] Methods with > 5 parameters — extract into a DTO?

#### 2. Code Review: "Explain to a Junior" `[ADAPT]`
Add to your `templates/skills/code-review/CHECKLIST.md`:
> If a solution cannot be explained to a junior developer in 5 minutes, it is too complex.

#### 3. AGENTS.md: "Simplicity > Pattern" Rule `[ADAPT]`
Add to your project's root `AGENTS.md`:

```markdown
### Simplicity vs Pattern
- Prefer `if/else` over `IStrategy` while branches < 3
- Prefer `db.Orders.Where(...)` over `IRepository<Order>` while there are no tests for DB replacement
- Prefer record/DTO over `IResponseMapper<TDomain, TDto>` while mapping is trivial
- Any abstraction must have **two** implementations or **one** compelling reason (testing, DI)
- async void is forbidden outside framework event handlers
- Method with > 5 parameters → extract into a parameter object
```

#### 4. Metric: Interface-to-Class Ratio
An architectural test counts the interface-to-class ratio in a layer:

```csharp
// If Application > 0.8 — alert
var interfaces = assembly.GetTypes().Count(t => t.IsInterface);
var classes = assembly.GetTypes().Count(t => t.IsClass && !t.IsAbstract);
var ratio = (double)interfaces / classes;
```

#### 5. Objective Metrics + Dead Guardrail
Automated tests catch measurable complexity **only if that pattern actually occurs in your codebase**:

- `GenericParameters_ShouldNotExceed_3` — public types with > 3 generic parameters
- `MethodNames_ShouldNotExceed_40Chars` — method names > 40 characters
- `MethodParameters_ShouldNotExceed_5` — methods with > 5 parameters
- `AsyncVoid_ShouldNotExist` — async void in production code
- `FileLength_ShouldNotExceed_300Lines` — files > 300 effective lines

> **But:** if generic > 3, inheritance > 3, or nested Func **never occur** in your project, these checks are a **dead guardrail**. It creates a false sense of security, wastes CI time, and dilutes attention.
>
> ```csharp
> // You added SimplicityGuardTest with 8 checks
> // But in your project:
> // - Generic > 3 parameters — never happened
> // - Inheritance depth > 3 — never happened
> // - Nested Func — never happened
> // 
> // Result: 27 tests, 0 failures, 0 value.
> // This is architectural dead code — exactly what the guardrail should catch.
> ```
>
> **Rule:** keep a guardrail only if it has caught at least one real bug. Otherwise — delete it.

#### 6. "No Abstraction Without Pain" Rule
The project adopts the principle: an abstraction is introduced only when there is **already** pain from its absence (tests break, code duplicates, implementation replacement is needed).

Not: "might come in handy". Only: "already hurts without it".

### Pattern

See `templates/skills/simplicity-audit/SKILL.md` and `templates/skills/simplicity-audit/CHECKLIST.md`


## Vibe Refactoring
### Scenario

The agent decides to "clean up the code":
- Deletes 3000 lines of "unused" code
- Removes attributes that "don't affect anything"
- Changes architecture because "it's better this way"

```csharp
// Agent: "[SensitiveData] is not used at runtime, I'll remove it for cleanliness"
// Before:
[SensitiveData]
public string Email { get; init; }

// After:
public string Email { get; init; }
```

### Consequences

- The PII field is no longer marked — logs start leaking
- Validation was removed during refactoring, but no one noticed
- Compliance tests fail, but the cause is unclear

### Solution

1. **NetArchTest** — regulates what is and isn't allowed. Forbidding `FindAsync` in read-path, forbidding direct Infrastructure dependencies from Api
2. **Ratchet tests** — use reflection to count public types in a layer and tests. Count decreases → test fails
3. **Code Review Agent** — a separate agent checks the diff before commit

### Pattern

See `tests/patterns/RatchetTest.cs` and `tests/patterns/ArchitectureRules.cs`


## Stale Stack
### Scenario

The agent generates code based on training data, not on the actual state of the ecosystem:

- Uses .NET 10 preview, although the team standard is stable SDK only
- Pins EF Core 8 in a .NET 9 project, although EF Core 9 is available
- Suggests `Microsoft.Extensions.Caching.Memory` 6.x instead of the current 9.x
- In the frontend part: React 17 + class components instead of functional + hooks
- Uses packages with `-preview`, `-rc`, `-beta` flags without explicit agreement

```csharp
// Agent: "Here is an example with .NET 10 Preview 3"
// global.json
{
  "sdk": {
    "version": "10.0.100-preview.3",
    "rollForward": "latestFeature"
  }
}

// PackageReference — version from training cutoff
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
```

### Consequences

- Security patches are not applied automatically (preview packages often don't receive updates)
- New platform features are unavailable (e.g., C# 13 features in .NET 9)
- Extra transitive dependencies from old packages
- Incompatibility with the rest of the team's stack
- The agent writes code with outdated APIs that are already deprecated

### Why Standard Layers Don't Catch It

| Layer | Why it doesn't catch |
|-------|----------------------|
| Compiler | Code compiles — preview SDK is valid |
| Architecture | NetArchTest doesn't check package versions |
| Tests | Unit tests check logic, not the manifest |
| Code Review | The agent-reviewer also relies on the training cutoff |
| E2E | Application works, but with deprecated dependencies |

### Solution

1. **VersionAuditTest** — test scans `global.json`, `*.csproj`, `package.json`:
   - Forbids `preview`, `rc`, `beta` in `global.json` without an explicit whitelist
   - Checks that `TargetFramework` matches the team standard
   - Scans `PackageReference` for outdated major versions

2. **SKILL.md version-audit** — periodic audit:
   - Compares `PackageReference` with current versions via `nuget.org` API or `dotnet list package --outdated`
   - Checks that frontend dependencies don't lag more than 1 major version

3. **AGENTS.md rule** — "Do not use preview versions without explicit agreement in PR"

### Pattern

See `tests/patterns/VersionAuditTest.cs` and `templates/skills/version-audit/`
