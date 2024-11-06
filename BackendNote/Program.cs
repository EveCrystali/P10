using BackendNote.Data;
using BackendNote.Models;
using BackendNote.Services;
using BackendPatient.Extensions;
using SharedAuthLibrary;
using SharedAuthorizationLibrary;
using SharedCorsLibrary;
using SharedSwaggerLibrary;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add Authorization policies and authentification
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Cors configuration
builder.AddCorsConfiguration("AllowApiGateway", "http://apigateway:5000");

builder.Services.AddControllers()
       .AddXmlDataContractSerializerFormatters()
       // Add Json DateOnly type support
       .AddJsonOptions(options =>
       {
           options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
       });

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddScoped<DataSeeder>();

// Configure authorization policies
builder.Services.AddAuthorizationPolicies();

builder.Services.Configure<NoteDatabaseSettings>(
                                                 builder.Configuration.GetSection("NoteDatabase"));

builder.Services.AddSingleton<NotesService>();

builder.WebHost.UseUrls("http://*:7202");

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    DataSeeder dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await dataSeeder.SeedNotesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors("AllowApiGateway");

app.MapControllers();

app.MapGet("/", requestDelegate: async context =>
{
    await context.Response.WriteAsync("BackendNote is well running.");
});

app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();