using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace SharedSwaggerLibrary;

public static class SwaggerConfiguration
{
    public static void AddSwaggerDocumentation(this IServiceCollection services, string apiTitle, string apiVersion = "v1")
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(apiVersion, new OpenApiInfo { Title = apiTitle, Version = apiVersion });
            c.IncludeXmlComments(Assembly.GetExecutingAssembly().Location);

            // Définition de sécurité pour JWT
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Veuillez entrer 'Bearer' suivi d'un espace puis du token JWT dans l'en-tête",
            });

            // Configuration de la sécurité pour Swagger
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
            });
        });
    }
}
