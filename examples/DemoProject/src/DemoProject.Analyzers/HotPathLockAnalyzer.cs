using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DemoProject.Analyzers;

// TRAP: An agent adds a lock to a hot path — directly, or "harmlessly" one call
//       away: a helper with a lock inside, Lazy<T> with the default publication
//       mode, a static constructor. Under parallel load the threads serialize on
//       the lock and throughput collapses; single-threaded tests never notice.
// GUARDRAIL: SAE013 catches `lock` and blocking Monitor calls directly inside
//            [HotPath] methods — a cheap syntax walk, always on in the IDE.
//            SAE014 catches locks one or more calls away via a reverse call
//            graph — expensive, so it is gated by EnableHotPathDeepAnalysis
//            (CI-only) and never runs on every keystroke.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HotPathLockAnalyzer : DiagnosticAnalyzer
{
    public const string LockDiagnosticId = "SAE013";
    public const string TransitiveLockDiagnosticId = "SAE014";
    private const string Category = "Performance";

    // CI-only switch for SAE014. The build_property prefix reaches analyzers only
    // for MSBuild properties declared as compiler-visible — see the
    // CompilerVisibleProperty item in the Directory.Build.props next to this
    // solution. CI passes the property on the build command line; IDE builds
    // leave it unset, so the IDE never pays for call-graph work.
    private const string DeepAnalysisSwitch = "build_property.EnableHotPathDeepAnalysis";

    // The lock-statement rule covers the common case because a lock statement
    // lowers to a Monitor.Enter call. The members below are the equivalent direct
    // calls — extend the set for your stack with SemaphoreSlim.Wait, Mutex.WaitOne
    // and friends.
    private static readonly ImmutableHashSet<string> BlockingMonitorMembers =
        ImmutableHashSet.Create("Enter", "TryEnter", "Wait");

    private static readonly DiagnosticDescriptor LockRule = new(
        id: LockDiagnosticId,
        title: "Avoid lock in hot path",
        messageFormat: "Avoid `lock` and blocking Monitor calls in `[HotPath]` methods — use lock-free structures or move the lock off the hot path",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A lock in a hot path serializes every thread that hits it; under parallel load throughput collapses. Contention itself is invisible to static analysis — pair this rule with a lock-contention budget test (tests/patterns/LockContentionBudgetTest.cs).");

    private static readonly DiagnosticDescriptor TransitiveLockRule = new(
        id: TransitiveLockDiagnosticId,
        title: "Hot path transitively acquires a lock",
        messageFormat: "Hot path method '{0}' transitively acquires a lock via {1} — an implicit lock one or more calls away (CI deep analysis; direct locks are SAE013)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The lock lives in a callee, not in the hot method itself — the sneakiest variant. Call-graph analysis is CI-only: EnableHotPathDeepAnalysis=true.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(LockRule, TransitiveLockRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // SAE013 — cheap, always on (IDE + build)
        context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
        context.RegisterSyntaxNodeAction(AnalyzeBlockingInvocation, SyntaxKind.InvocationExpression);

        // SAE014 — reverse call graph, CI only (see DeepAnalysisSwitch)
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    // --- SAE013: direct lock in a [HotPath] method ---

    private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
    {
        var lockStatement = (LockStatementSyntax)context.Node;
        if (!IsInHotPathMethod(context, lockStatement))
            return;

        context.ReportDiagnostic(Diagnostic.Create(LockRule, lockStatement.LockKeyword.GetLocation()));
    }

    private static void AnalyzeBlockingInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return;

        if (!IsBlockingMonitorCall(method))
            return;
        if (!IsInHotPathMethod(context, invocation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(LockRule, invocation.GetLocation()));
    }

    private static bool IsBlockingMonitorCall(IMethodSymbol method)
    {
        return method.ContainingType?.ToDisplayString() == "System.Threading.Monitor"
            && BlockingMonitorMembers.Contains(method.Name);
    }

    // --- SAE014: transitive lock, CI-gated reverse call graph ---

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (!IsDeepAnalysisEnabled(context.Options))
            return;

        var state = new DeepAnalysisState();
        context.RegisterSyntaxNodeAction(state.OnLockStatement, SyntaxKind.LockStatement);
        context.RegisterSyntaxNodeAction(state.OnInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(state.OnMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterCompilationEndAction(state.OnCompilationEnd);
    }

    private static bool IsDeepAnalysisEnabled(AnalyzerOptions options)
    {
        return options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(DeepAnalysisSwitch, out var enabled)
            && string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase);
    }

    // --- shared helpers ---

    private static bool IsInHotPathMethod(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        var symbol = context.SemanticModel.GetEnclosingSymbol(node.SpanStart);
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

    // LIMITATIONS (deliberate for the demo — extend for your project):
    // - Virtual and interface calls resolve to the DECLARED symbol; an override
    //   that takes a lock is a separate graph node and stays invisible.
    // - Invocations through a delegate value do not resolve to the target body.
    // - Calls into other assemblies are opaque — cross-assembly implicit locks
    //   are runtime evidence territory. See docs/solutions/lock-contention-evidence.md
    private sealed class DeepAnalysisState
    {
        private readonly ConcurrentDictionary<IMethodSymbol, byte> _lockMethods = new(SymbolEqualityComparer.Default);
        private readonly ConcurrentDictionary<IMethodSymbol, Location> _hotPathMethods = new(SymbolEqualityComparer.Default);

        // PERF: recording every invocation is acceptable for a one-shot CI pass.
        // For large solutions pre-filter by callee NAME (a syntax-only pass first
        // collects names of lock-holding methods) before paying for GetSymbolInfo.
        private readonly ConcurrentQueue<(IMethodSymbol Caller, IMethodSymbol Callee)> _edges = new();

        public void OnLockStatement(SyntaxNodeAnalysisContext context)
        {
            var enclosing = ResolveTopmostMethod(context.SemanticModel, context.Node.SpanStart);
            if (enclosing != null)
                _lockMethods.TryAdd(enclosing, 0);
        }

        public void OnInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (context.SemanticModel.GetSymbolInfo(invocation).Symbol?.OriginalDefinition is not IMethodSymbol callee)
                return;

            var caller = ResolveTopmostMethod(context.SemanticModel, invocation.SpanStart);
            if (caller == null)
                return;

            if (IsBlockingMonitorCall(callee))
            {
                // A direct Monitor.Enter in the caller is the same seed as a lock statement
                _lockMethods.TryAdd(caller, 0);
                return;
            }

            _edges.Enqueue((caller, callee));
        }

        public void OnMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declaration = (MethodDeclarationSyntax)context.Node;
            if (context.SemanticModel.GetDeclaredSymbol(declaration) is not { } symbol || !HasHotPathAttribute(symbol))
                return;

            _hotPathMethods.TryAdd(symbol, declaration.Identifier.GetLocation());
        }

        public void OnCompilationEnd(CompilationAnalysisContext context)
        {
            if (_lockMethods.IsEmpty || _hotPathMethods.IsEmpty)
                return;

            var callersByCallee = BuildReverseIndex();
            var reported = new HashSet<(IMethodSymbol Hot, IMethodSymbol LockMethod)>();

            foreach (var lockMethod in _lockMethods.Keys)
                ReportHotPathsAbove(context, lockMethod, callersByCallee, reported);
        }

        private Dictionary<IMethodSymbol, List<IMethodSymbol>> BuildReverseIndex()
        {
            var callersByCallee = new Dictionary<IMethodSymbol, List<IMethodSymbol>>(SymbolEqualityComparer.Default);
            foreach (var (caller, callee) in _edges)
            {
                if (!callersByCallee.TryGetValue(callee, out var callers))
                    callersByCallee[callee] = callers = new List<IMethodSymbol>();
                callers.Add(caller);
            }
            return callersByCallee;
        }

        private void ReportHotPathsAbove(
            CompilationAnalysisContext context,
            IMethodSymbol lockMethod,
            Dictionary<IMethodSymbol, List<IMethodSymbol>> callersByCallee,
            HashSet<(IMethodSymbol Hot, IMethodSymbol LockMethod)> reported)
        {
            // BFS upward from the lock holder toward [HotPath] entry points,
            // carrying the call chain for the diagnostic message text.
            var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default) { lockMethod };
            var queue = new Queue<(IMethodSymbol Method, string Chain)>();
            queue.Enqueue((lockMethod, DisplayName(lockMethod)));

            while (queue.Count > 0)
            {
                var (method, chain) = queue.Dequeue();

                ReportIfHotPath(context, method, lockMethod, chain, reported);

                if (!callersByCallee.TryGetValue(method, out var callers))
                    continue;

                foreach (var caller in callers.Where(visited.Add))
                    queue.Enqueue((caller, DisplayName(caller) + " → " + chain));
            }
        }

        private void ReportIfHotPath(
            CompilationAnalysisContext context,
            IMethodSymbol method,
            IMethodSymbol lockMethod,
            string chain,
            HashSet<(IMethodSymbol Hot, IMethodSymbol LockMethod)> reported)
        {
            if (!_hotPathMethods.TryGetValue(method, out var location))
                return;

            // A lock directly inside the hot method is SAE013's job — no double
            // report. The walk above continues regardless: a hot method may itself
            // be called by another hot method.
            if (method.Equals(lockMethod, SymbolEqualityComparer.Default) || !reported.Add((method, lockMethod)))
                return;

            context.ReportDiagnostic(Diagnostic.Create(TransitiveLockRule, location, DisplayName(method), chain));
        }

        private static IMethodSymbol? ResolveTopmostMethod(SemanticModel semanticModel, int position)
        {
            var symbol = semanticModel.GetEnclosingSymbol(position);
            // Attribute calls made inside lambdas and local functions to the
            // containing method, so a lambda that calls a lock-holding helper
            // still connects to its parent method in the call graph.
            while (symbol is IMethodSymbol method &&
                   (method.MethodKind == MethodKind.LambdaMethod || method.MethodKind == MethodKind.LocalFunction))
            {
                symbol = symbol.ContainingSymbol;
            }

            return symbol as IMethodSymbol;
        }

        private static string DisplayName(IMethodSymbol method) =>
            method.ContainingType?.Name + "." + method.Name;
    }
}
