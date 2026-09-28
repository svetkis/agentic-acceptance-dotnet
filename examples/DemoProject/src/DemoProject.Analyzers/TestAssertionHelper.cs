using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace DemoProject.Analyzers;

// Shared semantic model for assertions across SAE006-SAE009.
// TUnit-first because DemoProject uses TUnit, but the patterns generalize to
// xUnit, NUnit, FluentAssertions, NSubstitute and test-project helpers.
internal static class TestAssertionHelper
{
    private static readonly string[] TestAttributeNames =
    {
        "TestAttribute",
        "FactAttribute",
        "TheoryAttribute",
        "TestMethodAttribute",
    };

    public static bool IsTestMethod(IMethodSymbol? method)
    {
        if (method is null)
            return false;

        return method.GetAttributes().Any(a =>
            a.AttributeClass is { } attr &&
            TestAttributeNames.Any(n => attr.Name == n));
    }

    // An assertion or verification call counts as a behavior check.
    // Custom helpers are recognized by convention: Assert*/Verify*/Expect*/Should*.
    // Owner: svetkis. Review: 2027-09-28. Permanent-by-design, see the rationale on the pragma line.
#pragma warning disable S1541 // Semantic recognition of many frameworks is inherently branch-heavy.
    public static bool IsAssertionInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return IsAssertionInvocation(invocation, semanticModel, Array.Empty<string>());
    }

    public static bool IsAssertionInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel, IReadOnlyList<string> additionalPrefixes)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return invocation.Expression is IdentifierNameSyntax id && IsVerificationLikeName(id.Identifier.Text, additionalPrefixes);
        }

        var memberName = memberAccess.Name.Identifier.Text;

        // TUnit / NUnit: Assert.That(...).IsEqualTo(...).
        if (memberName == "That" && IsKnownAssertType(memberAccess.Expression, semanticModel))
            return true;

        // xUnit / NUnit classic: Assert.True(...), Assert.Equal(...), etc.
        if (TryGetTypeName(memberAccess.Expression, semanticModel) == "Assert")
            return true;

        // FluentAssertions: value.Should().Be(...).
        if (memberName.StartsWith("Should"))
            return true;

        // NSubstitute / mocking verifications.
        if (memberName is "Received" or "DidNotReceive" or "ReceivedWithAnyArgs" or "DidNotReceiveWithAnyArgs")
            return true;

        // Convention-based helpers.
        return IsVerificationLikeName(memberName, additionalPrefixes);
    }
#pragma warning restore S1541

    private static bool IsKnownAssertType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var typeName = TryGetTypeName(expression, semanticModel);
        return typeName is "Assert" or "AssertionExtensions";
    }

    // The only behavior checked is null / not-null.
    public static bool IsNullOnlyAssertion(InvocationExpressionSyntax invocation)
    {
        var chainNames = new HashSet<string>(GetInvocationChain(invocation).Select(GetInvocationName));
        var directName = GetInvocationName(invocation);

        var chainNullChecks = new[] { "IsNull", "IsNotNull", "Null", "BeNull", "NotBeNull" };
        var directNullChecks = new[] { "Null", "NotNull", "IsNull", "IsNotNull" };

        return chainNames.Overlaps(chainNullChecks) || directNullChecks.Contains(directName);
    }

    // True when the assertion cannot fail by construction.
    public static bool IsTautologicalAssertion(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var chainNames = new HashSet<string>(GetInvocationChain(invocation).Select(GetInvocationName));

        if (IsLiteralTrueAssertion(invocation, semanticModel, chainNames))
            return true;

        if (IsSelfComparison(invocation, chainNames))
            return true;

        if (IsLiteralLiteralComparison(invocation, semanticModel, chainNames))
            return true;

        return false;
    }

    private static bool IsLiteralTrueAssertion(InvocationExpressionSyntax invocation, SemanticModel semanticModel, HashSet<string> chainNames)
    {
        if (!chainNames.Overlaps(new[] { "IsTrue", "True", "IsEqualTo", "AreEqual" }))
            return false;

        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 1)
            return semanticModel.GetConstantValue(args[0].Expression) is { HasValue: true, Value: true };

        if (args.Count == 0 && chainNames.Overlaps(new[] { "IsTrue", "True" }))
        {
            var thatArgument = TryGetThatArgument(invocation);
            return thatArgument is not null &&
                   semanticModel.GetConstantValue(thatArgument) is { HasValue: true, Value: true };
        }

        return false;
    }

    private static bool IsSelfComparison(InvocationExpressionSyntax invocation, HashSet<string> chainNames)
    {
        if (!chainNames.Overlaps(new[] { "IsEqualTo", "IsSameReferenceAs", "AreEqual", "SameAs", "IsEquivalentTo", "EquivalentTo" }))
            return false;

        var args = invocation.ArgumentList.Arguments;
        if (args.Count != 1)
            return false;

        var left = TryGetThatArgument(invocation);
        var right = args[0].Expression;
        return left is not null && AreEquivalentExpressions(left, right);
    }

    private static bool IsLiteralLiteralComparison(InvocationExpressionSyntax invocation, SemanticModel semanticModel, HashSet<string> chainNames)
    {
        if (!chainNames.Overlaps(new[] { "IsEqualTo", "AreEqual" }))
            return false;

        var args = invocation.ArgumentList.Arguments;
        if (args.Count != 1)
            return false;

        var left = TryGetThatArgument(invocation);
        if (left is null)
            return false;

        var leftConstant = semanticModel.GetConstantValue(left);
        var rightConstant = semanticModel.GetConstantValue(args[0].Expression);

        return leftConstant is { HasValue: true } &&
               rightConstant is { HasValue: true } &&
               Equals(leftConstant.Value, rightConstant.Value);
    }

    public static bool IsVerificationLikeName(string name) =>
        name.StartsWith("Assert") ||
        name.StartsWith("Verify") ||
        name.StartsWith("Expect") ||
        name.StartsWith("Should");

    public static bool IsVerificationLikeName(string name, IReadOnlyList<string> additionalPrefixes)
    {
        if (IsVerificationLikeName(name))
            return true;

        foreach (var prefix in additionalPrefixes)
        {
            if (!string.IsNullOrWhiteSpace(prefix) && name.StartsWith(prefix))
                return true;
        }

        return false;
    }

    // True when the invocation sits inside a lambda, anonymous method or local
    // function that is not guaranteed to execute on every test path.
    public static bool IsInsideNestedFunction(SyntaxNode node)
    {
        return node.Ancestors().Any(a =>
            a is AnonymousFunctionExpressionSyntax ||
            a is LocalFunctionStatementSyntax);
    }

    private static string? TryGetTypeName(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(expression);
        return symbolInfo.Symbol switch
        {
            ITypeSymbol type => type.Name,
            IMethodSymbol method => method.ContainingType?.Name,
            IPropertySymbol property => property.ContainingType?.Name,
            _ => null,
        };
    }

    private static string GetInvocationName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            return memberAccess.Name.Identifier.Text;

        if (invocation.Expression is IdentifierNameSyntax id)
            return id.Identifier.Text;

        return string.Empty;
    }

    private static IReadOnlyList<InvocationExpressionSyntax> GetInvocationChain(InvocationExpressionSyntax leaf)
    {
        var list = new List<InvocationExpressionSyntax> { leaf };
        var current = leaf.Expression;

        while (current is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression is InvocationExpressionSyntax parentInvocation)
            {
                list.Add(parentInvocation);
                current = parentInvocation.Expression;
            }
            else
            {
                break;
            }
        }

        return list;
    }

    private static ExpressionSyntax? TryGetThatArgument(InvocationExpressionSyntax invocation)
    {
        // Walk up the chain to find Assert.That(argument).
        var current = invocation;
        while (true)
        {
            if (current.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is InvocationExpressionSyntax parent &&
                GetInvocationName(parent) == "That" &&
                parent.ArgumentList.Arguments.Count == 1)
            {
                return parent.ArgumentList.Arguments[0].Expression;
            }

            if (current.Expression is MemberAccessExpressionSyntax ma &&
                ma.Expression is InvocationExpressionSyntax parent2)
            {
                current = parent2;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    private static bool AreEquivalentExpressions(ExpressionSyntax left, ExpressionSyntax right) =>
        left.ToString() == right.ToString();
}
