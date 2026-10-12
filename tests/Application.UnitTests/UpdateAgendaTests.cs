using FluentAssertions;
using MediCore.Application.Abstractions;
using MediCore.Application.Agendas;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class UpdateAgendaTests
{
    [Fact]
    public async Task UpdateAgenda_creates_agenda_with_weekly_blocks()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("UpdateAgendaCreate"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "John",
            LastName = "Doe",
            NationalLicenseMN = "12345",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = Guid.Empty
        };

        context.Professionals.Add(professional);
        await context.SaveChangesAsync();

        var handler = new UpdateAgenda.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new UpdateAgenda.Command(
            professional.Id,
            2,
            [
                new UpdateAgenda.AvailabilityBlockRequest(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)),
                new UpdateAgenda.AvailabilityBlockRequest(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0))
            ]));

        result.IsSuccess.Should().BeTrue();

        var agenda = await context.Agendas
            .Include(a => a.Blocks)
            .FirstOrDefaultAsync(a => a.ProfessionalId == professional.Id);

        agenda.Should().NotBeNull();
        agenda!.MaxSobreturnosPerDay.Should().Be(2);
        agenda.Blocks.Should().HaveCount(2);
        agenda.Blocks.Should().ContainSingle(b => b.DayOfWeek == DayOfWeek.Monday && b.Start == new TimeOnly(8, 0) && b.End == new TimeOnly(12, 0));
    }

    [Fact]
    public async Task UpdateAgenda_replaces_existing_blocks()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("UpdateAgendaReplace"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "John",
            LastName = "Doe",
            NationalLicenseMN = "12345",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = Guid.Empty
        };

        context.Professionals.Add(professional);
        await context.SaveChangesAsync();

        var handler = new UpdateAgenda.Handler(context, new TestCurrentUserService());

        await handler.HandleAsync(new UpdateAgenda.Command(
            professional.Id,
            1,
            [new UpdateAgenda.AvailabilityBlockRequest(DayOfWeek.Tuesday, new TimeOnly(8, 0), new TimeOnly(12, 0))]));

        var result = await handler.HandleAsync(new UpdateAgenda.Command(
            professional.Id,
            2,
            [new UpdateAgenda.AvailabilityBlockRequest(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(13, 0))]));

        result.IsSuccess.Should().BeTrue();

        var agenda = await context.Agendas
            .Include(a => a.Blocks)
            .FirstOrDefaultAsync(a => a.ProfessionalId == professional.Id);

        agenda!.MaxSobreturnosPerDay.Should().Be(2);
        agenda.Blocks.Should().HaveCount(1);
        agenda.Blocks.Single().DayOfWeek.Should().Be(DayOfWeek.Wednesday);
    }

    [Fact]
    public async Task UpdateAgenda_with_overlapping_blocks_returns_validation_error()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("UpdateAgendaOverlap"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "John",
            LastName = "Doe",
            NationalLicenseMN = "12345",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = Guid.Empty
        };

        context.Professionals.Add(professional);
        await context.SaveChangesAsync();

        var handler = new UpdateAgenda.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new UpdateAgenda.Command(
            professional.Id,
            1,
            [
                new UpdateAgenda.AvailabilityBlockRequest(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)),
                new UpdateAgenda.AvailabilityBlockRequest(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(15, 0))
            ]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Agenda.OverlappingBlocks");
    }

    [Fact]
    public async Task UpdateAgenda_with_invalid_time_range_returns_validation_error()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("UpdateAgendaInvalidTime"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var professional = new Professional(Guid.NewGuid())
        {
            FirstName = "John",
            LastName = "Doe",
            NationalLicenseMN = "12345",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = Guid.Empty
        };

        context.Professionals.Add(professional);
        await context.SaveChangesAsync();

        var handler = new UpdateAgenda.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new UpdateAgenda.Command(
            professional.Id,
            1,
            [new UpdateAgenda.AvailabilityBlockRequest(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(8, 0))]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Agenda.BlockStartMustBeBeforeEnd");
    }

    [Fact]
    public async Task UpdateAgenda_for_unknown_professional_returns_not_found()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("UpdateAgendaNotFound"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new UpdateAgenda.Handler(context, new TestCurrentUserService());

        var result = await handler.HandleAsync(new UpdateAgenda.Command(
            Guid.NewGuid(),
            1,
            [new UpdateAgenda.AvailabilityBlockRequest(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Professional.NotFound");
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string? UserName => "tester";
        public IReadOnlyList<string> Roles => ["Admin"];
    }
}
