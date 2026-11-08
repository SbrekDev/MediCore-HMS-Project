using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Agendas;

public static class DeleteAgendaException
{
    public sealed record Command(Guid ProfessionalId, Guid ExceptionId);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<Unit>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var agenda = await _context.Agendas
                .FirstOrDefaultAsync(a => a.ProfessionalId == command.ProfessionalId, cancellationToken);

            if (agenda is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "Professional.NotFound",
                    $"Professional '{command.ProfessionalId}' was not found."));
            }

            var exception = await _context.AgendaExceptions
                .FirstOrDefaultAsync(e => e.Id == command.ExceptionId && e.AgendaId == agenda.Id, cancellationToken);

            if (exception is null)
            {
                return Result<Unit>.Failure(DomainError.NotFound(
                    "AgendaException.NotFound",
                    $"Exception '{command.ExceptionId}' was not found for professional '{command.ProfessionalId}'."));
            }

            _context.AgendaExceptions.Remove(exception);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
