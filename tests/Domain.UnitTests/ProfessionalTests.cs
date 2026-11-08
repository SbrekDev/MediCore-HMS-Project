using FluentAssertions;
using MediCore.Domain.Entities;

namespace MediCore.Domain.UnitTests;

public class ProfessionalTests
{
    [Fact]
    public void Professional_holds_national_license_and_provincial_licenses()
    {
        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "John",
            LastName = "Doe",
            NationalLicenseMN = "12345",
            ProvincialLicenses =
            [
                new ProvincialLicenseMP { Number = "MP-001", Province = "Buenos Aires" },
                new ProvincialLicenseMP { Number = "MP-002", Province = "Córdoba" }
            ]
        };

        professional.NationalLicenseMN.Should().Be("12345");
        professional.ProvincialLicenses.Should().HaveCount(2);
    }
}
