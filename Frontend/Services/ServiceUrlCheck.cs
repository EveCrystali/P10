namespace Frontend.Services;

public class ServiceUrl(IConfiguration configuration, ILogger logger)
{

    public string GetServiceUrl(string? service)
    {
        string? serviceUrl = configuration[$"{service}:BaseUrl"];
        if (string.IsNullOrEmpty(serviceUrl) || string.IsNullOrWhiteSpace(service))
        {
            string messageLog = $"{service} URL is not configured.";
            throw new InvalidOperationException(messageLog);
        }
        logger.LogInformation("Service URL for {Service} is {ServiceUrl}", service, serviceUrl);
        return serviceUrl;
    }
}