# Decision Guards — Conscious Deviation Registry Template

> **Purpose:** Document every conscious deviation from the "standard" with a unique ID and rationale, so the agent doesn't try to "fix" it.  
> **Relationship to ADR:** a **Decision Guard is not an ADR** and not a synonym for one. It is a lightweight, code-anchored *reference* to a decision: the inline `PERF-###` comment plus a short registry entry. If your team already keeps full ADRs (context/options/decision in `docs/adr/`), a Decision Guard entry should *link to* the ADR (`Supersedes` / `ADR:` fields), not duplicate it.  
> **Consumer:** Human (Tech Lead) writes, agent reads comments in code.  
> **Result:** Registry of decisions like `PERF-###`, `DB-###`, `AUD-###` that are checked by an architecture uniqueness test.

---

## Why This Is a Separate File

`ARCHITECTURE-INVENTORY.md` is the "terrain map" (C4, assemblies, stack).  
And `DECISION-GUARDS.md` is the **compromise journal**: why there is no index here, why the global `QueryFilter` is not used, why `SELECT *` is justified in this place.

Without such a journal, the agent will see "strange" code in 3 months and roll back the optimization.

---

## Record Template

```markdown
### {PREFIX}-###: {Short Name}
**Status:** active | superseded | retired  
**Date:** YYYY-MM-DD  
**Owner:** @username  
**Review date:** YYYY-MM-DD  
**Superseded by:** {PREFIX-### or ADR link, if status = superseded}  
**ADR:** {link to full ADR, if one exists}  
**Context:** {why we came to this decision}  
**Decision:** {what exactly we did}  
**Consequences:** {what will break if rolled back}  
**Where in code:** {file:line}
```

Lifecycle rules:

- **active** — the decision is in force; the inline comment must match the code.
- **superseded** — a newer decision replaced it; keep the entry for history, fill in `Superseded by`, and remove the inline comment from code.
- **retired** — the deviation no longer exists (code removed/refactored); remove the inline comment from code.
- **Review date** — the date when the decision must be re-evaluated. An expired review date does not invalidate the decision, but is a finding for Control Maintenance.

---

## Example

```markdown
### PERF-022: Global QueryFilter on SoftDelete Not Used
**Status:** active  
**Date:** 2026-03-15  
**Owner:** @lead  
**Review date:** 2026-09-15  
**Context:** A global query filter added a JOIN to users via Tenant.Owner.DeletedAt in every EXISTS subquery. Under load — 400ms degradation.  
**Decision:** Do not call `HasQueryFilter(s => !s.IsDeleted)` for this entity. Soft delete is implemented explicitly (`Where(s => !s.IsDeleted)`) in the affected queries.  
**Consequences:** An agent seeing `HasQueryFilter` in other configs will try to "fix" this by adding a global filter. The number stops it.  
**Where in code:** `src/Infrastructure/Persistence/Configuration/EntityConfiguration.cs:31`
```

In code, a brief comment is left next to the decision. Note: the filter is **not called** — the comment replaces the removed code, it does not annotate a live call:

```csharp
// PERF-022: global QueryFilter intentionally NOT configured here — JOIN added
// 400ms under load, see docs/DECISION-GUARDS.md. Soft delete is explicit in queries.
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<Session>(b => { /* no HasQueryFilter — PERF-022 */ });
}
```

---

## Naming Rules

| Prefix | What we document | Checked by test |
|--------|------------------|-----------------|
| `PERF-###` | Optimization, deviation from standard EF | `ArchitectureRules.cs` (ID uniqueness) |
| `DB-###` | DB schema decision (data type, index) | `ArchitectureRules.cs` (ID uniqueness) |
| `AUD-###` | Audit or logging decision | `ArchitectureRules.cs` (ID uniqueness) |
| `ARCH-###` | Layer or module boundary decision | Add to test yourself |
| `SEC-###` | Security rule exception (public webhook) | Add to test yourself |

> **Uniqueness test:** `ArchitectureRules.cs` template checks that `PERF-###`, `DB-###`, `AUD-###` are unique across the codebase. Duplicate = build failure. Prefixes `ARCH-###` and `SEC-###` — extensions; add them to the regex test (`(PERF|DB|AUD|ARCH|SEC)-\d{3}`) as needed.

---

## Where to Store in Project

Save this file next to `AGENTS.md` and `ARCHITECTURE-INVENTORY.md`:

```
docs/
  AGENTS.md
  ARCHITECTURE-INVENTORY.md
  DECISION-GUARDS.md   ← here
```

If there are many decisions — they can be split by domain: `docs/decisions/PERF.md`, `docs/decisions/DB.md`.

---

> **Principle:** The comment `// PERF-022` in code stops the agent. And this file explains to a human why — and whether the decision is still in force.
