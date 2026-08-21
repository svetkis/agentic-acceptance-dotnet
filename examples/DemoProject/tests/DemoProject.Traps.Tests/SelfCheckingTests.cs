// TRAP: Non-validating tests stay green while proving nothing. This test asserts
// that the analyzer produces exactly the SAE006-SAE009 diagnostic set for a
// snippet that contains zero-assert, null-only, bypassed and tautological tests.
// If the analyzer misses one pattern or adds a false positive, the assertion
// fails and the trap is caught at test time.
// GUARDRAIL: SAE006-SAE009 (NonValidatingTestAnalyzer) detect these patterns
// before the test run, not after a regression slips through.

using System.Collections.Immutable;
using DemoProject.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit;

namespace DemoProject.Traps.Tests;

public class SelfCheckingTests
{
    [Test]
    public async Task NonValidatingTests_ProduceExactDiagnosticSet()
    {
        const string code = """
            using System;
            using System.Threading.Tasks;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsNotNull() { }
                    public void IsEqualTo(object? expected) { }
                    public void IsTrue() { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public async Task ZeroAssertTest()
                {
                    var payment = new object();
                    await Task.CompletedTask;
                }

                [Test]
                public void NullOnlyTest()
                {
                    var payment = new object();
                    Assert.That(payment).IsNotNull();
                }

                [Test]
                public void BypassedAssertionTest()
                {
                    var payment = new object();
                    var flag = true;
                    if (flag)
                    {
                        Assert.That(payment).IsEqualTo(1);
                    }
                }

                [Test]
                public void TautologicalAssertionTest()
                {
                    var payment = new object();
                    Assert.That(payment).IsEqualTo(payment);
                    Assert.That(true).IsTrue();
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);
        var ids = diagnostics.Select(d => d.Id).OrderBy(id => id).ToArray();

        // TRAP: this assertion is intentionally strict. The snippet above contains
        // exactly one SAE006, one SAE007, one SAE008 and two SAE009 diagnostics.
        // If the analyzer implementation drifts, the test fails.
        await Assert.That(ids)
            .IsEquivalentTo(new[]
            {
                NonValidatingTestAnalyzer.MustAssertDiagnosticId,
                NonValidatingTestAnalyzer.NullOnlyDiagnosticId,
                NonValidatingTestAnalyzer.BypassedDiagnosticId,
                NonValidatingTestAnalyzer.TautologicalDiagnosticId,
                NonValidatingTestAnalyzer.TautologicalDiagnosticId,
            })
            .Because("The analyzer must report exactly the SAE006-SAE009 set for the trap fixture.");
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunAnalyzerAsync<T>(string sourceCode)
        where T : DiagnosticAnalyzer, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            assemblyName: "TrapAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: new[] { MetadataReference.CreateFromFile(GetSystemRuntimeReference()) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationErrors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (compilationErrors.Count > 0)
            throw new InvalidOperationException($"Trap analyzer test input failed to compile: {string.Join(Environment.NewLine, compilationErrors)}");

        var analyzer = new T();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static string GetSystemRuntimeReference()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var version = Path.GetFileName(runtimeDir);
        var dotnetRoot = Directory.GetParent(Directory.GetParent(runtimeDir)!.Parent!.FullName)!.FullName;

        var refAssemblyPath = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", version, "ref", "net10.0", "System.Runtime.dll");
        if (File.Exists(refAssemblyPath))
            return refAssemblyPath;

        throw new InvalidOperationException($"Could not locate System.Runtime.dll reference assembly. Expected: {refAssemblyPath}");
    }
}
