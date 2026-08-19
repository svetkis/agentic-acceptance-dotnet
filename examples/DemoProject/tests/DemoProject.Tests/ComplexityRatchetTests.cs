// GUARDRAIL: The number of methods violating S3776/S1541 does not grow.
// This file is a working adaptation of the template from tests/patterns/ComplexityRatchetTest.cs

using System.Diagnostics;
using System.Text.RegularExpressions;
using TUnit;

namespace DemoProject.Tests;

public class ComplexityRatchetTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public async Task SonarComplexityViolations_ShouldNotIncrease()
    {
        var violationCount = CountSonarComplexityViolations(RepoRoot);
        var baseline = GetBaselineOrSet(violationCount, "complexity-baseline.txt");

        await Assert.That(violationCount)
            .IsLessThanOrEqualTo(baseline)
            .Because("Cognitive/cyclomatic complexity violations must not increase.");
    }

    private static int CountSonarComplexityViolations(string rootDir)
    {
        var solutionFile = Directory.GetFiles(rootDir, "*.sln", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(rootDir, "*.slnx", SearchOption.TopDirectoryOnly))
            .FirstOrDefault();

        if (solutionFile == null)
            return 0;

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{solutionFile}\" -warnaserror:false -clp:NoSummary",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = rootDir
        };

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var combinedOutput = $"{output}{Environment.NewLine}{error}";
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Complexity scan failed for '{solutionFile}'.{Environment.NewLine}{combinedOutput}");

        return combinedOutput
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(IsProductionDiagnosticLine)
            .Distinct(StringComparer.Ordinal)
            .Count(line => Regex.IsMatch(line, @"\b(S3776|S1541)\b"));
    }

    private static bool IsProductionDiagnosticLine(string line)
    {
        return Regex.IsMatch(line, @"\b(S3776|S1541)\b")
            && line.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetBaselineOrSet(int current, string baselineFile)
    {
        var path = Path.Combine(RepoRoot, baselineFile);
        if (File.Exists(path) && int.TryParse(File.ReadAllText(path), out var baseline))
            return baseline;

        File.WriteAllText(path, current.ToString());
        return current;
    }
}
