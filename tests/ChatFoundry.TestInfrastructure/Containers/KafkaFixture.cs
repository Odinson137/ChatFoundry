using Testcontainers.Kafka;

namespace ChatFoundry.TestInfrastructure.Containers;

public class KafkaFixture
{
    private static readonly Lazy<KafkaContainer> Container = new(() =>
        new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.6.0")
            .Build());

    public static KafkaContainer Instance => Container.Value;

    public static string BootstrapServers => Instance.GetBootstrapAddress();

    public static async Task StartAsync()
    {
        await Instance.StartAsync();
    }
}
