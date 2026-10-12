using MediCore.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace MediCore.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments");

        group.MapPost("/", [Authorize(Policy = AuthorizationPolicies.ReceptionStaff)] () =>
        {
            return Results.NoContent();
        });

        return app;
    }
}
