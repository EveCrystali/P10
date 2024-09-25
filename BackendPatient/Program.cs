using System.Reflection;
using BackendPatient.Data;
using BackendPatient.Extensions;
using BackendPatient.Models;
using BackendPatient.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie name shared between services
    options.Cookie.Name = "P10AuthCookie";
    // Redirect to login page if unauthorized
    options.LoginPath = "/Auth/Login";
    // Redirect to access denied page if unauthorized
    options.AccessDeniedPath = "/Auth/AccessDenied";
    // Set if the cookie should be HttpOnly or not meaning it cannot be accessed via JavaScript or not
    options.Cookie.HttpOnly = true;
    // Attribute that helps protect against cross-site request forgery (CSRF) attacks
    // by specifying whether a cookie should be sent along with cross-site requests
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    // Extend the cookie expiration if the user remains active
    options.SlidingExpiration = true;
});

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

app.MapControllers();

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("BackendPatient is well running.");
});

app.UseAuthorization();

app.Run();