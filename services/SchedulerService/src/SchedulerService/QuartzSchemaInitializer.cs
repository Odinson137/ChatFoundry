using Npgsql;

namespace SchedulerService;

public class QuartzSchemaInitializer(
    string connectionString,
    ILogger<QuartzSchemaInitializer> logger) : IHostedService
{
    private const string CheckTableSql = """
        SELECT EXISTS (
            SELECT FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name = 'qrtz_job_details'
        )
        """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var exists = await CheckTableExists(conn, cancellationToken);
        if (exists)
        {
            logger.LogInformation("Quartz tables already exist, skipping initialization");
            return;
        }

        logger.LogInformation("Quartz tables not found, creating schema...");

        var sql = await GetEmbeddedSql();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Quartz schema created successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<bool> CheckTableExists(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(CheckTableSql, conn);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToBoolean(result);
    }

    private static async Task<string> GetEmbeddedSql()
    {
        var assembly = typeof(QuartzSchemaInitializer).Assembly;
        var resourceName = "SchedulerService.Scripts.quartz_postgres.sql";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
