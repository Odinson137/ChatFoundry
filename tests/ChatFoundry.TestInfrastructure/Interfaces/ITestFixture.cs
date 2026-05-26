using ChatFoundry.TestInfrastructure.Database;

namespace ChatFoundry.TestInfrastructure.Interfaces;

public interface ITestFixture
{
    DatabaseRespawner? Respawner { get; }
    HttpClient CreateClient();
    IServiceProvider Services { get; }
}
