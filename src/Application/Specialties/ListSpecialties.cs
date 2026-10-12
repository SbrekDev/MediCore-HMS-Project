using MediCore.Application.Abstractions;
using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Specialties;

public static class ListSpecialties
{
    public sealed record Query(bool ActiveOnly = false);

    public sealed record SpecialtyDto(Guid Id, string Code, string Name, int DefaultDurationMinutes, bool IsActive);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<SpecialtyDto>> HandleAsync(Query query, CancellationToken cancellationToken = default)
        {
            IQueryable<Specialty> queryable = _context.Specialties;

            if (query.ActiveOnly)
            {
                queryable = queryable.Where(s => s.IsActive);
            }

            var specialties = await queryable
                .OrderBy(s => s.Name)
                .Select(s => new SpecialtyDto(
                    s.Id,
                    s.Code,
                    s.Name,
                    s.DefaultDurationMinutes,
                    s.IsActive))
                .ToListAsync(cancellationToken);

            return specialties;
        }
    }
}
