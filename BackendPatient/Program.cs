using System.Reflection;
using System.Text;
using BackendPatient.Data;
using BackendPatient.Extensions;
using BackendPatient.Models;
using BackendPatient.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

IConfiguration Configuration = builder.Configuration;

// Add Authorization policies and cookie authentification
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    ConfigurationManager configuration = builder.Configuration;
    string? secretKey = configuration["JwtSettings:JWT_SECRET_KEY"] ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
    if (string.IsNullOrEmpty(secretKey))
    {
        throw new ArgumentNullException(secretKey, "JWT Key configuration is missing.");
    }
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudiences = configuration.GetSection("JwtSettings:Audience").Get<string[]>(),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero,
    };
});


builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowApiGateway",
            builder =>
            {
                builder.WithOrigins("http://localhost:5000") // URL de l'API Gateway
                       .AllowCredentials() // Permettre les cookies
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
    });

builder.Services.AddControllers()
    // Add XML annotations to swagger documentation
    .AddXmlSerializerFormatters()
    .AddXmlDataContractSerializerFormatters()
    // Add Json DateOnly type support
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    });

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v0.1", new OpenApiInfo { Title = "BackendPatient API", Version = "v0.1" });
    c.IncludeXmlComments(Assembly.GetExecutingAssembly());
     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Veuillez entrer 'Bearer' suivi de l'espace et du token JWT dans la case de l'en-tête",
    });

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
builder.Services.AddScoped(typeof(IUpdateService<>), typeof(UpdateService<>));
builder.Services.AddScoped<Patient>();
builder.Services.AddScoped<DataSeeder>();

// Add Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequirePractitionerRole", policy => policy.RequireRole("Practitioner"))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole("User"))
    .AddPolicy("RequirePractitionerRoleOrHigher", policy => policy.RequireRole("Practitioner", "Admin"));


builder.Services.AddMvc();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    DataSeeder dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    dataSeeder.SeedPatients();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v0.1/swagger.json", "BackendPatient API v0.1");
    });
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowApiGateway");

app.MapControllers();

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("BackendPatient is well running.");
});

app.UseAuthentication();
app.UseAuthorization();


await app.RunAsync();