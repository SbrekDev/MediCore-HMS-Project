using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Patients;

public static class AddCoverage
{
    public sealed record Command(
        Guid PatientId,
        string Type,
        Guid? InsurerId,
        string PlanName,
        string? AffiliateNumber);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public Handler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.PlanName))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Coverage.PlanNameRequired",
                    "Plan name is required."));
            }

            if (!TryParseCoverageType(command.Type, out var coverageType))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Coverage.InvalidType",
                    $"Coverage type '{command.Type}' is not supported."));
            }

            if (coverageType == CoverageType.Particular)
            {
                if (command.InsurerId is not null)
                {
                    return Result<Guid>.Failure(DomainError.Validation(
                        "Coverage.InsurerNotAllowed",
                        "Particular coverage does not allow an insurer."));
                }

                if (!string.IsNullOrWhiteSpace(command.AffiliateNumber))
                {
                    return Result<Guid>.Failure(DomainError.Validation(
                        "Coverage.AffiliateNumberNotAllowed",
                        "Particular coverage does not allow an affiliate number."));
                }
            }
            else
            {
                if (command.InsurerId is null)
                {
                    return Result<Guid>.Failure(DomainError.Validation(
                        "Coverage.InsurerRequired",
                        "Insurer is required for ObraSocial and Prepaga coverage."));
                }

                if (string.IsNullOrWhiteSpace(command.AffiliateNumber))
                {
                    return Result<Guid>.Failure(DomainError.Validation(
                        "Coverage.AffiliateNumberRequired",
                        "Affiliate number is required for ObraSocial and Prepaga coverage."));
                }
            }

            var patientExists = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p => p.Id == command.PatientId, cancellationToken);

            if (!patientExists)
            {
                return Result<Guid>.Failure(DomainError.NotFound(
                    "Patient.NotFound",
                    $"Patient '{command.PatientId}' was not found."));
            }

            var now = DateTimeOffset.UtcNow;
            var userId = _currentUserService.UserId ?? Guid.Empty;

            var activeEntry = await _context.CoverageEntries
                .FirstOrDefaultAsync(
                    c => c.PatientId == command.PatientId && c.IsActive,
                    cancellationToken);

            if (activeEntry is not null)
            {
                activeEntry.IsActive = false;
                activeEntry.ModifiedAt = now;
                activeEntry.ModifiedBy = userId;
            }

            var entry = new CoverageEntry(Guid.NewGuid())
            {
                PatientId = command.PatientId,
                Type = coverageType,
                InsurerId = command.InsurerId,
                PlanName = command.PlanName,
                AffiliateNumber = command.AffiliateNumber,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId,
                ModifiedAt = now,
                ModifiedBy = userId
            };

            _context.CoverageEntries.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(entry.Id);
        }

        private static bool TryParseCoverageType(string value, out CoverageType coverageType)
        {
            coverageType = default;
            if (string.Equals(value, nameof(CoverageType.ObraSocial), StringComparison.OrdinalIgnoreCase))
            {
                coverageType = CoverageType.ObraSocial;
                return true;
            }

            if (string.Equals(value, nameof(CoverageType.Prepaga), StringComparison.OrdinalIgnoreCase))
            {
                coverageType = CoverageType.Prepaga;
                return true;
            }

            if (string.Equals(value, nameof(CoverageType.Particular), StringComparison.OrdinalIgnoreCase))
            {
                coverageType = CoverageType.Particular;
                return true;
            }

            return false;
        }
    }
}
