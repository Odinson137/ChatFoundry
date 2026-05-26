using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using FileService.IntegrationTests.Fixtures;
using FileService.Data;
using Xunit;

namespace FileService.IntegrationTests.Tests;

public class SmokeTests : BaseIntegrationTest<FileServiceFixture, FileDbContext>
{
    public SmokeTests(FileServiceFixture fixture) : base(fixture)
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
        content.Should().Contain("File Service is running");
    }
}