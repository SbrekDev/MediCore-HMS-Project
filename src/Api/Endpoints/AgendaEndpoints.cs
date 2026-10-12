using MediCore.Api.Authorization;
using MediCore.Application.Agendas;
using Microsoft.AspNetCore.Authorization;

namespace MediCore.Api.Endpoints;

public static class AgendaEndpoints
{
    public static IEndpointRouteBuilder MapAgendaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/professionals");

        group.MapPut("/{id:guid}/agenda", [Authorize(Policy = AuthorizationPolicies.AdminOnly)] async (
            Guid id,
            UpdateAgendaRequest request,
            UpdateAgenda.Handler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new UpdateAgenda.Command(
                    id,
                    request.MaxSobreturnosPerDay,
                    request.Blocks.Select(b => new UpdateAgenda.AvailabilityBlockRequest(
                        Enum.Parse<DayOfWeek>(b.DayOfWeek, true),
                        TimeOnly.Parse(b.Start),
                        TimeOnly.Parse(b.End))).ToList()),
                cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : MapErrorResult(result.Error.Code, result.Error.Message);
        });

        return app;
    }

    private static IResult MapErrorResult(string code, string message)
    {
        return code switch
        {
            "Professional.NotFound" => Results.NotFound(new { code, message }),
            _ => Results.BadRequest(new { code, message })
        };
    }
}

public sealed record AvailabilityBlockRequest(string DayOfWeek, string Start, string End);

public sealed record UpdateAgendaRequest(int MaxSobreturnosPerDay, IReadOnlyList<AvailabilityBlockRequest> Blocks);
