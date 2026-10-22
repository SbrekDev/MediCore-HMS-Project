using FluentAssertions;
using MediCore.Domain.Scheduling;

namespace MediCore.Domain.UnitTests;

public class AppointmentSchedulerTests
{
    private static readonly Guid ProfessionalId = Guid.NewGuid();
    private static readonly Guid SpecialtyId = Guid.NewGuid();
    private static readonly DateTimeOffset Day = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Schedule_rejects_professional_overlap()
    {
        var context = new ScheduleContext(
        [
            new ExistingAppointment(ProfessionalId, Guid.NewGuid(), Day.AddHours(10), Day.AddHours(10).AddMinutes(30))
        ], [SpecialtyId], Now);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, SpecialtyId, Day.AddHours(10).AddMinutes(15), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Appointment.ProfessionalOverlap");
    }

    [Fact]
    public void Schedule_allows_adjacent_appointment_after_existing()
    {
        var context = new ScheduleContext(
        [
            new ExistingAppointment(ProfessionalId, Guid.NewGuid(), Day.AddHours(10), Day.AddHours(10).AddMinutes(30))
        ], [SpecialtyId], Now);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, SpecialtyId, Day.AddHours(10).AddMinutes(30), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(Day.AddHours(10).AddMinutes(30));
        result.Value.End.Should().Be(Day.AddHours(11));
    }

    [Fact]
    public void Schedule_allows_adjacent_appointment_before_existing()
    {
        var context = new ScheduleContext(
        [
            new ExistingAppointment(ProfessionalId, Guid.NewGuid(), Day.AddHours(10).AddMinutes(30), Day.AddHours(11))
        ], [SpecialtyId], Now);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, SpecialtyId, Day.AddHours(10), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(Day.AddHours(10));
        result.Value.End.Should().Be(Day.AddHours(10).AddMinutes(30));
    }

    [Fact]
    public void Schedule_rejects_patient_overlap_across_professionals()
    {
        var patientId = Guid.NewGuid();
        var context = new ScheduleContext(
        [
            new ExistingAppointment(Guid.NewGuid(), patientId, Day.AddHours(10), Day.AddHours(10).AddMinutes(30))
        ], [SpecialtyId], Now);
        var request = new SchedulingRequest(patientId, ProfessionalId, SpecialtyId, Day.AddHours(10).AddMinutes(10), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Appointment.PatientOverlap");
    }

    [Fact]
    public void Schedule_rejects_specialty_not_held_by_professional()
    {
        var context = new ScheduleContext([], [SpecialtyId], Now);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, Guid.NewGuid(), Day.AddHours(10), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Appointment.SpecialtyNotHeld");
    }

    [Fact]
    public void Schedule_rejects_booking_less_than_two_hours_from_now()
    {
        var context = new ScheduleContext([], [SpecialtyId], Now);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, SpecialtyId, Now.AddHours(1), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Appointment.OutsideBookingWindow");
    }

    [Fact]
    public void Schedule_rejects_booking_more_than_90_days_ahead()
    {
        var context = new ScheduleContext([], [SpecialtyId], Now);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, SpecialtyId, Now.AddDays(91), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Appointment.OutsideBookingWindow");
    }

    [Fact]
    public void Schedule_allows_booking_within_window()
    {
        var context = new ScheduleContext([], [SpecialtyId], Now);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, SpecialtyId, Now.AddDays(3), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(Now.AddDays(3));
        result.Value.End.Should().Be(Now.AddDays(3).AddMinutes(30));
    }
}
