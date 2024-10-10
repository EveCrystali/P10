using Auth.Data;
using Auth.Models;
using Auth.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedAuthLibrary;
using SharedSwaggerLibrary;
using SharedAuthorizationLibrary;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT configuration
builder.Services.AddJwtAuthentication(builder.Configuration);


// Identity configuration
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure Identity options
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
});

builder.Services.AddControllers();

builder.Services.AddSwaggerDocumentation("Auth API", "v0.1");

// Add Services to the container
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddScoped<IJwtRevocationService, JwtRevocationService>();

// Configure authorization policies
builder.Services.AddAuthorizationPolicies();

// Discover API endpoints
builder.Services.AddEndpointsApiExplorer();

WebApplication app = builder.Build();

// Seed users and roles
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    UserManager<User> userManager = services.GetRequiredService<UserManager<User>>();
    RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
    await DataSeeder.SeedUsers(userManager, logger);
    await DataSeeder.SeedRoles(roleManager, logger);
    await DataSeeder.SeedAffectationsRolesToUsers(userManager, roleManager, logger);
}

// Configure the HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Apply authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();