using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class ProvincialLicenseMPConfiguration : IEntityTypeConfiguration<ProvincialLicenseMP>
{
    public void Configure(EntityTypeBuilder<ProvincialLicenseMP> builder)
    {
        builder.ToTable("ProvincialLicenseMPs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Number)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Province)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(l => new { l.ProfessionalId, l.Number })
            .IsUnique();
    }
}
