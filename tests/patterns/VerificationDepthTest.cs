// TRAP: The agent, told to "verify the guardrails", built gates for gates, then
// tests for those gates, then mutation runs over those tests — no level backed
// by an incident; caught only by human review (round 29 in the observed case).
// Meta-work is always green, so nothing inside the loop ever fails.
// GUARDRAIL: Mechanical detection of the recursion's artifacts: a test project
// must not reference another test project (tests over gates), and mutation
// configs must not target test assemblies (mutation over gate tests). The one
// legitimate meta-level (e.g., analyzer tests) goes through the allowlist with
// a decision ID — see docs/traps/agent-behavior.md#gates-for-gates.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...)
// - xUnit:  [Fact] + Assert.True(...)
// - NUnit:  [Test] + Assert.That(...)
// - MSTest: [TestMethod] + Assert.IsTrue(...)

using System.Text.Json;
using System.Xml.Linq;
using TUnit;

namespace Tests.Patterns;

public class VerificationDepthTest
{
    // The documented base case: the control plane itself may be tested ONE level
    // deep, after a real incident (an observed false green), with a decision ID
    // in the registry. Format: "TestProject -> ReferencedTestProject".
    private static readonly string[] MetaVerificationAllowlist =
    [
        // "MyProject.Analyzers.Tests -> MyProject.Gates.Tests  // AUD-031: analyzer fixtures live in the gate project (false green, 2026-09-14)"
    ];

    // A project counts as a test project by framework package or by the SDK's
    // IsTestProject property — adapt if your layout differs.
    private static readonly string[] TestFrameworkPackages =
    [
        "Microsoft.NET.Test.Sdk", "TUnit", "xunit", "NUnit", "MSTest", "Microsoft.Testing.Platform"
    ];

    // TRAP: Level 3 of the recursion — unit tests whose subject is a gate, in a
    // test project referencing another test project. The diff reads as diligence
    // ("tests for our gates") and is green by construction.
    // GUARDRAIL: No test project may reference another test project unless the
    // pair is allowlisted with a decision ID.
    [Test]
    public async Task TestProjects_ShouldNotReference_OtherTestProjects()
    {
        var solutionRoot = FindSolutionRoot();
        if (solutionRoot is null)
            Assert.Fail("Solution root not found");

        var projects = Directory.GetFiles(solutionRoot!, "*.csproj", SearchOption.AllDirectories);
        var offenders = new List<string>();

        foreach (var proj in projects)
        {
            if (!IsTestProject(proj))
                continue;

            foreach (var referenced in GetProjectReferences(proj))
            {
                if (!IsTestProject(referenced))
                    continue;

                var key = $"{Path.GetFileNameWithoutExtension(proj)} -> {Path.GetFileNameWithoutExtension(referenced)}";
                if (!MetaVerificationAllowlist.Contains(key, StringComparer.OrdinalIgnoreCase))
                    offenders.Add(key);
            }
        }

        await Assert.That(offenders).IsEmpty()
            .Because($"Test-over-test references (verification recursion, level 3): {string.Join(" | ", offenders)}. " +
                     "If this is the documented base case, add the pair to MetaVerificationAllowlist with a decision ID.");
    }

    // TRAP: Level 4 of the recursion — the mutation run quietly points at a test
    // assembly (the tests of the gates). The run is long, green, and reports
    // "improved fault sensitivity" while mutating nothing of value.
    // GUARDRAIL: Stryker configs must target product assemblies only.
    // NOTE: covers the config-file route (stryker-config.json). CLI-only runs
    //       (`dotnet stryker --project <test-project>`) are invisible to a static
    //       check — catch those in review (code-review checklist, Gates for Gates).
    [Test]
    public async Task StrykerConfigs_ShouldNotTarget_TestAssemblies()
    {
        var solutionRoot = FindSolutionRoot();
        if (solutionRoot is null)
            Assert.Fail("Solution root not found");

        var projects = Directory.GetFiles(solutionRoot!, "*.csproj", SearchOption.AllDirectories)
            .ToDictionary(p => Path.GetFileNameWithoutExtension(p)!, StringComparer.OrdinalIgnoreCase);
        var configs = Directory.GetFiles(solutionRoot!, "stryker-config.json", SearchOption.AllDirectories);
        var offenders = new List<string>();

        foreach (var config in configs)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(config));
            if (!doc.RootElement.TryGetProperty("stryker-core", out var core)
                || !core.TryGetProperty("project", out var projectProp)
                || projectProp.ValueKind != JsonValueKind.String)
                continue;

            var targetName = Path.GetFileNameWithoutExtension(projectProp.GetString()!);
            if (projects.TryGetValue(targetName, out var csproj) && IsTestProject(csproj))
                offenders.Add($"{Path.GetFileName(config)} -> {targetName}");
        }

        await Assert.That(offenders).IsEmpty()
            .Because($"Mutation configs targeting test assemblies (verification recursion, level 4): {string.Join(" | ", offenders)}. " +
                     "Mutation testing belongs to product test suites.");
    }

    // TRAP: A test class named FooGateTests whose subject "FooGate" is itself a
    // test class — the subject of the test is a test.
    // GUARDRAIL: By naming convention XxxTests -> Xxx, the subject must resolve
    // to a production type, not to another test class.
    // NOTE: Uncomment and adapt to your assembly and naming convention.
    [Test]
    public async Task TestClasses_ShouldNotHave_TestsAsTheirSubject()
    {
        // Adaptation:
        // var testAssembly = typeof(YourTestsMarkerClass).Assembly;
        // var testClassNames = testAssembly.GetTypes()
        //     .Where(t => t.GetCustomAttributes().Any(a => a.GetType().Name.StartsWith("Test")))
        //     .Select(t => t.Name)
        //     .ToHashSet(StringComparer.Ordinal);
        // var offenders = testAssembly.GetTypes()
        //     .Where(t => t.Name.EndsWith("Tests", StringComparison.Ordinal))
        //     .Where(t => t.Name.Length > "Tests".Length)
        //     .Select(t => t.Name[..^"Tests".Length])
        //     .Where(testClassNames.Contains)
        //     .ToList();
        //
        // await Assert.That(offenders).IsEmpty()
        //     .Because($"Tests whose subject is another test: {string.Join(" | ", offenders)}");

        await Assert.That(true).IsTrue()
            .Because("Template: adapt this test to your assembly and naming convention. See commented code.");
    }

    // --- Helpers ---

    private static bool IsTestProject(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var isTestProject = doc.Descendants(ns + "IsTestProject")
            .Any(e => string.Equals(e.Value, "true", StringComparison.OrdinalIgnoreCase));
        if (isTestProject)
            return true;

        return doc.Descendants(ns + "PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Any(include => include is not null
                && TestFrameworkPackages.Any(p => include.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> GetProjectReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var baseDir = Path.GetDirectoryName(csprojPath)!;

        foreach (var reference in doc.Descendants(ns + "ProjectReference"))
        {
            var include = reference.Attribute("Include")?.Value;
            if (!string.IsNullOrEmpty(include))
                yield return Path.GetFullPath(Path.Combine(baseDir, include));
        }
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
}
