// GUARDRAIL: A [HotPath] method hammered by parallel workers does not contend on
// monitor locks beyond the noise floor.
// This file is a working adaptation of the template from tests/patterns/LockContentionBudgetTest.cs

using System.Reflection;
using DemoProject.Application;
using DemoProject.Domain;
using TUnit;

namespace DemoProject.Tests;

// TRAP: Monitor.LockContentionCount is process-global; TUnit runs test classes in
//       parallel, so a concurrently running class can add contention events inside
//       the measurement window and the budget flakes.
// GUARDRAIL: [NotInParallel] isolates the measurement window from other test classes.
[NotInParallel]
public class LockContentionBudgetTests
{
    private static readonly BookingService Service = new();

    [Test]
    public async Task GetPendingCount_LockContentionBudget()
    {
        var budget = MeasureLockContentionBudget(
            action: () => Service.GetPendingCount(),
            warmupIterations: 3,
            measureIterationsPerWorker: 250,
            workers: 4);

        // Budget per operation: a lock-free path contends exactly 0 times; the
        // headroom absorbs ambient test-host noise. A serialized lock that parks
        // its holder (see DemoProject.Traps.LockContentionHotspot) yields several
        // times the floor (~0.3-0.5/op measured) — that gap is the margin that
        // keeps this test stable.
        const double maxContentionsPerOperation = 0.10;

        await Assert.That(budget.ContentionsPerOperation)
            .IsLessThanOrEqualTo(maxContentionsPerOperation)
            .Because($"Hot path must not contend on monitor locks. Contentions/op={budget.ContentionsPerOperation:0.###} " +
                     $"(of {budget.Operations} ops), Budget/op={maxContentionsPerOperation}");
    }

    // TRAP: The agent added a [HotPath] method but forgot to write a lock-contention test for it.
    // GUARDRAIL: Every public method with [HotPath] has a matching {MethodName}_LockContentionBudget test.
    //        Zero [HotPath] methods found = broken scan (wrong assembly, attribute renamed) —
    //        fail instead of passing vacuously.
    [Test]
    public async Task AllHotPathMethods_HaveLockContentionBudgetTests()
    {
        var hotPathMethods = GetHotPathMethods(typeof(BookingService).Assembly).ToList();

        await Assert.That(hotPathMethods.Count > 0).IsTrue()
            .Because("no [HotPath] methods found — wrong assembly scanned or the attribute was renamed");

        var testMethods = GetTestMethods(typeof(LockContentionBudgetTests).Assembly)
            .Select(m => m.Name)
            .ToHashSet();

        var missing = hotPathMethods
            .Where(m => !testMethods.Contains($"{m.Name}_LockContentionBudget"))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .ToList();

        await Assert.That(missing).IsEmpty()
            .Because("Every [HotPath] method must have a matching {MethodName}_LockContentionBudget test.");
    }

    // --- Helpers ---

    private static LockContentionBudget MeasureLockContentionBudget(
        Action action, int warmupIterations, int measureIterationsPerWorker, int workers)
    {
        for (var i = 0; i < warmupIterations; i++)
            action();

        // No GC.Collect: contention events come from the runtime-wide counter,
        // not from a per-thread measurement like GC.GetAllocatedBytesForCurrentThread.

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
        public double ContentionsPerOperation => Operations > 0
            ? (double)TotalContentions / Operations
            : TotalContentions;
    }
}
