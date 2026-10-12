using MediCore.Api.Authorization;
using MediCore.Application.Patients;
using Microsoft.AspNetCore.Authorization;

namespace MediCore.Api.Endpoints;

public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients");

        group.MapPost("/", [Authorize(Policy = AuthorizationPolicies.ReceptionStaff)] async (
            CreatePatient.Command command,
            CreatePatient.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var patientId = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/api/patients/{patientId}", new { patientId });
        });

        return app;
    }
}
