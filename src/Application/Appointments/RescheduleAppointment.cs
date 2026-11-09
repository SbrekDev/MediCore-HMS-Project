using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Enums;
using MediCore.Domain.Errors;
using MediCore.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Appointments;

public static class RescheduleAppointment
{
    public sealed record Command(Guid AppointmentId, DateTimeOffset Start);

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
            var start = command.Start.ToUniversalTime();
            var requestDate = start.UtcDateTime.Date;

            var identity = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == command.AppointmentId)
                .Select(a => new { a.ProfessionalId, a.PatientId })
                .FirstOrDefaultAsync(cancellationToken);

            if (identity is null)
            {
                return Result<Guid>.Failure(DomainError.NotFound(
                    "Appointment.NotFound",
                    $"Appointment '{command.AppointmentId}' was not found."));
            }

            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

            try
            {
                await _context.LockAnchorRowsAsync(identity.ProfessionalId, identity.PatientId, cancellationToken);

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
                        $"Only a Confirmed appointment can be rescheduled (current status: {appointment.Status})."));
                }

                var professionalSpecialtyIds = await _context.ProfessionalSpecialties
                    .Where(ps => ps.ProfessionalId == appointment.ProfessionalId)
                    .Select(ps => ps.SpecialtyId)
                    .ToListAsync(cancellationToken);

                var dayStart = new DateTimeOffset(requestDate, TimeSpan.Zero);
                var dayEnd = dayStart.AddDays(1);

                // Scan one extra day back: an appointment starting the previous day can cross
                // midnight into the requested day and must still be considered for overlap.
                var scanStart = dayStart.AddDays(-1);

                var existingAppointments = await _context.Appointments
                    .Where(a =>
                        a.Id != appointment.Id &&
                        (a.ProfessionalId == appointment.ProfessionalId || a.PatientId == appointment.PatientId) &&
                        a.StartDateTime >= scanStart &&
                        a.StartDateTime < dayEnd &&
                        a.Status == AppointmentStatus.Confirmed)
                    .Select(a => new ExistingAppointment(
                        a.ProfessionalId,
                        a.PatientId,
                        a.StartDateTime,
                        a.StartDateTime.AddMinutes(a.DurationMinutes),
                        a.IsSobreturno))
                    .ToListAsync(cancellationToken);

                var schedulingRequest = new SchedulingRequest(
                    appointment.PatientId,
                    appointment.ProfessionalId,
                    appointment.SpecialtyId,
                    start,
                    appointment.DurationMinutes,
                    IsSobreturno: false,
                    BookedByReceptionStaff: true);

                var scheduleContext = new ScheduleContext(
                    existingAppointments,
                    professionalSpecialtyIds,
                    _timeProvider.GetUtcNow(),
                    DailyNormalCapacity: 0,
                    MaxSobreturnosPerDay: 0);

                var scheduleResult = AppointmentScheduler.Schedule(schedulingRequest, scheduleContext);
                if (scheduleResult.IsFailure)
                {
                    return Result<Guid>.Failure(scheduleResult.Error);
                }

                appointment.StartDateTime = start;
                appointment.ModifiedAt = _timeProvider.GetUtcNow();
                appointment.ModifiedBy = _currentUserService.UserId ?? Guid.Empty;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Result<Guid>.Success(appointment.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<Guid>.Failure(DomainError.Conflict(
                    "Appointment.InvalidState",
                    "The appointment was modified concurrently; retry the reschedule."));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
