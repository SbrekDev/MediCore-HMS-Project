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

    [Fact]
    public async Task Post_reschedule_to_free_slot_updates_start_and_keeps_confirmed()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var newStart = start.AddHours(2);
        var response = await client.PostAsJsonAsync($"/api/appointments/{appointment!.Id}/reschedule", new
        {
            start = newStart
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        updated.StartDateTime.Should().Be(newStart);
        updated.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Post_reschedule_to_overlapping_slot_returns_conflict_and_leaves_appointment_unchanged()
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

        var secondPatientId = await CreatePatientAsync(client, "DNI", "39222333");
        var secondStart = start.AddHours(2);
        var second = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId = secondPatientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start = secondStart,
            isSobreturno = false
        });
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondAppointment = await second.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var response = await client.PostAsJsonAsync($"/api/appointments/{secondAppointment!.Id}/reschedule", new
        {
            start = start.AddMinutes(15)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.ProfessionalOverlap");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await dbContext.Appointments.SingleAsync(a => a.Id == secondAppointment.Id);
        unchanged.StartDateTime.Should().Be(secondStart);
        unchanged.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Post_reschedule_within_two_hours_returns_bad_request_and_leaves_appointment_unchanged()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var tooSoon = DateTimeOffset.UtcNow.AddHours(1);
        var response = await client.PostAsJsonAsync($"/api/appointments/{appointment!.Id}/reschedule", new
        {
            start = tooSoon
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.OutsideBookingWindow");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        unchanged.StartDateTime.Should().Be(start);
        unchanged.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Post_reschedule_own_overlapping_slot_succeeds()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var newStart = start.AddMinutes(15);
        var response = await client.PostAsJsonAsync($"/api/appointments/{appointment!.Id}/reschedule", new
        {
            start = newStart
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        updated.StartDateTime.Should().Be(newStart);
        updated.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Post_reschedule_cancelled_appointment_returns_conflict()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var cancelledId = Guid.NewGuid();

        using (var seedScope = Factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            seedDb.Appointments.Add(new Appointment(cancelledId)
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
            });
            await seedDb.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync($"/api/appointments/{cancelledId}/reschedule", new
        {
            start = start.AddHours(3)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.InvalidState");
    }

    [Fact]
    public async Task Post_reschedule_missing_appointment_returns_not_found()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, _, _) = await SeedScenarioAsync();

        var response = await client.PostAsJsonAsync($"/api/appointments/{Guid.NewGuid()}/reschedule", new
        {
            start = BuildStart().AddHours(3)
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.NotFound");
    }

    [Fact]
    public async Task Post_cancel_more_than_24h_before_start_marks_cancelled_without_late_flag()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cancelled = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        cancelled.Status.Should().Be(AppointmentStatus.Cancelled);
        cancelled.IsLateCancellation.Should().BeFalse();
    }

    [Fact]
    public async Task Post_cancel_within_24h_marks_cancelled_and_flagged_late()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = DateTimeOffset.UtcNow.AddHours(3);
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cancelled = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        cancelled.Status.Should().Be(AppointmentStatus.Cancelled);
        cancelled.IsLateCancellation.Should().BeTrue();
    }

    [Fact]
    public async Task Post_complete_own_appointment_marks_completed()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        await SeedUserAsync("professional-own@ceibos.demo", "TestPass123!", "Professional", professionalId);
        var professionalToken = await LoginAsync(client, "professional-own@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var completed = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        completed.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public async Task Post_complete_another_professionals_appointment_returns_forbidden_and_leaves_appointment_unchanged()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, adminToken, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var otherProfessionalId = await CreateProfessionalAsync(client, "Ana", "Gomez", "OTH0001");

        await SeedUserAsync("professional-other@ceibos.demo", "TestPass123!", "Professional", otherProfessionalId);
        var otherToken = await LoginAsync(client, "professional-other@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.NotOwner");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        unchanged.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Post_noshow_own_appointment_marks_noshow_and_increments_patient_counter()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        await SeedUserAsync("professional-noshow@ceibos.demo", "TestPass123!", "Professional", professionalId);
        var professionalToken = await LoginAsync(client, "professional-noshow@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/no-show", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var noShow = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        noShow.Status.Should().Be(AppointmentStatus.NoShow);

        var patient = await dbContext.Patients.SingleAsync(p => p.Id == patientId);
        patient.NoShowCount.Should().Be(1);
    }

    [Fact]
    public async Task Post_complete_cancelled_appointment_returns_conflict()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var cancelledId = Guid.NewGuid();

        using (var seedScope = Factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            seedDb.Appointments.Add(new Appointment(cancelledId)
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
            });
            await seedDb.SaveChangesAsync();
        }

        await SeedUserAsync("professional-cancelled@ceibos.demo", "TestPass123!", "Professional", professionalId);
        var professionalToken = await LoginAsync(client, "professional-cancelled@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var response = await client.PostAsync($"/api/appointments/{cancelledId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.InvalidState");
    }

    [Fact]
    public async Task Post_noshow_another_professionals_appointment_returns_forbidden_and_leaves_counter_unchanged()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, adminToken, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var otherProfessionalId = await CreateProfessionalAsync(client, "Ana", "Gomez", "OTH0002");

        await SeedUserAsync("professional-noshow-other@ceibos.demo", "TestPass123!", "Professional", otherProfessionalId);
        var otherToken = await LoginAsync(client, "professional-noshow-other@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/no-show", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.NotOwner");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        unchanged.Status.Should().Be(AppointmentStatus.Confirmed);

        var patient = await dbContext.Patients.SingleAsync(p => p.Id == patientId);
        patient.NoShowCount.Should().Be(0);
    }

    [Fact]
    public async Task Post_complete_by_professional_without_linked_professional_id_returns_forbidden()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        await SeedUserAsync("professional-unlinked@ceibos.demo", "TestPass123!", "Professional");
        var unlinkedToken = await LoginAsync(client, "professional-unlinked@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", unlinkedToken);

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.NotOwner");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        unchanged.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Post_cancel_completed_appointment_returns_conflict()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var completedId = Guid.NewGuid();

        using (var seedScope = Factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            seedDb.Appointments.Add(new Appointment(completedId)
            {
                PatientId = patientId,
                ProfessionalId = professionalId,
                SpecialtyId = CardiologySpecialtyId,
                StartDateTime = start,
                DurationMinutes = 30,
                Status = AppointmentStatus.Completed,
                IsSobreturno = false,
                IsLateCancellation = false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.Empty,
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedBy = Guid.Empty
            });
            await seedDb.SaveChangesAsync();
        }

        var response = await client.PostAsync($"/api/appointments/{completedId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.InvalidState");
    }

    [Fact]
    public async Task Post_reschedule_more_than_90_days_ahead_returns_bad_request_and_leaves_appointment_unchanged()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var tooFar = DateTimeOffset.UtcNow.AddDays(91);
        var response = await client.PostAsJsonAsync($"/api/appointments/{appointment!.Id}/reschedule", new
        {
            start = tooFar
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.OutsideBookingWindow");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        unchanged.StartDateTime.Should().Be(start);
        unchanged.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Post_reschedule_onto_slot_overlapping_previous_day_appointment_returns_conflict_and_leaves_appointment_unchanged()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        // Existing appointment starts the day before the requested slot and crosses midnight into it.
        var crossingStart = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(4).AddHours(23).AddMinutes(40), TimeSpan.Zero);
        using (var seedScope = Factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            seedDb.Appointments.Add(new Appointment(Guid.NewGuid())
            {
                PatientId = patientId,
                ProfessionalId = professionalId,
                SpecialtyId = CardiologySpecialtyId,
                StartDateTime = crossingStart,
                DurationMinutes = 30,
                Status = AppointmentStatus.Confirmed,
                IsSobreturno = false,
                IsLateCancellation = false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.Empty,
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedBy = Guid.Empty
            });
            await seedDb.SaveChangesAsync();
        }

        var requested = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(5), TimeSpan.Zero);
        var response = await client.PostAsJsonAsync($"/api/appointments/{appointment!.Id}/reschedule", new
        {
            start = requested
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.ProfessionalOverlap");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await dbContext.Appointments.SingleAsync(a => a.Id == appointment.Id);
        unchanged.StartDateTime.Should().Be(start);
        unchanged.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Post_noshow_concurrently_for_the_same_patient_increments_the_counter_once_per_appointment()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var appointmentIds = new List<Guid>();

        for (var slot = 0; slot < 6; slot++)
        {
            var booked = await client.PostAsJsonAsync("/api/appointments", new
            {
                patientId,
                professionalId,
                specialtyId = CardiologySpecialtyId,
                start = start.AddMinutes(slot * 30),
                isSobreturno = false
            });
            booked.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();
            appointmentIds.Add(created!.Id);
        }

        await SeedUserAsync("professional-noshow-concurrent@ceibos.demo", "TestPass123!", "Professional", professionalId);
        var professionalToken = await LoginAsync(client, "professional-noshow-concurrent@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var responses = await Task.WhenAll(appointmentIds.Select(id =>
            client.PostAsync($"/api/appointments/{id}/no-show", null)));

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var patient = await dbContext.Patients.AsNoTracking().SingleAsync(p => p.Id == patientId);
        patient.NoShowCount.Should().Be(appointmentIds.Count);
    }

    [Fact]
    public async Task Post_noshow_twice_concurrently_for_the_same_appointment_marks_it_once_and_counts_once()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        await SeedUserAsync("professional-noshow-race@ceibos.demo", "TestPass123!", "Professional", professionalId);
        var professionalToken = await LoginAsync(client, "professional-noshow-race@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var race = await Task.WhenAll(
            client.PostAsync($"/api/appointments/{appointment!.Id}/no-show", null),
            client.PostAsync($"/api/appointments/{appointment!.Id}/no-show", null));

        race.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        race.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var patient = await dbContext.Patients.AsNoTracking().SingleAsync(p => p.Id == patientId);
        patient.NoShowCount.Should().Be(1);
    }

    [Fact]
    public async Task Post_noshow_writes_the_patient_counter_audit_entry()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        await SeedUserAsync("professional-noshow-audit@ceibos.demo", "TestPass123!", "Professional", professionalId);
        var professionalToken = await LoginAsync(client, "professional-noshow-audit@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/no-show", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var counterAudits = await dbContext.AuditEntries
            .AsNoTracking()
            .Where(e => e.EntityId == patientId && e.Action == "Updated")
            .Select(e => e.ChangesJson)
            .ToListAsync();

        counterAudits.Should().ContainSingle();
        counterAudits[0].Should().Contain("NoShowCount");
    }

    [Fact]
    public async Task Post_appointment_into_a_previous_day_midnight_crossing_appointment_returns_conflict_and_creates_no_row()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        // Existing appointment starts the day before the requested slot and crosses midnight into it.
        var crossingStart = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(4).AddHours(23).AddMinutes(40), TimeSpan.Zero);
        using (var seedScope = Factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            seedDb.Appointments.Add(new Appointment(Guid.NewGuid())
            {
                PatientId = patientId,
                ProfessionalId = professionalId,
                SpecialtyId = CardiologySpecialtyId,
                StartDateTime = crossingStart,
                DurationMinutes = 30,
                Status = AppointmentStatus.Confirmed,
                IsSobreturno = false,
                IsLateCancellation = false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.Empty,
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedBy = Guid.Empty
            });
            await seedDb.SaveChangesAsync();
        }

        var requested = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(5).AddMinutes(5), TimeSpan.Zero);
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start = requested,
            isSobreturno = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.ProfessionalOverlap");

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var created = await dbContext.Appointments.AnyAsync(a => a.StartDateTime == requested);
        created.Should().BeFalse();
    }

    [Fact]
    public async Task Get_appointments_by_date_returns_list_ordered_by_start_and_optionally_filtered_by_professional()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, adminToken, professionalA, patientA) = await SeedScenarioAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var professionalB = await CreateProfessionalAsync(client, "Ana", "Gomez", "LST0001");
        var assignB = await client.PutAsJsonAsync($"/api/professionals/{professionalB}/specialties/{CardiologySpecialtyId}", new { isPrimary = true });
        assignB.EnsureSuccessStatusCode();
        var patientB = await CreatePatientAsync(client, "DNI", "39111333");

        // Admin is allowed to book and list appointments.
        var date = DateTimeOffset.UtcNow.Date.AddDays(3);
        var startA1 = new DateTimeOffset(date.AddHours(10), TimeSpan.Zero);
        var startB1 = new DateTimeOffset(date.AddHours(9), TimeSpan.Zero);
        var startA2 = new DateTimeOffset(date.AddHours(14), TimeSpan.Zero);

        var bookingA1 = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId = patientA,
            professionalId = professionalA,
            specialtyId = CardiologySpecialtyId,
            start = startA1,
            isSobreturno = false
        });
        bookingA1.StatusCode.Should().Be(HttpStatusCode.Created);

        var bookingB1 = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId = patientB,
            professionalId = professionalB,
            specialtyId = CardiologySpecialtyId,
            start = startB1,
            isSobreturno = false
        });
        bookingB1.StatusCode.Should().Be(HttpStatusCode.Created);

        var bookingA2 = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId = patientA,
            professionalId = professionalA,
            specialtyId = CardiologySpecialtyId,
            start = startA2,
            isSobreturno = false
        });
        bookingA2.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync(
            $"/api/appointments?from={date:yyyy-MM-dd}T00:00:00Z&to={date.AddDays(1):yyyy-MM-dd}T00:00:00Z");
        listResponse.EnsureSuccessStatusCode();

        var appointments = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<AppointmentListItem>>();
        appointments!.Should().HaveCount(3);
        appointments.Select(a => a.Start).Should().BeInAscendingOrder();
        appointments.Select(a => a.ProfessionalId).Should().Contain(professionalA);
        appointments.Select(a => a.ProfessionalId).Should().Contain(professionalB);

        var filteredResponse = await client.GetAsync(
            $"/api/appointments?from={date:yyyy-MM-dd}T00:00:00Z&to={date.AddDays(1):yyyy-MM-dd}T00:00:00Z&professionalId={professionalA}");
        filteredResponse.EnsureSuccessStatusCode();

        var filtered = await filteredResponse.Content.ReadFromJsonAsync<IReadOnlyList<AppointmentListItem>>();
        filtered!.Should().HaveCount(2);
        filtered.Should().OnlyContain(a => a.ProfessionalId == professionalA);
    }

    [Fact]
    public async Task Get_appointments_without_range_returns_bad_request_RangeRequired()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, _, _) = await SeedScenarioAsync();

        var response = await client.GetAsync("/api/appointments");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.RangeRequired");
    }

    [Fact]
    public async Task Get_appointments_with_a_date_but_no_offset_returns_bad_request_RangeRequired()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, _, _) = await SeedScenarioAsync();

        var response = await client.GetAsync("/api/appointments?from=2026-11-12&to=2026-11-13");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Code.Should().Be("Appointment.RangeRequired");
    }

    [Fact]
    public async Task Get_appointments_with_an_empty_or_inverted_range_returns_bad_request_InvalidRange()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, _, _) = await SeedScenarioAsync();

        var inverted = await client.GetAsync(
            "/api/appointments?from=2026-11-13T00:00:00Z&to=2026-11-12T00:00:00Z");
        inverted.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await inverted.Content.ReadFromJsonAsync<ErrorResponse>())!.Code
            .Should().Be("Appointment.InvalidRange");

        var empty = await client.GetAsync(
            "/api/appointments?from=2026-11-12T00:00:00Z&to=2026-11-12T00:00:00Z");
        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await empty.Content.ReadFromJsonAsync<ErrorResponse>())!.Code
            .Should().Be("Appointment.InvalidRange");
    }

    [Fact]
    public async Task Get_appointments_with_the_local_day_range_includes_a_late_evening_local_slot()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        // 21:00 on the local day (UTC-03:00) is already the next UTC day at 00:00Z.
        var localDay = DateTime.UtcNow.Date.AddDays(4);
        var start = new DateTimeOffset(localDay.AddDays(1), TimeSpan.Zero);

        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);

        // The local day D spans [D 03:00Z, D+1 03:00Z) for UTC-03:00.
        var localRangeFrom = localDay.AddHours(3);
        var localRangeTo = localDay.AddDays(1).AddHours(3);
        var localRange = await client.GetAsync(
            $"/api/appointments?from={localRangeFrom:yyyy-MM-dd'T'HH:mm:ss}Z&to={localRangeTo:yyyy-MM-dd'T'HH:mm:ss}Z");
        localRange.EnsureSuccessStatusCode();

        var inLocalDay = await localRange.Content.ReadFromJsonAsync<IReadOnlyList<AppointmentListItem>>();
        inLocalDay!.Select(a => a.Start).Should().Contain(start);

        // The UTC day D+1 starts at the same instant, so a UTC-day query would include it too;
        // the UTC day D ends exactly there and would drop it.
        var utcDay = await client.GetAsync(
            $"/api/appointments?from={localDay:yyyy-MM-dd'T'HH:mm:ss}Z&to={localDay.AddDays(1):yyyy-MM-dd'T'HH:mm:ss}Z");
        utcDay.EnsureSuccessStatusCode();

        var inUtcDay = await utcDay.Content.ReadFromJsonAsync<IReadOnlyList<AppointmentListItem>>();
        inUtcDay!.Select(a => a.Start).Should().NotContain(start);
    }

    [Fact]
    public async Task Get_appointments_requires_reception_staff()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, _) = await SeedScenarioAsync();

        await SeedUserAsync("professional-list@ceibos.demo", "TestPass123!", "Professional", professionalId);
        var professionalToken = await LoginAsync(client, "professional-list@ceibos.demo", "TestPass123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var date = DateTimeOffset.UtcNow.Date.AddDays(3);
        var response = await client.GetAsync(
            $"/api/appointments?from={date:yyyy-MM-dd}T00:00:00Z&to={date.AddDays(1):yyyy-MM-dd}T00:00:00Z");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_cancel_more_than_24h_response_includes_isLateCancellation_false()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = BuildStart();
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CancelResponse>();
        body!.Id.Should().Be(appointment.Id);
        body.Status.Should().Be("Cancelled");
        body.IsLateCancellation.Should().BeFalse();

        // A missing property deserializes into the default false, so pin the key itself.
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("isLateCancellation");
    }

    [Fact]
    public async Task Post_cancel_within_24h_response_includes_isLateCancellation_true()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for appointment endpoint tests");

        var (client, _, professionalId, patientId) = await SeedScenarioAsync();

        var start = DateTimeOffset.UtcNow.AddHours(3);
        var booked = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId,
            professionalId,
            specialtyId = CardiologySpecialtyId,
            start,
            isSobreturno = false
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointment = await booked.Content.ReadFromJsonAsync<CreateAppointmentResponse>();

        var response = await client.PostAsync($"/api/appointments/{appointment!.Id}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CancelResponse>();
        body!.Id.Should().Be(appointment.Id);
        body.Status.Should().Be("Cancelled");
        body.IsLateCancellation.Should().BeTrue();
    }

    private sealed record CancelResponse(Guid Id, string Status, bool IsLateCancellation);

    private sealed record AppointmentListItem(
        Guid Id,
        Guid PatientId,
        Guid ProfessionalId,
        Guid SpecialtyId,
        DateTimeOffset Start,
        int DurationMinutes,
        string Status,
        bool IsSobreturno,
        bool IsLateCancellation);

    private sealed record CreateAppointmentResponse(Guid Id);

    private sealed record CreateProfessionalResponse(Guid ProfessionalId);

    private sealed record CreatePatientResponse(Guid PatientId);

    private sealed record ErrorResponse(string Code, string Message);

}
