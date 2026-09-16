// TRAP: EF Core InMemory is not a database — it is a collection in memory. It silently
//        forgives what a real database breaks in production: tracking semantics
//        (AsNoTracking leaking into the write path), transaction loss, invalid SQL
//        translation, constraint violations. Unit tests against InMemory stay green
//        while the write path is broken. The observed case: an AsNoTracking()
//        "read optimization" leaked into a save path and lived ~24 hours in beta;
//        InMemory tests never noticed — the entity was simply not tracked.
// GUARDRAIL: Integration tests run against a REAL database in Docker (Testcontainers).
//        A fixed Postgres container per test class; the test asserts persistence
//        behavior (what survives SaveChanges + a fresh query), not object graphs.
//
// Requires: dotnet add package Testcontainers.PostgreSql
//           dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
//
// Framework adaptation:
// - TUnit:  [Test] + Assert.That(...), IAsyncInitializer for container startup
// - xUnit:  [Fact] + IClassFixture<PostgresFixture>
// - NUnit:  [Test] + [OneTimeSetUp]
// - MSTest: [TestMethod] + [ClassInitialize]

using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TUnit;

namespace Tests.Patterns;

public class IntegrationDatabaseTest
{
    // TRAP: A new container per test method is slow; a shared static container per class
    //        is fast enough for CI and still isolates tests via unique data per test.
    private static readonly PostgreSqlContainer Postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("app_test")
        .Build();

    public static async Task InitializeAsync()
    {
        await Postgres.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync(); // or: await db.Database.MigrateAsync();
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Postgres.GetConnectionString())
            .Options);

    // TRAP: The agent added AsNoTracking() to a query that is later used to UPDATE.
    //        Under InMemory the context still "knows" the entity in many setups, so
    //        unit tests pass. Under real Postgres with a fresh context the update
    //        is a no-op — the slot stays booked-able, two clients book the same slot.
    // GUARDRAIL: Assert the persisted state through a SECOND context (fresh query),
    //        not through the tracked instance from the first context.
    [Test]
    public async Task CancelBooking_ShouldPersistToRealDatabase()
    {
        await using (var arrange = CreateDbContext())
        {
            arrange.Slots.Add(new Slot { Id = 42, IsBooked = true });
            await arrange.SaveChangesAsync();
        }

        await using (var act = CreateDbContext())
        {
            // The write path under test. If it silently uses a non-tracked read
            // (AsNoTracking) before mutation, SaveChanges writes nothing.
            var slot = await act.Slots.SingleAsync(s => s.Id == 42);
            slot.IsBooked = false;
            await act.SaveChangesAsync();
        }

        // Verify through a THIRD context: what actually survives in the database.
        await using (var assert = CreateDbContext())
        {
            var reloaded = await assert.Slots.AsNoTracking().SingleAsync(s => s.Id == 42);
            Assert.That(reloaded.IsBooked).IsFalse()
                .Because("CancelBooking must persist; a no-op SaveChanges here is the " +
                         "AsNoTracking-in-write-path trap that InMemory tests cannot catch");
        }
    }
}

// --- Placeholder types: replace with your real entity and context ---

public class Slot
{
    public long Id { get; set; }
    public bool IsBooked { get; set; }
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Slot> Slots => Set<Slot>();
}
