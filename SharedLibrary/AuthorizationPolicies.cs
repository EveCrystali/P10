using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SharedAuthorizationLibrary;

public static class AuthorizationPolicies
{
    public static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
        .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
        .AddPolicy("RequirePractitionerRole", policy => policy.RequireRole("Practitioner"))
        .AddPolicy("RequireUserRole", policy => policy.RequireRole("User"))
        .AddPolicy("RequirePractitionerRoleOrHigher", policy => policy.RequireRole("Practitioner", "Admin"));
    }
}
