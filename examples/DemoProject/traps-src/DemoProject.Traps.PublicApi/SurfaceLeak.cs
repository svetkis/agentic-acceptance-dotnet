namespace DemoProject.Traps.PublicApi;

// A small library that "shipped" in v1.0. Its declared contract is in
// PublicAPI.Shipped.txt: OrderNumberFormatter.Format and OrderRegistry.Cancel.
//
// Then an agent "improved" the library in one PR — three faces of the same leak:
//
// GUARDRAIL: PublicApiAnalyzers turns every face into a build error:
//   RS0016 — a public symbol exists that is not declared (it leaked out);
//   RS0017 — a declared symbol no longer exists (renamed or deleted).
// The only way to a green build is a conscious edit of PublicAPI.Unshipped.txt:
// declare the new symbol, and mark removals with the *REMOVED* prefix.
// That file's diff is the report: "this PR changes the contract".

// TRAP: Face 1 — "another project might need this helper" — a new public type
// that was never decided to be part of the contract.
public static class OrderNumberParser
{
    public static int ParseLength(string orderNumber) => orderNumber.Length;
}

public static class OrderNumberFormatter
{
    // TRAP: Face 2 — "Format is a misleading name" — renamed to FormatPretty.
    // Consumers of the shipped Format break at their next build.
    public static string FormatPretty(string prefix, int sequence) => $"{prefix}-{sequence:D6}";
}

public static class OrderRegistry
{
    // TRAP: Face 3 — "Cancel has no callers in this repo" — deleted as dead code.
    // No callers *here*; every consuming repository had them.
}
