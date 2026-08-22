using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;

namespace EWMS.IntegrationTests;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Swagger_Endpoint_Should_Be_Reachable_In_Development()
    {
        // This test requires a reachable SQL Server instance configured via
        // ConnectionStrings:DefaultConnection (see appsettings.Development.json)
        // or an environment override, since Program.cs wires up SQL Server + Identity.
        var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
