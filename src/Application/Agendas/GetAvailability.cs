using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Agendas;

public static class GetAvailability
{
    public sealed record AvailabilityWindow(TimeOnly Start, TimeOnly End);

    public sealed record AvailabilityResponse(
        Guid ProfessionalId,
        DateOnly Date,
        IReadOnlyList<AvailabilityWindow> Windows,
        int MaxSobreturnosPerDay);

    public sealed record Query(Guid ProfessionalId, DateOnly Date);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<AvailabilityResponse>> HandleAsync(
            Query query,
            CancellationToken cancellationToken = default)
        {
            if (query.Date == default)
            {
                return Result<AvailabilityResponse>.Failure(DomainError.Validation(
                    "Availability.DateRequired",
                    "A date must be provided."));
            }

            var professionalExists = await _context.Professionals
                .AnyAsync(p => p.Id == query.ProfessionalId, cancellationToken);

            if (!professionalExists)
            {
                return Result<AvailabilityResponse>.Failure(DomainError.NotFound(
                    "Professional.NotFound",
                    $"Professional '{query.ProfessionalId}' was not found."));
            }

            var agenda = await _context.Agendas
                .Include(a => a.Blocks)
                .FirstOrDefaultAsync(a => a.ProfessionalId == query.ProfessionalId, cancellationToken);

            if (agenda is null)
            {
                return Result<AvailabilityResponse>.Success(new AvailabilityResponse(
                    query.ProfessionalId,
                    query.Date,
                    Array.Empty<AvailabilityWindow>(),
                    0));
            }

            var exceptions = await _context.AgendaExceptions
                .Where(e => e.AgendaId == agenda.Id && e.Date == query.Date)
                .ToListAsync(cancellationToken);

            var windows = ComputeAvailableWindows(
                agenda.Blocks.Where(b => b.DayOfWeek == query.Date.DayOfWeek),
                exceptions);

            return Result<AvailabilityResponse>.Success(new AvailabilityResponse(
                query.ProfessionalId,
                query.Date,
                windows,
                agenda.MaxSobreturnosPerDay));
        }

        private static IReadOnlyList<AvailabilityWindow> ComputeAvailableWindows(
            IEnumerable<AvailabilityBlock> blocks,
            IReadOnlyList<AgendaException> exceptions)
        {
            if (exceptions.Any(e => e.IsFullDay))
            {
                return Array.Empty<AvailabilityWindow>();
            }

            var exceptionIntervals = exceptions
                .Where(e => !e.IsFullDay)
                .Select(e => (Start: e.Start!.Value, End: e.End!.Value))
                .ToList();

            var availableWindows = new List<AvailabilityWindow>();

            foreach (var block in blocks.OrderBy(b => b.Start))
            {
                var current = block.Start;
                var blockEnd = block.End;

                foreach (var interval in exceptionIntervals
                    .Where(i => i.End > current && i.Start < blockEnd)
                    .OrderBy(i => i.Start))
                {
                    if (interval.Start > current)
                    {
                        availableWindows.Add(new AvailabilityWindow(current, Earlier(interval.Start, blockEnd)));
                        current = interval.End;

                        if (current >= blockEnd)
                        {
                            break;
                        }
                    }
                    else
                    {
                        current = Later(current, interval.End);

                        if (current >= blockEnd)
                        {
                            break;
                        }
                    }
                }

                if (current < blockEnd)
                {
                    availableWindows.Add(new AvailabilityWindow(current, blockEnd));
                }
            }

            return availableWindows;
        }

        private static TimeOnly Earlier(TimeOnly a, TimeOnly b) => a < b ? a : b;
        private static TimeOnly Later(TimeOnly a, TimeOnly b) => a > b ? a : b;
    }
}
