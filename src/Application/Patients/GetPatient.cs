using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Patients;

public static class GetPatient
{
    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<ListPatients.PatientDto>> HandleAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

            if (patient is null)
            {
                return Result<ListPatients.PatientDto>.Failure(DomainError.NotFound(
                    "Patient.NotFound",
                    $"Patient '{patientId}' was not found."));
            }

            return Result<ListPatients.PatientDto>.Success(ListPatients.MapToDto(patient));
        }
    }
}
