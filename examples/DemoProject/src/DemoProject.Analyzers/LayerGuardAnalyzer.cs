using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DemoProject.Analyzers;

// TRAP: Architectural violations (a DbContext in a controller, a domain entity in the API
// layer, a query that mutates state) used to surface only in NetArchTest tests — minutes
// later, after the agent already "finished" the task. BannedApiAnalyzers does not help:
// it is a flat blacklist and knows nothing about layers.
// GUARDRAIL: SAE010-SAE012 catch these in the IDE / at dotnet build — the feedback loop
// drops from "run the architecture tests" to "red squiggle".
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LayerGuardAnalyzer : DiagnosticAnalyzer
{
    public const string DbContextOutsideInfrastructureId = "SAE010";
    public const string DomainEntityInApiLayerId = "SAE011";
    public const string QueryMutatesStateId = "SAE012";
    private const string Category = "Architecture";

    private static readonly DiagnosticDescriptor DbContextOutsideInfrastructureRule = new(
        id: DbContextOutsideInfrastructureId,
        title: "DbContext must not leave the Infrastructure layer",
        messageFormat: "DbContext usage outside the Infrastructure layer. Persistence concerns belong to Infrastructure (the caller should see an application service).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A DbContext (or a derived type) referenced from Application/Api/Controllers couples the caller to EF Core and the database. Catch it at compile time instead of in NetArchTest.");

    private static readonly DiagnosticDescriptor DomainEntityInApiLayerRule = new(
        id: DomainEntityInApiLayerId,
        title: "Domain entity must not leak into the API layer",
        messageFormat: "Domain type '{0}' is referenced from the API layer. Return a DTO instead of a domain entity.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Exposing domain entities in controllers/endpoints couples the API contract to the database schema and invites over-posting. Map to a DTO at the application boundary.");

    private static readonly DiagnosticDescriptor QueryMutatesStateRule = new(
        id: QueryMutatesStateId,
        title: "Query must not mutate state",
        messageFormat: "The [Query] method '{0}' mutates state ({1}). Read path must be side-effect free — move the mutation into a command.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A query that assigns to entities or calls persistence mutations (SaveChanges/Add/Remove/...) breaks CQRS read/write separation and produces phantom writes under load.");

    private static readonly string[] MutationMethodNames =
    {
        "SaveChanges", "SaveChangesAsync", "Add", "AddAsync", "AddRange", "AddRangeAsync",
        "Attach", "Remove", "RemoveRange", "Update",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DbContextOutsideInfrastructureRule,
        DomainEntityInApiLayerRule,
        QueryMutatesStateRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    // SAE010: `new AppDbContext()` or `AppDbContext _db;` outside Infrastructure.
    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var type = context.SemanticModel.GetTypeInfo(context.Node).Type;
        if (!IsDbContextLike(type))
            return;

        if (IsInInfrastructure(context))
            return;

        context.ReportDiagnostic(Diagnostic.Create(DbContextOutsideInfrastructureRule, context.Node.GetLocation()));
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
    {
        var declaration = (VariableDeclarationSyntax)context.Node;
        if (declaration.Type is null)
            return;

        var type = context.SemanticModel.GetTypeInfo(declaration.Type).Type;
        if (!IsDbContextLike(type))
            return;

        if (IsInInfrastructure(context))
            return;

        context.ReportDiagnostic(Diagnostic.Create(DbContextOutsideInfrastructureRule, declaration.Type.GetLocation()));
    }

    // SAE011: a type from *.Domain referenced from the API layer.
    private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
    {
        var identifier = (IdentifierNameSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(identifier).Symbol is not INamedTypeSymbol typeSymbol)
            return;

        var typeNamespace = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "";
        if (!(typeNamespace.EndsWith(".Domain", StringComparison.Ordinal) || typeNamespace == "Domain"))
            return;

        if (!IsInApiLayer(context))
            return;

        context.ReportDiagnostic(Diagnostic.Create(DomainEntityInApiLayerRule, identifier.GetLocation(), typeSymbol.Name));
    }

    // SAE012: [Query] method assigns to a member or calls a persistence mutation.
    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        if (!HasQueryAttribute(method))
            return;

        foreach (var node in method.Body?.DescendantNodes() ?? method.ExpressionBody?.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>())
        {
            ReportMemberAssignment(context, method, node);
            ReportPersistenceMutation(context, method, node);
        }
    }

    private static void ReportMemberAssignment(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, SyntaxNode node)
    {
        if (node is AssignmentExpressionSyntax assignment &&
            assignment.Left is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)
        {
            context.ReportDiagnostic(Diagnostic.Create(QueryMutatesStateRule, assignment.Left.GetLocation(), method.Identifier.ValueText, "member assignment"));
        }
    }

    private static void ReportPersistenceMutation(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, SyntaxNode node)
    {
        if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } &&
            MutationMethodNames.Contains(memberAccess.Name.Identifier.ValueText))
        {
            context.ReportDiagnostic(Diagnostic.Create(QueryMutatesStateRule, memberAccess.Name.GetLocation(), method.Identifier.ValueText, $"'{memberAccess.Name.Identifier.ValueText}' call"));
        }
    }

    private static bool IsDbContextLike(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var ns = current.ContainingNamespace?.ToDisplayString() ?? "";
            if (current.Name == "DbContext" || ns.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool IsInInfrastructure(SyntaxNodeAnalysisContext context) =>
        MatchesLayer(context, ".Infrastructure", "Infrastructure");

    private static bool IsInApiLayer(SyntaxNodeAnalysisContext context) =>
        MatchesLayer(context, ".Api", "Api", ".Controllers", "Controllers", ".Endpoints", "Endpoints");

    private static bool MatchesLayer(SyntaxNodeAnalysisContext context, params string[] layerSuffixes)
    {
        var ns = context.ContainingSymbol?.ContainingNamespace?.ToDisplayString() ?? "";
        foreach (var suffix in layerSuffixes)
        {
            if (ns.EndsWith(suffix, StringComparison.Ordinal) || ns == suffix.TrimStart('.'))
                return true;
        }

        return false;
    }

    private static bool HasQueryAttribute(MethodDeclarationSyntax method)
    {
        foreach (var list in method.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                var name = attribute.Name.ToString().Replace("Attribute", string.Empty);
                if (name is "Query")
                    return true;
            }
        }

        return false;
    }
}
