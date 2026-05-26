using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using SchedulerService.IntegrationTests.Fixtures;
using Xunit;

namespace SchedulerService.IntegrationTests.Tests;

public class SmokeTests : BaseStatelessIntegrationTest<SchedulerServiceFixture>
{
    public SmokeTests(SchedulerServiceFixture fixture) : base(fixture)
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
        content.Should().Contain("Scheduler Service is running");
    }
}