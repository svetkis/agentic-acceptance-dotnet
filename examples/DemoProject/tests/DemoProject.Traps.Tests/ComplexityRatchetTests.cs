// GUARDRAIL: Methods with cyclomatic complexity above the threshold do not pass the ratchet.
// This project is a failing demo: the method ComplexityHotspot.Calculate intentionally
// violates the threshold, so the test MUST fail.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit;

namespace DemoProject.Traps.Tests;

public class ComplexityRatchetTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public async Task CyclomaticComplexity_ShouldNotExceedThreshold()
    {
        var violations = ScanCyclomaticComplexity(RepoRoot, threshold: 3);

        await Assert.That(violations).IsEmpty()
            .Because($"Methods with cyclomatic complexity > 3 must be refactored. Violations: {string.Join(", ", violations)}");
    }

    private static IReadOnlyList<string> ScanCyclomaticComplexity(string rootDir, int threshold)
    {
        var violations = new List<string>();
        var files = Directory.GetFiles(Path.Combine(rootDir, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("tests", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = tree.GetRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var complexity = CalculateCyclomaticComplexity(method);
                if (complexity > threshold)
                {
                    var line = tree.GetLineSpan(method.Identifier.Span).StartLinePosition.Line + 1;
                    violations.Add($"{file}:{line} {method.Identifier.Text} (complexity {complexity})");
                }
            }
        }

        return violations;
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
            .Count(t => t.IsKind(SyntaxKind.AmpersandAmpersandToken) || t.IsKind(SyntaxKind.BarBarToken));

        return 1 + decisionPoints + logicalOperators;
    }
}
