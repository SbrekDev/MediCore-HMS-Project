using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Specialties;

public static class ToggleSpecialtyActive
{
    public sealed record Command(Guid SpecialtyId, bool IsActive);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<Unit>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var specialty = await _context.Specialties
                .FirstOrDefaultAsync(s => s.Id == command.SpecialtyId, cancellationToken);

            if (specialty is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "Specialty.NotFound",
                    $"Specialty '{command.SpecialtyId}' was not found."));
            }

            specialty.IsActive = command.IsActive;
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
