using MediCore.Domain.Common;
using MediCore.Domain.Errors;

namespace MediCore.Domain.Scheduling;

public sealed record SchedulingRequest(
    Guid PatientId,
    Guid ProfessionalId,
    Guid SpecialtyId,
    DateTimeOffset Start,
    int DurationMinutes,
    bool IsSobreturno,
    bool BookedByReceptionStaff);

public sealed record ExistingAppointment(
    Guid ProfessionalId,
    Guid PatientId,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsSobreturno);

public sealed record ScheduleContext(
    IReadOnlyList<ExistingAppointment> ExistingAppointments,
    IReadOnlyList<Guid> ProfessionalSpecialtyIds,
    DateTimeOffset Now,
    int DailyNormalCapacity,
    int MaxSobreturnosPerDay);

public sealed record ScheduledSlot(DateTimeOffset Start, DateTimeOffset End, bool IsSobreturno);

public static class AppointmentScheduler
{
    public static Result<ScheduledSlot> Schedule(SchedulingRequest request, ScheduleContext context)
    {
        var end = request.Start.AddMinutes(request.DurationMinutes);

        if (request.Start < context.Now.AddHours(2) || request.Start > context.Now.AddDays(90))
        {
            return Result<ScheduledSlot>.Failure(DomainError.Validation(
                "Appointment.OutsideBookingWindow",
                "The requested start time is outside the booking window (minimum 2 hours from now, maximum 90 days ahead)."));
        }

        if (!context.ProfessionalSpecialtyIds.Contains(request.SpecialtyId))
        {
            return Result<ScheduledSlot>.Failure(DomainError.Validation(
                "Appointment.SpecialtyNotHeld",
                "The professional does not hold the requested specialty."));
        }

        if (request.IsSobreturno)
        {
            return ScheduleSobreturno(request, context, end);
        }

        var overlapsProfessional = context.ExistingAppointments.Any(appointment =>
            appointment.ProfessionalId == request.ProfessionalId &&
            request.Start < appointment.End &&
            appointment.Start < end);

        if (overlapsProfessional)
        {
            return Result<ScheduledSlot>.Failure(DomainError.Conflict(
                "Appointment.ProfessionalOverlap",
                "The professional already has an appointment overlapping the requested time range."));
        }

        var overlapsPatient = context.ExistingAppointments.Any(appointment =>
            appointment.PatientId == request.PatientId &&
            request.Start < appointment.End &&
            appointment.Start < end);

        if (overlapsPatient)
        {
            return Result<ScheduledSlot>.Failure(DomainError.Conflict(
                "Appointment.PatientOverlap",
                "The patient already has an appointment overlapping the requested time range."));
        }

        return Result<ScheduledSlot>.Success(new ScheduledSlot(request.Start, end, IsSobreturno: false));
    }

    private static Result<ScheduledSlot> ScheduleSobreturno(SchedulingRequest request, ScheduleContext context, DateTimeOffset end)
    {
        if (!request.BookedByReceptionStaff)
        {
            return Result<ScheduledSlot>.Failure(DomainError.Forbidden(
                "Appointment.SobreturnoNotAuthorized",
                "Only reception staff (Receptionist or Admin) can book sobreturno appointments."));
        }

        var sobreturnosThatDay = context.ExistingAppointments.Count(appointment =>
            appointment.IsSobreturno &&
            appointment.ProfessionalId == request.ProfessionalId &&
            appointment.Start.Date == request.Start.Date);

        if (sobreturnosThatDay >= context.MaxSobreturnosPerDay)
        {
            return Result<ScheduledSlot>.Failure(DomainError.Conflict(
                "Appointment.SobreturnoCapExceeded",
                "The daily sobreturno cap for this agenda is already reached."));
        }

        var normalAppointmentsThatDay = context.ExistingAppointments.Count(appointment =>
            !appointment.IsSobreturno &&
            appointment.ProfessionalId == request.ProfessionalId &&
            appointment.Start.Date == request.Start.Date);

        if (normalAppointmentsThatDay < context.DailyNormalCapacity)
        {
            return Result<ScheduledSlot>.Failure(DomainError.Validation(
                "Appointment.SobreturnoWithinNormalCapacity",
                "A sobreturno can only be booked beyond the normal block capacity."));
        }

        return Result<ScheduledSlot>.Success(new ScheduledSlot(request.Start, end, IsSobreturno: true));
    }
}
