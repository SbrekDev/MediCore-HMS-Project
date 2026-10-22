using FluentAssertions;
using MediCore.Domain.Scheduling;

namespace MediCore.Domain.UnitTests;

public class AppointmentSchedulerTests
{
    private static readonly Guid ProfessionalId = Guid.NewGuid();
    private static readonly DateTimeOffset Day = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Schedule_rejects_professional_overlap()
    {
        var context = new ScheduleContext(
        [
            new ExistingAppointment(ProfessionalId, Guid.NewGuid(), Day.AddHours(10), Day.AddHours(10).AddMinutes(30))
        ]);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, Guid.NewGuid(), Day.AddHours(10).AddMinutes(15), 30, IsSobreturno: false);

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
        ]);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, Guid.NewGuid(), Day.AddHours(10).AddMinutes(30), 30, IsSobreturno: false);

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
        ]);
        var request = new SchedulingRequest(Guid.NewGuid(), ProfessionalId, Guid.NewGuid(), Day.AddHours(10), 30, IsSobreturno: false);

        var result = AppointmentScheduler.Schedule(request, context);

        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(Day.AddHours(10));
        result.Value.End.Should().Be(Day.AddHours(10).AddMinutes(30));
    }
}
