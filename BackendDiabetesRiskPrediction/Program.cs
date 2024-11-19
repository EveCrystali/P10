using BackendDiabetesRiskPrediction.Services;
using SharedLibrary;
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

// Configure authorization policies
builder.Services.AddAuthorizationPolicies();

builder.Services.AddSingleton<ElasticsearchService>();
builder.Services.AddSingleton<DiabetesRiskNotePredictionService>();

builder.WebHost.UseUrls("http://*:7204");

WebApplication app = builder.Build();

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
    await context.Response.WriteAsync("BackendDiabetesRiskPrediction is well running.");
});

app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();