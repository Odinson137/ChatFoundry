using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using ClientService.IntegrationTests.Fixtures;
using ClientService.Data;
using Xunit;

namespace ClientService.IntegrationTests.Tests;

public class SmokeTests : BaseIntegrationTest<ClientServiceFixture, ClientDbContext>
{
    public SmokeTests(ClientServiceFixture fixture) : base(fixture)
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
        content.Should().Contain("Client Service is running");
    }
}