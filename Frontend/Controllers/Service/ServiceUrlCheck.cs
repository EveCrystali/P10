using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace Frontend.Controllers.Service;

public class ServiceUrl(IConfiguration configuration, ILogger logger)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger _logger = logger;

    public string GetServiceUrl(string? service)
    {
        string? serviceUrl = _configuration[$"{service}:BaseUrl"];
        if (string.IsNullOrEmpty(serviceUrl) || string.IsNullOrWhiteSpace(service))
        {
            string messageLog = $"{service} URL is not configured.";
            throw new InvalidOperationException(messageLog); 
        }
        _logger.LogInformation("Service URL for {Service} is {ServiceUrl}", service, serviceUrl);
        return serviceUrl;
    }
}
