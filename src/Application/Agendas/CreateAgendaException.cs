using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Agendas;

public static class CreateAgendaException
{
    public sealed record Command(
        Guid ProfessionalId,
        DateOnly Date,
        TimeOnly? Start,
        TimeOnly? End,
        string Reason);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "AgendaException.ReasonRequired",
                    "Exception reason is required."));
            }

            var hasStart = command.Start.HasValue;
            var hasEnd = command.End.HasValue;

            if (hasStart != hasEnd)
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "AgendaException.PartialRangeIncomplete",
                    "Both start and end times must be provided for a partial-day exception."));
            }

            if (hasStart && command.Start!.Value >= command.End!.Value)
            {
                return Result<Guid>.Failure(DomainError.Validation(
                    "AgendaException.InvalidTimeRange",
                    "Exception start time must be before end time."));
            }

            var agenda = await _context.Agendas
                .FirstOrDefaultAsync(a => a.ProfessionalId == command.ProfessionalId, cancellationToken);

            if (agenda is null)
            {
                return Result<Guid>.Failure(DomainError.NotFound(
                    "Professional.NotFound",
                    $"Professional '{command.ProfessionalId}' was not found."));
            }

            var existing = await _context.AgendaExceptions
                .FirstOrDefaultAsync(e => e.AgendaId == agenda.Id && e.Date == command.Date, cancellationToken);

            if (existing is not null)
            {
                return Result<Guid>.Failure(DomainError.Conflict(
                    "AgendaException.DuplicateDate",
                    $"An exception already exists for professional on {command.Date:yyyy-MM-dd}."));
            }

            var exception = new AgendaException
            {
                Id = Guid.NewGuid(),
                AgendaId = agenda.Id,
                Date = command.Date,
                Start = command.Start,
                End = command.End,
                Reason = command.Reason.Trim()
            };

            _context.AgendaExceptions.Add(exception);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(exception.Id);
        }
    }
}
