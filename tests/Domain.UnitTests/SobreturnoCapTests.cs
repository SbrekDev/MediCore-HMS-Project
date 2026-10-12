using FluentAssertions;
using MediCore.Domain.Entities;

namespace MediCore.Domain.UnitTests;

public class SobreturnoCapTests
{
    [Fact]
    public void Agenda_allows_sobreturno_when_under_daily_cap()
    {
        var agenda = new Agenda(Guid.NewGuid()) { MaxSobreturnosPerDay = 2 };

        agenda.CanAddSobreturno(0).Should().BeTrue();
        agenda.CanAddSobreturno(1).Should().BeTrue();
    }

    [Fact]
    public void Agenda_rejects_sobreturno_when_daily_cap_reached()
    {
        var agenda = new Agenda(Guid.NewGuid()) { MaxSobreturnosPerDay = 2 };

        agenda.CanAddSobreturno(2).Should().BeFalse();
        agenda.CanAddSobreturno(3).Should().BeFalse();
    }

    [Fact]
    public void Agenda_rejects_sobreturno_when_cap_is_zero()
    {
        var agenda = new Agenda(Guid.NewGuid()) { MaxSobreturnosPerDay = 0 };

        agenda.CanAddSobreturno(0).Should().BeFalse();
    }
}
