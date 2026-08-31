// TRAP: Hand-picked example tests stay green while real-world inputs break in production.
//       The agent happily copies the two happy-path cases it saw in the file and never
//       generates "8-905 (905) 123 45", "+7 905…", or a leading space.
// GUARDRAIL: Property-based tests (FsCheck) replace examples with invariants:
//       a generator produces hundreds of inputs, an assertion checks a property
//       that must hold for ALL of them (format, idempotence, round-trip, bounds).
//
// Framework adaptation:
// - TUnit (this template): plain [Test] methods sample the generator and assert in a loop
// - xUnit / NUnit / MSTest: same shape — [Fact]/[Test]/[TestMethod] + Gen.Sample
//
// NOTE: uses FsCheck 3.x C# fluent API (`FsCheck.Fluent.Gen`, LINQ `from` syntax).
//       FsCheck 3 has no TUnit integration attribute, so we run properties inside
//       ordinary tests via Gen.Sample — deterministic, no hidden runner required.
//
// Package: dotnet add package FsCheck

using FsCheck.Fluent;

namespace Tests.Patterns;

public class PropertyBasedTests
{
    private const int Samples = 200;

    private static readonly char[] Junk = ['+', '-', '(', ')', '.', ' '];

    // Example system under test: a typical phone normalizer.
    // Replace with your own type — keep the properties, they transfer as-is.
    public static string Normalize(string phone)
    {
        ArgumentNullException.ThrowIfNull(phone);
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits.StartsWith('8'))
            digits = "7" + digits[1..];
        return "+" + digits;
    }

    // Generator: phone-like strings — digits wrapped in separators,
    // the shapes users and bots actually send.
    private static FsCheck.Gen<string> PhoneLike =>
        from digits in Gen.Choose(0, 9).ListOf()
        from junkCount in Gen.Choose(0, 5)
        from junkFirst in Gen.Elements([true, false])
        let junk = new string(Enumerable.Range(0, junkCount)
            .Select(_ => Junk[Random.Shared.Next(Junk.Length)]).ToArray())
        let body = new string(digits.Select(d => (char)('0' + d)).ToArray())
        select junkFirst ? junk + body : body + junk;

    // Property 1: structural invariant — output is always "+" followed by digits only.
    [Test]
    public void Normalize_OutputContainsOnlyDigitsAfterPlus()
    {
        foreach (var phone in Gen.Sample(PhoneLike, Samples))
        {
            var result = Normalize(phone);
            Assert.That(result.Length > 0
                && result[0] == '+'
                && result[1..].All(char.IsDigit))
                .IsTrue()
                .Because($"input: '{phone}', output: '{result}'");
        }
    }

    // TRAP: The one-off test author forgets the Russian "8" prefix.
    // GUARDRAIL: A property stated as "for ALL 11-digit inputs starting with 8"
    // forces the generator to probe the boundary systematically.
    [Test]
    public void Normalize_ReplacesLeadingEightWithSeven()
    {
        FsCheck.Gen<string> elevenDigitsStartingWithEight =
            from rest in Gen.Choose(0, 9).ListOf(10)
            select "8" + new string(rest.Select(d => (char)('0' + d)).ToArray());

        foreach (var phone in Gen.Sample(elevenDigitsStartingWithEight, Samples))
        {
            var result = Normalize(phone);
            Assert.That(result.StartsWith("+7"))
                .IsTrue()
                .Because($"input: '{phone}', output: '{result}'");
        }
    }

    // Property 3: idempotence — normalizing twice must not change the result.
    // Critical when the same value passes through two layers (bot + API):
    // if the second pass changes anything, account linking by phone silently breaks.
    [Test]
    public void Normalize_IsIdempotent()
    {
        foreach (var phone in Gen.Sample(PhoneLike, Samples))
        {
            var once = Normalize(phone);
            var twice = Normalize(once);
            Assert.That(twice).IsEqualTo(once)
                .Because($"double normalization changed result: '{phone}' -> '{once}' -> '{twice}'");
        }
    }
}
