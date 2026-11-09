using MediCore.Application.Abstractions;
using MediCore.Domain.Common;
using MediCore.Domain.Entities;
using MediCore.Domain.Enums;
using MediCore.Domain.Errors;
using MediCore.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Application.Appointments;

public static class BookAppointment
{
    public sealed record Command(
        Guid PatientId,
        Guid ProfessionalId,
        Guid SpecialtyId,
        DateTimeOffset Start,
        bool IsSobreturno = false);

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
            var requestDateOnly = DateOnly.FromDateTime(requestDate);

            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

            try
            {
                var (professionalExists, patientExists) = await _context.LockAnchorRowsAsync(
                    command.ProfessionalId,
                    command.PatientId,
                    cancellationToken);

                if (!professionalExists)
                {
                    return Result<Guid>.Failure(DomainError.NotFound(
                        "Professional.NotFound",
                        $"Professional '{command.ProfessionalId}' was not found."));
                }

                if (!patientExists)
                {
                    return Result<Guid>.Failure(DomainError.NotFound(
                        "Patient.NotFound",
                        $"Patient '{command.PatientId}' was not found."));
                }

                var professionalSpecialtyIds = await _context.ProfessionalSpecialties
                    .Where(ps => ps.ProfessionalId == command.ProfessionalId)
                    .Select(ps => ps.SpecialtyId)
                    .ToListAsync(cancellationToken);

                var durationOverride = await _context.ProfessionalSpecialties
                    .Where(ps => ps.ProfessionalId == command.ProfessionalId && ps.SpecialtyId == command.SpecialtyId)
                    .Select(ps => (int?)ps.DurationOverrideMinutes)
                    .FirstOrDefaultAsync(cancellationToken);

                var specialtyDefault = await _context.Specialties
                    .Where(s => s.Id == command.SpecialtyId)
                    .Select(s => (int?)s.DefaultDurationMinutes)
                    .FirstOrDefaultAsync(cancellationToken);

                if (specialtyDefault is null)
                {
                    return Result<Guid>.Failure(DomainError.NotFound(
                        "Specialty.NotFound",
                        $"Specialty '{command.SpecialtyId}' was not found."));
                }

                var durationMinutes = durationOverride ?? specialtyDefault.Value;

                var agenda = await _context.Agendas
                    .Include(a => a.Blocks)
                    .FirstOrDefaultAsync(a => a.ProfessionalId == command.ProfessionalId, cancellationToken);

                var exceptions = agenda is null
                    ? new List<AgendaException>()
                    : await _context.AgendaExceptions
                        .Where(e => e.AgendaId == agenda.Id && e.Date == requestDateOnly)
                        .ToListAsync(cancellationToken);

                var dailyNormalCapacity = ComputeDailyNormalCapacity(
                    agenda?.Blocks ?? new List<AvailabilityBlock>(),
                    exceptions,
                    requestDateOnly,
                    durationMinutes);

                var maxSobreturnosPerDay = agenda?.MaxSobreturnosPerDay ?? 0;

                var dayStart = new DateTimeOffset(requestDate, TimeSpan.Zero);
                var dayEnd = dayStart.AddDays(1);

                // Scan one extra day back: an appointment starting the previous day can cross
                // midnight into the requested day and must still be considered for overlap.
                var scanStart = dayStart.AddDays(-1);

                var existingAppointments = await _context.Appointments
                    .Where(a =>
                        (a.ProfessionalId == command.ProfessionalId || a.PatientId == command.PatientId) &&
                        a.StartDateTime >= scanStart &&
                        a.StartDateTime < dayEnd &&
                        a.Status == AppointmentStatus.Confirmed)
                    .ToListAsync(cancellationToken);

                var existingForScheduler = existingAppointments
                    .Select(a => new ExistingAppointment(
                        a.ProfessionalId,
                        a.PatientId,
                        a.StartDateTime,
                        a.StartDateTime.AddMinutes(a.DurationMinutes),
                        a.IsSobreturno))
                    .ToList();

                var schedulingRequest = new SchedulingRequest(
                    command.PatientId,
                    command.ProfessionalId,
                    command.SpecialtyId,
                    start,
                    durationMinutes,
                    command.IsSobreturno,
                    _currentUserService.Roles.Any(r => r is "Admin" or "Receptionist"));

                var scheduleContext = new ScheduleContext(
                    existingForScheduler,
                    professionalSpecialtyIds,
                    _timeProvider.GetUtcNow(),
                    dailyNormalCapacity,
                    maxSobreturnosPerDay);

                var scheduleResult = AppointmentScheduler.Schedule(schedulingRequest, scheduleContext);
                if (scheduleResult.IsFailure)
                {
                    return Result<Guid>.Failure(scheduleResult.Error);
                }

                var now = _timeProvider.GetUtcNow();
                var userId = _currentUserService.UserId ?? Guid.Empty;

                var appointment = new Appointment(Guid.NewGuid())
                {
                    PatientId = command.PatientId,
                    ProfessionalId = command.ProfessionalId,
                    SpecialtyId = command.SpecialtyId,
                    StartDateTime = start,
                    DurationMinutes = durationMinutes,
                    Status = AppointmentStatus.Confirmed,
                    IsSobreturno = command.IsSobreturno,
                    IsLateCancellation = false,
                    CreatedAt = now,
                    CreatedBy = userId,
                    ModifiedAt = now,
                    ModifiedBy = userId
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Result<Guid>.Success(appointment.Id);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static int ComputeDailyNormalCapacity(
            IEnumerable<AvailabilityBlock> blocks,
            IEnumerable<AgendaException> exceptions,
            DateOnly date,
            int durationMinutes)
        {
            if (durationMinutes <= 0)
            {
                return 0;
            }

            if (exceptions.Any(e => e.IsFullDay))
            {
                return 0;
            }

            var exceptionIntervals = exceptions
                .Where(e => !e.IsFullDay)
                .Select(e => (Start: e.Start!.Value, End: e.End!.Value))
                .ToList();

            var availableWindows = new List<AvailabilityWindow>();

            foreach (var block in blocks.Where(b => b.DayOfWeek == date.DayOfWeek).OrderBy(b => b.Start))
            {
                var current = block.Start;
                var blockEnd = block.End;

                foreach (var interval in exceptionIntervals
                    .Where(i => i.End > current && i.Start < blockEnd)
                    .OrderBy(i => i.Start))
                {
                    if (interval.Start > current)
                    {
                        availableWindows.Add(new AvailabilityWindow(current, Min(interval.Start, blockEnd)));
                        current = interval.End;

                        if (current >= blockEnd)
                        {
                            break;
                        }
                    }
                    else
                    {
                        current = Max(current, interval.End);

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

            return availableWindows.Sum(w => (int)((w.End - w.Start).TotalMinutes / durationMinutes));
        }

        private sealed record AvailabilityWindow(TimeOnly Start, TimeOnly End);

        private static TimeOnly Min(TimeOnly a, TimeOnly b) => a < b ? a : b;
        private static TimeOnly Max(TimeOnly a, TimeOnly b) => a > b ? a : b;
    }
}
