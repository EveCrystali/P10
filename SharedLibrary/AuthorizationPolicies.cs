using Microsoft.Extensions.DependencyInjection;
namespace SharedAuthorizationLibrary;

public static class AuthorizationPolicies
{
    public static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
                .AddPolicy("RequireAdminRole", configurePolicy: policy => policy.RequireRole("Admin"))
                .AddPolicy("RequirePractitionerRole", configurePolicy: policy => policy.RequireRole("Practitioner"))
                .AddPolicy("RequireUserRole", configurePolicy: policy => policy.RequireRole("User"))
                .AddPolicy("RequirePractitionerRoleOrHigher", configurePolicy: policy => policy.RequireRole("Practitioner", "Admin"));
    }
}