# Lock Contention Evidence (hot paths)

> Detecting **implicit locks** in hot paths: static guardrails (fast, incomplete)
> plus runtime evidence (complete for locks that actually hurt). The static half is
> `HotPathLockAnalyzer` — SAE013 (direct lock, always on) and SAE014 (call-chain
> analysis, CI-only, see [`architecture-tests.md §Roslyn`](architecture-tests.md#11-roslyn-analyzers-as-the-default-for-c)
> and [`tests/conventions/AnalyzerDiagnostics.md`](../../tests/conventions/AnalyzerDiagnostics.md)).
> This document is the runtime half and the ladder that connects both.

## The trap

An agent "fixes" a race by taking a lock — directly, or one call away where review
does not look: a helper with a lock inside, `Lazy<T>` with the default publication
mode, a static constructor (the CLR runs type initializers under a lock),
`Console.WriteLine`, a cache with eviction. Single-threaded tests stay green, the
allocation budget stays green; under parallel load the threads serialize on the
lock and throughput collapses.

No single layer sees the whole picture:

| Layer | Sees | Blind to | Cost |
|-------|------|----------|------|
| SAE013 (IDE, always on) | direct `lock` / `Monitor.Enter` in `[HotPath]` | anything indirect | ~0 |
| SAE014 (CI build, `EnableHotPathDeepAnalysis=true`) | same-compilation call chains, with the chain in the message | virtual dispatch, delegate targets, other assemblies | one pass per CI build |
| [LockContentionBudgetTest](../../tests/patterns/LockContentionBudgetTest.cs) (CI tests) | ANY lock that actually contends — including framework and NuGet code | uncontended locks (usually cheap anyway: ~20 ns) | seconds per hot path |
| [BenchmarkTest](../../tests/patterns/BenchmarkTest.cs) + `[ThreadingDiagnoser]` | Lock Contentions per op, candidate vs baseline in one run | spin-holds (count parked waiters only — may show ~0) | one benchmark run |
| Load test + contention trace stacks | names the exact culprit under load, cross-assembly included | needs a runnable scenario | one load run |
| Prod telemetry (monitor-lock-contention counter) | real-world contention rate, alerting | root cause — needs a trace follow-up | always-on, cheap |

Static layers see all locks but cannot say which ones hurt; runtime layers see
only the harmful ones but see them anywhere, including inside dependencies. Use
them as one ladder, not as alternatives.

## The ladder

1. **Typing / build** — SAE013 squiggle in the IDE; zero config.
2. **CI build** — SAE014 on the CI command line
   (`-p:EnableHotPathDeepAnalysis=true`, wired in `examples/DemoProject/.github`
   workflows and `Directory.Build.props`): catches the lock one call away with
   `Hot path 'Process' transitively acquires a lock via Process → GetOrCreate`.
3. **CI tests** — `{MethodName}_LockContentionBudget` for every `[HotPath]` method:
   a parallel hammer (4+ workers) asserts `Monitor.LockContentionCount` stays
   within the noise floor. This is the only layer that catches a lock inside a
   NuGet dependency. Working green: `examples/DemoProject/tests/DemoProject.Tests/LockContentionBudgetTests.cs`;
   working red: `examples/DemoProject/traps-src/DemoProject.Traps/LockContentionHotspot.cs`.
4. **A/B evidence** — `[ThreadingDiagnoser]` next to `[MemoryDiagnoser]` in the
   benchmark: a candidate that is faster single-threaded but takes a lock shows a
   non-zero Lock Contentions column against a zero baseline.
5. **Under load** — NBomber scenario with a contention trace; the stacks name the
   culprit even when it lives in a dependency:

   ```bash
   # runtime contention counter while the load scenario runs
   dotnet-counters monitor --counters System.Runtime[monitor-lock-contention-count] -- <app>

   # capture contention events with stacks (keyword 0x4000 = Contention)
   dotnet-trace collect --providers "Microsoft-Windows-DotNETRuntime:0x4000:4" -- <app>
   ```

   PerfView / Visual Studio / speedscope show Contention stacks: the method that
   was blocked and the method that held the lock.
6. **Production** — the same System.Runtime counter via OpenTelemetry or
   `dotnet monitor` metrics; alert on a growing monitor-lock-contention rate, then
   pull a trace (step 5) for the stacks.

## Honest boundaries

- The runtime counter registers **parked waiters only**. A lock whose holder
  busy-waits can show ~0 because waiters acquire through adaptive spin / direct
  handoff without parking — verified while building the trap demo. Reproductions
  and traps must park the holder (`Thread.Sleep`), not burn CPU under the lock.
- The counter is **process-global**: budget tests must run isolated from parallel
  test classes (`[NotInParallel]` in TUnit) or the ambient noise flakes the budget.
- **Uncontended locks are invisible** — and that is mostly fine (~20 ns). If an
  uncontended lock still matters for your throughput, that is a benchmark
  question, not a contention question.
- Budget floors and trap margins from the demo: noise floor 0.10 contentions/op,
  the trap (4 workers, 40 ms parked hold) measures ~0.3–0.5/op. Re-measure both
  per project before adopting.

## Wiring in this repository

| Piece | Where |
|-------|-------|
| Analyzer (SAE013/SAE014) | `examples/DemoProject/src/DemoProject.Analyzers/HotPathLockAnalyzer.cs` |
| Property gate (`CompilerVisibleProperty`) | `examples/DemoProject/Directory.Build.props` |
| CI switch (`-p:EnableHotPathDeepAnalysis=true`) | `.github/workflows/demo-project-ci.yml` (Build steps) |
| Pattern | `tests/patterns/LockContentionBudgetTest.cs` |
| Green example | `examples/DemoProject/tests/DemoProject.Tests/LockContentionBudgetTests.cs` |
| Red trap (must fail) | `examples/DemoProject/traps-src/DemoProject.Traps/LockContentionHotspot.cs` + `examples/DemoProject/tests/DemoProject.Traps.Tests/LockContentionBudgetTests.cs` |
