// GUARDRAIL: [HotPath] methods must not contend on monitor locks beyond the noise floor.
// This project is a failing demo: LockContentionHotspot.Process intentionally holds
// a lock with Thread.Sleep(1), so the test MUST fail.

using System.Reflection;
using DemoProject.Traps;
using TUnit;

namespace DemoProject.Traps.Tests;

[NotInParallel]
public class LockContentionBudgetTests
{
    private static readonly LockContentionHotspot Hotspot = new();

    [Test]
    public async Task Process_LockContentionBudget()
    {
        var budget = MeasureLockContentionBudget(
            action: () => Hotspot.Process(42),
            warmupIterations: 3,
            measureIterationsPerWorker: 20,
            workers: 4);

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
        var hotPathMethods = GetHotPathMethods(typeof(LockContentionHotspot).Assembly).ToList();

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
