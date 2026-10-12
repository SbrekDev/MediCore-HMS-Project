using FluentAssertions;
using MediCore.Domain.Entities;

namespace MediCore.Domain.UnitTests;

public class AgendaTests
{
    [Fact]
    public void Agenda_holds_weekly_availability_blocks_for_professional()
    {
        var professionalId = Guid.NewGuid();
        var agenda = new Agenda(Guid.NewGuid())
        {
            ProfessionalId = professionalId,
            MaxSobreturnosPerDay = 2,
            Blocks =
            [
                new AvailabilityBlock
                {
                    DayOfWeek = DayOfWeek.Monday,
                    Start = new TimeOnly(8, 0),
                    End = new TimeOnly(12, 0)
                },
                new AvailabilityBlock
                {
                    DayOfWeek = DayOfWeek.Monday,
                    Start = new TimeOnly(14, 0),
                    End = new TimeOnly(18, 0)
                }
            ]
        };

        agenda.ProfessionalId.Should().Be(professionalId);
        agenda.MaxSobreturnosPerDay.Should().Be(2);
        agenda.Blocks.Should().HaveCount(2);
    }

    [Fact]
    public void Availability_block_contains_time_within_window()
    {
        var block = new AvailabilityBlock
        {
            DayOfWeek = DayOfWeek.Monday,
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(12, 0)
        };

        block.Contains(new TimeOnly(9, 0)).Should().BeTrue();
        block.Contains(new TimeOnly(8, 0)).Should().BeTrue();
        block.Contains(new TimeOnly(11, 59)).Should().BeTrue();
    }

    [Fact]
    public void Availability_block_does_not_contain_time_outside_window()
    {
        var block = new AvailabilityBlock
        {
            DayOfWeek = DayOfWeek.Monday,
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(12, 0)
        };

        block.Contains(new TimeOnly(7, 59)).Should().BeFalse();
        block.Contains(new TimeOnly(12, 0)).Should().BeFalse();
    }
}
