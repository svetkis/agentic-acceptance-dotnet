---
name: ux-audit
description: >
  UX audit of client scenarios. Finds dead ends, empty states without CTA,
  generic errors, UI race conditions, and lack of feedback. Runs when
  redesigning UI or before public beta.
---

# UX Audit — Skill

> Optional interaction convention (agent-specific): when this skill is active,
> some agents add 🎯 to their STARTER_CHARACTER stack (e.g. `🍀 🎯` = base
> rules + UX Audit role active; prepend `♻️` when re-reading). The skill is
> fully usable without this marker.

> **Repo-internal / for methodology archive.** This skill describes a methodological guardrail inside the `agentic-acceptance-dotnet` repository. The methodological core (scenario analysis, states and feedback, UI race conditions, cross-layer invariants) applies to any project, but the examples are illustrations, not a universal template.

## Purpose and Non-Goals

You are a UX auditor. Your task is to find places where the user gets stuck,
does not understand what is happening, or cannot complete an action.
Do not hunt bugs — hunt friction in user experience.

> Persona: UX auditor. Runs when redesigning UI or before public beta.
> Finds friction points: dead ends, empty states, generic errors, lack of feedback.

## Applicability and Exclusions

- **No frontend (API only)** → focus on API error responses and client edge cases.
- **Telegram bot** → check message flow, buttons, callback handling. See also `templates/skills/bot-audit/`.
- **Web/Mobile** → check screen states, CTAs, loading indicators, validation feedback.

## Required Inputs

- Access to the relevant handlers, UI components, and API contracts.
- List of key user scenarios (or permission to derive them from the codebase).
- Ability to trace the API → frontend contract for each scenario.

## Procedure

### Scenario Analysis
For each key scenario, walk through from start to finish:

**Scenario 1: New user**
- [ ] Path: sign-in / onboarding → first action
- [ ] What if interrupted at each step?
- [ ] Field validation — is there a specific error (not generic)?
- [ ] Empty states — is there a CTA (what to do next)?

**Scenario 2: Core action (reservation, purchase, creation)**
- [ ] Path: select → confirm → result
- [ ] No data? (empty list, no items, no products)
- [ ] Stale data? (item/resource taken while choosing, price changed)
- [ ] Double confirmation — does it prevent accidental action?

**Scenario 3: Cancel / modify**
- [ ] Path: find → cancel / modify → confirm
- [ ] Already passed / already cancelled — what feedback?
- [ ] No alternatives? (modify instead of cancel)

**Scenario 4: Payment**
- [ ] Path: select plan → pay → activate
- [ ] Failed payment — specific error or generic?
- [ ] Double payment / duplicate webhook — protected?
- [ ] Expired subscription / trial ended — does user see explanation?

### States and Feedback
- [ ] **Empty states:** every empty screen/list has a CTA or explanation
- [ ] **Loading states:** long operations (>1 sec) show an indicator
- [ ] **Error states:** errors are specific (not "something went wrong"), suggest action (Retry, Contact support)
- [ ] **Success states:** user sees the result of the action (not silence)
- [ ] **Dead ends:** user can always exit (cancel, back, main menu)

### UI Race Conditions
- [ ] Double button press — creates a duplicate?
- [ ] Fast input + submit — old data not sent?
- [ ] Polling / refresh — old result does not overwrite new one?

### API → Frontend Contract
- [ ] API returns enough data for all UI states?
- [ ] API returns flags for special states (`OperationPaused`, `TrialExpired`)?
- [ ] API errors have machine-readable codes (`resource_unavailable`), not just text?

## Evidence Requirements

Every finding MUST include:
1. **Exact file and line:** `src/.../Handler.cs:42`
2. **Code quote:** 3-5 lines showing the problem
3. **Repro steps:** how to reproduce (press X, enter Y)
4. **What the user sees:** exact message text or behavior description
5. **Why this is a problem:** reference to rule above

**NEVER report:**
- "Flow is bad" without a specific dead end and repro steps
- "Text is unclear" without quoting the text and explaining what is unclear
- Problems you cannot confirm with code or behavior description

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

- **BLOCKER** — user cannot complete action (dead end without exit, payment without feedback, duplicates on double press)
- **CRITICAL** — confusion, data loss, unclear error (generic error, empty screen without CTA, orphaned state)
- **MAJOR** — inconvenience, extra click, illogical label
- **MINOR** — cosmetic, minor text inaccuracy

- **CONFIRMED** — found specific dead end, empty screen without CTA, generic error instead of specifics, duplicates on double-submit
- **NEEDS_REVIEW** — subjective assessment: "clarity" of text, "logic" of flow. Requires human judgment.

## Outputs and Downstream Consumer

```markdown
## UX Audit — {date}

### BLOCKER
- [ ] [CONFIRMED] {description of dead end} → `{file:line}`

### CRITICAL
- [ ] [CONFIRMED] {generic error — client does not know the result of the operation} → `{file:line}`
  → Fix: return `Status` in response + specific error code

### MAJOR
- [ ] [NEEDS_REVIEW] {missing loading indicator for a long operation} → `{file:line}`
  → Fix: `isLoading` state + spinner
```

**Input from:** Bot Audit (Telegram-specific findings), Code Review Agent (UI diff).
**Output to:** Programmer Agent (flow/text fixes), Frontend Agent (UI fixes).

## Trigger or Schedule

Runs:
- When redesigning UI.
- Before public beta.
- When users complain about "confusing" flow.

## Limitations and Expected False Positives

- Text "clarity" and flow "logic" are subjective — mark `NEEDS_REVIEW`.
- A state may be handled on the client rather than the API (or vice versa); verify the full path before flagging a missing state.
- Without a running UI, race-condition findings are inferred from code, not observed.
