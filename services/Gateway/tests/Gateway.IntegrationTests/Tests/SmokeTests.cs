using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using Gateway.IntegrationTests.Fixtures;
using Xunit;

namespace Gateway.IntegrationTests.Tests;

public class SmokeTests : BaseStatelessIntegrationTest<GatewayFixture>
{
    public SmokeTests(GatewayFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Service_ShouldStartAndRespond()
    {
        // Act
        var response = await Client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}