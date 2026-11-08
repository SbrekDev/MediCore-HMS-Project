namespace MediCore.Domain.Entities;

public static class SpecialtySeedData
{
    public static readonly IReadOnlyList<Specialty> Catalog =
    [
        new Specialty(Guid.Parse("11111111-1111-1111-1111-111111111111")) { Code = "CARDIOLOGY", Name = "Cardiología", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("22222222-2222-2222-2222-222222222222")) { Code = "GENERAL_MEDICINE", Name = "Clínica Médica", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("33333333-3333-3333-3333-333333333333")) { Code = "PEDIATRICS", Name = "Pediatría", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("44444444-4444-4444-4444-444444444444")) { Code = "DERMATOLOGY", Name = "Dermatología", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("55555555-5555-5555-5555-555555555555")) { Code = "GYNECOLOGY", Name = "Ginecología", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("66666666-6666-6666-6666-666666666666")) { Code = "ORTHOPEDICS", Name = "Traumatología", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("77777777-7777-7777-7777-777777777777")) { Code = "NEUROLOGY", Name = "Neurología", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("88888888-8888-8888-8888-888888888888")) { Code = "OPHTHALMOLOGY", Name = "Oftalmología", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("99999999-9999-9999-9999-999999999999")) { Code = "PSYCHOLOGY", Name = "Psicología", DefaultDurationMinutes = 30, IsActive = true },
        new Specialty(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")) { Code = "DENTISTRY", Name = "Odontología", DefaultDurationMinutes = 30, IsActive = true }
    ];
}
