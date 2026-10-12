using FluentAssertions;
using MediCore.Domain.Entities;

namespace MediCore.Domain.UnitTests;

public class AgendaExceptionTests
{
    [Fact]
    public void Full_day_exception_blocks_any_time_on_that_date()
    {
        var exception = new AgendaException
        {
            AgendaId = Guid.NewGuid(),
            Date = new DateOnly(2026, 10, 12),
            Reason = "Holiday"
        };

        exception.IsFullDay.Should().BeTrue();
        exception.IsBlocking(new TimeOnly(8, 0)).Should().BeTrue();
        exception.IsBlocking(new TimeOnly(12, 0)).Should().BeTrue();
        exception.IsBlocking(new TimeOnly(18, 0)).Should().BeTrue();
    }

    [Fact]
    public void Partial_exception_blocks_only_hours_within_range_and_leaves_remaining_hours_available()
    {
        var exception = new AgendaException
        {
            AgendaId = Guid.NewGuid(),
            Date = new DateOnly(2026, 10, 12),
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(12, 0),
            Reason = "Morning leave"
        };

        exception.IsFullDay.Should().BeFalse();
        exception.IsBlocking(new TimeOnly(7, 59)).Should().BeFalse();
        exception.IsBlocking(new TimeOnly(8, 0)).Should().BeTrue();
        exception.IsBlocking(new TimeOnly(11, 59)).Should().BeTrue();
        exception.IsBlocking(new TimeOnly(12, 0)).Should().BeFalse();
        exception.IsBlocking(new TimeOnly(14, 0)).Should().BeFalse();
    }
}
