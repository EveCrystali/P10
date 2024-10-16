using BackendNote.Models;
using BackendNote.Services;
using SharedAuthLibrary;
using SharedCorsLibrary;
using SharedSwaggerLibrary;
using SharedAuthorizationLibrary;
using BackendPatient.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Authorization policies and authentification
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Cors configuration
builder.AddCorsConfiguration("AllowApiGateway", "https://localhost:5000");

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

// Configure authorization policies
builder.Services.AddAuthorizationPolicies();

builder.Services.Configure<NoteDatabaseSettings>(
    builder.Configuration.GetSection("NoteDatabase"));

builder.Services.AddSingleton<NotesService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowApiGateway");

app.MapControllers();

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("BackendNote is well running.");
});

app.UseAuthentication();
app.UseAuthorization();


await app.RunAsync();
