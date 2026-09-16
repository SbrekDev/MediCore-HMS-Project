using MediCore.Application;

namespace MediCore.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddApplication();
        return services;
    }
}
