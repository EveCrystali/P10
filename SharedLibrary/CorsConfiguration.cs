using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCorsLibrary;

public static class CorsConfiguration
{
    public static void AddCorsConfiguration(this WebApplicationBuilder builder, string corsPolicyName, string origins)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsPolicyName,
                builder => builder.WithOrigins(origins)
                                .AllowAnyMethod()
                                .AllowAnyHeader()
                                .AllowCredentials());
        });
    }
}