// TRAP: The agent added +1 using or ProjectReference — the diff looks harmless,
// but it introduced a circular dependency between layers or projects.
// GUARDRAIL: NetArchTest + .csproj parsing + assembly reflection catch graph violations.
// It works the same way for C++ #include or any import graph.

using System.Reflection;
using System.Xml.Linq;
using NetArchTest.Rules;
using TUnit;

namespace Tests.Patterns;

public class DependencyDriftTest
{
    // TRAP: The agent added a project reference "for the sake of one extension method".
    // GUARDRAIL: The ProjectReference graph contains no cycles.
    [Test]
    public void ProjectReferences_ShouldNotHaveCycles()
    {
        var solutionRoot = FindSolutionRoot();
        if (solutionRoot is null)
            Assert.Fail("Solution root not found");

        var projects = Directory.GetFiles(solutionRoot!, "*.csproj", SearchOption.AllDirectories);
        var graph = BuildProjectGraph(projects);
        var cycles = FindCycles(graph);

        Assert.That(cycles).IsEmpty()
            .Because($"Circular project references detected: {string.Join(" | ", cycles)}");
    }

    // TRAP: The agent introduced a cross-layer using in a "cosmetic" refactoring.
    // GUARDRAIL: NetArchTest catches real IL dependencies, not lines in files.
    // NOTE: Replace "MyProject.Domain" and "MyProject.Infrastructure" with your assemblies.
    //       For pure reflection see DomainAssembly_ShouldNotReference_InfrastructureRuntime below.
    [Test]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        // Adaptation: load your assemblies via typeof(DomainType).Assembly
        // var domain = typeof(YourDomainEntity).Assembly;
        // var infra = typeof(YourInfrastructureService).Assembly;
        //
        // var result = Types.InAssembly(domain)
        //     .ShouldNot()
        //     .HaveDependencyOnAny(infra.GetName().Name!)
        //     .GetResult();
        //
        // Assert.That(result.IsSuccessful).IsTrue();

        Assert.That(true).IsTrue()
            .Because("Template: adapt this test to your assembly names. See commented code.");
    }

    // TRAP: Regex scanning of usings gives false positives on dead code.
    // GUARDRAIL: Assembly.GetReferencedAssemblies() shows the runtime graph, not the text.
    // NOTE: Replace with your assemblies.
    [Test]
    public void DomainAssembly_ShouldNotReference_InfrastructureRuntime()
    {
        // Adaptation:
        // var domainRefs = typeof(YourDomainEntity).Assembly
        //     .GetReferencedAssemblies()
        //     .Select(a => a.Name)
        //     .ToHashSet(StringComparer.OrdinalIgnoreCase);
        //
        // var infraName = typeof(YourInfrastructureService).Assembly.GetName().Name;
        // Assert.That(domainRefs).DoesNotContain(infraName!);

        Assert.That(true).IsTrue()
            .Because("Template: adapt this test to your assembly names. See commented code.");
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
