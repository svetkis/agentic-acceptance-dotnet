# New Skill Template

> Use this template when ready-made artifacts from `agentic-acceptance-dotnet`
> don't fit the project stack or architecture.

---

```markdown
---
name: {skill-name}
description: >
  {Brief description: what it checks, for which stack/architecture}
---

# {Name} — Skill

## Why Created

Ready skill `{original-skill}` doesn't fit because:
- {project stack differs}
- {architecture requires different checks}
- {project-specific risks}

## Role

You are a {role} in a .NET project. Your task is to {what to do}.

## Project Context

- .NET: {version}
- Application type: {Web API / Worker / Desktop / etc.}
- ORM/Data: {EF Core / Dapper / Mongo / etc.}
- Architecture: {Clean / Vertical Slice / Modular / etc.}
- Specifics: {what the agent needs to know}

## Principle

{One paragraph: what we protect and why it's important}

## Rules / Checklist

### {Category 1}
- [ ] {rule 1}
- [ ] {rule 2}

### {Category 2}
- [ ] {rule 3}
- [ ] {rule 4}

## Evidence Requirements

Every finding MUST include:
1. **Exact file path** and **line number**
2. **Code quote** (3-5 lines)
3. **Violated rule** (from the list above)
4. **Fix**: specific action or code

If you can't specify 1-4 — DON'T report the finding.

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

- **Severity** — impact and urgency: BLOCKER (stop the change) / CRITICAL (fix this iteration) / MAJOR (schedule) / MINOR (backlog).
- **Confidence** — CONFIRMED (proven by evidence) / NEEDS_REVIEW (investigation signal, needs human judgment).

Full contract: [`SKILL-CONTRACT.md`](../SKILL-CONTRACT.md).

## Report Format

```markdown
## {Audit Name} — {date}

### CRITICAL
- [ ] [CONFIRMED] {description} → {file:line}

### MAJOR
- [ ] [CONFIRMED|NEEDS_REVIEW] {description} → {file:line}

### MINOR
- {description}
```

## Launch Instructions

- **When to run:** {on every PR / once per sprint / on trigger}
- **What to look at:** {which files/changes trigger the run}
- **Consumer:** {programmer / QA / human gate}

## Integration

- **Input from:** {where we get context}
- **Output to:** {who we send results to}
- **Runs before/after:** {relation to other skills}
```

---

## Examples of Skill Creation by Project Type

### Example 1: Vertical Slice Architecture

```markdown
---
name: architecture-audit-vslice

> Note: NetArchTest DOES work with Vertical Slice — you just need
> custom rules about feature boundaries, not about layers. Regex is not the only path.
> See examples in `SKILL-ARCHITECTURE.md`.

---

# Architecture Audit — Vertical Slice

## Why Created
NetArchTest checks dependencies between layers (Domain/Application/Infrastructure),
but in Vertical Slice boundaries are by features (Features/Orders/, Features/Payments/),
not by layers. Need a scanner that checks:
- Slice A doesn't import Slice B internals
- Each Slice has a clear API (Handler/Endpoint/Validator)
- No shared database without explicit contract
```

### Example 2: Dapper + SQL Server

```markdown
name: dba-audit-dapper
---
# DBA Audit — Dapper

## Why Created
Ready `dba-audit` is tuned for EF Core (migrations, Include, AsNoTracking).
In a Dapper project we check:
- Raw SQL is parameterized (no string interpolation in SQL)
- No SELECT * (explicit column list)
- Async methods are used (QueryAsync, ExecuteAsync)
- No N+1 without explicit comment // DECISION:
```

### Example 3: .NET Framework 4.8 + WPF

```markdown
name: code-review-wpf
---
# Code Review — WPF Desktop

## Why Created
Ready `code-review` is about Minimal API and EF Core.
In WPF we check:
- ViewModel doesn't access DB directly (via Service)
- INotifyPropertyChanged implemented correctly
- No blocking call in UI thread (async/await)
- Commands use AsyncCommand, not void
```
```
