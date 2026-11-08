using FluentAssertions;
using MediCore.Application.Abstractions;
using MediCore.Application.Agendas;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class GetAvailabilityTests
{
    [Fact]
    public async Task GetAvailability_returns_weekly_blocks_for_date()
    {
        var (context, professionalId) = await CreateScenarioAsync(
            blocks:
            [
                new AvailabilityBlock { DayOfWeek = DayOfWeek.Monday, Start = new TimeOnly(8, 0), End = new TimeOnly(12, 0) }
            ]);

        var handler = new GetAvailability.Handler(context);
        var result = await handler.HandleAsync(new GetAvailability.Query(
            professionalId,
            new DateOnly(2026, 10, 5)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Windows.Should().HaveCount(1);
        result.Value.Windows.Single().Should().BeEquivalentTo(
            new GetAvailability.AvailabilityWindow(new TimeOnly(8, 0), new TimeOnly(12, 0)));
        result.Value.MaxSobreturnosPerDay.Should().Be(2);
    }

    [Fact]
    public async Task GetAvailability_returns_empty_when_no_agenda()
    {
        var (context, professionalId) = await CreateProfessionalOnlyAsync();

        var handler = new GetAvailability.Handler(context);
        var result = await handler.HandleAsync(new GetAvailability.Query(
            professionalId,
            new DateOnly(2026, 10, 5)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Windows.Should().BeEmpty();
        result.Value.MaxSobreturnosPerDay.Should().Be(0);
    }

    [Fact]
    public async Task GetAvailability_returns_not_found_for_unknown_professional()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("AvailabilityUnknown"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new GetAvailability.Handler(context);
        var result = await handler.HandleAsync(new GetAvailability.Query(
            Guid.NewGuid(),
            new DateOnly(2026, 10, 5)));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Professional.NotFound");
    }

    [Fact]
    public async Task GetAvailability_removes_full_day_exception()
    {
        var (context, professionalId) = await CreateScenarioAsync(
            blocks:
            [
                new AvailabilityBlock { DayOfWeek = DayOfWeek.Monday, Start = new TimeOnly(8, 0), End = new TimeOnly(12, 0) }
            ],
            exceptions:
            [
                new AgendaException { Date = new DateOnly(2026, 10, 5), Reason = "Holiday" }
            ]);

        var handler = new GetAvailability.Handler(context);
        var result = await handler.HandleAsync(new GetAvailability.Query(
            professionalId,
            new DateOnly(2026, 10, 5)));

        result.Value.Windows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailability_keeps_remaining_hours_after_partial_exception()
    {
        var (context, professionalId) = await CreateScenarioAsync(
            blocks:
            [
                new AvailabilityBlock { DayOfWeek = DayOfWeek.Monday, Start = new TimeOnly(8, 0), End = new TimeOnly(18, 0) }
            ],
            exceptions:
            [
                new AgendaException
                {
                    Date = new DateOnly(2026, 10, 5),
                    Start = new TimeOnly(8, 0),
                    End = new TimeOnly(12, 0),
                    Reason = "Leave"
                }
            ]);

        var handler = new GetAvailability.Handler(context);
        var result = await handler.HandleAsync(new GetAvailability.Query(
            professionalId,
            new DateOnly(2026, 10, 5)));

        result.Value.Windows.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(
                new GetAvailability.AvailabilityWindow(new TimeOnly(12, 0), new TimeOnly(18, 0)));
    }

    private static async Task<(AppDbContext Context, Guid ProfessionalId)> CreateScenarioAsync(
        IReadOnlyList<AvailabilityBlock> blocks,
        IReadOnlyList<AgendaException>? exceptions = null)
    {
        var (context, professionalId) = await CreateProfessionalOnlyAsync();

        var agenda = new Agenda(Guid.NewGuid())
        {
            ProfessionalId = professionalId,
            MaxSobreturnosPerDay = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = Guid.Empty
        };

        foreach (var block in blocks)
        {
            block.Id = Guid.NewGuid();
            block.AgendaId = agenda.Id;
            agenda.Blocks.Add(block);
        }

        context.Agendas.Add(agenda);

        if (exceptions is not null)
        {
            foreach (var exception in exceptions)
            {
                exception.Id = Guid.NewGuid();
                exception.AgendaId = agenda.Id;
                context.AgendaExceptions.Add(exception);
            }
        }

        await context.SaveChangesAsync();
        return (context, professionalId);
    }

    private static async Task<(AppDbContext Context, Guid ProfessionalId)> CreateProfessionalOnlyAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
        return (context, professional.Id);
    }
}
