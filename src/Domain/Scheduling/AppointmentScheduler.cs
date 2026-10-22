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

public sealed record ScheduleContext(IReadOnlyList<ExistingAppointment> ExistingAppointments);

public sealed record ScheduledSlot(DateTimeOffset Start, DateTimeOffset End, bool IsSobreturno);

public static class AppointmentScheduler
{
    public static Result<ScheduledSlot> Schedule(SchedulingRequest request, ScheduleContext context)
    {
        var end = request.Start.AddMinutes(request.DurationMinutes);

        var overlaps = context.ExistingAppointments.Any(appointment =>
            appointment.ProfessionalId == request.ProfessionalId &&
            request.Start < appointment.End &&
            appointment.Start < end);

        if (overlaps)
        {
            return Result<ScheduledSlot>.Failure(DomainError.Conflict(
                "Appointment.ProfessionalOverlap",
                "The professional already has an appointment overlapping the requested time range."));
        }

        return Result<ScheduledSlot>.Success(new ScheduledSlot(request.Start, end, request.IsSobreturno));
    }
}
