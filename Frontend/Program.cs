using System.Net.Security;
using System.Text;
using Frontend.Controllers;
using Frontend.Controllers.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequirePractitionerRole", policy => policy.RequireRole("Practitioner"))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole("User"))
    .AddPolicy("RequirePractitionerRoleOrHigher", policy => policy.RequireRole("Practitioner", "Admin"));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddHttpClient<HttpClientService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<JwtValidationService>();

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
    options.AddPolicy("AllowFrontend",
        builder => builder.WithOrigins("https://localhost:7000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
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