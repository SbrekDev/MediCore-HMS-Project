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
    public void The_appointment_foreign_keys_restrict_deletes()
    {
        Skip.IfNot(ContainerAvailable, "SQL Server container is required for the appointment FK tests");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var patientForeignKey = context.Model
            .FindEntityType(typeof(Appointment))!
            .GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(Appointment.PatientId)));

        var professionalForeignKey = context.Model
            .FindEntityType(typeof(Appointment))!
            .GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(Appointment.ProfessionalId)));

        patientForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        professionalForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
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
