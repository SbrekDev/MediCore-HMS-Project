using MediCore.Domain.Common;
using MediCore.Domain.Errors;

namespace MediCore.Domain.Scheduling;

public sealed record SchedulingRequest(
    Guid PatientId,
    Guid ProfessionalId,
    Guid SpecialtyId,
    DateTimeOffset Start,
    int DurationMinutes,
    bool IsSobreturno);

public sealed record ExistingAppointment(
    Guid ProfessionalId,
    Guid PatientId,
    DateTimeOffset Start,
    DateTimeOffset End);

public sealed record ScheduleContext(
    IReadOnlyList<ExistingAppointment> ExistingAppointments,
    IReadOnlyList<Guid> ProfessionalSpecialtyIds,
    DateTimeOffset Now);

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

        return Result<ScheduledSlot>.Success(new ScheduledSlot(request.Start, end, request.IsSobreturno));
    }
}
