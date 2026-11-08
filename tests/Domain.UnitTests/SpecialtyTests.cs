using FluentAssertions;
using MediCore.Domain.Entities;

namespace MediCore.Domain.UnitTests;

public class SpecialtyTests
{
    [Fact]
    public void Specialty_seed_data_has_unique_codes_and_thirty_minute_defaults()
    {
        var specialties = SpecialtySeedData.Catalog;

        specialties.Should().NotBeEmpty();
        specialties.Should().OnlyHaveUniqueItems(s => s.Code);
        specialties.Should().AllSatisfy(s =>
        {
            s.Name.Should().NotBeNullOrWhiteSpace();
            s.DefaultDurationMinutes.Should().Be(30);
            s.IsActive.Should().BeTrue();
        });
    }
}
