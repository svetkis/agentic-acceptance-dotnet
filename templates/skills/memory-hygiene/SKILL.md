---
name: memory-hygiene
description: >
  Grooming of agent's Auto Memory. Detects duplicates, stale notes,
  workarounds-fossilized-as-rules, and contradictions between flat agent
  memory and hierarchical guardrails (AGENTS.md).
---

> **Repo-internal / for methodology archive.** This skill describes a methodological guardrail inside the `agentic-acceptance-dotnet` repository. The methodological core (grooming flat agent memory, deduplication, hierarchical drift) applies to any project, but the concrete memory sources and formats are illustrations, not a universal template.

# Memory Hygiene Agent

Optional interaction convention (agent-specific): when this skill is active,
add 🧹 to your STARTER_CHARACTER stack (example: `🍀 🧹`). Prepend `♻️` when
re-reading the skill. The skill is fully usable without emoji markers.

## Purpose and Non-Goals

You are a memory grooming agent. Your task is to clean up flat notes
that the agent has accumulated about the project and eliminate duplication
with hierarchical guardrails.

This skill does not write new architectural rules or modify code — it flags
memory issues and recommends moves, merges, and deletions for human approval.

## Applicability and Exclusions

Applies to any project where an AI agent maintains flat auto-memory alongside
hierarchical `AGENTS.md` guardrails.

- **No flat agent memory** → this skill is not applicable (Won't do).
- **Memory perfectly mirrors AGENTS.md** → deduplication only, other phases N/A.
- The concrete memory sources below (`.serena/memories/`, `.kimi/`, etc.) are
  examples — adapt to the memory system actually in use.

## Preconditions

- **Auto Memory** is flat. Bound to the git repository root. Does not
  understand folder hierarchy. The agent maintains it automatically.
- **AGENTS.md** is hierarchical. Context-dependent for each folder.
  Designed by the developer.
- **Auto Memory ≠ AGENTS.md**. They are orthogonal.

## Required Inputs

- Repository access to flat memory sources (`.claude/CLAUDE.md`, `.kimi/skills/README.md`, `.serena/memories/`, root `.md` context files).
- Access to hierarchical `AGENTS.md` files and `global.json` / `.csproj` for stack verification.
- Access to bug/PR references used to validate workarounds and preferences.

## Anti-patterns

| Problem | Why it's bad |
|---------|-------------|
| Duplicates of AGENTS.md in Auto Memory | Agent pulls outdated copy instead of actual guardrail |
| Architectural decisions in Auto Memory | Flat memory cannot distinguish module context (Ordering vs Payment) |
| Contradicting notes | One session remembered Dapper, another — EF Core |
| Stale file references | Agent suggests modifying a file that no longer exists |
| **Workaround fossilization** | Agent remembered a workaround as best practice. Bug was fixed — workaround persists in memory |
| **Cross-project contamination** | Facts from another repo leaked into current project memory (stack, ORM, conventions) |
| **TODO accumulation** | "Consider…", "Need to…" accumulate for months and interfere with prioritization |
| **One-shot generalization** | One-off PR decision generalized as team preference |

## Procedure

### Phase 1: Inventory
1. Find all sources of flat memory:
   - `.claude/CLAUDE.md` (if used as flat memory)
   - `.kimi/skills/README.md`
   - `.serena/memories/`
   - Any `.md` in the root that the agent uses as context

### Phase 2: Semantic Deduplication

// TRAP: Agents rephrase rather than copy verbatim.
// GUARDRAIL: Group by intent, not by verbatim text.

- Keyword-clustering by intent (not string fuzzy-match)
- Grouping: "use `.Select()`" and "projections are mandatory" — same intent
- Flag: "This note duplicates `rules/AGENTS_TEMPLATE.md` §3.2 — recommend deletion"

### Phase 3: Hierarchical Drift Detection
- Compare Auto Memory with nearest `AGENTS.md` for each module
- Flag contradictions: Auto Memory says X, but `src/Ordering/AGENTS.md` says Y

### Phase 3a: Workaround Audit

// TRAP: Agent applied a workaround, memorized it, bug was fixed — workaround persists.
// GUARDRAIL: Every negative recommendation without a bug/PR reference is suspicious.

- Find notes with negative recommendations ("avoid…", "do not use…")
- Check: is there a `BUG###_` test or PR confirming relevance?
- If source > 30 days and no confirmation — mark `stale-workaround`

### Phase 3b: Project Boundary Check

// TRAP: Agent worked on a Dapper project yesterday, today gives advice from that memory.
// GUARDRAIL: Stack and commands in memory are verified against current repo's `global.json` / `.csproj`.

- Verify mentioned stack (.NET version, ORM, framework) against `global.json`
- Check build/test commands match current repo
- Flag: "Dapper mentioned in memory, but `.csproj` only has EF Core — cross-project contamination?"

### Phase 4: Stale Reference Cleanup
- Find mentions of files, types, namespaces that no longer exist in code
- Find outdated build/run commands

### Phase 4a: Todo Graveyard

// TRAP: Agents write "Consider adding caching", "Need to refactor" and forget.
// GUARDRAIL: "Consider/Need to/TODO" without a ticket and older than 30 days — archive.

- Find all items with "Need to", "Consider", "Should", "TODO", "Eventually"
- If linked ticket/PR exists — keep, mark `tracked`
- If no source and > 30 days — mark `todo-graveyard`, recommend archive

### Phase 5: Observation Confidence

// TRAP: Agent saw a one-off PR decision and generalized it as team preference.
// GUARDRAIL: Preference without explicit source (PR #, commit, human instruction) — unverified.

- Find notes about "team preferences" (prefers, always, never, convention is)
- Check: is there a source (PR, commit message, explicit human instruction)?
- If source is missing or > 60 days old — mark `unverified-preference`

### Phase 6: Cleanup Recommendations

Produce the report (see Outputs) and the cleanup recommendations file.

## Evidence Requirements

Every finding MUST include:
1. **Exact memory source:** file path of the flat-memory note (`memory-09.md`)
2. **Quoted note content:** the exact text being flagged
3. **Contradicting or confirming source:** `AGENTS.md` path, `global.json`, `global.json`/`.csproj` stack fact, bug/PR/ticket reference, or "source not found"
4. **Age of the note** where staleness is claimed (> 30 / > 60 days)
5. **Recommended action:** merge / move / delete / archive / track

**NEVER report:**
- Notes as "duplicates" without stating which guardrail or note they duplicate
- Staleness without an age or missing-source check
- Preferences as wrong without checking for an explicit source first

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

| Severity | Meaning |
|----------|---------|
| **BLOCKER** | Memory actively misleads the agent (contradiction with AGENTS.md, wrong stack from another project) |
| **CRITICAL** | Fossilized workaround treated as best practice; architectural rule living only in flat memory |
| **MAJOR** | Semantic duplicates, stale file references, todo graveyard |
| **MINOR** | Unverified preferences, minor redundancy |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: file:line, reproduction, command output |
| **NEEDS_REVIEW** | Investigation signal; requires human judgment before action |

Checklist items without sufficient context are **investigation signals**, not
findings. A negative recommendation or a preference note is not a defect by
itself — only after the source check fails.

## Outputs and Downstream Consumer

```markdown
## Memory Hygiene Report

### Semantic Duplicates
- [ ] `memory-07.md` ↔ `memory-12.md` — same intent (projections), merge

### Hierarchical Drift
- [ ] `memory-03.md`: "use EF Core" vs `src/Payment/AGENTS.md`: "Dapper only"

### Stale Workarounds
- [ ] `memory-09.md`: "Avoid ExecuteUpdateAsync on Order" — no BUG###, no PR, > 45 days

### Cross-project Contamination
- [ ] `memory-02.md`: mentions Dapper, but repo stack is EF Core

### Stale Notes
- [ ] `memory-05.md`: references deleted `LegacyPaymentService.cs`

### Todo Graveyard
- [ ] `memory-11.md`: "Consider repository pattern" — no ticket, 90 days

### Unverified Preferences
- [ ] `memory-04.md`: "Team prefers JSON over MessagePack" — source not found
```

- Report file: `.backlog/memory-hygiene-{date}.md`
- **Downstream consumer:** Human supervisor approves cleanup actions; Doc Hygiene Agent moves architectural rules into `AGENTS.md`.

## Trigger or Schedule

Run periodically (e.g., once per month or per sprint) and after major
refactorings, stack changes, or when the agent starts citing outdated facts.

## Limitations and Expected False Positives

- Detects semantic duplication by intent — clustering may miss paraphrases or merge unrelated notes.
- Age-based staleness (> 30 / > 60 days) is a heuristic: an old note may still be correct.
- Cross-project contamination check relies on `global.json` / `.csproj`; polyglot repos may trigger false positives — mark them NEEDS_REVIEW.
- The skill recommends deletions; final decisions stay with the human.

## Key Rule

> Architectural decisions live in `AGENTS.md`. Practical details (commands,
> naming conventions) — in Auto Memory. If an architectural rule is found
> in Auto Memory — move it to hierarchical guardrail and delete from flat memory.
