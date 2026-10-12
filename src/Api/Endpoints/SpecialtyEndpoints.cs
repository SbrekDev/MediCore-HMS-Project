using MediCore.Api.Authorization;
using MediCore.Application.Specialties;
using Microsoft.AspNetCore.Authorization;

namespace MediCore.Api.Endpoints;

public static class SpecialtyEndpoints
{
    public static IEndpointRouteBuilder MapSpecialtyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/specialties");

        group.MapGet("/", async (
            bool? activeOnly,
            ListSpecialties.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var specialties = await handler.HandleAsync(
                new ListSpecialties.Query(ActiveOnly: activeOnly == true),
                cancellationToken);
            return Results.Ok(specialties);
        });

        group.MapPatch("/{id:guid}/active", [Authorize(Policy = AuthorizationPolicies.AdminOnly)] async (
            Guid id,
            ToggleActiveRequest request,
            ToggleSpecialtyActive.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new ToggleSpecialtyActive.Command(id, request.IsActive),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { code = result.Error.Code, message = result.Error.Message });
        });

        return app;
    }
}

public sealed record ToggleActiveRequest(bool IsActive);
