using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Professionals;

public static class GetProfessional
{
    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<ListProfessionals.ProfessionalDto>> HandleAsync(
            Guid professionalId,
            CancellationToken cancellationToken = default)
        {
            var professional = await _context.Professionals
                .AsNoTracking()
                .Include(p => p.ProvincialLicenses)
                .FirstOrDefaultAsync(p => p.Id == professionalId, cancellationToken);

            if (professional is null)
            {
                return Result<ListProfessionals.ProfessionalDto>.Failure(DomainError.NotFound(
                    "Professional.NotFound",
                    $"Professional '{professionalId}' was not found."));
            }

            return Result<ListProfessionals.ProfessionalDto>.Success(
                ListProfessionals.MapToDto(professional));
        }
    }
}
