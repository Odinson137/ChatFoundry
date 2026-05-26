using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using IdentityServer.IntegrationTests.Fixtures;
using IdentityServer.Data;
using Xunit;

namespace IdentityServer.IntegrationTests.Tests;

public class SmokeTests : BaseIntegrationTest<IdentityServerFixture, IdentityDbContext>
{
    public SmokeTests(IdentityServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Service_ShouldStartAndRespond()
    {
        // Act
        var response = await Client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Identity Server is running");
    }
}