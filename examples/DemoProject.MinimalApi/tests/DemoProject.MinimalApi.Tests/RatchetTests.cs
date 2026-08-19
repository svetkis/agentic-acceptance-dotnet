// TRAP: During refactoring, an agent silently removes types and services or breaks the test runner.
// GUARDRAIL: Count public types and tests via reflection. If the count decreased, an agent broke something.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...).IsGreaterThanOrEqualTo(...)
// - xUnit:  [Fact] + Assert.True(current >= baseline)
// - NUnit:  [Test] + Assert.That(current, Is.GreaterThanOrEqualTo(baseline))

using System.Reflection;
using TUnit;

namespace DemoProject.MinimalApi.Tests;

public class RatchetTests
{
    private static readonly Assembly AppAssembly = typeof(Program).Assembly;
    private static readonly Assembly TestAssembly = typeof(RatchetTests).Assembly;

    [Test]
    public async Task PublicTypeCount_ShouldNotDecrease()
    {
        var currentCount = CountPublicTypes(AppAssembly);
        const int baselineCount = 9; // Order, Payment, OrderStatus, PaymentStatus, OrderService, PaymentService, Program, CreateOrderRequest, CreatePaymentRequest

        await Assert.That(currentCount).IsGreaterThanOrEqualTo(baselineCount)
            .Because($"Public types decreased from baseline {baselineCount} to {currentCount}. Agent may have deleted types during 'cleanup'.");
    }

    [Test]
    public async Task TestCount_ShouldNotDecrease()
    {
        var currentCount = GetTestMethods(TestAssembly).Count();
        const int baselineCount = 3; // ArchitectureRules + Ratchet + DuplicationGuard

        await Assert.That(currentCount).IsGreaterThanOrEqualTo(baselineCount)
            .Because($"Test count decreased from baseline {baselineCount} to {currentCount}. Test runner may be broken.");
    }

    private static int CountPublicTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Count(t => t.IsPublic && !t.IsNested && !t.Name.Contains('<'));
    }

    private static IEnumerable<MethodInfo> GetTestMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<TestAttribute>() != null);
    }
}
