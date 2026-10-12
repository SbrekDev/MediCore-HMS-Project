using MediCore.Api;
using MediCore.Application;
using MediCore.Infrastructure;
using MediCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedRolesAsync(scope.ServiceProvider);
        await DbSeeder.SeedSpecialtiesAsync(dbContext);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to migrate or seed database on startup.");
    }
}

app.UseHttpsRedirection();
app.UseApiMiddleware();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapApiEndpoints();

app.Run();

public partial class Program { }
