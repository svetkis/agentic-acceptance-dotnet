# AI-Driven Development Patterns

> Practices observed in projects where all application code is written by an agent (observed cases, not a controlled study).

---

## 1. Namespace/Type Inventory Ratchet — Architectural Inventory

**Pattern:** Use reflection to count public types in the Application layer (or a given namespace). Baseline is fixed manually.

- `PublicTypeCount_ShouldNotDecrease` — number of `public` types in the assembly >= baseline. The agent cannot silently remove a service or DTO.
- `TestCount_ShouldNotDecrease` — number of tests >= baseline. Catches "0 tests ran".

**Why non-standard:** Usually inventory is kept in people's heads. Here it is a compile-time enforced contract: you cannot remove a public type without explicitly updating the baseline.

**Generalization:** `PublicTypeCount` / `InterfaceCount` / `EndpointCount` + inventory test + coverage guard. Applicable to any layer where the agent likes to "clean up".

**Pattern:** `tests/patterns/RatchetTest.cs`

---

## 2. Roslyn-First Source Guardrails — Traps for AI

**Pattern:** Source-level C# rules are implemented as Roslyn analyzers:
- `FindAsync()` — forbidden in read-path, allowed only in explicitly marked write-path
- `.Include()` — forbidden in `*QueryService`, where `.Select()` projections are required
- `[HotPath]` — forbids `new`, array allocations, `async` state machines, and boxing

**Why non-standard:** Most teams leave these checks to tests or code review. Here the rule moves into the compiler layer: the agent sees a diagnostic in the IDE / `dotnet build`, before the test runner. Roslyn understands the C# semantic model; regex only sees strings.

**Generalization:** Roslyn analyzer for C# semantic rules. Regex remains for markdown/config/manifests and temporary spike checks.

**Pattern:** `examples/DemoProject/src/DemoProject.Analyzers/`, [architecture-tests.md §Roslyn](architecture-tests.md#11-roslyn-analyzers-as-the-default-for-c)

---

## 3. BUG-numbering as a Regression Test System

**Pattern:** Every bug is a separate file with a full test fixture. Not just one test, but coverage of **all code paths** where the bug could manifest.

```
BUG055_NewMaster_ShouldGenerateSlots.cs
BUG074_RaceCondition_ConcurrentBooking.cs
BUG090_DateTimeSerialization_ShouldHaveUtcMarker.cs
BUG098_PhoneVsNameMerge.cs (8 tests covering all paths!)
```

**AGENTS.md rule:** "every bug fix MUST include a test". This is AI-directed regression: agent fixes a bug → immediately writes a failing test → fixes code → test turns green.

**Generalization:** `BUG###_DescriptiveName` convention + full fixture per bug + multi-path coverage.

**Pattern:** `tests/conventions/BUG_TEMPLATE.cs`

---

## 4. Numbered optimization decisions in comments

**Pattern:** A conscious deviation from the standard is documented with a ticket number right in the code:

```csharp
// PERF-022: QueryFilter removed — it added JOIN to users via Workspace.Owner.DeletedAt
// in every EXISTS subquery, causing slow queries under load.
builder.HasQueryFilter(s => !s.IsDeleted); // REMOVED — see PERF-022
```

The agent, seeing that other configurations have QueryFilter, will try to "fix" this. The `PERF-022` comment stops it.

**Generalization:** Numbered optimization decisions in code comments (`PERF-###`, `DB-###`). Prevents AI "undo".

**Pattern:** `tests/patterns/ArchitectureRules.cs` (uniqueness ID test)

---

## 5. Serialization contract tests at API boundary

**Pattern:** Integration test checks that an API endpoint returns `DateTime` strings with a `Z` suffix.

```csharp
// BUG-090: test makes an HTTP request and parses JSON as a string,
// looking for format "2026-...T...Z"
```

**Why non-standard:** This is a cross-layer contract test: .NET serialization -> JSON -> JavaScript parsing. Symptom: "client sees 15:30, provider sees 12:30" (difference = UTC offset).

**Generalization:** Tests for JSON format, not business logic. Catch timezone errors that unit tests don't see.

**Pattern:** `tests/patterns/SnapshotTest.cs`

---

## 6. Shared cache contract tests

**Pattern:** Two levels of cache protection:
1. Arch test checks that every `cache.Set()` uses `SetSized()`
2. BUG test checks that two services using the same shared cache do not put different types under the same key

**Why non-standard:** The BUG test reproduces the bug with two calls in different order: "ServiceA -> ServiceB" and "ServiceB -> ServiceA". This is a test for ordering-dependent failure in shared state.

**Generalization:** Shared cache contract tests + mandatory wrapper methods enforced by source scanning.

**Pattern:** `tests/patterns/ArchitectureRules.cs`

---

## 7. Hierarchical agent instructions

**Pattern:** Every layer of the project has its own `AGENTS.md` with context-specific rules:
- Root — workflow, deployment, commits
- Domain — "zero dependencies, migration mandatory"
- Infrastructure — `Select()` is mandatory in read-path
- Api — "static endpoint classes"
- Tests — "dotnet run, not dotnet test"

**Why non-standard:** This is essentially a system prompt distributed across the file system. When the agent works in `src/Infrastructure/`, it automatically gets the rules of that layer.

**Generalization:** Hierarchical agent instructions co-located with code layers. A single `AGENTS.md` quickly bloats and loses relevance.

---

## 8. Concurrency integration test with real database

**Pattern:** Creates two `IServiceScope`s, gets two service instances, and runs `Task.WhenAll` on the same operation. Checks that both calls returned the same result (did not create duplicates).

**Why non-standard:** Race condition tests are rarely written for CRUD operations. Here it is a real integration test with PostgreSQL via Testcontainers. Two scopes = two DbContexts = real concurrency.

**Generalization:** Concurrency integration test with real database + scoped DI.

---

## 9. Attribute-driven PII redaction — compile-time + runtime

**Pattern:** Custom `[SensitiveData]` attribute on properties + three levels of enforcement:

1. **Compile-time:** Roslyn analyzer forbids `Log*($"...{email}...")` — interpolated strings in logging
2. **Inventory test:** All properties with `*Email*`, `*Phone*`, `*Password*` must have `[SensitiveData]`
3. **Runtime:** Serilog destructuring policy automatically redacts marked fields

```csharp
// Agent added DTO:
public record UserDto(
    [SensitiveData] string Email,  // ✅ Ratchet: attribute required
    [SensitiveData] string Phone,
    string Name                    // Not sensitive — no attribute
);

// Agent wants to log:
_logger.LogInformation("User {Email} logged in", user.Email);
// ❌ PiiGuardTest fails: parameter of type with [SensitiveData] passed to Log*

// Correct — via structured logging with redaction:
_logger.LogInformation("User {UserId} logged in", user.Id);
```

**Why non-standard:** Usually PII is protected by review and policies. Here it is compile-time + inventory + runtime: you cannot add Email to a DTO without `[SensitiveData]`, and you cannot pass such a type to `Log*`.

**Generalization:** `[SensitiveData]` + compile-time analyzer + inventory ratchet + runtime destructuring. Applicable to any sensitive data: credentials, tokens, health data.

**Pattern:** `tests/patterns/PiiGuardTest.cs`, `docs/traps/runtime.md#log-leak`

---

## 10. Context Markers — visual markers for active context

**Pattern:** Use emojis at the start of every agent response to signal active context:

- 🍀 = ground rules loaded
- 🔴/🌱/🌀 = current TDD phase (red/green/refactor)
- ✅ = committer role active
- 🔍 = code reviewer role active
- ❗️ = agent is flagging an error
- ♻️ = rules were just re-read
- ✨📂 = creating a new repository

**Stacking:** Markers stack: `🍀 ✅` = base rules + committer role. This allows you to see at a glance what context the agent is operating under.

**Impromptu markers:** When giving a critically important instruction mid-conversation, ask the agent to reply with an additional emoji. For example, after the instruction "all DateTime is UTC", ask to add 🕒. This makes the invisible state visible.

**Why non-standard:** Usually agent context is invisible. It is impossible to tell whether the agent read the ground rules, remembers the TDD cycle, or lost context after a long session. Markers turn invisible state into an explicit signal.

**Generalization:** Any project with multiple agent roles (committer, reviewer, TDD) or long sessions. Applicable to any AI agent that supports system prompts.

**Pattern:** `rules/AGENTS_TEMPLATE.md` §Context Markers

---

## Summary Table of Patterns

| Pattern | Essence | Where to look |
|---------|---------|---------------|
| **Namespace/Type inventory ratchet** | Count public types in layer + tests | `tests/patterns/RatchetTest.cs` |
| **Roslyn-first source guardrails** | C# diagnostics in IDE / `dotnet build`, not regex over strings | `examples/DemoProject/src/DemoProject.Analyzers/` |
| **Bug-as-fixture** | One file = one bug = all code paths | `tests/conventions/BUG_TEMPLATE.cs` |
| **Numbered optimization decisions** | `PERF-022`, `DB-013` in code comments | [`templates/skills/skeptical-ai-bootstrap/DECISION-GUARDS.md`](../../templates/skills/skeptical-ai-bootstrap/DECISION-GUARDS.md) |
| **Serialization contract tests** | Tests for JSON format, not business logic | `tests/patterns/SnapshotTest.cs` |
| **Shared state contract tests** | Test for ordering-dependent failures in shared cache | `tests/patterns/ArchitectureRules.cs` |
| **Hierarchical agent instructions** | AGENTS.md per directory, not one per project | `rules/AGENTS_TEMPLATE.md` |
| **Concurrency with real DB** | Race condition tests on Testcontainers | `tests/patterns/` |
| **PII redaction guard** | `[SensitiveData]` + compile-time + runtime redaction | `tests/patterns/PiiGuardTest.cs` |
| **Context Markers** | Visual markers for active context in agent responses | `rules/AGENTS_TEMPLATE.md` |
