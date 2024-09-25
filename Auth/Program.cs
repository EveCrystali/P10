using Auth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration de la base de données
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuration d'Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configuration des options d'Identity
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
});

// Configuration des cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "P10AuthCookie";
    options.LoginPath = "/auth/login";
    options.AccessDeniedPath = "/auth/accessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// Configuration de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Configuration des politiques d'autorisation
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequirePractitionerRole", policy => policy.RequireRole("Practitioner"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
    options.AddPolicy("RequirePractitionerRoleOrHigher", policy => policy.RequireRole("Practitioner", "Admin"));
});

// Ajout des services MVC
builder.Services.AddControllersWithViews();

// Configuration de Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "P10.Auth.Api", Version = "v1" });
    options.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
    {
        Description = "Entrer votre nom de cookie pour avoir l'authorisation",
        Type = SecuritySchemeType.ApiKey,
        Name = "P10AuthCookie", // Assurez-vous que le nom correspond au cookie configuré
        In = ParameterLocation.Cookie
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "CookieAuth"
                }
            },
            new string[] {}
        }
    });
});

// Seed des données
builder.Services.AddScoped<DataSeeder>();

// Découverte des endpoints API
builder.Services.AddEndpointsApiExplorer();

WebApplication app = builder.Build();

// Seed des utilisateurs et des rôles
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    UserManager<IdentityUser> userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
    await DataSeeder.SeedUsers(userManager, logger);
    await DataSeeder.SeedRoles(roleManager, logger);
    await DataSeeder.SeedAffectationsRolesToUsers(userManager, roleManager, logger);
}

// Configuration du pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Application des politiques CORS
app.UseCors("AllowAllOrigins");

// Application des middlewares d'authentification et d'autorisation
app.UseAuthentication();
app.UseAuthorization();

// Gestion des cookies
app.UseCookiePolicy();

app.MapControllers();

app.Run();