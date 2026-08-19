// TRAP: During refactoring the agent silently deletes types and services, or breaks the test runner.
// GUARDRAIL: We count public types and tests via reflection. If the count decreased, the agent broke something.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...).IsGreaterThanOrEqualTo(...)
// - xUnit:  [Fact] + Assert.True(current >= baseline)
// - NUnit:  [Test] + Assert.That(current, Is.GreaterThanOrEqualTo(baseline))
// - MSTest: [TestMethod] + Assert.IsTrue(current >= baseline)

using System.Reflection;
using TUnit;

namespace Tests.Patterns;

public class RatchetTests
{
    // TRAP: The agent deleted services or DTOs during "cleanup", thinking they were unused.
    // GUARDRAIL: This test fails if the number of public types in the layer decreased.
    [Test]
    public void PublicTypeCount_ShouldNotDecrease()
    {
        // Arrange
        var assembly = typeof(YourApplicationAssembly).Assembly;
        var currentCount = CountPublicTypes(assembly);

         // Baseline value — recorded manually during the audit
        const int baselineCount = 12;

        // Assert
        Assert.That(currentCount).IsGreaterThanOrEqualTo(baselineCount);
    }

    // TRAP: The agent broke the test runner or deleted the test project — "0 tests ran, exit 0".
    // GUARDRAIL: An architectural test verifies that the test count did not decrease.
    [Test]
    public void TestCount_ShouldNotDecrease()
    {
        var testAssembly = typeof(RatchetTests).Assembly;
        var currentCount = GetTestMethods(testAssembly).Count();

         // Baseline value — recorded during the audit. Update manually after coverage grows.
        const int baselineCount = 10;

        Assert.That(currentCount).IsGreaterThanOrEqualTo(baselineCount)
            .Because("Test count must not silently decrease. If runner breaks, this catches it.");
    }

    private static int CountPublicTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested)
            .Count();
    }

    private static IEnumerable<MethodInfo> GetTestMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<TestAttribute>() != null);
    }
}
