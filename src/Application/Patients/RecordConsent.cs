using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Patients;

public static class RecordConsent
{
    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public Handler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Unit>> HandleAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

            if (patient is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "Patient.NotFound",
                    $"Patient '{patientId}' was not found."));
            }

            var now = DateTimeOffset.UtcNow;
            patient.RecordConsent(now);
            patient.ModifiedAt = now;
            patient.ModifiedBy = _currentUserService.UserId ?? Guid.Empty;

            await _context.SaveChangesAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
