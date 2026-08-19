// TRAP: The agent changed a DTO, and the frontend did not find out. The contract broke silently.
// GUARDRAIL: An OpenAPI snapshot test catches any change in the API contract.

using System.Net.Http.Json;
using System.Text.Json;
using TUnit;

namespace Tests.Patterns;

public class SnapshotTests
{
    private const string SnapshotPath = "../../../Snapshots/openapi-snapshot.json";

    // TRAP: The agent added a field to a Response DTO, removed or renamed one.
    // CI is green, tests pass, but the frontend breaks.
    // GUARDRAIL: We compare the current OpenAPI with the snapshot. Any diff = fail.
    [Test]
    public async Task OpenApi_ShouldMatchSnapshot()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var response = await client.GetAsync("/openapi/v1.json");
        var currentOpenApi = await response.Content.ReadAsStringAsync();

        if (!File.Exists(SnapshotPath))
        {
             // First run — create the snapshot
            Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath)!);
            await File.WriteAllTextAsync(SnapshotPath, currentOpenApi);
            return;
        }

        var snapshot = await File.ReadAllTextAsync(SnapshotPath);

         // Normalize the JSON for comparison
        var currentNormalized = NormalizeJson(currentOpenApi);
        var snapshotNormalized = NormalizeJson(snapshot);

        Assert.That(currentNormalized).IsEqualTo(snapshotNormalized);
    }

    private static string NormalizeJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}
