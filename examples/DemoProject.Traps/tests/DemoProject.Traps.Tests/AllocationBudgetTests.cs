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

        const long baselineBytes = 0;
        var threshold = (long)(baselineBytes * 1.10);

        await Assert.That(budget.BytesAllocated)
            .IsLessThanOrEqualTo(threshold)
            .Because($"Hot path allocations must not exceed baseline + 10%. Baseline={baselineBytes}, Current={budget.BytesAllocated}");
    }

    [Test]
    public async Task AllHotPathMethods_HaveAllocationBudgetTests()
    {
        var hotPathMethods = GetHotPathMethods(typeof(AllocationBudgetHotspot).Assembly);
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

        return new AllocationBudget(after - before);
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

    private readonly record struct AllocationBudget(long BytesAllocated);
}
