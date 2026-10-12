using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;

namespace MediCore.Api.IntegrationTests;

public class RoleAccessTests : IntegrationTestBase
{
    [Theory]
    [InlineData("admin@ceibos.demo", "Admin", HttpStatusCode.NoContent)]
    [InlineData("receptionist@ceibos.demo", "Receptionist", HttpStatusCode.NoContent)]
    [InlineData("professional@ceibos.demo", "Professional", HttpStatusCode.Forbidden)]
    public async Task Booking_endpoint_allows_Admin_and_Receptionist_and_forbids_Professional(
        string email,
        string role,
        HttpStatusCode expectedStatus)
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for role access integration tests");

        var client = Factory.CreateClient();
        await SeedUserAsync(email, "TestPass123!", role);
        var accessToken = await LoginAsync(client, email, "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/api/appointments", new { });

        response.StatusCode.Should().Be(expectedStatus);
    }
}
