using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;

namespace MediCore.Api.IntegrationTests;

public class AgendaEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task Get_availability_returns_windows_minus_exception_and_surfaces_sobreturno_cap()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for agenda endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("admin@ceibos.demo", "TestPass123!", "Admin");
        var adminToken = await LoginAsync(client, "admin@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var create = await client.PostAsJsonAsync("/api/professionals", new
        {
            firstName = "John",
            lastName = "Doe",
            nationalLicenseMN = "12345",
            provincialLicenses = Array.Empty<object>()
        });
        create.EnsureSuccessStatusCode();
        var professional = await create.Content.ReadFromJsonAsync<CreateProfessionalResponse>();

        var putAgenda = await client.PutAsJsonAsync($"/api/professionals/{professional!.ProfessionalId}/agenda", new
        {
            maxSobreturnosPerDay = 2,
            blocks = new[]
            {
                new { dayOfWeek = "Monday", start = "08:00", end = "12:00" },
                new { dayOfWeek = "Monday", start = "14:00", end = "18:00" }
            }
        });
        putAgenda.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var postException = await client.PostAsJsonAsync($"/api/professionals/{professional.ProfessionalId}/agenda/exceptions", new
        {
            date = "2026-10-05",
            start = "08:00",
            end = "12:00",
            reason = "Morning leave"
        });
        postException.EnsureSuccessStatusCode();

        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var receptionToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", receptionToken);

        var response = await client.GetAsync($"/api/professionals/{professional.ProfessionalId}/availability?date=2026-10-05");
        response.EnsureSuccessStatusCode();

        var availability = await response.Content.ReadFromJsonAsync<AvailabilityResponse>();
        availability!.ProfessionalId.Should().Be(professional.ProfessionalId);
        availability.Date.Should().Be(new DateOnly(2026, 10, 5));
        availability.MaxSobreturnosPerDay.Should().Be(2);
        availability.Windows.Should().ContainSingle();
        availability.Windows.Single().Start.Should().Be(new TimeOnly(14, 0));
        availability.Windows.Single().End.Should().Be(new TimeOnly(18, 0));
    }

    [Fact]
    public async Task Get_availability_for_unknown_professional_returns_not_found()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for agenda endpoint tests");

        var client = Factory.CreateClient();
        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var token = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/professionals/{Guid.NewGuid()}/availability?date=2026-10-05");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CreateProfessionalResponse(Guid ProfessionalId);

    private sealed record AvailabilityResponse(
        Guid ProfessionalId,
        DateOnly Date,
        IReadOnlyList<AvailabilityWindow> Windows,
        int MaxSobreturnosPerDay);

    private sealed record AvailabilityWindow(TimeOnly Start, TimeOnly End);
}
