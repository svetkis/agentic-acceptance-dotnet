// TRAP: An agent adds an IMPLICIT lock to a hot path — a helper method with a
//       lock inside, Lazy<T> with the default publication mode, a static
//       constructor, Console.WriteLine, a cache with eviction. Single-threaded
//       tests stay green, the allocation budget stays green; under parallel load
//       the threads serialize on the lock and throughput collapses.
// GUARDRAIL: Every [HotPath] method has a lock-contention budget: the method is
//       hammered by several workers simultaneously, and the runtime-wide monitor
//       contention counter (Monitor.LockContentionCount) must not grow beyond the
//       noise floor. Naming: {MethodName}_LockContentionBudget — required by the
//       meta-test below.
//
// Framework adaptation:
// - TUnit:  [Test] + [NotInParallel] on the class (the counter is process-global)
// - xUnit:  [Fact] + a dedicated non-parallel collection
// - NUnit:  [Test] + [NonParallelizable]
// - MSTest: [TestMethod] + [DoNotParallelize]
//
// NOTE: Monitor.LockContentionCount (.NET Core 3.0+) counts only CONTENDED
//       acquisitions and is process-wide. A single worker sees nothing at all —
//       the parallel hammer is what makes an implicit lock visible. Never port
//       this test to a single-threaded loop.
// NOTE: the runtime counts PARKED waiters. A lock whose holder busy-waits often
//       registers almost nothing, because waiters take the lock through adaptive
//       spin / direct handoff without parking. Traps and reproductions must park
//       the holder (Thread.Sleep), not burn CPU under the lock.
// NOTE: the noise floor must sit ABOVE ambient test-host contention and FAR BELOW
//       one real serialized lock (demo margins: floor 0.10/op vs trap ~0.3-0.5/op).
// NOTE: warmup removes one-shot initialization (Lazy, static ctor) from the
//       measurement. If the first-touch lock itself is the suspected trap, write
//       a dedicated first-touch test instead of removing the warmup.
// When NOT to use: cold paths, startup-only code, paths intentionally serialized
//       by design (record that as a Decision Guard, not as a budget exemption).

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TUnit;

namespace Tests.Patterns;

// Hot path marker. You can replace it with your project's own attribute.
[AttributeUsage(AttributeTargets.Method)]
public class HotPathAttribute : Attribute { }

// The counter is process-global: without isolation, a concurrently running test
// class adds contention events inside the measurement window and the budget flakes.
[NotInParallel]
public class LockContentionBudgetTests
{
    // TRAP: The agent "fixed" a race by taking a lock inside a [HotPath] method
    //       (or by calling a helper that does) — single-threaded checks stay green
    //       while parallel throughput collapses.
    // GUARDRAIL: Contentions of a [HotPath] method per operation stay within the
    //        noise floor (per operation, so the budget is independent of iteration count).
    [Test]
    public void GetAvailableSlots_LockContentionBudget()
    {
        var budget = MeasureLockContentionBudget(
            action: () => YourHotPathService.GetAvailableSlots(DateTime.UtcNow),
            warmupIterations: 3,
            measureIterationsPerWorker: 250,
            workers: 4);

        // A lock-free path contends exactly 0 times. The headroom above zero
        // absorbs ambient test-host noise; a serialized lock that parks its holder
        // yields several times the floor (~0.3-0.5/op in the trap demo). Record
        // the floor per project after measuring ambient noise once.
        const double maxContentionsPerOperation = 0.10;

        Assert.That(budget.ContentionsPerOperation)
            .IsLessThanOrEqualTo(maxContentionsPerOperation)
            .Because($"Hot path must not contend on monitor locks. Contentions/op={budget.ContentionsPerOperation} " +
                     $"(of {budget.Operations} ops), Budget/op={maxContentionsPerOperation}");
    }

    // TRAP: The agent added a [HotPath] method but forgot to write a lock-contention test for it.
    // GUARDRAIL: Every public method with [HotPath] has a matching {MethodName}_LockContentionBudget test.
    //        Zero [HotPath] methods found = broken scan (wrong assembly, attribute renamed) —
    //        fail instead of passing vacuously.
    [Test]
    public void AllHotPathMethods_HaveLockContentionBudgetTests()
    {
        var hotPathMethods = GetHotPathMethods(typeof(YourHotPathService).Assembly).ToList();

        Assert.That(hotPathMethods.Count > 0).IsTrue()
            .Because("no [HotPath] methods found — wrong assembly scanned or the attribute was renamed");

        var testMethods = GetTestMethods(typeof(LockContentionBudgetTests).Assembly)
            .Select(m => m.Name)
            .ToHashSet();

        var missing = hotPathMethods
            .Where(m => !testMethods.Contains($"{m.Name}_LockContentionBudget"))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .ToList();

        Assert.That(missing).IsEmpty()
            .Because("Every [HotPath] method must have a matching {MethodName}_LockContentionBudget test.");
    }

    // --- Helpers ---

    private static LockContentionBudget MeasureLockContentionBudget(
        Action action, int warmupIterations, int measureIterationsPerWorker, int workers)
    {
        // Warmup: JIT, caches, first-use initialization (Lazy, static ctors).
        for (var i = 0; i < warmupIterations; i++)
            action();

        // No GC.Collect here: contention events come from a runtime-wide counter,
        // unlike allocated bytes which are measured per thread.

        var before = Monitor.LockContentionCount;
        var tasks = new Task[workers];
        for (var w = 0; w < workers; w++)
        {
            tasks[w] = Task.Run(() =>
            {
                for (var i = 0; i < measureIterationsPerWorker; i++)
                    action();
            });
        }
        Task.WaitAll(tasks);
        var after = Monitor.LockContentionCount;

        // The counter is approximate; clamp so a transient dip cannot fake a negative budget
        return new LockContentionBudget(
            TotalContentions: Math.Max(0, after - before),
            Operations: workers * measureIterationsPerWorker);
    }

    private static IEnumerable<MethodInfo> GetHotPathMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(m => m.GetCustomAttribute<HotPathAttribute>() != null);
    }

    private static IEnumerable<MethodInfo> GetTestMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<TestAttribute>() != null);
    }

    private readonly record struct LockContentionBudget(long TotalContentions, int Operations)
    {
        // Per-op, not total: same rationale as AllocationBudgetTest — normalize
        // before asserting so the budget is independent of the iteration count.
        public double ContentionsPerOperation => Operations > 0
            ? (double)TotalContentions / Operations
            : TotalContentions;
    }
}
