using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Enums;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Appointments;

public static class CompleteAppointment
{
    public sealed record Command(Guid AppointmentId);

    public sealed class Handler
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly TimeProvider _timeProvider;

        public Handler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            TimeProvider timeProvider)
        {
            _context = context;
            _currentUserService = currentUserService;
            _timeProvider = timeProvider;
        }

        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == command.AppointmentId, cancellationToken);

            if (appointment is null)
            {
                return Result<Guid>.Failure(DomainError.NotFound(
                    "Appointment.NotFound",
                    $"Appointment '{command.AppointmentId}' was not found."));
            }

            if (_currentUserService.ProfessionalId is null ||
                _currentUserService.ProfessionalId != appointment.ProfessionalId)
            {
                return Result<Guid>.Failure(DomainError.Forbidden(
                    "Appointment.NotOwner",
                    "Only the professional who owns the appointment can mark it as completed."));
            }

            if (appointment.Status != AppointmentStatus.Confirmed)
            {
                return Result<Guid>.Failure(DomainError.Conflict(
                    "Appointment.InvalidState",
                    $"Only a Confirmed appointment can be completed (current status: {appointment.Status})."));
            }

            appointment.Status = AppointmentStatus.Completed;
            appointment.ModifiedAt = _timeProvider.GetUtcNow();
            appointment.ModifiedBy = _currentUserService.UserId ?? Guid.Empty;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<Guid>.Failure(DomainError.Conflict(
                    "Appointment.InvalidState",
                    "The appointment was modified concurrently; retry the completion."));
            }

            return Result<Guid>.Success(appointment.Id);
        }
    }
}
