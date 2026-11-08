using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class ProfessionalConfiguration : IEntityTypeConfiguration<Professional>
{
    public void Configure(EntityTypeBuilder<Professional> builder)
    {
        builder.ToTable("Professionals");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.NationalLicenseMN)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.NationalLicenseMN)
            .IsUnique();

        builder.HasMany(p => p.ProvincialLicenses)
            .WithOne()
            .HasForeignKey(l => l.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
