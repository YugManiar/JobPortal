using Xunit;

namespace JobPortal.IntegrationTests.Fixtures
{
    // Groups every integration test class into one shared "collection" so
    // xUnit creates ONE TestDatabaseFixture for the whole test run instead
    // of one per class. This avoids two classes racing to EnsureDeleted()/
    // EnsureCreated() the same LocalDB database at the same time.
    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
    {
        // Intentionally empty — this class only exists to attach the
        // [CollectionDefinition] attribute to ICollectionFixture<T>.
    }
}