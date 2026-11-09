using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Agendas;

public static class GetAgenda
{
    public sealed record AvailabilityBlockDto(string DayOfWeek, string Start, string End);

    public sealed record AgendaExceptionDto(Guid Id, string Date, string? Start, string? End, string Reason);

    public sealed record AgendaResponse(
        Guid ProfessionalId,
        int MaxSobreturnosPerDay,
        IReadOnlyList<AvailabilityBlockDto> Blocks,
        IReadOnlyList<AgendaExceptionDto> Exceptions);

    public sealed record Query(Guid ProfessionalId);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<AgendaResponse>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var professionalExists = await _context.Professionals
                .AnyAsync(p => p.Id == query.ProfessionalId, cancellationToken);

            if (!professionalExists)
            {
                return Result<AgendaResponse>.Failure(DomainError.NotFound(
                    "Professional.NotFound",
                    $"Professional '{query.ProfessionalId}' was not found."));
            }

            var agenda = await _context.Agendas
                .AsNoTracking()
                .Include(a => a.Blocks)
                .FirstOrDefaultAsync(a => a.ProfessionalId == query.ProfessionalId, cancellationToken);

            if (agenda is null)
            {
                return Result<AgendaResponse>.Success(new AgendaResponse(
                    query.ProfessionalId,
                    0,
                    Array.Empty<AvailabilityBlockDto>(),
                    Array.Empty<AgendaExceptionDto>()));
            }

            var exceptions = await _context.AgendaExceptions
                .AsNoTracking()
                .Where(e => e.AgendaId == agenda.Id)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Start)
                .Select(e => new AgendaExceptionDto(
                    e.Id,
                    e.Date.ToString("yyyy-MM-dd"),
                    e.Start.HasValue ? e.Start.Value.ToString("HH:mm") : null,
                    e.End.HasValue ? e.End.Value.ToString("HH:mm") : null,
                    e.Reason))
                .ToListAsync(cancellationToken);

            var blocks = agenda.Blocks
                .OrderBy(b => b.DayOfWeek)
                .ThenBy(b => b.Start)
                .Select(b => new AvailabilityBlockDto(
                    b.DayOfWeek.ToString(),
                    b.Start.ToString("HH:mm"),
                    b.End.ToString("HH:mm")))
                .ToList();

            return Result<AgendaResponse>.Success(new AgendaResponse(
                query.ProfessionalId,
                agenda.MaxSobreturnosPerDay,
                blocks,
                exceptions));
        }
    }
}
