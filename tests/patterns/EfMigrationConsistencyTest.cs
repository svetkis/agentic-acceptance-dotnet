// TRAP: The agent changes an EF Core entity (renames a column, adds a field,
//       alters a relationship) but forgets to generate the migration — or writes
//       the migration by hand instead of calling `dotnet ef migrations add`,
//       hallucinating SQL that never runs against a real database.
//       Everything is green in unit tests; the schema drift explodes at deploy.
// GUARDRAIL: CI fails when the model has pending changes with no migration,
//       and every migration is verified by applying the full chain to an empty
//       container. Zero tokens for the agent to argue with.
//
// Two checks in this pattern:
//   1. has-pending-model-changes — model and migrations are in sync (cheap, fast);
//   2. migrate-empty-database    — the migration chain actually runs on a real DB engine.
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...).IsTrue()
// - xUnit:  [Fact] + Assert.True(...)
// - NUnit:  [Test] + Assert.That(..., Is.True)
// - MSTest: [TestMethod] + Assert.IsTrue(...)
//
// Requires: `dotnet ef` tool installed on CI (dotnet tool install --global dotnet-ef)
// and a real provider (Npgsql/sqlite-pcl) referenced by the migrations project.

using System.Diagnostics;
using TUnit;
using Testcontainers.PostgreSql;

namespace Tests.Patterns;

public class EfMigrationConsistencyTests
{
    // TRAP: The agent renamed Order.CustomerId to Order.ClientId "for clarity"
    //       and pushed — no migration, and nothing failed until `database update`
    //       on staging produced a runtime InvalidColumnName.
    // GUARDRAIL: `dotnet ef migrations has-pending-model-changes` exits non-zero
    //       when the model no longer matches the last migration.
    // NOTE: The command prints human-readable text but signals via exit code:
    //       0 = in sync, 1 = changes pending (dotnet-ef 8+).
    [Test]
    public async Task Model_ShouldHaveNoPendingMigrationChanges()
    {
        var (exitCode, output) = await RunDotnetEf(
            "migrations has-pending-model-changes --project src/YourApp.Infrastructure");

        Assert.That(exitCode)
            .IsEqualTo(0)
            .Because($"EF model has changes not covered by a migration. " +
                     $"Run `dotnet ef migrations add <Name>`. dotnet-ef said: {output}");
    }

    // TRAP: The agent hand-wrote a migration with plausible-looking SQL.
    //       It compiles, `has-pending-model-changes` is happy (the snapshot was
    //       faked too), but the SQL is dialect-hallucinated and dies on Postgres.
    // GUARDRAIL: Apply the FULL migration chain to an empty container.
    //       A hallucinated column type / index / constraint fails here, in CI,
    //       not at deploy time. Also catches a broken "middle" migration that
    //       works only on databases migrated step-by-step during development.
    [Test]
    public async Task Migrations_ShouldApplyToEmptyDatabase()
    {
        await using var postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .Build();
        await postgres.StartAsync();

        var (exitCode, output) = await RunDotnetEf(
            $"database update --project src/YourApp.Infrastructure --connection \"{postgres.GetConnectionString()}\"");

        Assert.That(exitCode)
            .IsEqualTo(0)
            .Because($"The migration chain must apply cleanly to an empty database. " +
                     $"dotnet-ef said: {output}");
    }

    // --- Helpers ---

    private static async Task<(int ExitCode, string Output)> RunDotnetEf(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"ef {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, $"{stdout}\n{stderr}");
    }
}

// TRAP: `has-pending-model-changes` compares against the model snapshot in the
//       Migrations folder — a determined agent can "fix" the red test by editing
//       the snapshot by hand. The empty-database check above is the backstop:
//       an edited snapshot does not make hallucinated SQL run.
