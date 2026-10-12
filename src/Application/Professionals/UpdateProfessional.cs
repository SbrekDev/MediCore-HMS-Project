using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Professionals;

public static class UpdateProfessional
{
    public sealed record Command(
        Guid ProfessionalId,
        string FirstName,
        string LastName,
        string NationalLicenseMN,
        IReadOnlyList<CreateProfessional.ProvincialLicenseRequest> ProvincialLicenses);

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
            if (string.IsNullOrWhiteSpace(command.NationalLicenseMN))
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Professional.NationalLicenseMNRequired",
                    "National license (MN) is required."));
            }

            if (string.IsNullOrWhiteSpace(command.FirstName))
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Professional.FirstNameRequired",
                    "First name is required."));
            }

            if (string.IsNullOrWhiteSpace(command.LastName))
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Professional.LastNameRequired",
                    "Last name is required."));
            }

            var professional = await _context.Professionals
                .Include(p => p.ProvincialLicenses)
                .FirstOrDefaultAsync(p => p.Id == command.ProfessionalId, cancellationToken);

            if (professional is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "Professional.NotFound",
                    $"Professional '{command.ProfessionalId}' was not found."));
            }

            professional.FirstName = command.FirstName;
            professional.LastName = command.LastName;
            professional.NationalLicenseMN = command.NationalLicenseMN;
            professional.ModifiedAt = DateTimeOffset.UtcNow;
            professional.ModifiedBy = _currentUserService.UserId ?? Guid.Empty;

            professional.ProvincialLicenses.Clear();
            foreach (var license in command.ProvincialLicenses)
            {
                professional.ProvincialLicenses.Add(new ProvincialLicenseMP
                {
                    Id = Guid.NewGuid(),
                    Number = license.Number,
                    Province = license.Province
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
