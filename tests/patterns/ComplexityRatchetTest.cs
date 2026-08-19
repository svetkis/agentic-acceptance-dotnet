// TRAP: The agent keeps adding branches and nesting to a method, turning it into an unreadable tangle.
// GUARDRAIL: The number of methods violating S3776 (cognitive) / S1541 (cyclomatic) does not grow.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...)
// - xUnit:  [Fact] + Assert.True(...)
// - NUnit:  [Test] + Assert.That(...)
// - MSTest: [TestMethod] + Assert.IsTrue(...)
//
// NOTE: This pattern prefers SonarAnalyzer.CSharp (S3776/S1541) as the complexity source.
//       An alternative is Microsoft.CodeAnalysis.Metrics, but it is slower and requires extra packages.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit;

namespace Tests.Patterns;

public class ComplexityRatchetTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    // TRAP: The agent wrote a method with 5 levels of if/switch/foreach nesting.
    // GUARDRAIL: We pin the violation baseline and do not let it grow.
    [Test]
    public void SonarComplexityViolations_ShouldNotIncrease()
    {
        var violationCount = CountSonarComplexityViolations(RepoRoot);
        var baseline = GetBaselineOrSet(violationCount, "complexity-baseline.txt");

        Assert.That(violationCount)
            .IsLessThanOrEqualTo(baseline)
            .Because($"Cognitive/cyclomatic complexity violations must not increase. " +
                     $"Current={violationCount}, Baseline={baseline}. Refactor the new method or update the baseline consciously.");
    }

    // TRAP: In a legacy project there are too many violations, and the ratchet does not work.
    // GUARDRAIL: We verify that the worst methods did not get more complex (top-10 hotspots ratchet).
    [Test]
    public void TopHotspotComplexity_ShouldNotIncrease()
    {
        var hotspots = GetTopComplexityHotspots(RepoRoot, topN: 10);
        var currentMax = hotspots.Any() ? hotspots.Max(h => h.Complexity) : 0;
        var baselineMax = GetBaselineOrSet(currentMax, "complexity-hotspot-baseline.txt");

        Assert.That(currentMax)
            .IsLessThanOrEqualTo(baselineMax)
            .Because("Max complexity of top hotspots must not increase. " +
                     "CurrentMax={0}, BaselineMax={1}", currentMax, baselineMax);
    }

    // --- Helpers ---

    private static int CountSonarComplexityViolations(string rootDir)
    {
        // Build with SonarAnalyzer and capture S3776/S1541 warnings.
        // Requires SonarAnalyzer.CSharp package and TreatWarningsAsErrors=false for this scan.
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

    private static IReadOnlyList<Hotspot> GetTopComplexityHotspots(string rootDir, int topN)
    {
        var files = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionSourceFile);

        var hotspots = new List<Hotspot>();

        foreach (var file in files)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = tree.GetRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var complexity = CalculateCyclomaticComplexity(method);
                var line = tree.GetLineSpan(method.Identifier.Span).StartLinePosition.Line + 1;
                hotspots.Add(new Hotspot($"{file}:{line} {method.Identifier.Text}", complexity));
            }
        }

        return hotspots
            .OrderByDescending(h => h.Complexity)
            .ThenBy(h => h.MethodName, StringComparer.Ordinal)
            .Take(topN)
            .ToList();
    }

    private static int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        var decisionPoints = method.DescendantNodes()
            .Count(node => node is IfStatementSyntax
                or WhileStatementSyntax
                or ForStatementSyntax
                or ForEachStatementSyntax
                or CaseSwitchLabelSyntax
                or CatchClauseSyntax
                or ConditionalExpressionSyntax);

        var logicalOperators = method.DescendantTokens()
            .Count(t => t.RawKind == (int)SyntaxKind.AmpersandAmpersandToken
                || t.RawKind == (int)SyntaxKind.BarBarToken);

        return 1 + decisionPoints + logicalOperators;
    }

    private static bool IsProductionDiagnosticLine(string line)
    {
        return Regex.IsMatch(line, @"\b(S3776|S1541)\b")
            && !Regex.IsMatch(line, @"[/\\](tests?|test)[/\\]", RegexOptions.IgnoreCase);
    }

    private static bool IsProductionSourceFile(string filePath)
    {
        if (filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            return false;

        return !Regex.IsMatch(filePath, @"[/\\](tests?|test)[/\\]", RegexOptions.IgnoreCase);
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

    private readonly record struct Hotspot(string MethodName, int Complexity);
}
