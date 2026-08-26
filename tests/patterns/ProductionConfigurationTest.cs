// TRAP: Production configuration breaks silently — a Dockerfile env var, a GC limit,
//       a connection string default. No code path exercises it, so no unit test fails;
//       the incident is discovered in production.
// GUARDRAIL: Configuration is code — assert it in tests. Files read from the repo root,
//       exact strings checked, regression named BUG_CONFIG### like any other bug.
//
// Real-world example this pattern comes from: .NET GC env vars
// (DOTNET_GCHeapHardLimitPercent / DOTNET_GCHighMemPercent) are parsed as HEXADECIMAL.
// Writing "75" means 0x75 = 117%. The container silently gets a wrong GC budget.
// A test is the only thing that catches it — the value never executes in a test run.
//
// Framework adaptation:
// - TUnit:  [Test], [Arguments(...)] + FluentAssertions
// - xUnit:  [Theory], [InlineData(...)] + Assert.Contains / Assert.DoesNotContain
// - NUnit:  [Test], [TestCase(...)] + Assert.Contains / Assert.DoesNotContain
// - MSTest: [TestMethod], [DataRow(...)] + StringAssert.Contains

using TUnit;
using FluentAssertions;

namespace Tests.Patterns;

public class ProductionConfigurationTest
{
    // TRAP: The agent "fixed" the GC limit to decimal 75, or a refactor dropped it.
    // GUARDRAIL: The exact expected strings are pinned; decimal form is asserted absent.
    [Test]
    [Arguments("src/MyApp.Api/Dockerfile")]
    [Arguments("src/MyApp.Worker/Dockerfile")]
    public void BUG_CONFIG001_GcPercentEnvironmentVariables_ShouldUseHexadecimal75Percent(
        string relativePath)
    {
        var repositoryRoot = FindRepositoryRoot();
        var dockerfile = File.ReadAllText(Path.Combine(repositoryRoot, relativePath));

        dockerfile.Should().Contain("ENV DOTNET_GCHeapHardLimitPercent=0x4B");
        dockerfile.Should().Contain("ENV DOTNET_GCHighMemPercent=0x4B");
        dockerfile.Should().NotContain("ENV DOTNET_GCHeapHardLimitPercent=75");
        dockerfile.Should().NotContain("ENV DOTNET_GCHighMemPercent=75");
    }

    // TRAP: An env var was added to code but not to the deployment manifest (or vice versa).
    // GUARDRAIL: The set of env vars the code reads matches the set deployment provides.
    // NOTE: Adapt the extraction to your stack: regex over appsettings / compose / Dockerfile.
    //       The simple list-parsing below misses compose map syntax (`KEY: value`) and
    //       catches non-env list items (volumes) — for real projects use a YAML parser.
    [Test]
    public void BUG_CONFIG002_EnvVars_InDeployment_ShouldBeReadByCode()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manifestVars = ExtractEnvVarNames(
            Path.Combine(repositoryRoot, "deploy", "docker-compose.yml"));
        var codeVars = ExtractConfiguredKeys(Path.Combine(repositoryRoot, "src"));

        codeVars.Should().BeSubsetOf(
            manifestVars,
            because: "an env var the code reads but deployment never sets " +
                     "fails only in production");
    }

    // --- Helpers ---

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
            directory = directory.Parent!;
        return directory;
    }

    private static IEnumerable<string> ExtractEnvVarNames(string composePath) =>
        File.Exists(composePath)
            ? File.ReadAllLines(composePath)
                .Select(l => l.Trim())
                .Where(l => l.StartsWith("- ", StringComparison.Ordinal))
                .Select(l => l[2..].Split(':')[0].Split('=')[0])
                .Distinct()
            : [];

    private static IEnumerable<string> ExtractConfiguredKeys(string srcPath) =>
        Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .SelectMany(File.ReadAllLines)
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("Environment.GetEnvironmentVariable(\"", StringComparison.Ordinal))
            .Select(l => l["Environment.GetEnvironmentVariable(\"".Length..].Split('\"')[0])
            .Distinct();
}
