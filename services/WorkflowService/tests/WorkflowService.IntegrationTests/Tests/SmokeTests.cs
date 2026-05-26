using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using WorkflowService.IntegrationTests.Fixtures;
using WorkflowService.Data;
using Xunit;

namespace WorkflowService.IntegrationTests.Tests;

public class SmokeTests : BaseIntegrationTest<WorkflowServiceFixture, WorkflowDbContext>
{
    public SmokeTests(WorkflowServiceFixture fixture) : base(fixture)
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
        content.Should().Contain("Workflow Service is running");
    }
}