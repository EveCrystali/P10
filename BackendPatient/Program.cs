using BackendPatient.Data;
using BackendPatient.Extensions;
using BackendPatient.Models;
using BackendPatient.Services;
using Microsoft.EntityFrameworkCore;
using SharedAuthLibrary;
using SharedSwaggerLibrary;
using SharedAuthorizationLibrary;
using SharedCorsLibrary;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Authorization policies and authentification
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Cors configuration
builder.AddCorsConfiguration("AllowApiGateway", "http://localhost:5000");

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
builder.Services.AddSwaggerDocumentation("BackendPatient API", "v0.1");
builder.Services.AddScoped(typeof(IUpdateService<>), typeof(UpdateService<>));
builder.Services.AddScoped<Patient>();
builder.Services.AddScoped<DataSeeder>();

// Configure authorization policies
builder.Services.AddAuthorizationPolicies();

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

app.UseCors("AllowApiGateway");

app.MapControllers();

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("BackendPatient is well running.");
});

app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();