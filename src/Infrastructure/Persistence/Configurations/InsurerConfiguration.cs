using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class InsurerConfiguration : IEntityTypeConfiguration<Insurer>
{
    public void Configure(EntityTypeBuilder<Insurer> builder)
    {
        builder.ToTable("Insurers");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Type)
            .IsRequired();

        builder.HasIndex(i => i.Name)
            .IsUnique();
    }
}
