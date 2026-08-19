// TRAP: An agent returns a Domain Entity from an Application service instead of a DTO.
// GUARDRAIL: A NetArchTest custom rule (Mono.Cecil) catches Entity leaks in method signatures.
// This file is a working adaptation of the template from tests/patterns/EntityLeakTest.cs

using Mono.Cecil;
using NetArchTest.Rules;
using System.Reflection;
using TUnit;

namespace DemoProject.Tests;

public class EntityLeakTest
{
    // TRAP: An agent wrote Task<Booking> instead of Task<BookingDto>.
    // GUARDRAIL: Application interfaces must not return Domain Entities.
    // NOTE: There are currently 2 violations (legacy). The test is a ratchet: it counts and pins them.
    [Test]
    public async Task ApplicationInterfaces_EntityLeakCount_ShouldNotGrow()
    {
        var appAssembly = typeof(Application.BookingService).Assembly;
        var entityRule = new EntityLeakRule(
            entityNamespace: "DemoProject.Domain",
            excludedSuffixes: new[] { "Dto", "ViewModel", "Request", "Response" });

        var violations = CountViolations(appAssembly, entityRule);
        const int baseline = 2; // Legacy: IBookingService.GetByIdAsync and GetPendingAsync return Booking

        await Assert.That(violations).IsLessThanOrEqualTo(baseline)
            .Because($"Entity leaks must not grow. Current: {violations}, baseline: {baseline}. " +
                     $"If you added a new method returning Entity — refactor to DTO.");
    }

    private static int CountViolations(Assembly appAssembly, EntityLeakRule rule)
    {
        Types.InAssembly(appAssembly)
            .That().AreInterfaces()
            .Should().MeetCustomRule(rule)
            .GetResult();

        // NetArchTest does not expose a count directly, so we count manually via inverse logic
        // Or use Mono.Cecil reflection directly for an exact count
        var assemblyPath = appAssembly.Location;
        var module = ModuleDefinition.ReadModule(assemblyPath);
        var count = 0;

        foreach (var type in module.Types.Where(t => t.IsInterface))
        {
            if (!rule.MeetsRule(type))
                count++;
        }

        return count;
    }

    public class EntityLeakRule : ICustomRule
    {
        private readonly string _entityNamespace;
        private readonly string[] _excludedSuffixes;

        public EntityLeakRule(string entityNamespace, string[] excludedSuffixes)
        {
            _entityNamespace = entityNamespace;
            _excludedSuffixes = excludedSuffixes;
        }

        public bool MeetsRule(TypeDefinition type)
        {
            if (!type.IsInterface)
                return true;

            foreach (var method in type.Methods)
            {
                var returnTypeName = GetTypeName(method.ReturnType);
                if (IsEntity(returnTypeName))
                    return false;
            }

            return true;
        }

        private bool IsEntity(string typeName)
        {
            if (!typeName.Contains(_entityNamespace))
                return false;

            return !_excludedSuffixes.Any(suffix => typeName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetTypeName(TypeReference typeRef)
        {
            if (typeRef is GenericInstanceType generic)
            {
                var elementType = generic.ElementType?.FullName ?? "";
                if (IsGenericContainer(elementType))
                {
                    var arg = generic.GenericArguments.FirstOrDefault();
                    return arg?.FullName ?? typeRef.FullName;
                }
            }

            return typeRef.FullName;
        }

        private static bool IsGenericContainer(string elementType)
        {
            return elementType.StartsWith("System.Threading.Tasks.Task`1", StringComparison.OrdinalIgnoreCase)
                || elementType.StartsWith("System.Collections.Generic.IEnumerable`1", StringComparison.OrdinalIgnoreCase)
                || elementType.StartsWith("System.Collections.Generic.IReadOnlyList`1", StringComparison.OrdinalIgnoreCase)
                || elementType.StartsWith("System.Collections.Generic.IReadOnlyCollection`1", StringComparison.OrdinalIgnoreCase)
                || elementType.StartsWith("System.Collections.Generic.List`1", StringComparison.OrdinalIgnoreCase)
                || elementType.StartsWith("System.Collections.Generic.ICollection`1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
