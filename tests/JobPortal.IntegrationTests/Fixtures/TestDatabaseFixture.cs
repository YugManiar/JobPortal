using JobPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.IntegrationTests.Fixtures
{
    // Creates a fresh, real LocalDB database for the test run, and drops it
    // afterward. This is intentionally NOT the in-memory provider — unique
    // indexes and cascade-delete rules are SQL Server features that the
    // in-memory provider does not enforce, so a fake database would let
    // real bugs in our Fluent API configuration pass silently.
    public class TestDatabaseFixture : IDisposable
    {
        public ApplicationDbContext Context { get; }

        private const string ConnectionString =
            "Server=(localdb)\\mssqllocaldb;Database=JobPortalTestDb;Trusted_Connection=True;TrustServerCertificate=True";

        public TestDatabaseFixture()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            Context = new ApplicationDbContext(options);

            // Start from a clean slate every test run.
            Context.Database.EnsureDeleted();
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Database.EnsureDeleted();
            Context.Dispose();
        }
    }
}