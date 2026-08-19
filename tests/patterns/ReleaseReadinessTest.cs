// TRAP: Before a release, people forget to check critical guardrails: security headers, rate limiting, OpenAPI snapshot, smoke.
// GUARDRAIL: A composite test verifies that mandatory artifacts exist and key checks pass.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...)
// - xUnit:  [Fact] + Assert.True(...)
// - NUnit:  [Test] + Assert.That(...)
// - MSTest: [TestMethod] + Assert.IsTrue(...)
//
// NOTE: This is not a replacement for full audits, but a quick gate before release. Do not duplicate the logic of other tests.

using TUnit;

namespace Tests.Patterns;

public class ReleaseReadinessTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://localhost:5000") };

    // TRAP: A release goes out without checking the health endpoint.
    // GUARDRAIL: /health responds with 200 OK.
    [Test]
    public async Task HealthEndpoint_ShouldBeHealthy()
    {
        var response = await HttpClient.GetAsync("/health");
        Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
    }

    // TRAP: Security headers are not configured, or the agent removed them.
    // GUARDRAIL: We verify that basic security headers are present.
    [Test]
    public async Task SecurityHeaders_ShouldBePresent()
    {
        var response = await HttpClient.GetAsync("/health");
        var headers = response.Headers;

        Assert.That(headers.Contains("X-Content-Type-Options")).IsTrue();
        Assert.That(headers.Contains("X-Frame-Options")).IsTrue();
        Assert.That(headers.Contains("Referrer-Policy")).IsTrue();
    }

    // TRAP: The OpenAPI contract broke, and the frontend did not find out.
    // GUARDRAIL: /openapi/v1.json is available and valid.
    [Test]
    public async Task OpenApiContract_ShouldBeAvailable()
    {
        var response = await HttpClient.GetAsync("/openapi/v1.json");
        Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content).Contains("\"openapi\"");
    }

    // TRAP: Important configuration files or documents are missing before the release.
    // GUARDRAIL: We verify that mandatory artifacts exist.
    [Test]
    public void RequiredReleaseArtifacts_ShouldExist()
    {
        var requiredFiles = new[]
        {
            Path.Combine(RepoRoot, "AGENTS.md"),
            Path.Combine(RepoRoot, "docs", "DEPLOYMENT.md"),
            Path.Combine(RepoRoot, ".github", "workflows", "deploy-api.yml")
        };

        var missing = requiredFiles.Where(f => !File.Exists(f)).ToList();
        Assert.That(missing).IsEmpty()
            .Because("Required release artifacts must exist: {0}", string.Join(", ", missing));
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.Exists(Path.Combine(dir, ".git"))
                || Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Any()
                || Directory.GetFiles(dir, "*.slnx", SearchOption.TopDirectoryOnly).Any())
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find repository root. Ensure .git, .sln or .slnx exists.");
    }
}
