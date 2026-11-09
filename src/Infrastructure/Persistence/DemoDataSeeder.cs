using MediCore.Domain.Entities;
using MediCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Infrastructure.Persistence;

/// <summary>
/// Development-only demo accounts. The application has no user-creation path at all, so
/// without these a real user can never hold the <c>professional_id</c> claim the ownership
/// guard in complete/no-show depends on, and the login -> book -> complete flow cannot be
/// walked by hand.
///
/// These credentials are public by design, which is exactly why <see cref="SeedAsync"/> must
/// only ever be called from the development branch of the host.
/// </summary>
public static class DemoDataSeeder
{
    public const string AdminEmail = "admin@medicore.local";
    public const string ReceptionistEmail = "recepcion@medicore.local";
    public const string ProfessionalEmail = "profesional@medicore.local";
    public const string Password = "MediCore1!";
    public const string ProfessionalLicenseMN = "MN-DEMO-0001";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var professional = await EnsureProfessionalAsync(context);
        await EnsureUserAsync(userManager, AdminEmail, "Admin");
        await EnsureUserAsync(userManager, ReceptionistEmail, "Receptionist");
        await EnsureUserAsync(userManager, ProfessionalEmail, "Professional", professional.Id);
    }

    private static async Task<Professional> EnsureProfessionalAsync(AppDbContext context)
    {
        var existing = await context.Professionals
            .Include(p => p.Specialties)
            .FirstOrDefaultAsync(p => p.NationalLicenseMN == ProfessionalLicenseMN);

        // A booking is rejected unless the professional holds the requested specialty, so the
        // demo professional needs one link or the walkthrough stops at the first appointment.
        var specialty = await context.Specialties
            .Where(s => s.IsActive)
            .OrderBy(s => s.Code)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            // A half-seeded row from an older run must be repaired, not trusted: without the
            // link the professional is silently unbookable.
            if (specialty is not null && existing.Specialties.Count == 0)
            {
                existing.Specialties.Add(new ProfessionalSpecialty
                {
                    ProfessionalId = existing.Id,
                    SpecialtyId = specialty.Id,
                    IsPrimary = true
                });

                await context.SaveChangesAsync();
            }

            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "Ana",
            LastName = "Demo",
            NationalLicenseMN = ProfessionalLicenseMN,
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            ModifiedAt = now,
            ModifiedBy = Guid.Empty
        };

        if (specialty is not null)
        {
            professional.Specialties.Add(new ProfessionalSpecialty
            {
                ProfessionalId = professional.Id,
                SpecialtyId = specialty.Id,
                IsPrimary = true
            });
        }

        context.Professionals.Add(professional);
        await context.SaveChangesAsync();

        return professional;
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string role,
        Guid? professionalId = null)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                ProfessionalId = professionalId
            };

            // Hashing is implicit in CreateAsync; the password policy requires digits,
            // mixed case, a non-alphanumeric character and at least 8 characters.
            var created = await userManager.CreateAsync(user, Password);

            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create demo user '{email}': {string.Join(", ", created.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var assigned = await userManager.AddToRoleAsync(user, role);

            if (!assigned.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to add demo user '{email}' to role '{role}': {string.Join(", ", assigned.Errors.Select(e => e.Description))}");
            }
        }

        // An account created before the link existed would keep working but silently fail the
        // ownership guard, so a missing link is repaired here.
        if (professionalId.HasValue && user.ProfessionalId != professionalId)
        {
            user.ProfessionalId = professionalId;
            var linked = await userManager.UpdateAsync(user);

            if (!linked.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to link demo user '{email}' to professional '{professionalId}': {string.Join(", ", linked.Errors.Select(e => e.Description))}");
            }
        }
    }
}
