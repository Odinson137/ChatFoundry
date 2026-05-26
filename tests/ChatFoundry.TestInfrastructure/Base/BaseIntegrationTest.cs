using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Interfaces;
using ChatFoundry.TestInfrastructure.Extensions;
using Xunit;

namespace ChatFoundry.TestInfrastructure.Base;

public abstract class BaseIntegrationTest<TFixture, TDbContext>
    : IClassFixture<TFixture>, IAsyncLifetime
    where TFixture : class, ITestFixture
    where TDbContext : DbContext
{
    protected TFixture Fixture { get; }
    protected HttpClient Client { get; }
    protected IServiceScope Scope { get; private set; } = null!;
    protected TDbContext DbContext { get; private set; } = null!;
    protected IServiceProvider Services => Scope.ServiceProvider;

    protected BaseIntegrationTest(TFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
    }

    public virtual async Task InitializeAsync()
    {
        if (Fixture.Respawner != null)
        {
            await Fixture.Respawner.ResetAsync();
        }

        Scope = Fixture.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<TDbContext>();
    }

    public virtual Task DisposeAsync()
    {
        Scope.Dispose();
        return Task.CompletedTask;
    }

    // GraphQL helpers
    protected Task<T> GraphQlQueryAsync<T>(string query, object? variables = null) =>
        Client.PostGraphQlAsync<T>(query, variables);

    protected Task<T> GraphQlMutationAsync<T>(string mutation, object? variables = null) =>
        Client.PostGraphQlAsync<T>(mutation, variables);

    // Auth helpers
    protected void SetAuthUser(Guid userId, Guid? companyId = null, params string[] scopes)
    {
        Client.WithAuth(userId, companyId, scopes);
    }
}

public abstract class BaseStatelessIntegrationTest<TFixture>
    : IClassFixture<TFixture>, IAsyncLifetime
    where TFixture : class, ITestFixture
{
    protected TFixture Fixture { get; }
    protected HttpClient Client { get; }
    protected IServiceScope Scope { get; private set; } = null!;
    protected IServiceProvider Services => Scope.ServiceProvider;

    protected BaseStatelessIntegrationTest(TFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
    }

    public virtual Task InitializeAsync()
    {
        Scope = Fixture.Services.CreateScope();
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync()
    {
        Scope.Dispose();
        return Task.CompletedTask;
    }

    // Auth helpers
    protected void SetAuthUser(Guid userId, Guid? companyId = null, params string[] scopes)
    {
        Client.WithAuth(userId, companyId, scopes);
    }
}
