// GUARDRAIL: NBomber shows that $Max$ latency has not degraded.
// This file is a working adaptation of the template from tests/patterns/LoadTest.cs
// For the demo we use in-memory load without external HTTP dependencies.

using DemoProject.Domain;
using NBomber.Contracts;
using NBomber.CSharp;
using TUnit;

namespace DemoProject.Tests;

public class LoadTests
{
    [Test]
    public async Task InMemoryReadWrite_ShouldNotDegradeLatency()
    {
        var readScenario = Scenario.Create("read_bookings", async context =>
        {
            var svc = new DemoProject.Application.BookingService();
            await svc.GetPendingAsync();
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5)));

        var writeScenario = Scenario.Create("confirm_booking", async context =>
        {
            var svc = new DemoProject.Application.BookingService();
            await svc.ConfirmAsync(BookingId.New());
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5)));

        var stats = NBomberRunner
            .RegisterScenarios(readScenario, writeScenario)
            .Run();

        var writeStats = stats.ScenarioStats.First(s => s.ScenarioName == "confirm_booking");

        await Assert.That(writeStats.Ok.Latency.MaxMs).IsLessThanOrEqualTo(1000)
            .Because("Max write latency must not degrade under load.");

        await Assert.That(writeStats.Fail.Request.Percent).IsEqualTo(0)
            .Because("No write requests should fail.");
    }
}
