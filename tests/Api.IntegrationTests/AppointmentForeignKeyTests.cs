using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediCore.Api.IntegrationTests.Infrastructure;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Api.IntegrationTests;

/// <summary>
/// The appointment table referenced patients and professionals by id without a foreign key,
/// so an appointment could point at rows that do not exist and a referenced row could be
/// deleted out from under it. These tests pin the database-level guarantee.
/// </summary>
public class AppointmentForeignKeyTests : IntegrationTestBase
{
    [Fact]
    public async Task An_appointment_cannot_reference_a_patient_that_does_not_exist()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the appointment FK tests");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var appointment = await NewAppointmentAsync(context, patientId: Guid.NewGuid());

        var insert = async () =>
        {
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
        };

        await insert.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task An_appointment_cannot_reference_a_professional_that_does_not_exist()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the appointment FK tests");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var appointment = await NewAppointmentAsync(context, professionalId: Guid.NewGuid());

        var insert = async () =>
        {
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
        };

        await insert.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task A_referenced_patient_and_professional_cannot_be_deleted()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the appointment FK tests");

        using var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@medicore.local", password = "MediCore1!" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patient = await client.PostAsJsonAsync("/api/patients", new
        {
            documentType = "DNI",
            documentNumber = "41999888",
            firstName = "Jorge",
            lastName = "Prueba",
            birthDate = new DateTimeOffset(1978, 3, 2, 0, 0, 0, TimeSpan.Zero),
            contact = new
            {
                mobilePhone = "+54 9 11 5555-5555",
                landlinePhone = (string?)null,
                email = (string?)null
            },
            address = new
            {
                street = "Calle Falsa",
                number = "742",
                city = "Buenos Aires",
                province = "CABA",
                postalCode = "C1000"
            }
        });
        patient.EnsureSuccessStatusCode();
        var patientId = (await patient.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("patientId").GetGuid();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "Sin",
            LastName = "Usuario",
            NationalLicenseMN = "MN-FK-PROBE",
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            ModifiedAt = now,
            ModifiedBy = Guid.Empty
        };
        context.Professionals.Add(professional);

        var appointment = await NewAppointmentAsync(context, patientId: patientId, professionalId: professional.Id);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var deletePatient = async () => await context.Database
            .ExecuteSqlInterpolatedAsync($"DELETE FROM Patients WHERE Id = {patientId}");
        var patientDelete = await deletePatient.Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>();
        patientDelete.Which.Message.Should().Contain("FK_Appointments_Patients_PatientId");

        var deleteProfessional = async () => await context.Database
            .ExecuteSqlInterpolatedAsync($"DELETE FROM Professionals WHERE Id = {professional.Id}");
        var professionalDelete = await deleteProfessional.Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>();
        professionalDelete.Which.Message.Should().Contain("FK_Appointments_Professionals_ProfessionalId");

        // Nothing was removed from under the appointment.
        var verificationScope = Factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verificationContext.Patients.AnyAsync(p => p.Id == patientId)).Should().BeTrue();
        (await verificationContext.Professionals.AnyAsync(p => p.Id == professional.Id)).Should().BeTrue();
        (await verificationContext.Appointments.AnyAsync(a => a.Id == appointment.Id)).Should().BeTrue();
    }

    /// <summary>
    /// Only the id that the test wants to break is faked; everything else points at a real
    /// specialty so the failure can only come from the missing link under test.
    /// </summary>
    private static async Task<Appointment> NewAppointmentAsync(
        AppDbContext context,
        Guid? patientId = null,
        Guid? professionalId = null)
    {
        var specialtyId = await context.Specialties.Select(s => s.Id).FirstAsync();
        var now = DateTimeOffset.UtcNow;

        return new Appointment(Guid.NewGuid())
        {
            PatientId = patientId ?? Guid.NewGuid(),
            ProfessionalId = professionalId ?? Guid.NewGuid(),
            SpecialtyId = specialtyId,
            StartDateTime = DateTimeOffset.UtcNow.AddDays(3),
            DurationMinutes = 30,
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            ModifiedAt = now,
            ModifiedBy = Guid.Empty
        };
    }
}
