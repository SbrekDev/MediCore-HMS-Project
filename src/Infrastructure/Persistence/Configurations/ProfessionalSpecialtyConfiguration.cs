using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class ProfessionalSpecialtyConfiguration : IEntityTypeConfiguration<ProfessionalSpecialty>
{
    public void Configure(EntityTypeBuilder<ProfessionalSpecialty> builder)
    {
        builder.ToTable("ProfessionalSpecialties");

        builder.HasKey(ps => new { ps.ProfessionalId, ps.SpecialtyId });

        builder.Property(ps => ps.IsPrimary)
            .IsRequired();

        builder.Property(ps => ps.DurationOverrideMinutes)
            .IsRequired(false);

        builder.HasIndex(ps => new { ps.ProfessionalId, ps.IsPrimary })
            .IsUnique()
            .HasFilter("[IsPrimary] = 1");

        builder.HasOne<Professional>()
            .WithMany(p => p.Specialties)
            .HasForeignKey(ps => ps.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Specialty>()
            .WithMany()
            .HasForeignKey(ps => ps.SpecialtyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
