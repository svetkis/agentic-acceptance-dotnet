// TRAP: An agent added +1 using or ProjectReference — the diff looks harmless,
// but introduced a circular dependency between layers or projects.
// GUARDRAIL: NetArchTest + .csproj parsing catch graph violations.
// This file is a working adaptation of the template from tests/patterns/DependencyDriftTest.cs

using System.Reflection;
using System.Xml.Linq;
using NetArchTest.Rules;
using TUnit;

namespace DemoProject.Tests;

public class DependencyDriftTest
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Booking).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.InfrastructureBookingService).Assembly;

    // TRAP: An agent added a project reference "for the sake of one extension method".
    // GUARDRAIL: The ProjectReference graph (via .csproj) contains no cycles.
    // NOTE: An alternative is reflection over Assembly.GetReferencedAssemblies() for the runtime graph.
    [Test]
    public async Task ProjectReferences_ShouldNotHaveCycles()
    {
        var solutionRoot = FindSolutionRoot();
        if (solutionRoot is null)
            Assert.Fail("Solution root not found");

        var projects = Directory.GetFiles(solutionRoot!, "*.csproj", SearchOption.AllDirectories);
        var graph = BuildProjectGraph(projects);
        var cycles = FindCycles(graph);

        await Assert.That(cycles).IsEmpty()
            .Because($"Circular project references detected: {string.Join(" | ", cycles)}");
    }

    // TRAP: An agent introduced a cross-layer using in a "cosmetic" refactoring.
    // GUARDRAIL: NetArchTest catches real IL type dependencies, not lines in files.
    // NOTE: This duplicates ArchitectureRules.Domain_ShouldNotDependOn_Infrastructure
    //       as a dedicated guard for graph drift. Both tests can coexist.
    [Test]
    public async Task Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureAssembly.GetName().Name!)
            .GetResult();

        await Assert.That(result.IsSuccessful).IsTrue()
            .Because(FormatFailingTypes(result));
    }

    private static string FormatFailingTypes(NetArchTest.Rules.TestResult result)
    {
        if (result.IsSuccessful)
            return "Domain layer must not depend on Infrastructure layer";

        var lines = result.FailingTypes
            .Select(t => $"- {t.FullName}: {t.Explanation}")
            .ToList();

        return "Domain layer must not depend on Infrastructure layer. Failing types:\n" + string.Join("\n", lines);
    }

    // TRAP: An agent added using Infrastructure in Domain — regex gave a false negative/positive.
    // GUARDRAIL: Assembly reflection confirms the absence of runtime references.
    // NOTE: The assembly reference graph is a runtime view, unlike .csproj (build intent).
    [Test]
    public async Task DomainAssembly_ShouldNotReference_InfrastructureRuntime()
    {
        var domainRefs = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var infraName = InfrastructureAssembly.GetName().Name;

        await Assert.That(domainRefs).DoesNotContain(infraName!)
            .Because("Domain assembly must not reference Infrastructure assembly at runtime");
    }

    private static string? FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6; i++)
        {
            if (dir is null) break;
            if (dir.GetFiles("*.sln").Any() || dir.GetDirectories("src").Any())
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static Dictionary<string, List<string>> BuildProjectGraph(string[] projectFiles)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var proj in projectFiles)
        {
            var projName = Path.GetFileNameWithoutExtension(proj);
            graph[projName] = new List<string>();

            var doc = XDocument.Load(proj);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            foreach (var reference in doc.Descendants(ns + "ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                if (!string.IsNullOrEmpty(include))
                {
                    var refName = Path.GetFileNameWithoutExtension(include);
                    graph[projName].Add(refName);
                }
            }
        }

        return graph;
    }

    private static List<string> FindCycles(Dictionary<string, List<string>> graph)
    {
        var cycles = new List<string>();
        var visited = new HashSet<string>();
        var recStack = new HashSet<string>();

        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node))
                Dfs(node, graph, visited, recStack, new List<string>(), cycles);
        }

        return cycles;
    }

    private static void Dfs(string node, Dictionary<string, List<string>> graph,
        HashSet<string> visited, HashSet<string> recStack, List<string> path, List<string> cycles)
    {
        visited.Add(node);
        recStack.Add(node);
        path.Add(node);

        foreach (var neighbor in graph.GetValueOrDefault(node) ?? new List<string>())
        {
            if (!visited.Contains(neighbor))
            {
                Dfs(neighbor, graph, visited, recStack, path, cycles);
            }
            else if (recStack.Contains(neighbor))
            {
                var cycleStart = path.IndexOf(neighbor);
                var cycle = path.Skip(cycleStart).ToList();
                cycle.Add(neighbor);
                cycles.Add(string.Join(" → ", cycle));
            }
        }

        path.RemoveAt(path.Count - 1);
        recStack.Remove(node);
    }
}
