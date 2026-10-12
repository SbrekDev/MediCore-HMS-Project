using MediCore.Api.Authorization;
using MediCore.Api.Endpoints;
using MediCore.Application;
using MediCore.Application.Patients;
using MediCore.Application.Specialties;

namespace MediCore.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddApplication();
        services.AddMediCoreAuthorizationPolicies();
        services.AddScoped<CreatePatient.Handler>();
        services.AddScoped<ListSpecialties.Handler>();
        services.AddScoped<ToggleSpecialtyActive.Handler>();
        return services;
    }

    public static IApplicationBuilder UseApiMiddleware(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapAppointmentEndpoints();
        app.MapPatientEndpoints();
        app.MapSpecialtyEndpoints();
        return app;
    }
}
