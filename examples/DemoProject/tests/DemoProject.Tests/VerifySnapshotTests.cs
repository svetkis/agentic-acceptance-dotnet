// GUARDRAIL: Verify.TUnit snapshots the JSON contract with readable diffs; the .verified file IS the contract.
// TRAP: blindly accepting a regenerated snapshot lets a contract change slip through review.
// This file is a working adaptation of the template from tests/patterns/VerifySnapshotTest.cs

using DemoProject.Domain;
using TUnit;

namespace DemoProject.Tests;

public class VerifySnapshotTests
{
    // TRAP: on first run the test FAILS until the .received file is accepted.
    //        An agent may "fix the red build" by auto-accepting — review .verified diffs in PRs instead.
    [Test]
    public async Task BookingDto_Contract_ShouldMatchVerifiedSnapshot()
    {
        var booking = new Booking
        {
            Id = new BookingId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            CustomerId = new CustomerId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            CustomerName = "Demo Customer",
            ScheduledAt = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc),
            Status = BookingStatus.Confirmed
        };

        await Verify(booking);
    }
}
