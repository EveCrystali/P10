using System.Text;
using Auth.Data;
using Auth.Models;
using Auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT configuration
IConfigurationSection? jwtSettings = builder.Configuration.GetSection("JwtSettings");
if (jwtSettings == null)
{
    throw new ArgumentNullException(nameof(jwtSettings), "JWT settings configuration is missing.");
}

string? secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (string.IsNullOrEmpty(secretKey))
{
    throw new ArgumentNullException(nameof(secretKey), "JWT Key configuration is missing.");
}

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
    // options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
});



builder.Services.AddControllers();

// Configure Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "P10.Auth.Api", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter 'Bearer' followed by a space and the JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    IConfiguration configuration = builder.Configuration;
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
        // ?: Multiple audiences are not supported
        ValidAudiences = configuration.GetSection("JwtSettings:Audience").Get<string[]>(),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero,
    };
});

// Add Services to the container
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddScoped<IJwtRevocationService, JwtRevocationService>();

// Configure authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
    .AddPolicy("RequirePractitionerRole", policy => policy.RequireRole("Practitioner"))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole("User"))
    .AddPolicy("RequirePractitionerRoleOrHigher", policy => policy.RequireRole("Practitioner", "Admin"));


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