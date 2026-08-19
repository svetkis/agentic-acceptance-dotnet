// TRAP: The agent optimizes reads with AsNoTracking but breaks writes.
// GUARDRAIL: NBomber shows that the $Max$ of write operations has degraded.

using NBomber.Contracts;
using NBomber.CSharp;
using TUnit;

namespace Tests.Patterns;

public class LoadTests
{
    // TRAP: The agent added AsNoTracking to GetPendingItems.
    // The InMemory test passes, but in production Status is not persisted.
    // GUARDRAIL: Drive read + write under load. The write $Max$ must not grow.
    [Test]
    public void ReadWriteMix_ShouldNotDegradeWriteLatency()
    {
        var httpClient = new HttpClient();

        var readScenario = Scenario.Create("read_items", async context =>
        {
            var response = await httpClient.GetAsync("/api/items/pending");
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)));

        var writeScenario = Scenario.Create("write_status", async context =>
        {
            var response = await httpClient.PostAsJsonAsync("/api/items/1/confirm", new { });
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)));

        var stats = NBomberRunner
            .RegisterScenarios(readScenario, writeScenario)
            .Run();

        // GUARDRAIL: The $Max$ write-operation latency must not exceed 500ms
        var writeStats = stats.ScenarioStats.First(s => s.ScenarioName == "write_status");
        Assert.That(writeStats.Ok.Latency.MaxMs).IsLessThanOrEqualTo(500);

        // GUARDRAIL: There must be no failed requests (otherwise the state machine is broken)
        Assert.That(writeStats.Fail.Count).IsEqualTo(0);
    }
}
