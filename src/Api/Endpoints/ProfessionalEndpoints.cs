using MediCore.Api.Authorization;
using MediCore.Application.Professionals;
using Microsoft.AspNetCore.Authorization;

namespace MediCore.Api.Endpoints;

public static class ProfessionalEndpoints
{
    public static IEndpointRouteBuilder MapProfessionalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/professionals");

        group.MapGet("/", [Authorize(Policy = AuthorizationPolicies.ClinicalStaff)] async (
            ListProfessionals.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var professionals = await handler.HandleAsync(cancellationToken);
            return Results.Ok(professionals);
        });

        group.MapGet("/{id:guid}", [Authorize(Policy = AuthorizationPolicies.ClinicalStaff)] async (
            Guid id,
            GetProfessional.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { code = result.Error.Code, message = result.Error.Message });
        });

        group.MapPost("/", [Authorize(Policy = AuthorizationPolicies.AdminOnly)] async (
            CreateProfessional.Command command,
            CreateProfessional.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/professionals/{result.Value}", new { professionalId = result.Value })
                : Results.BadRequest(new { code = result.Error.Code, message = result.Error.Message });
        });

        group.MapPut("/{id:guid}", [Authorize(Policy = AuthorizationPolicies.AdminOnly)] async (
            Guid id,
            UpdateProfessionalRequest request,
            UpdateProfessional.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new UpdateProfessional.Command(
                    id,
                    request.FirstName,
                    request.LastName,
                    request.NationalLicenseMN,
                    request.ProvincialLicenses.Select(l => new CreateProfessional.ProvincialLicenseRequest(l.Number, l.Province)).ToList()),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : MapErrorResult(result.Error);
        });

        return app;
    }

    private static IResult MapErrorResult(MediCore.Domain.Errors.DomainError error)
    {
        return error.Code switch
        {
            "Professional.NotFound" => Results.NotFound(new { code = error.Code, message = error.Message }),
            _ => Results.BadRequest(new { code = error.Code, message = error.Message })
        };
    }
}

public sealed record UpdateProfessionalRequest(
    string FirstName,
    string LastName,
    string NationalLicenseMN,
    IReadOnlyList<UpdateProvincialLicenseRequest> ProvincialLicenses);

public sealed record UpdateProvincialLicenseRequest(string Number, string Province);
