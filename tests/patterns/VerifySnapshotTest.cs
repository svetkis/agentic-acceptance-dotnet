// TRAP: The agent changed a DTO, the JSON contract broke silently — plain asserts show "strings differ",
//        and hand-rolled snapshot code rots (normalization, diff, first-run handling).
// GUARDRAIL: Verify.TUnit snapshots complex objects with readable diffs; the verified file is the contract.
//
// Requires: dotnet add package Verify.TUnit
// TRAP: Verify.TUnit drags in a recent TUnit.Core (>= 1.63 at the time of writing).
//        Pinning an old TUnit alongside a new Verify.TUnit fails at RUNTIME with
//        MissingFieldException 'TUnit.Core.Sources.BeforeEveryTestHooks' — keep them in lockstep.
// TRAP: Verify (since ~28) ships SponsorCheck: the BUILD fails (SC021) unless the team sponsors the project
//        or sets <Verify_SponsorshipLicenseIgnored>true</Verify_SponsorshipLicenseIgnored>
//        (which declares the build in breach of the package license). Evaluate this as a
//        supply-chain/licensing decision BEFORE adopting, and record it in DECISION-GUARDS.
// Framework adaptation: xUnit -> Verify.Xunit, NUnit -> Verify.NUnit, MSTest -> Verify.MSTest.
// The trap below (auto-accepting a broken snapshot) applies to ALL of them.

using TUnit;

namespace Tests.Patterns;

public class VerifySnapshotTests
{
    // TRAP: On first run Verify writes .received.txt and the test FAILS until you accept it
    //        (rename to .verified.txt or run DiffEngine traction). An agent under pressure
    //        may "fix the red build" by blindly accepting the new snapshot — the contract
    //        change slips through review unexamined.
    // GUARDRAIL: Accept snapshots only after reading the diff. Review .verified.* files in PRs
    //        like production code; add a CI grep that fails if .received.* files are committed.
    [Test]
    public async Task BookingResponse_Contract_ShouldMatchVerifiedSnapshot()
    {
        var response = new BookingResponse(
            Id: Guid.Parse("0c5e9d5c-6a2b-4f8a-9d1e-2b7c8f4a1d30"),
            GuestName: "Ada Lovelace",
            CheckInUtc: new DateTime(2026, 5, 1, 14, 0, 0, DateTimeKind.Utc),
            Nights: 3,
            Total: 450.00m);

        await Verify(response);
    }

    // Verify scrubs nondeterministic values automatically (Guid -> Guid_1, DateTime -> DateTime_1),
    // so the snapshot stays stable across runs without hand-rolled normalization.
    [Test]
    public async Task BookingResponse_NonDeterministicFields_AreScrubbed()
    {
        var response = new BookingResponse(
            Id: Guid.NewGuid(),                       // changes every run -> Guid_1 in the snapshot
            GuestName: "Grace Hopper",
            CheckInUtc: DateTime.UtcNow,              // changes every run -> DateTime_1
            Nights: 1,
            Total: 150.00m);

        await Verify(response);
    }
}

public record BookingResponse(
    Guid Id,
    string GuestName,
    DateTime CheckInUtc,
    int Nights,
    decimal Total);
