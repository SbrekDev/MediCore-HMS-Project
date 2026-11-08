namespace MediCore.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ReceptionStaff = "ReceptionStaff";
    public const string ClinicalStaff = "ClinicalStaff";

    public static IServiceCollection AddMediCoreAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminOnly, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(ReceptionStaff, policy =>
                policy.RequireRole("Admin", "Receptionist"));

            options.AddPolicy(ClinicalStaff, policy =>
                policy.RequireRole("Admin", "Receptionist", "Professional"));
        });

        return services;
    }
}
