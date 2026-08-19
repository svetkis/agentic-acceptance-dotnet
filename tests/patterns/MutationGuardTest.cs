// TRAP: Tests pass but do not verify logic — mutants survive, and bugs leak into production.
// GUARDRAIL: Stryker.NET runs before release; the mutation score does not drop.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...)
// - xUnit:  [Fact] + Assert.True(...)
// - NUnit:  [Test] + Assert.That(...)
// - MSTest: [TestMethod] + Assert.IsTrue(...)
//
// NOTE: As of 2026-06, Stryker.NET does not support TUnit / Microsoft Testing Platform.
//       Use this pattern as a periodic audit or a CI job via the project's dotnet test.

using System.Diagnostics;
using System.Text.Json;
using TUnit;

namespace Tests.Patterns;

public class MutationGuardTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    // TRAP: Line coverage is high, but asserts are weak — mutants survive.
    // GUARDRAIL: The mutation score for a critical assembly is >= baseline (e.g., 70%).
    [Test]
    public void StrykerMutationScore_ShouldMeetBaseline()
    {
        var score = RunStrykerAndGetScore(RepoRoot, targetProject: "src/YourProject.Domain/YourProject.Domain.csproj");
        var baseline = GetBaselineOrSet((int)(score * 100), "mutation-score-baseline.txt");
        var current = (int)(score * 100);

        Assert.That(current)
            .IsGreaterThanOrEqualTo(baseline)
            .Because("Mutation score must not decrease. Current={0}%, Baseline={1}%", current, baseline);
    }

    // --- Helpers ---

    private static double RunStrykerAndGetScore(string rootDir, string targetProject)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"stryker --project {targetProject} --break-at 0",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = rootDir
        };

        try
        {
            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse mutation score from Stryker output or report.
            // In real setup, read StrykerOutput/{timestamp}/reports/mutation-report.json
            var reportPath = Directory.GetFiles(Path.Combine(rootDir, "StrykerOutput"), "mutation-report.json", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTime)
                .FirstOrDefault();

            if (reportPath == null)
                return 0;

            var json = File.ReadAllText(reportPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("mutationScore", out var scoreProp) && scoreProp.TryGetDouble(out var score))
                return score / 100.0;

            return 0;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Stryker is not installed or failed: {ex.Message}");
            return 0;
        }
    }

    private static int GetBaselineOrSet(int current, string baselineFile)
    {
        var path = Path.Combine(RepoRoot, baselineFile);
        if (File.Exists(path) && int.TryParse(File.ReadAllText(path), out var baseline))
            return baseline;

        File.WriteAllText(path, current.ToString());
        return current;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.Exists(Path.Combine(dir, ".git"))
                || Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Any()
                || Directory.GetFiles(dir, "*.slnx", SearchOption.TopDirectoryOnly).Any())
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find repository root. Ensure .git, .sln or .slnx exists.");
    }
}
