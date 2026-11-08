namespace MediCore.Domain.Entities;

public static class InsurerSeedData
{
    public static readonly IReadOnlyList<Insurer> Catalog =
    [
        new Insurer(Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"))
        {
            Name = "Sanatorio Los Ceibos Salud",
            Type = InsurerType.ObraSocial
        },
        new Insurer(Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"))
        {
            Name = "MediCare Prepaga",
            Type = InsurerType.Prepaga
        },
        new Insurer(Guid.Parse("bbbbbbbb-3333-3333-3333-333333333333"))
        {
            Name = "Federación Médica Mutual",
            Type = InsurerType.ObraSocial
        }
    ];
}
