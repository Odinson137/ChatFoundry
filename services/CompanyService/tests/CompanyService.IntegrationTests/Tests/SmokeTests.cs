using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using CompanyService.IntegrationTests.Fixtures;
using CompanyService.Data;
using Xunit;

namespace CompanyService.IntegrationTests.Tests;

public class SmokeTests : BaseIntegrationTest<CompanyServiceFixture, CompanyDbContext>
{
    public SmokeTests(CompanyServiceFixture fixture) : base(fixture)
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
        content.Should().Contain("Company Service is running");
    }
}