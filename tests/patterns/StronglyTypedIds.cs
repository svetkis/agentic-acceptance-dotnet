// TRAP: Out of habit the agent uses Guid/string/int for identifiers in Domain entities
// instead of creating a strongly typed ID (BookingId, ClientId, AgentId).
// This opens the door to passing a ClientId into a method expecting an AgentId.
// GUARDRAIL: An architectural test scans the Domain assembly and fails if it finds
// a "bare" primitive in a property whose name ends with Id.

using System.Reflection;
using TUnit;

namespace Tests.Patterns;

public class StronglyTypedIds
{
    // TRAP: The agent wrote new Booking { Id = Guid.NewGuid() } instead of BookingId.New().
    // GUARDRAIL: All *Id properties in Domain entities must have a type
    // ending with Id (not Guid/string/int/long).
    [Test]
    public void DomainEntities_ShouldNotUseRawPrimitivesForIds()
    {
        // Adaptation: replace with your assembly and convention.
        // var domainAssembly = typeof(YourDomainEntity).Assembly;
        // var violations = GetRawIdViolations(domainAssembly);
        //
        // Assert.That(violations).IsEmpty()
        //     .Because($"Domain entities must use strongly typed IDs. Violations: {string.Join(", ", violations)}");

        Assert.That(true).IsTrue()
            .Because("Template: adapt this test to your assembly. See commented code and helper below.");
    }

    // TRAP: The agent added an entity with a Guid Id, but the baseline was not updated — the test silently passes.
    // GUARDRAIL: Ratchet — count the current number of entities with strongly typed IDs
    // and verify it does not decrease (or that the number of violations does not grow).
    [Test]
    public void StronglyTypedIdUsage_ShouldNotDecrease()
    {
        // Adaptation:
        // var domainAssembly = typeof(YourDomainEntity).Assembly;
        // var stronglyTypedCount = CountStronglyTypedIds(domainAssembly);
        //
        // const int baseline = 5; // Pin the current value
        // Assert.That(stronglyTypedCount).IsGreaterThanOrEqualTo(baseline)
        //     .Because($"Strongly typed IDs must not decrease. Current: {stronglyTypedCount}, baseline: {baseline}");

        Assert.That(true).IsTrue()
            .Because("Template: adapt this test to count strongly typed IDs. See commented code.");
    }

    // --- Helper: finds *Id properties with "bare" primitives in Domain entities ---
    private static IEnumerable<string> GetRawIdViolations(Assembly domainAssembly)
    {
        var rawTypes = new HashSet<string> { "Guid", "String", "Int32", "Int64" };

        var entityTypes = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace?.Contains("Domain") == true);

        foreach (var type in entityTypes)
        {
            var idProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name.EndsWith("Id", StringComparison.Ordinal));

            foreach (var prop in idProperties)
            {
                var propTypeName = prop.PropertyType.Name;
                if (rawTypes.Contains(propTypeName) ||
                    (prop.PropertyType.IsGenericType && rawTypes.Contains(prop.PropertyType.GetGenericTypeDefinition().Name)))
                {
                    yield return $"{type.Name}.{prop.Name} : {propTypeName}";
                }
            }
        }
    }

    private static int CountStronglyTypedIds(Assembly domainAssembly)
    {
        var rawTypes = new HashSet<string> { "Guid", "String", "Int32", "Int64" };

        return domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace?.Contains("Domain") == true)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Count(p => p.Name.EndsWith("Id", StringComparison.Ordinal) &&
                        !rawTypes.Contains(p.PropertyType.Name) &&
                        !(p.PropertyType.IsGenericType && rawTypes.Contains(p.PropertyType.GetGenericTypeDefinition().Name)));
    }
}
