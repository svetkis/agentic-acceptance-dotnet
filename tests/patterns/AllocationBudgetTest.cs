// TRAP: The agent adds new/async/boxing to a [HotPath] method, and latency degrades in production.
// GUARDRAIL: Every [HotPath] method has an allocation budget; regressions are caught in tests.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...)
// - xUnit:  [Fact] + Assert.True(...)
// - NUnit:  [Test] + Assert.That(...)
// - MSTest: [TestMethod] + Assert.IsTrue(...)
//
// NOTE: For stability, run in an isolated environment (same OS, .NET runtime, GC mode).
//       Use warmup + several iterations to avoid flaky tests.
// NOTE: GC.GetAllocatedBytesForCurrentThread sees ONLY the calling thread. If the hot
//       path offloads work (Task.Run, ThreadPool), those allocations escape the budget.

using System.Reflection;
using TUnit;

namespace Tests.Patterns;

// Hot path marker. You can replace it with your project's own attribute.
[AttributeUsage(AttributeTargets.Method)]
public class HotPathAttribute : Attribute { }

public class AllocationBudgetTests
{
    // TRAP: The agent added extra allocations to a critical method.
    // GUARDRAIL: Allocations of a [HotPath] method do not exceed baseline + 10% (per operation).
    // Naming: {HotPathMethodName}_AllocationBudget — required by the meta-test below.
    [Test]
    public void GetAvailableSlots_AllocationBudget()
    {
        var budget = MeasureAllocationBudget(
            action: () => YourHotPathService.GetAvailableSlots(DateTime.UtcNow),
            warmupIterations: 3,
            measureIterations: 100);

        // Baseline is PER OPERATION — recorded during the first audit the same way
        // (total bytes / iterations). Update manually after a deliberate optimization.
        const long baselineBytesPerOp = 10;
        var threshold = (long)(baselineBytesPerOp * 1.10);

        Assert.That(budget.BytesAllocatedPerOperation)
            .IsLessThanOrEqualTo(threshold)
            .Because($"Hot path allocations must not exceed baseline + 10% (per op). " +
                     $"Baseline/op={baselineBytesPerOp}, Current/op={budget.BytesAllocatedPerOperation}, Threshold/op={threshold}");
    }

    // TRAP: The agent added a [HotPath] method but forgot to write an allocation test for it.
    // GUARDRAIL: Every public method with [HotPath] has a matching {MethodName}_AllocationBudget test.
    //        Zero [HotPath] methods found = broken scan (wrong assembly, attribute renamed) —
    //        fail instead of passing vacuously.
    [Test]
    public void AllHotPathMethods_HaveAllocationBudgetTests()
    {
        var hotPathMethods = GetHotPathMethods(typeof(YourHotPathService).Assembly).ToList();

        Assert.That(hotPathMethods.Count > 0).IsTrue()
            .Because("no [HotPath] methods found — wrong assembly scanned or the attribute was renamed");

        var testMethods = GetTestMethods(typeof(AllocationBudgetTests).Assembly)
            .Select(m => m.Name)
            .ToHashSet();

        var missing = hotPathMethods
            .Where(m => !testMethods.Contains($"{m.Name}_AllocationBudget"))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .ToList();

        Assert.That(missing).IsEmpty()
            .Because("Every [HotPath] method must have a matching {MethodName}_AllocationBudget test.");
    }

    // --- Helpers ---

    private static AllocationBudget MeasureAllocationBudget(Action action, int warmupIterations, int measureIterations)
    {
        // Warmup
        for (var i = 0; i < warmupIterations; i++)
            action();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < measureIterations; i++)
            action();
        var after = GC.GetAllocatedBytesForCurrentThread();

        return new AllocationBudget(TotalBytes: after - before, Operations: measureIterations);
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

    private readonly record struct AllocationBudget(long TotalBytes, int Operations)
    {
        // Per-op, not total: a baseline recorded for 100 iterations must not be compared
        // against a run with 1000 — normalize before asserting.
        public long BytesAllocatedPerOperation => Operations > 0 ? TotalBytes / Operations : TotalBytes;
    }
}
