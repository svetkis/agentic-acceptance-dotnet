// GUARDRAIL: Custom Roslyn analyzers have positive/negative unit tests.
// This file is a working adaptation of the template from tests/patterns/AnalyzerTests.cs

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DemoProject.Analyzers;
using DemoProject.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit;

namespace DemoProject.Tests;

public class AnalyzerTests
{
    [Test]
    public async Task StrongTypedIdAnalyzer_FlagsPrimitiveIdInDomainEntity()
    {
        const string code = """
            namespace DemoProject.Domain;

            public class Booking
            {
                public long Id { get; set; }
            }
            """;

        await AssertSingleDiagnosticAsync(code, StronglyTypedIdAnalyzer.PropertyDiagnosticId, "long");
    }

    [Test]
    public async Task StrongTypedIdAnalyzer_FlagsRawGuidParameterInDomainMethod()
    {
        const string code = """
            using System;

            namespace DemoProject.Domain;

            public class BookingService
            {
                public void Load(Guid bookingId)
                {
                }
            }
            """;

        await AssertSingleDiagnosticAsync(code, StronglyTypedIdAnalyzer.ParameterDiagnosticId, "Guid");
    }

    [Test]
    public async Task StrongTypedIdAnalyzer_IgnoresTypedId()
    {
        const string code = """
            namespace DemoProject.Domain;

            public readonly record struct BookingId(long Value);

            public class Booking
            {
                public BookingId Id { get; set; }
            }
            """;

        await AssertNoAnalyzerDiagnosticsAsync(code);
    }

    [Test]
    public async Task StrongTypedIdAnalyzer_IgnoresTypedIdParameter()
    {
        const string code = """
            using System;

            namespace DemoProject.Domain;

            public readonly record struct BookingId(Guid Value);

            public class BookingService
            {
                public void Load(BookingId bookingId)
                {
                }
            }
            """;

        await AssertNoAnalyzerDiagnosticsAsync(code);
    }

    [Test]
    public async Task HotPathAnalyzer_FlagsNewAllocationInHotPath()
    {
        const string code = """
            using System;
            using DemoProject.Domain;

            namespace DemoProject.Application;

            public class HotPathService
            {
                [HotPath]
                public byte[] Process()
                {
                    return new byte[1024];
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<HotPathAnalyzer>(code,
            MetadataReference.CreateFromFile(typeof(HotPathAttribute).Assembly.Location));

        await Assert.That(diagnostics)
            .Contains(d => d.Id == HotPathAnalyzer.NewDiagnosticId)
            .Because("`new` allocation in a [HotPath] method must trigger SAE003.");
    }

    [Test]
    public async Task HotPathAnalyzer_FlagsAsyncStateMachineInHotPath()
    {
        const string code = """
            using System.Threading.Tasks;
            using DemoProject.Domain;

            namespace DemoProject.Application;

            public class HotPathService
            {
                [HotPath]
                public async Task<int> ProcessAsync()
                {
                    return await Task.FromResult(42);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<HotPathAnalyzer>(code,
            MetadataReference.CreateFromFile(typeof(HotPathAttribute).Assembly.Location));

        await Assert.That(diagnostics)
            .Contains(d => d.Id == HotPathAnalyzer.AsyncDiagnosticId)
            .Because("`async` in a [HotPath] method must trigger SAE004.");
    }

    [Test]
    public async Task HotPathAnalyzer_IgnoresNewOutsideHotPath()
    {
        const string code = """
            using System;
            using DemoProject.Domain;

            namespace DemoProject.Application;

            public class RegularService
            {
                public byte[] Process()
                {
                    return new byte[1024];
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<HotPathAnalyzer>(code,
            MetadataReference.CreateFromFile(typeof(HotPathAttribute).Assembly.Location));

        await Assert.That(diagnostics)
            .IsEmpty()
            .Because("Allocations outside [HotPath] methods must not trigger HotPathAnalyzer diagnostics.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsZeroAssertTest()
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
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public async Task PaymentIsProcessed()
                {
                    var payment = new object();
                    await Task.CompletedTask;
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.MustAssertDiagnosticId)
            .Because("A test with no assertion must trigger SAE006.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsNullOnlyAssertion()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsNotNull() { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    Assert.That(payment).IsNotNull();
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.NullOnlyDiagnosticId)
            .Because("A test that only checks non-null must trigger SAE007.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsBypassedAssertion()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    var flag = true;
                    if (flag)
                    {
                        Assert.That(payment).IsEqualTo(1);
                    }
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("An assertion inside an unconditional `if` branch can be bypassed on the green path; must trigger SAE008.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsTautologicalAssertion()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    Assert.That(payment).IsEqualTo(payment);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.TautologicalDiagnosticId)
            .Because("An assertion comparing a value to itself must trigger SAE009.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_IgnoresValidTest()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                    public void IsNotNull() { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    Assert.That(payment).IsNotNull();
                    Assert.That(payment).IsEqualTo(42);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .IsEmpty()
            .Because("A valid self-checking test with guaranteed, non-tautological assertions must not trigger diagnostics.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_IgnoresBothBranchesAsserting()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    var flag = true;
                    if (flag)
                    {
                        Assert.That(payment).IsEqualTo(1);
                    }
                    else
                    {
                        Assert.That(payment).IsEqualTo(2);
                    }
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .DoesNotContain(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("When both if-branches assert, every path reaches a check.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_DoesNotFlagLiteralLiteralWhenValuesDiffer()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    Assert.That(1).IsEqualTo(2);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .DoesNotContain(d => d.Id == NonValidatingTestAnalyzer.TautologicalDiagnosticId)
            .Because("Assert.That(1).IsEqualTo(2) is a failing assertion, not a tautology.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsAssertionInsideUncalledLambda()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    Action check = () => Assert.That(payment).IsEqualTo(1);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.MustAssertDiagnosticId)
            .Because("An assertion inside an uncalled lambda does not validate the test.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsAssertionInsideUncalledLocalFunction()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    void Check() => Assert.That(payment).IsEqualTo(1);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.MustAssertDiagnosticId)
            .Because("An assertion inside an uncalled local function does not validate the test.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsEarlyReturnBeforeAssertion()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    var flag = true;
                    if (flag)
                    {
                        return;
                    }
                    Assert.That(payment).IsEqualTo(1);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("An early return before the assertion creates a green path without verification.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsTryCatchWithoutAssertionInCatch()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    try
                    {
                        Assert.That(payment).IsEqualTo(1);
                    }
                    catch (Exception)
                    {
                        // no assertion here
                    }
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("A catch block without assertion swallows the failure path.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsTryCatchWithEarlyReturnInTry()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                    public void IsNotNull() { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    var condition = true;
                    try
                    {
                        if (condition)
                        {
                            return;
                        }
                        Assert.That(payment).IsEqualTo(1);
                    }
                    catch (Exception)
                    {
                        Assert.That(payment).IsNotNull();
                    }
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("An early return inside try creates a green path without assertion.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsSwitchWithoutDefaultAssertion()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public static class Assert
            {
                public static AssertBuilder That(object? value) => new();
                public class AssertBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    var code = 1;
                    switch (code)
                    {
                        case 1:
                            Assert.That(payment).IsEqualTo(1);
                            break;
                    }
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("A switch without a default asserting branch can bypass verification.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_FlagsAsyncBypassViaEarlyReturn()
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
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public async Task PaymentReturnsAmount()
                {
                    var payment = new object();
                    var flag = true;
                    if (flag)
                    {
                        return;
                    }
                    await Task.Yield();
                    Assert.That(payment).IsEqualTo(1);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .Contains(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("An async early return before the assertion creates a green path without verification.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_IgnoresAssertionBeforeAwait()
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
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public async Task PaymentReturnsAmount()
                {
                    var payment = new object();
                    Assert.That(payment).IsEqualTo(1);
                    await Task.Yield();
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .DoesNotContain(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("An assertion before await is reached on every successful path.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_IgnoresAssertionAfterAwait()
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
                    public void IsEqualTo(object? expected) { }
                }
            }

            public class PaymentServiceTests
            {
                [Test]
                public async Task PaymentReturnsAmount()
                {
                    var payment = new object();
                    await Task.Yield();
                    Assert.That(payment).IsEqualTo(1);
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .DoesNotContain(d => d.Id == NonValidatingTestAnalyzer.BypassedDiagnosticId)
            .Because("An assertion after await is reached on every successful path.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_UsesAdditionalAssertionPrefixesFromConfig()
    {
        const string code = """
            using System;

            [AttributeUsage(AttributeTargets.Method)]
            public class TestAttribute : Attribute { }

            public class PaymentServiceTests
            {
                [Test]
                public void PaymentReturnsAmount()
                {
                    var payment = new object();
                    CheckThat(payment).IsEqualTo(1);
                }

                private static CheckBuilder CheckThat(object? value) => new();

                public class CheckBuilder
                {
                    public void IsEqualTo(object? expected) { }
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["dotnet_diagnostic.SAE006.additional_assertion_prefixes"] = "Check"
        };

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code, options);

        await Assert.That(diagnostics)
            .DoesNotContain(d => d.Id == NonValidatingTestAnalyzer.MustAssertDiagnosticId)
            .Because("Custom assertion prefixes from config must be recognized.");
    }

    [Test]
    public async Task NonValidatingTestAnalyzer_IgnoresNonTestMethod()
    {
        const string code = """
            using System;

            public class PaymentServiceTests
            {
                public void JustAMethod()
                {
                    var payment = new object();
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync<NonValidatingTestAnalyzer>(code);

        await Assert.That(diagnostics)
            .IsEmpty()
            .Because("Only methods decorated with a test attribute are analyzed.");
    }

    private static async Task AssertSingleDiagnosticAsync(string sourceCode, string expectedId, string expectedSnippet)
    {
        var diagnostics = await RunAnalyzerAsync(sourceCode);

        await Assert.That(diagnostics.Count)
            .IsEqualTo(1)
            .Because("The positive analyzer case should produce exactly one diagnostic.");

        var diagnostic = diagnostics.Single();
        await Assert.That(diagnostic.Id)
            .IsEqualTo(expectedId)
            .Because("The analyzer should report the expected diagnostic ID.");

        var expectedLocation = FindSnippetLocation(sourceCode, expectedSnippet);
        var actualLocation = diagnostic.Location.GetLineSpan().StartLinePosition;

        await Assert.That(actualLocation.Line)
            .IsEqualTo(expectedLocation.Line)
            .Because("The diagnostic should point to the exact line of the offending syntax.");

        await Assert.That(actualLocation.Character)
            .IsEqualTo(expectedLocation.Character)
            .Because("The diagnostic should point to the exact column of the offending syntax.");
    }

    private static async Task AssertNoAnalyzerDiagnosticsAsync(string sourceCode)
    {
        var diagnostics = await RunAnalyzerAsync(sourceCode);

        await Assert.That(diagnostics)
            .IsEmpty()
            .Because("Valid strongly typed ID usage must not trigger analyzer diagnostics.");
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunAnalyzerAsync(string sourceCode)
    {
        return await RunAnalyzerAsync<StronglyTypedIdAnalyzer>(sourceCode);
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunAnalyzerAsync<T>(string sourceCode, params MetadataReference[] additionalReferences)
        where T : DiagnosticAnalyzer, new()
    {
        return await RunAnalyzerAsync<T>(sourceCode, new Dictionary<string, string>(), additionalReferences);
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunAnalyzerAsync<T>(
        string sourceCode,
        IReadOnlyDictionary<string, string> analyzerConfigOptions,
        params MetadataReference[] additionalReferences)
        where T : DiagnosticAnalyzer, new()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(GetSystemRuntimeReference())
        };
        references.AddRange(additionalReferences);

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationErrors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (compilationErrors.Count > 0)
            throw new InvalidOperationException($"Analyzer test input failed to compile: {string.Join(Environment.NewLine, compilationErrors)}");

        var analyzer = new T();
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions);
        var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, optionsProvider);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
            analyzerOptions);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static string GetSystemRuntimeReference()
    {
        // Reference assemblies live in the .NET SDK packs directory.
        // The runtime assembly (System.Private.CoreLib) points to the shared runtime folder,
        // from which we can reach the corresponding ref assembly in packs.
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var version = Path.GetFileName(runtimeDir); // e.g. "10.0.8"
        var dotnetRoot = Directory.GetParent(Directory.GetParent(runtimeDir)!.Parent!.FullName)!.FullName;

        var refAssemblyPath = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", version, "ref", "net10.0", "System.Runtime.dll");
        if (File.Exists(refAssemblyPath))
            return refAssemblyPath;

        throw new InvalidOperationException($"Could not locate System.Runtime.dll reference assembly. Expected: {refAssemblyPath}");
    }

    private static (int Line, int Character) FindSnippetLocation(string sourceCode, string snippet)
    {
        var snippetIndex = sourceCode.IndexOf(snippet, StringComparison.Ordinal);
        if (snippetIndex < 0)
            throw new InvalidOperationException($"Could not find snippet '{snippet}' in analyzer test source.");

        var line = 0;
        var character = 0;

        for (var i = 0; i < snippetIndex; i++)
        {
            if (sourceCode[i] == '\n')
            {
                line++;
                character = 0;
            }
            else if (sourceCode[i] != '\r')
            {
                character++;
            }
        }

        return (line, character);
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options;

        public TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> options)
        {
            _options = new TestAnalyzerConfigOptions(options);
        }

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _options;

        public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return _options.TryGetValue(key, out value);
        }
    }
}
