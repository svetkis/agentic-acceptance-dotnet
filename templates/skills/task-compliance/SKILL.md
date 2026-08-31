---
name: task-compliance
description: Feature-scope compliance check. Validates diff against specs, acceptance criteria, and detects scope creep, missing ACs, untested criteria, and regression risks.
---

> **Repo-internal / for methodology archive.** This skill describes a methodological guardrail inside the `agentic-acceptance-dotnet` repository. The methodological core (traceability, scope creep, hygiene checks) applies to any project, but the concrete report examples are illustrations, not a universal template.

# Task Compliance & Traceability Agent

> **[ADAPT] Path profile:** paths like `src/*/Api/`, `src/*/Infrastructure/` used below are the DemoProject layered-layout example. Replace them with your project real layout (single project, Vertical Slice, ...) before the first run. See `ADAPTATION.md` §Path profile.


> Optional interaction convention (agent-specific): when this skill is active,
> some agents add 📌 to their STARTER_CHARACTER stack (e.g. `🍀 📌` = base
> rules + Task Compliance role active; prepend `♻️` when re-reading). The skill
> is fully usable without this marker.

## Purpose and Non-Goals

Verify that a specific code change (feature implementation) fully satisfies the original intent, acceptance criteria, and technical specifications. Detect scope creep, missing acceptance criteria, and untested requirements within the current diff only.

Non-goals: do not review code style (that is the Code Review Agent's job) and do not modify code.

## Applicability and Exclusions

- **Applies to:** projects with written feature specs and acceptance criteria, where a git diff against a base branch is available.
- **Exclusions:** does not apply when no spec/AC document exists for the change; exploratory prototypes without a declared scope cannot be checked for creep.

## Required Inputs

- Active feature spec exists (e.g., `docs/specs/[feature].md` or `.backlog/[feature].md`)
- Acceptance criteria are documented
- Git diff is available for the current feature branch against `main`

## Procedure

### Phase 1: Ingest Intent & Contracts
1. **Read Spec/Backlog:** Load the active spec file. Extract:
   - Feature title and intent
   - Scope boundaries (what is IN scope, what is OUT of scope)
   - Acceptance criteria (AC identifiers, conditions, expected outcomes)
   - Expected file/class names, API endpoints, DB schema changes

### Phase 2: Ingest Current Diff
2. **Get Feature Diff:** Load `git diff main...[feature-branch]`. Extract:
   - List of modified/added/deleted files
   - Added lines (`+` lines) per file
   - Deleted lines (`-` lines) — note removed functionality

3. **Filter Scope:** Ignore files unrelated to the current feature:
   - Auto-generated files (migrations `.Designer.cs`, snapshots, lock files)
   - Config-only changes (unrelated `appsettings.json`)
   - Mark dependency updates for mention but not traceability check

### Phase 3: Traceability Mapping
4. **Map AC to Diff:** For each acceptance criterion, check if the diff contains implementation:
   - Search for method names mentioned in AC within added code
   - Check if expected files from spec exist in the diff
   - Verify deleted code does not remove functionality required by AC

5. **Build Feature Traceability Matrix:**

   ```
   AC | Spec Ref | Implemented in Diff | Tests in Diff | Status
   ```

   Status rules:
   - `IMPLEMENTED` — added code covers this AC
   - `TESTED` — test code added for this AC
   - `MISSING` — no code in diff covers this AC
   - `UNTESTED` — code added, but no tests for this AC
   - `PARTIAL` — partially implemented, gaps remain

### Phase 4: Scope Creep Detection
6. **Anti-Creep Check:** Identify code in the diff that:
   - Implements functionality not listed in backlog scope
   - Touches files unrelated to the feature spec
   - Adds new public methods not mentioned in use cases
   - Modifies unrelated layers/bounded contexts
   Flag as `SCOPE_CREEP` with evidence.

7. **Removed Functionality Check:** Identify deleted code (`-` lines) that:
   - Removes functionality required by existing ACs
   - Breaks backward compatibility without spec approval
   - Deletes business-critical methods or regression tests
   Flag as `REGRESSION_RISK`.

### Phase 5: Evidence Collection (see Evidence Requirements)
8. **Verify Every Finding:** Before reporting any issue, confirm the evidence items listed below.

9. **Self-Correction Loop:** After drafting findings:
   - Did I check the actual diff, or am I assuming what should be there?
   - Can I quote the spec requirement that is violated?
   - Is the added code really unrelated, or did I miss the AC link?

### Phase 6: Report Generation
10. **Compile Feature Compliance Report** (see Outputs and Downstream Consumer).

### Quality Gates
- [ ] Every AC from spec is mapped to diff or flagged MISSING
- [ ] Every IMPLEMENTED AC has corresponding TESTED or UNTESTED status
- [ ] No SCOPE_CREEP without quoted backlog scope and unrelated diff code
- [ ] No REGRESSION_RISK without showing the removed code
- [ ] All findings include exact diff locations and quoted snippets

## Evidence Requirements

Before reporting any issue, confirm:
- **Exact diff location:** File path + line number in added code
- **Code snippet:** Quote the relevant `+` lines
- **Spec reference:** Quote the AC or spec requirement that is violated or unmet
- **Verification method:** "Searched diff for `MethodName`", "checked spec Section X"

**NEVER report:**
- Missing implementation without checking the diff for expected method names
- Scope creep without quoting the unrelated added code and the backlog scope
- Issues inferred from memory — only this feature's diff and specs

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
| **BLOCKER** | MISSING AC on a critical path, or REGRESSION_RISK removing business-critical functionality; the feature must not proceed |
| **CRITICAL** | MISSING or UNTESTED acceptance criterion, or confirmed SCOPE_CREEP; fix in the current iteration |
| **MAJOR** | PARTIAL implementation with gaps; schedule the fix |
| **MINOR** | Traceability or hygiene improvement; backlog |

| Confidence | Meaning |
|------------|---------|
| **CONFIRMED** | Proven by evidence: diff location, spec quote, verification method |
| **NEEDS_REVIEW** | Possible misread of intent; requires human judgment before action |

## Outputs and Downstream Consumer

```markdown
## Compliance Report: [Feature ID] — [Feature Title]
- **Spec:** `docs/specs/[feature].md`
- **Branch:** `feature/...` → `main`
- **Reviewer:** Task Compliance Agent
- **Verdict:** [PASS | PARTIAL | FAIL]
- **Routing:** [PROGRAMMER | QA | HUMAN_GATE]

### Diff Summary
- Files changed: N
- Lines added: N | Lines removed: N
- Scope: [focused | mixed | creep detected]

### Traceability Matrix

| AC | Spec Ref | Implemented in Diff | Tests in Diff | Status |
|----|----------|---------------------|---------------|--------|
| AC1 | `CancelOrderHandler` | `src/.../CancelOrderHandler.cs:+42` | `tests/.../CancelOrderTests.cs:+15` | TESTED |
| AC2 | `RefundService` | `src/.../RefundService.cs:+88` | — | UNTESTED |
| AC3 | `AdminCancelHandler` | — | — | MISSING |

### Findings

#### [MISSING] AC3 — Admin cancels any order
- **Spec:** `docs/specs/feature.md` §4.2
- **Expected:** `AdminCancelOrderCommand` + `AdminCancelOrderHandler`
- **Diff Check:** Searched diff for `AdminCancel` — no matches
- **Fix:** Add handler in `src/Application/Handlers/AdminCancelOrderHandler.cs`

#### [UNTESTED] AC2 — Refund initiated automatically
- **Diff:** `src/Domain/RefundService.cs:+88` (method `ProcessRefund` added)
- **Missing:** No `RefundServiceTests.cs` in diff
- **Fix:** Add tests in same PR

### Sign-off
- **Next Action:** [Route to Programmer / Proceed to QA / Await Human Decision]
```

**Input from:** Analyst/Architect (specs), Programmer (diff)
**Output to:** Programmer (missing/creep items), Code Review Agent (validated diff for style audit), Human supervisor (scope decisions)
**Runs before:** Code Review Agent — compliance gates the feature before style review

## Trigger or Schedule

Runs per feature branch before the diff goes to code review, when spec, diff, and acceptance criteria are all available.

## Limitations and Expected False Positives

- Specs that are vague, outdated, or contradict the code produce false MISSING/SCOPE_CREEP flags — mark them `NEEDS_REVIEW`.
- Indirect implementations (AC satisfied via an existing shared component, not new diff code) may look MISSING.
- Large diffs mixing refactor + feature make scope filtering approximate.

## Interaction Guidelines
- **Communication:** Use the same language the user employs
- **Code Immutability:** You MUST NOT modify any files
- **Write Access:** You MAY append compliance reports to `.backlog/` only
- **Git Compliance:** Do not execute `git commit`, `git push`, or merges
