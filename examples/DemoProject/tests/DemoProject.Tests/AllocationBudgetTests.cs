// GUARDRAIL: Allocations of [HotPath] methods do not exceed baseline + 10%.
// This file is a working adaptation of the template from tests/patterns/AllocationBudgetTest.cs

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DemoProject.Application;
using DemoProject.Domain;
using TUnit;

namespace DemoProject.Tests;

public class AllocationBudgetTests
{
    private static readonly BookingService Service = new();

    [Test]
    public async Task GetPendingCount_AllocationBudget()
    {
        var budget = MeasureAllocationBudget(
            action: () => Service.GetPendingCount(),
            warmupIterations: 3,
            measureIterations: 1000);

        const long baselineBytes = 0; // GetPendingCount does not allocate
        var threshold = (long)(baselineBytes * 1.10);

        await Assert.That(budget.BytesAllocated)
            .IsLessThanOrEqualTo(threshold)
            .Because("Hot path allocations must not exceed baseline + 10%.");
    }

    [Test]
    public async Task AllHotPathMethods_HaveAllocationBudgetTests()
    {
        var hotPathMethods = GetHotPathMethods(typeof(BookingService).Assembly);
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
