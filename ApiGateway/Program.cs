using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
// using ApiGateway;
using Serilog;
using Serilog.Extensions.Hosting;
using Ocelot.Infrastructure;

const string cookiePolicySecurityName = "P10AuthCookie";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration de Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

string parentDirectory = Path.GetDirectoryName(builder.Environment.ContentRootPath);
string sharedKeysPath = Path.Combine(parentDirectory, "SharedKeys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(sharedKeysPath))
    .SetApplicationName("P10AuthApp");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = cookiePolicySecurityName;
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/accessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
        };
    });


// Add Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequirePractitionerRole", policy => policy.RequireRole("Practitioner"))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole("User"))
    .AddPolicy("RequirePractitionerRoleOrHigher", policy => policy.RequireRole("Practitioner", "Admin"));

builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin",
            builder =>
            {
                builder.WithOrigins("https://localhost:7200", "https://localhost:7201", "https://localhost:5000", "https://localhost:7000") 
                       .AllowCredentials() // Permettre les cookies
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
    });


builder.Services.AddOcelot(builder.Configuration);

WebApplication app = builder.Build();

app.UseCors("AllowSpecificOrigin");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// app.UseMiddleware<CustomAuthMiddleware>();

await app.UseOcelot();

await app.RunAsync();