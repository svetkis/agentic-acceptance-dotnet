using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DemoProject.Analyzers;

// TRAP: An agent adds allocations (new, async state machines, boxing) to methods
// that run on every request — performance degrades silently.
// GUARDRAIL: The analyzer catches new/async/boxing in [HotPath] methods right in the IDE.
// Feedback loop: ~0.5 seconds (even before dotnet build), not a profiler in production.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HotPathAnalyzer : DiagnosticAnalyzer
{
    public const string NewDiagnosticId = "SAE003";
    public const string AsyncDiagnosticId = "SAE004";
    public const string BoxingDiagnosticId = "SAE005";
    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor NewRule = new(
        id: NewDiagnosticId,
        title: "Avoid allocation in hot path",
        messageFormat: "Avoid `new` allocations in `[HotPath]` methods",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Allocations in hot paths cause GC pressure. Use object pooling or stackalloc where possible.");

    private static readonly DiagnosticDescriptor AsyncRule = new(
        id: AsyncDiagnosticId,
        title: "Avoid async state machine in hot path",
        messageFormat: "Avoid `async` state machine in `[HotPath]` methods",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Async methods allocate state machines. In hot paths, prefer synchronous code or ValueTask with pooling.");

    private static readonly DiagnosticDescriptor BoxingRule = new(
        id: BoxingDiagnosticId,
        title: "Avoid boxing in hot path",
        messageFormat: "Avoid boxing conversion in `[HotPath]` methods",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Boxing allocates on the heap. In hot paths, use generics or strongly typed overloads to avoid boxing.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NewRule, AsyncRule, BoxingRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeArrayCreation, SyntaxKind.ArrayCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeArrayCreation, SyntaxKind.ImplicitArrayCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeCast, SyntaxKind.CastExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAs, SyntaxKind.AsExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (!IsInHotPathMethod(context, context.Node))
            return;
        context.ReportDiagnostic(Diagnostic.Create(NewRule, context.Node.GetLocation()));
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context) => AnalyzeObjectCreation(context);
    private static void AnalyzeArrayCreation(SyntaxNodeAnalysisContext context) => AnalyzeObjectCreation(context);

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
            return;

        var symbol = context.SemanticModel.GetDeclaredSymbol(method);
        if (symbol is null || !HasHotPathAttribute(symbol))
            return;

        var asyncToken = method.Modifiers.First(m => m.IsKind(SyntaxKind.AsyncKeyword));
        context.ReportDiagnostic(Diagnostic.Create(AsyncRule, asyncToken.GetLocation()));
    }

    private static void AnalyzeCast(SyntaxNodeAnalysisContext context)
    {
        var cast = (CastExpressionSyntax)context.Node;
        if (!IsInHotPathMethod(context, cast))
            return;

        if (!IsBoxingConversion(context, cast))
            return;

        context.ReportDiagnostic(Diagnostic.Create(BoxingRule, cast.GetLocation()));
    }

    private static void AnalyzeAs(SyntaxNodeAnalysisContext context)
    {
        var asExpr = (BinaryExpressionSyntax)context.Node;
        if (!IsInHotPathMethod(context, asExpr))
            return;

        if (!IsBoxingConversion(context, asExpr))
            return;

        context.ReportDiagnostic(Diagnostic.Create(BoxingRule, asExpr.GetLocation()));
    }

    private static bool IsBoxingConversion(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(expression);
        var conversion = context.SemanticModel.GetConversion(expression);

        if (conversion.IsBoxing)
            return true;

        // Fallback for cases where GetConversion does not report boxing explicitly
        // (e.g. reference assemblies or certain cast forms).
        if (typeInfo.Type is { IsValueType: true } && typeInfo.ConvertedType is { IsReferenceType: true })
            return true;

        return false;
    }

    private static bool IsInHotPathMethod(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        var symbol = context.SemanticModel.GetEnclosingSymbol(node.SpanStart);
        return IsInHotPathMethod(symbol);
    }

    private static bool IsInHotPathMethod(ISymbol? symbol)
    {
        while (symbol != null)
        {
            if (symbol is IMethodSymbol method && HasHotPathAttribute(method))
                return true;
            symbol = symbol.ContainingSymbol;
        }
        return false;
    }

    private static bool HasHotPathAttribute(ISymbol symbol)
    {
        // NOTE: when copying this analyzer into another project, change the namespace to your own
        return symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            == "global::DemoProject.Domain.HotPathAttribute");
    }
}
