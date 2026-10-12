using MediCore.Application.Abstractions;
using MediCore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Infrastructure.Persistence;

public static class DbSeeder
{
    private static readonly string[] Roles = ["Admin", "Receptionist", "Professional"];

    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    public static async Task SeedSpecialtiesAsync(IApplicationDbContext context)
    {
        var existingCodes = await context.Specialties
            .Select(s => s.Code)
            .ToListAsync();

        foreach (var seed in SpecialtySeedData.Catalog)
        {
            if (!existingCodes.Contains(seed.Code))
            {
                context.Specialties.Add(new Specialty(seed.Id)
                {
                    Code = seed.Code,
                    Name = seed.Name,
                    DefaultDurationMinutes = seed.DefaultDurationMinutes,
                    IsActive = seed.IsActive,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = Guid.Empty,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ModifiedBy = Guid.Empty
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
