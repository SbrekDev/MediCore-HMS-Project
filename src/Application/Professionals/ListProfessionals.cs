using MediCore.Application.Abstractions;
using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Professionals;

public static class ListProfessionals
{
    public sealed record ProvincialLicenseDto(Guid Id, string Number, string Province);

    public sealed record ProfessionalDto(
        Guid Id,
        string FirstName,
        string LastName,
        string NationalLicenseMN,
        IReadOnlyList<ProvincialLicenseDto> ProvincialLicenses);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProfessionalDto>> HandleAsync(CancellationToken cancellationToken = default)
        {
            var professionals = await _context.Professionals
                .AsNoTracking()
                .Include(p => p.ProvincialLicenses)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync(cancellationToken);

            return professionals.Select(MapToDto).ToList();
        }
    }

    public static ProfessionalDto MapToDto(Professional professional)
    {
        return new ProfessionalDto(
            professional.Id,
            professional.FirstName,
            professional.LastName,
            professional.NationalLicenseMN,
            professional.ProvincialLicenses
                .Select(l => new ProvincialLicenseDto(l.Id, l.Number, l.Province))
                .ToList());
    }
}
