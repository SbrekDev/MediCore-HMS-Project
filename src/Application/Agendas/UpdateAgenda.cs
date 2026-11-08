using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Agendas;

public static class UpdateAgenda
{
    public sealed record AvailabilityBlockRequest(DayOfWeek DayOfWeek, TimeOnly Start, TimeOnly End);

    public sealed record Command(
        Guid ProfessionalId,
        int MaxSobreturnosPerDay,
        IReadOnlyList<AvailabilityBlockRequest> Blocks);

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
            if (command.MaxSobreturnosPerDay < 0)
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Agenda.MaxSobreturnosPerDayNegative",
                    "Daily sobreturno cap cannot be negative."));
            }

            var professional = await _context.Professionals
                .FirstOrDefaultAsync(p => p.Id == command.ProfessionalId, cancellationToken);

            if (professional is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "Professional.NotFound",
                    $"Professional '{command.ProfessionalId}' was not found."));
            }

            foreach (var block in command.Blocks)
            {
                if (block.Start >= block.End)
                {
                    return Result<Unit>.Failure(DomainError.Validation(
                        "Agenda.BlockStartMustBeBeforeEnd",
                        "Availability block start time must be before end time."));
                }
            }

            var overlaps = command.Blocks
                .GroupBy(b => b.DayOfWeek)
                .SelectMany(g =>
                {
                    var ordered = g.OrderBy(b => b.Start).ToList();
                    return ordered
                        .SelectMany((current, i) => ordered.Skip(i + 1)
                            .Where(next => current.Start < next.End && next.Start < current.End));
                })
                .Any();

            if (overlaps)
            {
                return Result<Unit>.Failure(DomainError.Validation(
                    "Agenda.OverlappingBlocks",
                    "Availability blocks cannot overlap on the same day of week."));
            }

            var agenda = await _context.Agendas
                .FirstOrDefaultAsync(a => a.ProfessionalId == command.ProfessionalId, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var userId = _currentUserService.UserId ?? Guid.Empty;

            if (agenda is null)
            {
                agenda = new Agenda(Guid.NewGuid())
                {
                    ProfessionalId = command.ProfessionalId,
                    MaxSobreturnosPerDay = command.MaxSobreturnosPerDay,
                    CreatedAt = now,
                    CreatedBy = userId,
                    ModifiedAt = now,
                    ModifiedBy = userId
                };
                _context.Agendas.Add(agenda);
            }
            else
            {
                agenda.MaxSobreturnosPerDay = command.MaxSobreturnosPerDay;
                agenda.ModifiedAt = now;
                agenda.ModifiedBy = userId;

                var existingBlocks = await _context.AvailabilityBlocks
                    .Where(b => b.AgendaId == agenda.Id)
                    .ToListAsync(cancellationToken);
                _context.AvailabilityBlocks.RemoveRange(existingBlocks);
            }

            foreach (var block in command.Blocks)
            {
                _context.AvailabilityBlocks.Add(new AvailabilityBlock
                {
                    Id = Guid.NewGuid(),
                    AgendaId = agenda.Id,
                    DayOfWeek = block.DayOfWeek,
                    Start = block.Start,
                    End = block.End
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
