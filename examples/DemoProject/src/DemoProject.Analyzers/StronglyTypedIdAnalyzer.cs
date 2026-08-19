using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DemoProject.Analyzers;

// TRAP: Out of habit, an agent uses Guid/string/int/long for IDs in Domain entities
// and passes them around as method parameters.
// GUARDRAIL: Roslyn analyzers SAE001 / SAE002 catch this at compile time —
// faster than regex architecture tests (Layer 2) and free for every team member.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StronglyTypedIdAnalyzer : DiagnosticAnalyzer
{
    public const string PropertyDiagnosticId = "SAE001";
    public const string ParameterDiagnosticId = "SAE002";
    private const string Category = "Design";

    private static readonly DiagnosticDescriptor PropertyRule = new(
        id: PropertyDiagnosticId,
        title: "Use strongly typed ID instead of primitive",
        messageFormat: "Property '{0}' uses primitive type '{1}' for an identifier. Use a strongly typed ID (e.g., BookingId).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Domain entities should use strongly typed IDs instead of primitive types like Guid, string, int, or long.");

    private static readonly DiagnosticDescriptor ParameterRule = new(
        id: ParameterDiagnosticId,
        title: "Do not pass raw Guid as identifier parameter",
        messageFormat: "Parameter '{0}' uses raw Guid. Use a strongly typed ID parameter instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Method parameters that represent identifiers should use strongly typed IDs, not raw Guid.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(PropertyRule, ParameterRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
    {
        var property = (PropertyDeclarationSyntax)context.Node;

        var propertyName = property.Identifier.ValueText;
        if (!propertyName.EndsWith("Id", StringComparison.Ordinal))
            return;

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(property);
        if (propertySymbol is null)
            return;

        var ns = propertySymbol.ContainingType.ContainingNamespace?.ToDisplayString() ?? "";
        if (!(ns.EndsWith(".Domain", StringComparison.Ordinal) || ns == "Domain"))
            return;

        var typeName = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var primitiveTypes = new[] { "Guid", "string", "int", "long" };

        if (!primitiveTypes.Contains(typeName))
            return;

        var diagnostic = Diagnostic.Create(PropertyRule, property.Type.GetLocation(), propertyName, typeName);
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var parameter = (ParameterSyntax)context.Node;

        var paramName = parameter.Identifier.ValueText;
        if (!paramName.EndsWith("Id", StringComparison.Ordinal))
            return;

        var paramSymbol = context.SemanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        if (paramSymbol is null)
            return;

        var ns = paramSymbol.ContainingType.ContainingNamespace?.ToDisplayString() ?? "";
        if (!(ns.EndsWith(".Domain", StringComparison.Ordinal) || ns == "Domain"))
            return;

        var typeName = paramSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (typeName != "Guid")
            return;

        var location = parameter.Type?.GetLocation() ?? parameter.GetLocation();
        var diagnostic = Diagnostic.Create(ParameterRule, location, paramName);
        context.ReportDiagnostic(diagnostic);
    }
}
