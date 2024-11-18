using System.Net.Security;
using Frontend.Controllers;
using Frontend.Services;
using SharedLibrary;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

// Add Authorization policies and authentification
builder.Services.AddJwtAuthentication(builder.Configuration);

// Configure authorization policies
builder.Services.AddAuthorizationPolicies();

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
           string? apiGatewayBaseUrl = configuration["ApiGatewayAddress:BaseUrl"];
           if (string.IsNullOrEmpty(apiGatewayBaseUrl))
           {
               throw new ArgumentNullException(apiGatewayBaseUrl, "L'URL de base ne peut pas être nulle ou vide.");
           }
           client.BaseAddress = new Uri(apiGatewayBaseUrl);
       })
       .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
       {
           ServerCertificateCustomValidationCallback = (_, _, _, errors) => errors == SslPolicyErrors.None
       });

builder.Services.AddScoped<PatientService>();

// Add Cors configuration
builder.AddCorsConfiguration("AllowApiGateway", "http://apigateway:5000");

builder.WebHost.UseUrls("http://*:7000");

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
                       "default",
                       "{controller=Home}/{action=Index}/{id?}");

// Configure Cors policy to allow all origins because we are using Ocelot Api Gateway
// We need to allow all origins because Frontend and Auth are not on the same port
app.UseCors("AllowFrontend");

// Add protection gainst CSRF attacks and secure authentication
app.UseAuthentication();
app.UseAuthorization();

app.UseCookiePolicy();

await app.RunAsync();