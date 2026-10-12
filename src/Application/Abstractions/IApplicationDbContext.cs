using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
