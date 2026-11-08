using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;

namespace MediCore.Api.IntegrationTests;

public class RoleAccessTests : IntegrationTestBase
{
    [Theory]
    [InlineData("admin@ceibos.demo", "Admin", HttpStatusCode.NotFound)]
    [InlineData("receptionist@ceibos.demo", "Receptionist", HttpStatusCode.NotFound)]
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

    [Theory]
    [InlineData("reschedule", "admin-lc@ceibos.demo", "Admin", HttpStatusCode.NotFound)]
    [InlineData("reschedule", "receptionist-lc@ceibos.demo", "Receptionist", HttpStatusCode.NotFound)]
    [InlineData("reschedule", "professional-lc@ceibos.demo", "Professional", HttpStatusCode.Forbidden)]
    [InlineData("cancel", "admin-lc@ceibos.demo", "Admin", HttpStatusCode.NotFound)]
    [InlineData("cancel", "receptionist-lc@ceibos.demo", "Receptionist", HttpStatusCode.NotFound)]
    [InlineData("cancel", "professional-lc@ceibos.demo", "Professional", HttpStatusCode.Forbidden)]
    [InlineData("complete", "admin-lc@ceibos.demo", "Admin", HttpStatusCode.NotFound)]
    [InlineData("complete", "receptionist-lc@ceibos.demo", "Receptionist", HttpStatusCode.NotFound)]
    [InlineData("complete", "professional-lc@ceibos.demo", "Professional", HttpStatusCode.NotFound)]
    [InlineData("no-show", "admin-lc@ceibos.demo", "Admin", HttpStatusCode.NotFound)]
    [InlineData("no-show", "receptionist-lc@ceibos.demo", "Receptionist", HttpStatusCode.NotFound)]
    [InlineData("no-show", "professional-lc@ceibos.demo", "Professional", HttpStatusCode.NotFound)]
    public async Task Lifecycle_endpoints_enforce_role_policies(
        string action,
        string email,
        string role,
        HttpStatusCode expectedStatus)
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for role access integration tests");

        var client = Factory.CreateClient();
        await SeedUserAsync(email, "TestPass123!", role);
        var accessToken = await LoginAsync(client, email, "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync($"/api/appointments/{Guid.NewGuid()}/{action}", new
        {
            start = DateTimeOffset.UtcNow.AddDays(3)
        });

        response.StatusCode.Should().Be(expectedStatus);
    }
}
