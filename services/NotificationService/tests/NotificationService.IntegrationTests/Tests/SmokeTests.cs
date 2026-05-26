using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using NotificationService.IntegrationTests.Fixtures;
using NotificationService.Data;
using Xunit;

namespace NotificationService.IntegrationTests.Tests;

public class SmokeTests : BaseIntegrationTest<NotificationServiceFixture, NotificationDbContext>
{
    public SmokeTests(NotificationServiceFixture fixture) : base(fixture)
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
        content.Should().Contain("Notification Service is running");
    }
}