using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddOcelot(builder.Configuration);

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
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    // Extend the cookie expiration if the user remains active
    options.SlidingExpiration = true;
});

var app = builder.Build();

await app.UseOcelot();

app.Run();
