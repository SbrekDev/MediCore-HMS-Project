using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;

namespace MediCore.Api.IntegrationTests;

/// <summary>
/// Widening <c>GET /api/appointments</c> to clinical staff in T5 made the handler's ownership
/// rule the only thing between a professional and another professional's agenda, so the
/// filters that could tunnel past it are pinned here. The row-level leak vector is
/// <c>patientId</c>: passing another professional's patient must not return their appointment.
/// </summary>
public class AppointmentOwnershipScopingTests : IntegrationTestBase
{
    private static readonly Guid CardiologyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task A_professional_cannot_reach_another_professionals_appointments_through_any_filter()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the appointment scoping tests");

        using var client = Factory.CreateClient();
        await SeedUserAsync("scope-admin@ceibos.demo", "TestPass123!", "Admin");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "scope-admin@ceibos.demo", "TestPass123!"));

        var ownProfessional = await CreateProfessionalAsync(client, "Scope", "Owner", "SCOPE-MN-A");
        var otherProfessional = await CreateProfessionalAsync(client, "Scope", "Other", "SCOPE-MN-B");
        await AssignCardiologyAsync(client, ownProfessional);
        await AssignCardiologyAsync(client, otherProfessional);

        var ownPatient = await CreatePatientAsync(client, "39111001");
        var otherPatient = await CreatePatientAsync(client, "39111002");

        var day = DateTimeOffset.UtcNow.Date.AddDays(3);
        await BookAsync(client, ownPatient, ownProfessional, new DateTimeOffset(day.AddHours(10), TimeSpan.Zero));
        await BookAsync(client, otherPatient, otherProfessional, new DateTimeOffset(day.AddHours(9), TimeSpan.Zero));

        await SeedUserAsync("scope-professional@ceibos.demo", "TestPass123!", "Professional", ownProfessional);
        using var professionalClient = Factory.CreateClient();
        professionalClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAsync(professionalClient, "scope-professional@ceibos.demo", "TestPass123!"));

        var range = $"from={day:yyyy-MM-dd}T00:00:00Z&to={day.AddDays(1):yyyy-MM-dd}T00:00:00Z";

        // The tunnel: filtering by the OTHER professional's patient must not surface their row.
        var byOtherPatient = await professionalClient.GetAsync($"/api/appointments?{range}&patientId={otherPatient}");
        byOtherPatient.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAppointmentsAsync(byOtherPatient)).Should().BeEmpty();

        // The direct attempt: a foreign professional id, even combined with another filter.
        var byOtherProfessional =
            await professionalClient.GetAsync($"/api/appointments?{range}&professionalId={otherProfessional}&status=Confirmed");
        byOtherProfessional.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadErrorCodeAsync(byOtherProfessional)).Should().Be("Appointment.NotOwner");

        // And the happy path stays scoped: only the caller's own appointment, with the patient name.
        var unfiltered = await professionalClient.GetAsync($"/api/appointments?{range}");
        unfiltered.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await ReadAppointmentsAsync(unfiltered);
        rows.Should().ContainSingle();
        rows[0].GetProperty("professionalId").GetGuid().Should().Be(ownProfessional);
        rows[0].GetProperty("patientName").GetString().Should().StartWith("Scope ");
    }

    [Fact]
    public async Task A_professional_account_with_no_link_is_refused_instead_of_getting_an_empty_list()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the appointment scoping tests");

        using var client = Factory.CreateClient();
        await SeedUserAsync("scope-unlinked@ceibos.demo", "TestPass123!", "Professional");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "scope-unlinked@ceibos.demo", "TestPass123!"));

        var response = await client.GetAsync(
            $"/api/appointments?from={DateTimeOffset.UtcNow.Date:yyyy-MM-dd}T00:00:00Z" +
            $"&to={DateTimeOffset.UtcNow.Date.AddDays(1):yyyy-MM-dd}T00:00:00Z");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadErrorCodeAsync(response)).Should().Be("Appointment.NotOwner");
    }

    private static async Task<Guid> CreateProfessionalAsync(
        HttpClient client,
        string firstName,
        string lastName,
        string nationalLicense)
    {
        var response = await client.PostAsJsonAsync("/api/professionals", new
        {
            firstName,
            lastName,
            nationalLicenseMN = nationalLicense,
            provincialLicenses = Array.Empty<object>()
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("professionalId").GetGuid();
    }

    private static async Task AssignCardiologyAsync(HttpClient client, Guid professionalId)
    {
        var response = await client.PutAsJsonAsync(
            $"/api/professionals/{professionalId}/specialties/{CardiologyId}",
            new { isPrimary = true });

        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> CreatePatientAsync(HttpClient client, string documentNumber)
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            documentType = "DNI",
            documentNumber,
            firstName = "Scope",
            lastName = "Patient",
            birthDate = new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
            contact = new
            {
                mobilePhone = "+54 9 11 4000-0000",
                landlinePhone = (string?)null,
                email = (string?)null
            },
            address = new
            {
                street = "Calle Scope",
                number = "100",
                city = "Buenos Aires",
                province = "CABA",
                postalCode = "C1000"
            }
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("patientId").GetGuid();
    }

    private static async Task BookAsync(
        HttpClient client,
        Guid patientId,
        Guid professionalId,
        DateTimeOffset start)
    {
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologyId,
            start,
            isSobreturno = false
        });

        response.EnsureSuccessStatusCode();
    }

    private static async Task<IReadOnlyList<JsonElement>> ReadAppointmentsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.EnumerateArray().ToList();
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("code").GetString();
    }
}
