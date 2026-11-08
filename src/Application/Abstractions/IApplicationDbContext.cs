using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MediCore.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Patient> Patients { get; }
    DbSet<Professional> Professionals { get; }
    DbSet<ProfessionalSpecialty> ProfessionalSpecialties { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<CoverageEntry> CoverageEntries { get; }
    DbSet<Insurer> Insurers { get; }
    DbSet<Agenda> Agendas { get; }
    DbSet<AvailabilityBlock> AvailabilityBlocks { get; }
    DbSet<AgendaException> AgendaExceptions { get; }
    DbSet<Appointment> Appointments { get; }

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<(bool ProfessionalExists, bool PatientExists)> LockAnchorRowsAsync(
        Guid professionalId,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
