using FluentAssertions;
using MediCore.Domain.Entities;

namespace MediCore.Domain.UnitTests;

public class ProfessionalSpecialtyDurationTests
{
    [Fact]
    public void Duration_defaults_to_specialty_default_when_no_override_is_set()
    {
        var assignment = new ProfessionalSpecialty
        {
            ProfessionalId = Guid.NewGuid(),
            SpecialtyId = Guid.NewGuid(),
            IsPrimary = false,
            DurationOverrideMinutes = null
        };

        var duration = assignment.GetDurationMinutes(30);

        duration.Should().Be(30);
    }

    [Fact]
    public void Duration_uses_override_when_provided()
    {
        var assignment = new ProfessionalSpecialty
        {
            ProfessionalId = Guid.NewGuid(),
            SpecialtyId = Guid.NewGuid(),
            IsPrimary = false,
            DurationOverrideMinutes = 45
        };

        var duration = assignment.GetDurationMinutes(30);

        duration.Should().Be(45);
    }
}
