using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DemoProject.Analyzers;

// TRAP: A test compiles and goes green but does not verify the promised behavior:
//       zero-assert, IsNotNull()-only, assertion inside if, tautology.
// GUARDRAIL: Analyzers SAE006-SAE009 catch non-validating tests at compile
//            time, before any run. Reduces fault sensitivity risk in CI.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NonValidatingTestAnalyzer : DiagnosticAnalyzer
{
    public const string MustAssertDiagnosticId = "SAE006";
    public const string NullOnlyDiagnosticId = "SAE007";
    public const string BypassedDiagnosticId = "SAE008";
    public const string TautologicalDiagnosticId = "SAE009";

    private const string Category = "Design";

    private static readonly DiagnosticDescriptor MustAssertRule = new(
        id: MustAssertDiagnosticId,
        title: "Test method must contain an assertion",
        messageFormat: "Test method '{0}' does not contain any assertion or verification",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A test that never asserts proves nothing. Add an assertion that checks an observable postcondition.");

    private static readonly DiagnosticDescriptor NullOnlyRule = new(
        id: NullOnlyDiagnosticId,
        title: "Test asserts only non-null",
        messageFormat: "Test method '{0}' checks only null / not-null; it cannot verify the concrete behavior promised by its name",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Non-null is too weak when the test name promises a concrete value, state, or effect.");

    private static readonly DiagnosticDescriptor BypassedRule = new(
        id: BypassedDiagnosticId,
        title: "Assertion can be bypassed on the successful path",
        messageFormat: "Test method '{0}' has assertions that are not guaranteed to execute on every successful path",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An assertion inside a conditional branch that may be skipped does not verify the behavior on the green path.");

    private static readonly DiagnosticDescriptor TautologicalRule = new(
        id: TautologicalDiagnosticId,
        title: "Assertion is tautological",
        messageFormat: "Assertion cannot fail by construction",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Tautological assertions (e.g., x == x, literal true) give false safety.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MustAssertRule, NullOnlyRule, BypassedRule, TautologicalRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static IEnumerable<InvocationExpressionSyntax> FilterAssertionChains(IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        var innerAssertionSpans = new HashSet<TextSpan>(assertions
            .Where(a => a.Expression is MemberAccessExpressionSyntax)
            .Select(a => ((MemberAccessExpressionSyntax)a.Expression).Expression.Span));

        return assertions.Where(a => !innerAssertionSpans.Contains(a.Span));
    }

    private static IReadOnlyList<string> GetAdditionalAssertionPrefixes(SyntaxNodeAnalysisContext context)
    {
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        if (!options.TryGetValue("dotnet_diagnostic.SAE006.additional_assertion_prefixes", out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToArray();
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(method);
        if (symbol is null || !TestAssertionHelper.IsTestMethod(symbol))
            return;

        var additionalPrefixes = GetAdditionalAssertionPrefixes(context);

        // Collect assertions in the method body only, excluding nested lambdas /
        // local functions whose execution is not guaranteed by the test path.
        var assertionInvocations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => !TestAssertionHelper.IsInsideNestedFunction(inv))
            .Where(inv => TestAssertionHelper.IsAssertionInvocation(inv, context.SemanticModel, additionalPrefixes))
            .ToList();

        assertionInvocations = FilterAssertionChains(assertionInvocations).ToList();

        if (assertionInvocations.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(MustAssertRule, method.Identifier.GetLocation(), method.Identifier.Text));
            return;
        }

        // SAE007: null-only behavior check.
        if (assertionInvocations.All(inv => TestAssertionHelper.IsNullOnlyAssertion(inv)))
        {
            context.ReportDiagnostic(Diagnostic.Create(NullOnlyRule, method.Identifier.GetLocation(), method.Identifier.Text));
        }

        // SAE008: no assertion guaranteed on every successful path.
        if (!HasGuaranteedAssertion(method, context.SemanticModel, assertionInvocations))
        {
            var location = assertionInvocations[0].GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(BypassedRule, location, method.Identifier.Text));
        }

        // SAE009: tautological assertions.
        foreach (var assertion in assertionInvocations.Where(a => TestAssertionHelper.IsTautologicalAssertion(a, context.SemanticModel)))
        {
            context.ReportDiagnostic(Diagnostic.Create(TautologicalRule, assertion.GetLocation()));
        }
    }

    // Uses the compiler control-flow graph to prove that every successful path
    // from entry to exit passes through at least one assertion.
    private static bool HasGuaranteedAssertion(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        // Roslyn CFG models exceptional edges (catch/finally) differently across
        // versions; for methods with try/catch we use a documented heuristic
        // fallback (not a full proof) to avoid missing bypasses on the
        // exceptional path.
        if (method.DescendantNodes().OfType<TryStatementSyntax>().Any())
            return FallbackHasGuaranteedAssertion(method, assertions);

        var cfg = ControlFlowGraph.Create(method, semanticModel);
        return cfg is not null
            ? HasGuaranteedAssertionViaCfg(cfg, assertions)
            : FallbackHasGuaranteedAssertion(method, assertions);
    }

    private static bool HasGuaranteedAssertionViaCfg(
        ControlFlowGraph cfg,
        IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        var assertionBlocks = CollectAssertionBlocks(cfg, assertions);
        var entryBlock = cfg.Blocks.FirstOrDefault(b => b.Kind == BasicBlockKind.Entry);
        var exitBlock = cfg.Blocks.FirstOrDefault(b => b.Kind == BasicBlockKind.Exit);

        if (entryBlock is null || exitBlock is null)
            return false;

        return !IsEntryReachableFromExitWithoutAssertion(entryBlock, exitBlock, assertionBlocks);
    }

    private static HashSet<BasicBlock> CollectAssertionBlocks(
        ControlFlowGraph cfg,
        IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        var assertionBlocks = new HashSet<BasicBlock>();

        foreach (var block in cfg.Blocks.Where(b => b.Operations.Any(op => op is not null && assertions.Any(a => op.Syntax.Span.Contains(a.Span)))))
        {
            assertionBlocks.Add(block);
        }

        return assertionBlocks;
    }

    // Backward reachability from the exit block, refusing to traverse assertion
    // blocks. If the entry block is reachable without crossing an assertion, a
    // green path bypasses every check.
    private static bool IsEntryReachableFromExitWithoutAssertion(
        BasicBlock entryBlock,
        BasicBlock exitBlock,
        HashSet<BasicBlock> assertionBlocks)
    {
        var reachableWithoutAssert = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(exitBlock);

        while (stack.Count > 0)
        {
            var block = stack.Pop();
            if (!reachableWithoutAssert.Add(block))
                continue;

            if (assertionBlocks.Contains(block))
                continue;

            foreach (var predecessor in block.Predecessors.Where(p => p.Source is not null))
            {
                stack.Push(predecessor.Source!);
            }
        }

        return reachableWithoutAssert.Contains(entryBlock);
    }

    private static bool FallbackHasGuaranteedAssertion(MethodDeclarationSyntax method, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        if (method.ExpressionBody is not null)
            return ExpressionContainsAssertion(method.ExpressionBody.Expression, assertions);

        if (method.Body is null)
            return false;

        return StatementsGuaranteeAssertion(method.Body.Statements, assertions);
    }

    private static bool StatementGuaranteesAssertion(StatementSyntax statement, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        if (IsDirectAssertionStatement(statement, assertions))
            return true;

        return statement switch
        {
            BlockSyntax block => StatementsGuaranteeAssertion(block.Statements, assertions),
            IfStatementSyntax ifStatement when ifStatement.Else is not null => IfElseGuaranteesAssertion(ifStatement, assertions),
            TryStatementSyntax tryStatement => TryGuaranteesAssertion(tryStatement, assertions),
            SwitchStatementSyntax switchStatement => SwitchGuaranteesAssertion(switchStatement, assertions),
            _ => false,
        };
    }

    private static bool StatementsGuaranteeAssertion(SyntaxList<StatementSyntax> statements, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        foreach (var statement in statements)
        {
            if (StatementGuaranteesAssertion(statement, assertions))
                return true;

            if (StatementCanInterruptFlow(statement))
                return false;
        }

        return false;
    }

    // Heuristic: detects statements that may end execution before a later
    // assertion. Conservative — `throw` is always treated as an interruption,
    // even when a catch block contains the mandatory assertion. `if/else`
    // with a return in the `if` branch is not tracked (documented false
    // negative).
    private static bool StatementCanInterruptFlow(StatementSyntax statement)
    {
        return statement switch
        {
            ReturnStatementSyntax => true,
            ThrowStatementSyntax => true,
            IfStatementSyntax ifStatement when ifStatement.Else is null => StatementCanInterruptFlow(ifStatement.Statement),
            BlockSyntax block => block.Statements.Any(StatementCanInterruptFlow),
            _ => false,
        };
    }

    private static bool IfElseGuaranteesAssertion(IfStatementSyntax ifStatement, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        return StatementGuaranteesAssertion(ifStatement.Statement, assertions) &&
               StatementGuaranteesAssertion(ifStatement.Else!.Statement, assertions);
    }

    private static bool TryGuaranteesAssertion(TryStatementSyntax tryStatement, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        if (tryStatement.Finally is not null && StatementGuaranteesAssertion(tryStatement.Finally.Block, assertions))
            return true;

        return StatementGuaranteesAssertion(tryStatement.Block, assertions) &&
               tryStatement.Catches.All(c => StatementGuaranteesAssertion(c.Block, assertions));
    }

    private static bool SwitchGuaranteesAssertion(SwitchStatementSyntax switchStatement, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        return switchStatement.Sections.Any(s => s.Labels.Any(l => l.IsKind(SyntaxKind.DefaultSwitchLabel))) &&
               switchStatement.Sections.All(s => SwitchSectionGuaranteesAssertion(s, assertions));
    }

    private static bool SwitchSectionGuaranteesAssertion(SwitchSectionSyntax section, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        return StatementsGuaranteeAssertion(section.Statements, assertions);
    }

    private static bool IsDirectAssertionStatement(StatementSyntax statement, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        return statement is ExpressionStatementSyntax expressionStatement &&
               ExpressionContainsAssertion(expressionStatement.Expression, assertions);
    }

    private static bool ExpressionContainsAssertion(ExpressionSyntax expression, IReadOnlyList<InvocationExpressionSyntax> assertions)
    {
        return assertions.Any(a => expression.Span.Contains(a.Span));
    }
}
