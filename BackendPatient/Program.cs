using Microsoft.EntityFrameworkCore;
using BackendPatient.Data;
using BackendPatient.Models;
using BackendPatient.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Swashbuckle.AspNetCore.Swagger;
using BackendPatient.Extensions;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddMvc();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
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
