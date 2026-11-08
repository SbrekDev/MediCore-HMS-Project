using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Domain.Entities;
using MediCore.Domain.Enums;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

public class AppointmentEndpointTests : IntegrationTestBase
{
    private static readonly Guid CardiologySpecialtyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Post_appointment_by_receptionist_creates_confirmed_appointment_with_specialty_default_duration()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateAppointmentResponse>();
        result!.Id.Should().NotBe(Guid.Empty);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appointment = await dbContext.Appointments
            .SingleAsync(a => a.Id == result.Id);

        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
        appointment.DurationMinutes.Should().Be(30);
        appointment.IsSobreturno.Should().BeFalse();
    }

    [Fact]
    public async Task Post_appointment_uses_professional_specialty_override_duration()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, adminToken, professionalId, patientId) = await SeedScenarioAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var assign = await client.PutAsJsonAsync($"/api/professionals/{professionalId}/specialties/{CardiologySpecialtyId}", new
        {
            isPrimary = true,
            durationOverrideMinutes = 45
        });
        assign.EnsureSuccessStatusCode();

        await SeedUserAsync("receptionist-override@ceibos.demo", "TestPass123!", "Receptionist");
        var receptionToken = await LoginAsync(client, "receptionist-override@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", receptionToken);

        var start = BuildStart();
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appointment = await dbContext.Appointments
            .SingleAsync(a => a.Id == result!.Id);

        appointment.DurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task Post_overlapping_appointment_for_same_professional_returns_conflict()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, firstPatientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var first = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId = firstPatientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondPatientId = await CreatePatientAsync(client, "DNI", "39111222");
        var second = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId = secondPatientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await second.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.ProfessionalOverlap");
    }

    [Fact]
    public async Task Post_appointment_by_professional_role_returns_forbidden()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        await SeedUserAsync("professional-user@ceibos.demo", "TestPass123!", "Professional");
        var professionalToken = await LoginAsync(client, "professional-user@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start = BuildStart(),
            isSobreturno = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_concurrent_appointments_for_same_slot_results_in_exactly_one_created_and_one_conflict()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (adminClient, adminToken, professionalId, _) = await SeedScenarioAsync();

        var firstPatientId = await CreatePatientAsync(adminClient, "DNI", "40111222");
        var secondPatientId = await CreatePatientAsync(adminClient, "DNI", "41111222");

        await SeedUserAsync("receptionist-a@ceibos.demo", "TestPass123!", "Receptionist");
        var tokenA = await LoginAsync(adminClient, "receptionist-a@ceibos.demo", "TestPass123!");

        await SeedUserAsync("receptionist-b@ceibos.demo", "TestPass123!", "Receptionist");
        var tokenB = await LoginAsync(adminClient, "receptionist-b@ceibos.demo", "TestPass123!");

        var start = BuildStart();

        var clientA = Factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var clientB = Factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var taskA = clientA.PostAsJsonAsync("/api/appointments", new
        {
            patientId = firstPatientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        var taskB = clientB.PostAsJsonAsync("/api/appointments", new
        {
            patientId = secondPatientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });

        var responses = await Task.WhenAll(taskA, taskB);

        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.Created);
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.Conflict);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await dbContext.Appointments
            .CountAsync(a => a.ProfessionalId == professionalId && a.StartDateTime == start);

        count.Should().Be(1);
    }

    private async Task<(HttpClient Client, string AdminToken, Guid ProfessionalId, Guid PatientId)> SeedScenarioAsync()
    {
        var client = Factory.CreateClient();

        await SeedUserAsync("admin@ceibos.demo", "TestPass123!", "Admin");
        var adminToken = await LoginAsync(client, "admin@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var professionalId = await CreateProfessionalAsync(client, "Juan", "Perez", "12345MN");

        var assign = await client.PutAsJsonAsync($"/api/professionals/{professionalId}/specialties/{CardiologySpecialtyId}", new
        {
            isPrimary = true
        });
        assign.EnsureSuccessStatusCode();

        var patientId = await CreatePatientAsync(client, "DNI", "38111222");

        await SeedUserAsync("receptionist@ceibos.demo", "TestPass123!", "Receptionist");
        var receptionToken = await LoginAsync(client, "receptionist@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", receptionToken);

        return (client, adminToken, professionalId, patientId);
    }

    private static async Task<Guid> CreateProfessionalAsync(HttpClient client, string firstName, string lastName, string nationalLicense)
    {
        var response = await client.PostAsJsonAsync("/api/professionals", new
        {
            firstName,
            lastName,
            nationalLicenseMN = nationalLicense,
            provincialLicenses = Array.Empty<object>()
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateProfessionalResponse>();
        return result!.ProfessionalId;
    }

    private static async Task<Guid> CreatePatientAsync(HttpClient client, string documentType, string documentNumber)
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            documentType,
            documentNumber,
            firstName = "Maria",
            lastName = "Gonzalez",
            birthDate = new DateTimeOffset(1985, 5, 15, 0, 0, 0, TimeSpan.Zero),
            contact = new
            {
                mobilePhone = "+54 9 11 1234-5678",
                landlinePhone = (string?)null,
                email = (string?)null
            },
            address = new
            {
                street = "Av. Santa Fe",
                number = "1234",
                city = "Buenos Aires",
                province = "CABA",
                postalCode = "C1059"
            }
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatePatientResponse>();
        return result!.PatientId;
    }

    private static DateTimeOffset BuildStart()
    {
        return new DateTimeOffset(DateTime.UtcNow.Date.AddDays(3).AddHours(10), TimeSpan.Zero);
    }

    [Fact]
    public async Task Post_concurrent_appointments_for_same_patient_and_start_results_in_exactly_one_created_and_one_patient_overlap_conflict()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (adminClient, adminToken, _, patientId) = await SeedScenarioAsync();

        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var professionalA = await CreateProfessionalAsync(adminClient, "Juan", "Perez", "PATM0001");
        var professionalB = await CreateProfessionalAsync(adminClient, "Ana", "Gomez", "PATM0002");

        var assignA = await adminClient.PutAsJsonAsync($"/api/professionals/{professionalA}/specialties/{CardiologySpecialtyId}", new { isPrimary = true });
        assignA.EnsureSuccessStatusCode();

        var assignB = await adminClient.PutAsJsonAsync($"/api/professionals/{professionalB}/specialties/{CardiologySpecialtyId}", new { isPrimary = true });
        assignB.EnsureSuccessStatusCode();

        await SeedUserAsync("receptionist-pa@ceibos.demo", "TestPass123!", "Receptionist");
        var tokenA = await LoginAsync(adminClient, "receptionist-pa@ceibos.demo", "TestPass123!");

        await SeedUserAsync("receptionist-pb@ceibos.demo", "TestPass123!", "Receptionist");
        var tokenB = await LoginAsync(adminClient, "receptionist-pb@ceibos.demo", "TestPass123!");

        var start = BuildStart();

        var clientA = Factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var clientB = Factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var taskA = clientA.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId = professionalA,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        var taskB = clientB.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId = professionalB,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });

        var responses = await Task.WhenAll(taskA, taskB);

        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.Created);
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.Conflict);

        var conflict = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        var error = await conflict.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.PatientOverlap");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await dbContext.Appointments
            .CountAsync(a => a.PatientId == patientId && a.StartDateTime == start);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Post_appointment_over_cancelled_appointment_for_same_professional_succeeds()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cancelled = new Appointment(Guid.NewGuid())
            {
                PatientId = patientId,
                ProfessionalId = professionalId,
                SpecialtyId = CardiologySpecialtyId,
                StartDateTime = start,
                DurationMinutes = 30,
                Status = AppointmentStatus.Cancelled,
                IsSobreturno = false,
                IsLateCancellation = false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.Empty,
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedBy = Guid.Empty
            };

            dbContext.Appointments.Add(cancelled);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var verifyScope = Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var confirmedCount = await verifyDb.Appointments
            .CountAsync(a => a.ProfessionalId == professionalId &&
                             a.StartDateTime == start &&
                             a.Status == AppointmentStatus.Confirmed);
        confirmedCount.Should().Be(1);

        var cancelledCount = await verifyDb.Appointments
            .CountAsync(a => a.ProfessionalId == professionalId &&
                             a.StartDateTime == start &&
                             a.Status == AppointmentStatus.Cancelled);
        cancelledCount.Should().Be(1);
    }

    private sealed record CreateAppointmentResponse(Guid Id);

    private sealed record CreateProfessionalResponse(Guid ProfessionalId);

    private sealed record CreatePatientResponse(Guid PatientId);

    private sealed record ErrorResponse(string Code, string Message);

}
