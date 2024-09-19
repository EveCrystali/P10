using Auth.Data;
using Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Swashbuckle.AspNetCore.Swagger;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.

// Add Identity

// Ajouter Identity avec Cookie Authentication
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

// Add Identity API

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    // options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
});

// Add Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequirePractitionerRole", policy => policy.RequireRole("Practitioner"))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole("User"))
    .AddPolicy("RequirePractitionerRoleOrHigher", policy => policy.RequireRole("Practitioner", "Admin"));

builder.Services.AddControllersWithViews();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<DataSeeder>();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    UserManager<IdentityUser> userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
    // Seed users and roles
    await DataSeeder.SeedUsers(userManager, logger);
    await DataSeeder.SeedRoles(roleManager, logger);
    await DataSeeder.SeedAffectationsRolesToUsers(userManager, roleManager, logger);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add protection gainst CSRF attacks and secure authentication
app.UseAuthentication();
app.UseAuthorization();
app.UseCookiePolicy();

app.Run();
