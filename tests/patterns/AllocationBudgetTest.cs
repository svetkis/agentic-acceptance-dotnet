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

using System.Reflection;
using TUnit;

namespace Tests.Patterns;

// Hot path marker. You can replace it with your project's own attribute.
[AttributeUsage(AttributeTargets.Method)]
public class HotPathAttribute : Attribute { }

public class AllocationBudgetTests
{
    // TRAP: The agent added extra allocations to a critical method.
    // GUARDRAIL: Allocations of a [HotPath] method do not exceed baseline + 10%.
    [Test]
    public void HotPath_GetAvailableSlots_AllocationBudget()
    {
        var budget = MeasureAllocationBudget(
            action: () => YourHotPathService.GetAvailableSlots(DateTime.UtcNow),
            warmupIterations: 3,
            measureIterations: 100);

        // Baseline was recorded during the first audit. Update manually after a deliberate optimization.
        const long baselineBytes = 1024;
        var threshold = (long)(baselineBytes * 1.10);

        Assert.That(budget.BytesAllocated)
            .IsLessThanOrEqualTo(threshold)
            .Because($"Hot path allocations must not exceed baseline + 10%. " +
                     $"Baseline={baselineBytes}, Current={budget.BytesAllocated}, Threshold={threshold}");
    }

    // TRAP: The agent added a [HotPath] method but forgot to write an allocation test for it.
    // GUARDRAIL: Every public method with [HotPath] has a matching {MethodName}_AllocationBudget test.
    [Test]
    public void AllHotPathMethods_HaveAllocationBudgetTests()
    {
        var hotPathMethods = GetHotPathMethods(typeof(YourHotPathService).Assembly);
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
