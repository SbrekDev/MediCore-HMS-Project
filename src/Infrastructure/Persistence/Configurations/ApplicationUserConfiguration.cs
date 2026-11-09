using MediCore.Domain.Entities;
using MediCore.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediCore.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // The complete/no-show ownership guard trusts this link, so it must stay referentially
        // sound and single-valued even though no endpoint assigns it yet.
        builder.HasOne<Professional>()
            .WithMany()
            .HasForeignKey(u => u.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.ProfessionalId)
            .IsUnique()
            .HasFilter("[ProfessionalId] IS NOT NULL");
    }
}
