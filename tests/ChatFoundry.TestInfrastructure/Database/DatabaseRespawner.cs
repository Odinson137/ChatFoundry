using Npgsql;
using Respawn;

namespace ChatFoundry.TestInfrastructure.Database;

public class DatabaseRespawner
{
    private Respawner? _respawner;
    private readonly string _connectionString;

    public DatabaseRespawner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task ResetAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        if (_respawner == null)
        {
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new Respawn.Graph.Table[] { new Respawn.Graph.Table("__EFMigrationsHistory") }
            });
        }

        await _respawner.ResetAsync(connection);
    }
}
