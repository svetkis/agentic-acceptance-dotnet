// TRAP: The agent "optimizes" a hot path by intuition (or by a single Stopwatch
//       measurement), the PR looks convincing, and the change is actually slower
//       or allocates more than before. Also: BenchmarkDotNet results from one
//       machine are copied into a CI assert and the pipeline becomes flaky.
// GUARDRAIL: Hot-path optimizations are justified by a BenchmarkDotNet report
//       (mean + allocated bytes), committed as a baseline file; CI only compares
//       a build-to-build ratio, never an absolute number.
//
// When to use this pattern instead of AllocationBudgetTest:
// - comparing candidate implementations of the same method (A vs B);
// - justifying a deliberate optimization in a PR (evidence, not vibes);
// - micro-level allocation profiling (column Gen0 / Allocated).
// When NOT to use:
// - regression gate for every commit → use a budget test (AllocationBudgetTest);
// - whole-system latency under load → use a load test (NBomber, load-test-ops).
//
// Run convention (never `dotnet test` — benchmarks are console apps):
//   dotnet run -c Release --project tests/HotPathBenchmarks -- --filter '*'
//   dotnet run -c Release --project tests/HotPathBenchmarks -- --filter '*' -m memory
//
// TRAP: Running benchmarks in Debug or via `dotnet test` silently produces
//       garbage numbers (no JIT warmup control, debugger attached).
// GUARDRAIL: Benchmarks live in a separate console project, always `-c Release`.
//
// TRAP: On CI agents BenchmarkDotNet cannot enable high-priority process/HVCI,
//       runs with degraded accuracy and logs warnings — which read as "OK".
// GUARDRAIL: On CI use `--job short` or a custom config with
//       `Config.Default.With(Job.ShortRun)`, and treat the exit code as the gate.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Tests.Patterns;

// Separate console project (Program.cs is one line — see bottom of this file).
// The benchmark class mirrors the real hot path with production-like input.
[MemoryDiagnoser]                       // Allocated / Gen0 columns — the cheapest win
[SimpleJob(warmupCount: 3, iterationCount: 20)]  // deterministic, CI-friendly
public class SlotSearchBenchmarks
{
    private YourHotPathService _service = null!;
    private DateTime _targetDay;

    [GlobalSetup]
    public void Setup()
    {
        // TRAP: benchmarking against an empty/trivial dataset — the optimizer's
        //       "3x faster" claim holds only for 10 items and collapses on real volume.
        // GUARDRAIL: input volume and shape match production (row counts, distinct keys).
        _service = YourHotPathServiceFactory.CreateWithSeed(rows: 250_000, seed: 42);
        _targetDay = new DateTime(2026, 09, 15, 0, 0, 0, DateTimeKind.Utc);
    }

    [Benchmark(Baseline = true)]
    public int GetAvailableSlots_Current() =>
        _service.GetAvailableSlots(_targetDay);

    // The candidate implementation goes next to the baseline IN THE SAME RUN,
    // so environment noise affects both and the ratio stays meaningful.
    [Benchmark]
    public int GetAvailableSlots_Candidate() =>
        _service.GetAvailableSlotsCandidate(_targetDay);
}

// --- Reading the report (the guardrail, not the tool, is the point) ---
//
// | Method                    | Mean     | Allocated |
// | GetAvailableSlots_Current | 812.3 us |   48.5 KB |   ← baseline
// | GetAvailableSlots_Candidate | 790.1 us |  132.7 KB |
//
// GUARDRAIL: a candidate is accepted only if BOTH hold:
//   1. Mean improves beyond noise (> 3% on the same machine, same run);
//   2. Allocations do not grow more than the agreed budget.
// TRAP: "faster but allocates 3x more" — the mean win is eaten by Gen0 pauses
//       under real load; without [MemoryDiagnoser] the agent never sees it.

// --- CI gate: ratio, not absolute numbers ---
//
// Absolute means are machine-specific: they will flake in CI. Export the diff:
//   dotnet run -c Release --project tests/HotPathBenchmarks -- -f '*' -e json -a ./BenchmarkResults
// and assert only the build-to-build ratio (e.g. Mean_Candidate <= Mean_Baseline * 1.05)
// in a normal budget test. See AllocationBudgetTest.cs for the assert side.

// --- Program.cs of the benchmark console project (whole file) ---
//
// BenchmarkSwitcher.FromAssembly(typeof(SlotSearchBenchmarks).Assembly).Run(args);
