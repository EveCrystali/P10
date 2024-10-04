using System.Net.Security;
using Frontend.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration Configuration = builder.Configuration;

string cookiePolicySecurityName = "P10AuthCookie";

string parentDirectory = Path.GetDirectoryName(builder.Environment.ContentRootPath);
string sharedKeysPath = Path.Combine(parentDirectory, "SharedKeys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(sharedKeysPath))
    .SetApplicationName("P10AuthApp");

builder.Services.AddHttpContextAccessor();

// Add Authorization policies and cookie authentification

// Add Identity with Cookie Authentication
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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequirePractitionerRole", policy => policy.RequireRole("Practitioner"))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole("User"))
    .AddPolicy("RequirePractitionerRoleOrHigher", policy => policy.RequireRole("Practitioner", "Admin"));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Note: Must be removed when not in development
// Configure the HTTP request pipeline for avoiding self-signed certificates
builder.Services.AddHttpClient<HomeController>(client =>
    {
        // Remplacer l'URI codée en dur par une configuration
        string? apiGatewayBaseUrl = Configuration["ApiGatewayAddress:BaseUrl"];
        if (string.IsNullOrEmpty(apiGatewayBaseUrl))
        {
            throw new ArgumentNullException(apiGatewayBaseUrl, "L'URL de base ne peut pas être nulle ou vide.");
        }
        client.BaseAddress = new Uri(apiGatewayBaseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            return errors == SslPolicyErrors.None;
        }
    });

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


WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Configure Cors policy to allow all origins because we are using Ocelot Api Gateway
// We need to allow all origins because Frontend and Auth are not on the same port
app.UseCors("AllowSpecificOrigin");

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add protection gainst CSRF attacks and secure authentication
app.UseAuthentication();
app.UseAuthorization();
app.UseCookiePolicy();

await app.RunAsync();