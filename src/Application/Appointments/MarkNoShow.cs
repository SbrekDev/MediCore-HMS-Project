using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Enums;
using MediCore.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Appointments;

public static class MarkNoShow
{
    public sealed record Command(Guid AppointmentId);

    public sealed class Handler
    {
        private const string NoShowCountIncrementChangesJson = "{\"NoShowCount\":{\"Increment\":1}}";

        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;
        private readonly TimeProvider _timeProvider;

        public Handler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditService auditService,
            TimeProvider timeProvider)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _timeProvider = timeProvider;
        }

        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

            try
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
                        "Only the professional who owns the appointment can mark it as no-show."));
                }

                if (appointment.Status != AppointmentStatus.Confirmed)
                {
                    return Result<Guid>.Failure(DomainError.Conflict(
                        "Appointment.InvalidState",
                        $"Only a Confirmed appointment can be marked as no-show (current status: {appointment.Status})."));
                }

                var patientExists = await _context.Patients
                    .AnyAsync(p => p.Id == appointment.PatientId, cancellationToken);

                if (!patientExists)
                {
                    return Result<Guid>.Failure(DomainError.NotFound(
                        "Patient.NotFound",
                        $"Patient '{appointment.PatientId}' was not found."));
                }

                var now = _timeProvider.GetUtcNow();

                appointment.Status = AppointmentStatus.NoShow;
                appointment.ModifiedAt = now;
                appointment.ModifiedBy = _currentUserService.UserId ?? Guid.Empty;

                await _context.SaveChangesAsync(cancellationToken);

                // Atomic increment: two no-show markings for different appointments of the same
                // patient must both count, so the counter is incremented by the database itself
                // instead of a read-modify-write through a tracked entity (lost update).
                await _context.Patients
                    .Where(p => p.Id == appointment.PatientId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(p => p.NoShowCount, p => p.NoShowCount + 1),
                        cancellationToken);

                // The atomic UPDATE bypasses the ChangeTracker, so the audit record of the
                // counter change is written explicitly instead of by the AuditInterceptor.
                await _auditService.CaptureAsync(
                    action: "Updated",
                    entityType: nameof(Patient),
                    entityId: appointment.PatientId,
                    actorId: _currentUserService.UserId,
                    timestampUtc: now,
                    changesJson: NoShowCountIncrementChangesJson,
                    cancellationToken: cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return Result<Guid>.Success(appointment.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<Guid>.Failure(DomainError.Conflict(
                    "Appointment.InvalidState",
                    "The appointment was modified concurrently; retry the no-show marking."));
            }
        }
    }
}
