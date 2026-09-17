// GUARDRAIL: [HotPath] methods must not allocate beyond baseline + 10%.
// This project is a failing demo: AllocationBudgetHotspot.Process intentionally
// uses new List<int>, so the test MUST fail.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DemoProject.Traps;
using TUnit;

namespace DemoProject.Traps.Tests;

public class AllocationBudgetTests
{
    private static readonly AllocationBudgetHotspot Hotspot = new();

    [Test]
    public async Task Process_AllocationBudget()
    {
        var budget = MeasureAllocationBudget(
            action: () => Hotspot.Process(42),
            warmupIterations: 3,
            measureIterations: 100);

        const long baselineBytesPerOp = 0; // per operation
        var threshold = (long)(baselineBytesPerOp * 1.10);

        await Assert.That(budget.BytesAllocatedPerOperation)
            .IsLessThanOrEqualTo(threshold)
            .Because($"Hot path allocations must not exceed baseline + 10% (per op). Baseline/op={baselineBytesPerOp}, Current/op={budget.BytesAllocatedPerOperation}");
    }

    [Test]
    public async Task AllHotPathMethods_HaveAllocationBudgetTests()
    {
        var hotPathMethods = GetHotPathMethods(typeof(AllocationBudgetHotspot).Assembly).ToList();

        await Assert.That(hotPathMethods.Count > 0).IsTrue()
            .Because("no [HotPath] methods found — wrong assembly scanned or the attribute was renamed");
        var testMethods = GetTestMethods(typeof(AllocationBudgetTests).Assembly)
            .Select(m => m.Name)
            .ToHashSet();

        var missing = hotPathMethods
            .Where(m => !testMethods.Contains($"{m.Name}_AllocationBudget"))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .ToList();

        await Assert.That(missing).IsEmpty()
            .Because("Every [HotPath] method must have a matching {MethodName}_AllocationBudget test.");
    }

    [SuppressMessage("Minor Code Smell", "S1215:GC.Collect should not be forced",
        Justification = "Allocation budget tests need a clean GC state before measuring.")]
    private static AllocationBudget MeasureAllocationBudget(Action action, int warmupIterations, int measureIterations)
    {
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
        public long BytesAllocatedPerOperation => Operations > 0 ? TotalBytes / Operations : TotalBytes;
    }
}
