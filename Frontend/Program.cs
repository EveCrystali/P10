using Frontend.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add Authorization policies and cookie authentification

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie name shared between services
    options.Cookie.Name = "P10AuthCookie"; 
    // Redirect to login page if unauthorized
    options.LoginPath = "/auth/login"; 
    // Redirect to access denied page if unauthorized
    options.AccessDeniedPath = "/auth/accesscenied";
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
        client.BaseAddress = new Uri("http://localhost:5000");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler 
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
