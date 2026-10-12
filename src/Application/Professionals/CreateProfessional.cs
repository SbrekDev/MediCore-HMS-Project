using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;

namespace MediCore.Application.Professionals;

public static class CreateProfessional
{
    public sealed record ProvincialLicenseRequest(string Number, string Province);

    public sealed record Command(
        string FirstName,
        string LastName,
        string NationalLicenseMN,
        IReadOnlyList<ProvincialLicenseRequest> ProvincialLicenses);

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
            if (string.IsNullOrWhiteSpace(command.NationalLicenseMN))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Professional.NationalLicenseMNRequired",
                    "National license (MN) is required."));
            }

            if (string.IsNullOrWhiteSpace(command.FirstName))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Professional.FirstNameRequired",
                    "First name is required."));
            }

            if (string.IsNullOrWhiteSpace(command.LastName))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "Professional.LastNameRequired",
                    "Last name is required."));
            }

            var now = DateTimeOffset.UtcNow;
            var userId = _currentUserService.UserId ?? Guid.Empty;

            var professional = new Professional(Guid.NewGuid())
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                NationalLicenseMN = command.NationalLicenseMN,
                CreatedAt = now,
                CreatedBy = userId,
                ModifiedAt = now,
                ModifiedBy = userId
            };

            foreach (var license in command.ProvincialLicenses)
            {
                professional.ProvincialLicenses.Add(new ProvincialLicenseMP
                {
                    Id = Guid.NewGuid(),
                    Number = license.Number,
                    Province = license.Province
                });
            }

            _context.Professionals.Add(professional);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(professional.Id);
        }
    }
}
