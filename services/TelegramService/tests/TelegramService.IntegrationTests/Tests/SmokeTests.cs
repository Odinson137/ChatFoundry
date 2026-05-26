using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using TelegramService.IntegrationTests.Fixtures;
using Xunit;

namespace TelegramService.IntegrationTests.Tests;

public class SmokeTests : BaseStatelessIntegrationTest<TelegramServiceFixture>
{
    public SmokeTests(TelegramServiceFixture fixture) : base(fixture)
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
        content.Should().Contain("Telegram Service is running");
    }
}