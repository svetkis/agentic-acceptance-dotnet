// TRAP: The agent changed a DTO, the JSON contract broke silently — plain asserts show "objects differ",
//        and hand-maintained expected objects drift from real output.
// GUARDRAIL: Storm Petrel keeps the expected baseline IN TEST CODE; a source generator creates a
//        *TestStormPetrel copy whose manual run rewrites the baseline with the actual value.
//        Contract changes surface as ordinary code diffs in review — an agent cannot silently
//        "accept a snapshot file" like with file-based tools.
//
// Requires: dotnet add package Scand.StormPetrel.Generator + dotnet add package VarDump
//
// TUnit specifics (verified empirically 2026-09, TUnit 1.66.27 / Generator 3.0.1):
// TRAP 1: TUnit is NOT in the generator's default attribute list (as of Generator 3.0.1; PR pending —
//        https://github.com/Scandltd/storm-petrel/pull/6). Until merged, declare the attribute via env var:
//        SCAND_STORM_PETREL_GENERATOR_CONFIG={"CustomTestAttributes":[{"TestFrameworkKindName":"NUnit","FullName":"TUnit.Core.TestAttribute","KindName":"Test"}]}
//        Set it for EVERY build, otherwise the *TestStormPetrel copy is silently not generated.
// TRAP 2: TUnit registers tests via its OWN source generator and cannot see classes emitted by other
//        generators. Without Reflection mode the *TestStormPetrel copy compiles but never runs/discovered.
// GUARDRAIL: [assembly: TUnit.Core.ReflectionMode] in a DEDICATED file (the generator copies whole files;
//        a duplicated assembly attribute fails compilation with CS0579).
// TRAP 3: Rewritten baselines dump DateTime etc. using CultureInfo/DateTimeStyles; the generated copy
//        reuses the original file's usings — missing `using System.Globalization;` breaks the copy's compile.
// TRAP 4: Running the *TestStormPetrel copy rewrites baselines and leaves *.backup* files — gitignore them
//        and never commit; also filter the copies out of regular CI runs (they are update tools, not tests).
//
// Framework adaptation: xUnit / NUnit / MSTest work out of the box with no env var and no Reflection mode.

using System.Globalization; // TRAP 3: needed by the generated copy after baseline rewrite
using TUnit;

namespace Tests.Patterns;

public class StormPetrelSnapshotTests
{
    // GUARDRAIL: baseline lives here, in code. Update it ONLY by running the generated
    // BookingDto_ShouldMatchBaselineTestStormPetrel method (renames to *TestStormPetrel),
    // never by hand-editing: hand edits re-bake whatever the agent believes, not the actual output.
    [Test]
    public async Task BookingDto_ShouldMatchBaseline()
    {
        var expected = new Booking
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CustomerName = "Demo Customer",
            ScheduledAtUtc = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc)
        };

        var actual = BookingService.GetCurrentBooking();

        await Assert.That(actual).IsEquivalentTo(expected);
    }
}

public class Booking
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
}

public static class BookingService
{
    public static Booking GetCurrentBooking() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        CustomerName = "Demo Customer",
        ScheduledAtUtc = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc)
    };
}
