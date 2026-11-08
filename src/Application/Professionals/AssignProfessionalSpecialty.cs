using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Professionals;

public static class AssignProfessionalSpecialty
{
    public sealed record Command(
        Guid ProfessionalId,
        Guid SpecialtyId,
        bool IsPrimary,
        int? DurationOverrideMinutes = null);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public Handler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Unit>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var professional = await _context.Professionals
                .Include(p => p.Specialties)
                .FirstOrDefaultAsync(p => p.Id == command.ProfessionalId, cancellationToken);

            if (professional is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "Professional.NotFound",
                    $"Professional '{command.ProfessionalId}' was not found."));
            }

            var specialty = await _context.Specialties
                .FirstOrDefaultAsync(s => s.Id == command.SpecialtyId, cancellationToken);

            if (specialty is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "Specialty.NotFound",
                    $"Specialty '{command.SpecialtyId}' was not found."));
            }

            var existing = professional.Specialties
                .FirstOrDefault(ps => ps.SpecialtyId == command.SpecialtyId);

            if (existing is not null)
            {
                existing.IsPrimary = command.IsPrimary;
                existing.DurationOverrideMinutes = command.DurationOverrideMinutes;
            }
            else
            {
                professional.Specialties.Add(new ProfessionalSpecialty
                {
                    ProfessionalId = professional.Id,
                    SpecialtyId = command.SpecialtyId,
                    IsPrimary = command.IsPrimary,
                    DurationOverrideMinutes = command.DurationOverrideMinutes
                });
            }

            if (command.IsPrimary)
            {
                foreach (var assignment in professional.Specialties.Where(ps => ps.SpecialtyId != command.SpecialtyId))
                {
                    assignment.IsPrimary = false;
                }
            }

            professional.ModifiedAt = DateTimeOffset.UtcNow;
            professional.ModifiedBy = _currentUserService.UserId ?? Guid.Empty;

            await _context.SaveChangesAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
