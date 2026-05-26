using Testcontainers.Redis;

namespace ChatFoundry.TestInfrastructure.Containers;

public class RedisFixture
{
    private static readonly Lazy<RedisContainer> Container = new(() => 
        new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build());

    public static RedisContainer Instance => Container.Value;

    public static string ConnectionString => Instance.GetConnectionString();

    public static async Task StartAsync()
    {
        await Instance.StartAsync();
    }
}
