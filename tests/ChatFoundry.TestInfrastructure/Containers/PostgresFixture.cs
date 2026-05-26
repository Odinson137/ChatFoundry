using Testcontainers.PostgreSql;

namespace ChatFoundry.TestInfrastructure.Containers;

public class PostgresFixture
{
    private static readonly Lazy<PostgreSqlContainer> Container = new(() =>
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("chatfoundry_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build());

    public static PostgreSqlContainer Instance => Container.Value;

    public static string ConnectionString => Instance.GetConnectionString();

    public static async Task StartAsync()
    {
        await Instance.StartAsync();
    }
}
