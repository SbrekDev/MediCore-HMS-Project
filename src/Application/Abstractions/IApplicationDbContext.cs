using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Patient> Patients { get; }
    DbSet<Professional> Professionals { get; }
    DbSet<ProfessionalSpecialty> ProfessionalSpecialties { get; }
    DbSet<Specialty> Specialties { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
