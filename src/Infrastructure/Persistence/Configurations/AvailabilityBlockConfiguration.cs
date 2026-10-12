using MediCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class AvailabilityBlockConfiguration : IEntityTypeConfiguration<AvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<AvailabilityBlock> builder)
    {
        builder.ToTable("AvailabilityBlocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.AgendaId)
            .IsRequired();

        builder.Property(b => b.DayOfWeek)
            .IsRequired();

        builder.Property(b => b.Start)
            .IsRequired();

        builder.Property(b => b.End)
            .IsRequired();

        builder.HasIndex(b => new { b.AgendaId, b.DayOfWeek, b.Start, b.End })
            .IsUnique();
    }
}
