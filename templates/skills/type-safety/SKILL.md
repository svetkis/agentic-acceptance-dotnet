---
name: type-safety
description: >
  Type-safety audit for .NET codebases. Finds "naked" primitives used as
  identifiers instead of Strongly Typed IDs and Value Objects, and primitives
  passed across layer boundaries.
---

# Type Safety — Skill

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


> Optional interaction convention (agent-specific): when this skill is active,
> some agents add 🏷️ to their STARTER_CHARACTER stack (e.g. `🍀 🏷️` = base
> rules + Type Safety role active; prepend `♻️` when re-reading). The skill is
> fully usable without this marker.

## Purpose and Non-Goals

You are a type-safety auditor in a .NET project. Your task is to ensure the agent does not use `Guid`, `string`, `int` as identifiers and does not pass primitives across layer boundaries.

Why this matters:
- **Compiler as guardrail:** `GetAgentData(clientId)` will not compile if `ClientId` and `AgentId` are different types.
- **Impossible to mix argument order:** `(Guid agentId, Guid clientId)` vs `(AgentId agent, ClientId client)`.
- **Type-driven refactoring:** Renaming a type safely propagates across the entire codebase.

> Persona: Type-safety auditor. Runs on PR or when reviewing domain model.
> Finds "naked" primitives instead of Value Objects and Strongly Typed IDs.

Non-goals: do not judge naming style or force migration of stable legacy code.

## Applicability and Exclusions

- **Applies to:** .NET projects with a domain model, DTOs, and JSON/EF Core serialization boundaries.
- **Exclusions:** projects with no domain layer (thin CRUD over data tables), or legacy modules under an explicit no-refactor freeze — flag those as `NEEDS_REVIEW` instead of findings.

## Required Inputs

- Access to the diff or the domain model under review.
- Serialization configuration (System.Text.Json / Newtonsoft) and EF Core model configuration, if an ORM is used.

## Procedure

### Strongly Typed IDs
- [ ] All entity IDs have their own type (`ProductId`, `CustomerId`, `OrderId`), not `Guid`/`string`/`int`
- [ ] ID types are `readonly record struct` (value semantics, no heap allocation)
- [ ] ID types contain no business logic (only factories, parsing and formatting)
- [ ] JSON serialization is configured with `JsonConverter` (System.Text.Json or Newtonsoft)
- [ ] EF Core `ValueConverter` is configured if ORM is used

### Value Objects
- [ ] Primitives that together form a concept are united in a record/class (e.g., `Money`, `Address`, `DateRange`)
- [ ] Value Objects are immutable (`init` or `readonly record struct`)
- [ ] Value Objects implement `IEquatable<T>` (record does this automatically)

### Anti-patterns
- [ ] No methods with signature `void DoSomething(Guid id1, Guid id2, string code)`
- [ ] No DTOs with field `public string Status` instead of `public OrderStatus Status`
- [ ] No passing `int count` where semantics require `Quantity` or `PageSize`

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/Domain/Order.cs:12`
2. **Code quote:** the primitive-typed signature or property
3. **Rationale:** which rule above is violated (naked ID, primitive across boundary, missing converter)
4. **Fix:** the concrete Strongly Typed ID / Value Object to introduce

**NEVER report:**
- "Use Value Objects more" without a specific primitive-typed location
- Legacy primitives you did not verify against the project's migration policy

## Finding Schema

```text
ID
Severity: BLOCKER | CRITICAL | MAJOR | MINOR
Confidence: CONFIRMED | NEEDS_REVIEW
Category / Control
Evidence: file:line, command output, trace or reproduction
Impact
Recommended action
Owner / disposition
```

## Severity and Confidence

- **BLOCKER** — primitive ID on a public API/contract boundary that breaks the type-safe model (cross-layer primitive swap).
- **CRITICAL** — naked `Guid`/`string`/`int` identifier in new domain code, or missing `JsonConverter`/`ValueConverter` for a strongly typed ID.
- **MAJOR** — primitives forming a concept not united in a Value Object in new code.
- **MINOR** — stylistic type-safety improvements; backlog.

- **CONFIRMED** — using `Guid` instead of `ProductId` in new code, missing `JsonConverter` for strongly typed id.
- **NEEDS_REVIEW** — legacy code not yet migrated; requires human judgment on refactoring priority.

## Outputs and Downstream Consumer

```markdown
## Type Safety Audit — {date}

### CRITICAL
- [ ] [CONFIRMED] {description} → {file:line}

### MAJOR
- [ ] [CONFIRMED|NEEDS_REVIEW] {description} → {file:line}

### Recommendations (MINOR)
- {description}
```

**Output to:** Programmer Agent (introduce Strongly Typed IDs / Value Objects), Code Review Agent (PR gate), Backlog Hygiene Agent (legacy migration items).

## Trigger or Schedule

Runs on PR review or when reviewing the domain model.

## Limitations and Expected False Positives

- Legacy modules deliberately not migrated will trigger naked-primitive signals — mark `NEEDS_REVIEW`, do not report as defects.
- An `int`/`string` may be legitimately primitive when no domain semantics exist (e.g., a raw count in an infrastructure detail).
- Serialization boundaries differ per stack; verify converter configuration before flagging.
