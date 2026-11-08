using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Enums;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Appointments;

public static class CancelAppointment
{
    public sealed record Command(Guid AppointmentId);

    public sealed class Handler
    {
        private static readonly TimeSpan LateCancellationThreshold = TimeSpan.FromHours(24);

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

            if (appointment.Status != AppointmentStatus.Confirmed)
            {
                return Result<Guid>.Failure(DomainError.Conflict(
                    "Appointment.InvalidState",
                    $"Only a Confirmed appointment can be cancelled (current status: {appointment.Status})."));
            }

            var now = _timeProvider.GetUtcNow();

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.IsLateCancellation = appointment.StartDateTime - now < LateCancellationThreshold;
            appointment.ModifiedAt = now;
            appointment.ModifiedBy = _currentUserService.UserId ?? Guid.Empty;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<Guid>.Failure(DomainError.Conflict(
                    "Appointment.InvalidState",
                    "The appointment was modified concurrently; retry the cancellation."));
            }

            return Result<Guid>.Success(appointment.Id);
        }
    }
}
