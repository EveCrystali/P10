using Microsoft.EntityFrameworkCore;
using BackendPatient.Data;
using BackendPatient.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    app.UseSwaggerUI();
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
