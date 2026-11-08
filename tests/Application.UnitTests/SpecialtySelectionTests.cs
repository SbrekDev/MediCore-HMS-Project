using FluentAssertions;
using MediCore.Application.Specialties;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class SpecialtySelectionTests
{
    [Fact]
    public async Task Inactive_specialty_is_excluded_from_booking_options()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("SpecialtySelection"));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        context.Specialties.Add(new Specialty(Guid.NewGuid()) { Code = "ACTIVE", Name = "Active Specialty", IsActive = true });
        context.Specialties.Add(new Specialty(Guid.NewGuid()) { Code = "INACTIVE", Name = "Inactive Specialty", IsActive = false });
        await context.SaveChangesAsync();

        var handler = new ListSpecialties.Handler(context);
        var result = await handler.HandleAsync(new ListSpecialties.Query(ActiveOnly: true), CancellationToken.None);

        result.Should().ContainSingle(s => s.Code == "ACTIVE");
        result.Should().NotContain(s => s.Code == "INACTIVE");
    }
}
