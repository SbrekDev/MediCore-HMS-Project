using MediCore.Infrastructure.Persistence.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(ae => ae.Id);

        builder.Property(ae => ae.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ae => ae.EntityType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ae => ae.ChangesJson)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(ae => new { ae.EntityType, ae.EntityId });
        builder.HasIndex(ae => ae.TimestampUtc);
    }
}
