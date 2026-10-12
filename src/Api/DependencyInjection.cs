using MediCore.Api.Authorization;
using MediCore.Api.Endpoints;
using MediCore.Application;
using MediCore.Application.Agendas;
using MediCore.Application.Patients;
using MediCore.Application.Professionals;
using MediCore.Application.Specialties;

namespace MediCore.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddApplication();
        services.AddMediCoreAuthorizationPolicies();
        services.AddScoped<CreatePatient.Handler>();
        services.AddScoped<ListPatients.Handler>();
        services.AddScoped<GetPatient.Handler>();
        services.AddScoped<UpdatePatient.Handler>();
        services.AddScoped<AddCoverage.Handler>();
        services.AddScoped<RecordConsent.Handler>();
        services.AddScoped<AssignProfessionalSpecialty.Handler>();
        services.AddScoped<CreateProfessional.Handler>();
        services.AddScoped<ListProfessionals.Handler>();
        services.AddScoped<GetProfessional.Handler>();
        services.AddScoped<UpdateProfessional.Handler>();
        services.AddScoped<ListSpecialties.Handler>();
        services.AddScoped<ToggleSpecialtyActive.Handler>();
        services.AddScoped<UpdateAgenda.Handler>();
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
        app.MapAgendaEndpoints();
        app.MapAppointmentEndpoints();
        app.MapPatientEndpoints();
        app.MapProfessionalEndpoints();
        app.MapSpecialtyEndpoints();
        return app;
    }
}
