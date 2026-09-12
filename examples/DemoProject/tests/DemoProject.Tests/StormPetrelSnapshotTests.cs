// Working adaptation of the template from tests/patterns/StormPetrelSnapshotTest.cs
// Storm Petrel with TUnit requires SCAND_STORM_PETREL_GENERATOR_CONFIG env var
// (TUnit is not in the generator's default attribute list yet).

using System.Globalization; // copied into the *TestStormPetrel file: the rewritten baseline uses CultureInfo
using DemoProject.Domain;
using TUnit;

namespace DemoProject.Tests;

public class StormPetrelSnapshotTests
{
    [Test]
    public async Task BookingDto_ShouldMatchBaseline()
    {
        var expected = new Booking
        {
            Id = new BookingId
            {
                Value = new Guid("11111111-1111-1111-1111-111111111111")
            },
            CustomerId = new CustomerId
            {
                Value = new Guid("22222222-2222-2222-2222-222222222222")
            },
            CustomerName = "Demo Customer",
            ScheduledAt = DateTime.ParseExact("2026-05-30T10:00:00.0000000Z", "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Status = BookingStatus.Confirmed
        };

        var actual = new Booking
        {
            Id = new BookingId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            CustomerId = new CustomerId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            CustomerName = "Demo Customer",
            ScheduledAt = new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc),
            Status = BookingStatus.Confirmed
        };

        await Assert.That(actual).IsEquivalentTo(expected);
    }
}
