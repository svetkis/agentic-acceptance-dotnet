# Context Rot

> An inherent limitation of LLMs. Cannot be eliminated — only compensated.
>
> Adapted from [Context Rot](https://github.com/lexler/augmented-coding-patterns/blob/main/documents/obstacles/context-rot.md) in Augmented Coding Patterns.

## Description

Context degrades as the conversation grows. The model stops following early instructions, and performance unpredictably drops. This happens long before the context window limit is exhausted.

Context fades unevenly, in zones:

- **Focus zone** (~first 5–10 messages): Instructions are executed reliably. The agent remembers the `.Select()` requirement, the `BUG###` convention, UTC rules.
- **Effective context** (~messages 10–30): Still works, but weakens. The agent seems productive, but early directives start being ignored. May "forget" to add `AsNoTracking()` or skip `CancellationToken`.
- **Red zone** (~message 30+): Past instructions are systematically lost or contradicted. The agent adds `.FindAsync()` to read-path, uses `DateTime.Now` instead of `UtcNow`, does not put `[SensitiveData]` on new PII fields.

## Impact

| Zone | Symptom | What breaks |
|------|---------|-------------|
| Focus | None | — |
| Effective | Minor rule drift | Missed `CancellationToken`, inconsistent naming |
| Red | Systematic rule violation | `AsNoTracking()` in write-path, missing `BUG###`, leaks in DTO |

- The same question may produce completely different results later in the thread
- The agent "optimizes" read-path and silently breaks write-path (see `docs/traps/testing.md#silent-breakdown`)
- The agent adds new PII fields without `[SensitiveData]`, because the attribute rule faded
- The agent uses preview SDK, because the "use latest" instruction from a later message overrode "stable only" from message 3

## Why this is an obstacle, not a trap

It is impossible to "train" an agent to never forget. It is impossible to "remind" often enough. Context degradation is a physical property of how LLM distributes attention across tokens. The only strategy is **compensation**: make rules independent of the agent's memory.

## Compensation in our pyramid

Our guardrails are designed as **stateless compensators** of context degradation:

| Guardrail | Compensates | Why it works |
|-----------|-------------|--------------|
| Roslyn analyzer / `ArchitectureRules.cs` | Red zone — rule violation | The agent may forget the `.Select()` rule, but the compiler/architecture guardrail has no memory — it checks every build |
| `RatchetTest.cs` | Red zone — attribute loss | The agent may forget to add `[SensitiveData]`, but the ratchet fails on build |
| `Decision Guards (ADR)` (`PERF-###`) | Agent "cleans up" old code | The agent sees "strange" code and "fixes" it — a numbered ADR comment stops it |
| Roslyn analyzer / `PiiGuardTest.cs` | Forgotten `[SensitiveData]` | The compile-time/static guardrail does not depend on whether the agent remembers the rule from message 2 |
| `VersionAuditTest.cs` | Forgotten "stable SDK only" | The agent thinks preview is "latest" — the scanner catches `preview` in `global.json` |
| `Hierarchical AGENTS.md` | Context dilution | Short files with layer-specific rules reduce the degradation surface |

> **Key insight:** The compiler, Roslyn analyzers, architecture checks, and ratchet tests are our "external memory". They do not fade. The agent will eventually enter the Red zone — our job is to make the build fail before the broken code reaches `main`.
