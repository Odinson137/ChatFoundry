using FluentAssertions;
using ChatFoundry.TestInfrastructure.Base;
using BillingService.IntegrationTests.Fixtures;
using BillingService.Data;
using Xunit;

namespace BillingService.IntegrationTests.Tests;

public class SmokeTests : BaseIntegrationTest<BillingServiceFixture, BillingDbContext>
{
    public SmokeTests(BillingServiceFixture fixture) : base(fixture)
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
        content.Should().Contain("Billing Service is running");
    }
}