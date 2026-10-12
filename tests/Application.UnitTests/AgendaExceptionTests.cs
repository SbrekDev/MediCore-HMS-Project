using FluentAssertions;
using MediCore.Application.Abstractions;
using MediCore.Application.Agendas;
using MediCore.Domain.Entities;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class AgendaExceptionTests
{
    [Fact]
    public async Task CreateAgendaException_persists_full_day_exception()
    {
        var (context, professionalId) = await CreateScenarioAsync();
        var handler = new CreateAgendaException.Handler(context);

        var date = new DateOnly(2026, 10, 12);
        var result = await handler.HandleAsync(new CreateAgendaException.Command(
            professionalId, date, null, null, "Holiday"));

        result.IsSuccess.Should().BeTrue();

        var exception = await context.AgendaExceptions
            .FirstOrDefaultAsync(e => e.AgendaId == context.Agendas.Single(a => a.ProfessionalId == professionalId).Id);

        exception.Should().NotBeNull();
        exception!.Date.Should().Be(date);
        exception.Start.Should().BeNull();
        exception.End.Should().BeNull();
        exception.Reason.Should().Be("Holiday");
        exception.IsFullDay.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAgendaException_persists_partial_exception()
    {
        var (context, professionalId) = await CreateScenarioAsync();
        var handler = new CreateAgendaException.Handler(context);

        var date = new DateOnly(2026, 10, 13);
        var result = await handler.HandleAsync(new CreateAgendaException.Command(
            professionalId, date, new TimeOnly(8, 0), new TimeOnly(12, 0), "Morning leave"));

        result.IsSuccess.Should().BeTrue();

        var exception = await context.AgendaExceptions
            .FirstOrDefaultAsync(e => e.AgendaId == context.Agendas.Single(a => a.ProfessionalId == professionalId).Id && e.Date == date);

        exception.Should().NotBeNull();
        exception!.Start.Should().Be(new TimeOnly(8, 0));
        exception.End.Should().Be(new TimeOnly(12, 0));
        exception.IsFullDay.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAgendaException_for_unknown_professional_returns_not_found()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("ExceptionUnknownProfessional"));
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        var handler = new CreateAgendaException.Handler(context);
        var result = await handler.HandleAsync(new CreateAgendaException.Command(
            Guid.NewGuid(), new DateOnly(2026, 10, 12), null, null, "Holiday"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Professional.NotFound");
    }

    [Fact]
    public async Task CreateAgendaException_for_duplicate_date_returns_conflict()
    {
        var (context, professionalId) = await CreateScenarioAsync();
        var handler = new CreateAgendaException.Handler(context);

        var date = new DateOnly(2026, 10, 12);
        await handler.HandleAsync(new CreateAgendaException.Command(
            professionalId, date, null, null, "Holiday"));

        var result = await handler.HandleAsync(new CreateAgendaException.Command(
            professionalId, date, new TimeOnly(8, 0), new TimeOnly(12, 0), "Updated"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AgendaException.DuplicateDate");
    }

    [Fact]
    public async Task CreateAgendaException_with_partial_range_start_after_end_returns_validation_error()
    {
        var (context, professionalId) = await CreateScenarioAsync();
        var handler = new CreateAgendaException.Handler(context);

        var result = await handler.HandleAsync(new CreateAgendaException.Command(
            professionalId, new DateOnly(2026, 10, 12), new TimeOnly(12, 0), new TimeOnly(8, 0), "Invalid"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AgendaException.InvalidTimeRange");
    }

    [Fact]
    public async Task DeleteAgendaException_removes_exception()
    {
        var (context, professionalId) = await CreateScenarioAsync();
        var createHandler = new CreateAgendaException.Handler(context);
        var date = new DateOnly(2026, 10, 12);
        var createResult = await createHandler.HandleAsync(new CreateAgendaException.Command(
            professionalId, date, null, null, "Holiday"));
        var exceptionId = createResult.Value;

        var deleteHandler = new DeleteAgendaException.Handler(context);
        var deleteResult = await deleteHandler.HandleAsync(new DeleteAgendaException.Command(professionalId, exceptionId));

        deleteResult.IsSuccess.Should().BeTrue();
        (await context.AgendaExceptions.AnyAsync(e => e.Id == exceptionId)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAgendaException_for_unknown_exception_returns_not_found()
    {
        var (context, professionalId) = await CreateScenarioAsync();
        var handler = new DeleteAgendaException.Handler(context);

        var result = await handler.HandleAsync(new DeleteAgendaException.Command(professionalId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AgendaException.NotFound");
    }

    private static async Task<(AppDbContext Context, Guid ProfessionalId)> CreateScenarioAsync()
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

        var agenda = new Agenda(Guid.NewGuid())
        {
            ProfessionalId = professional.Id,
            MaxSobreturnosPerDay = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedAt = DateTimeOffset.UtcNow,
            ModifiedBy = Guid.Empty
        };

        context.Agendas.Add(agenda);
        await context.SaveChangesAsync();

        return (context, professional.Id);
    }
}
