using System.Net.Http.Headers;
using Frontend.Models;
using Newtonsoft.Json;

namespace Frontend.Services;

public class HttpClientService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<HttpClientService> logger)
{
    private readonly string _authServiceUrl = new ServiceUrl(configuration, logger).GetServiceUrl("Auth");

    private const string _bearerPrefix = "Bearer";

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest)
    {
        // Get token from HttpContext.Items first as it's the most up-to-date
        string? token = httpContextAccessor.HttpContext?.Items["Token"] as string;
        if (!string.IsNullOrEmpty(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_bearerPrefix, token);
        }
        else
        {
            AddJwtToken();
        }

        HttpResponseMessage response = await httpClient.SendAsync(httpRequest);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Check if token was just refreshed by middleware
            token = httpContextAccessor.HttpContext?.Items["Token"] as string;
            if (!string.IsNullOrEmpty(token))
            {
                logger.LogInformation("Using newly refreshed token from middleware");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_bearerPrefix, token);
                // Create a new request as the old one is already disposed
                HttpRequestMessage newRequest = new HttpRequestMessage(httpRequest.Method, httpRequest.RequestUri);
                if (httpRequest.Content != null)
                {
                    newRequest.Content = await CloneContent(httpRequest.Content);
                }
                return await httpClient.SendAsync(newRequest);
            }

            // If no new token from middleware, try to refresh
            TokenResponse? newTokens = await RefreshToken();
            if (newTokens != null)
            {
                logger.LogInformation("Successfully refreshed token in HttpClientService");
                AddJwtToken(newTokens.AccessToken);
                // Create a new request as the old one is already disposed
                HttpRequestMessage newRequest = new HttpRequestMessage(httpRequest.Method, httpRequest.RequestUri);
                if (httpRequest.Content != null)
                {
                    newRequest.Content = await CloneContent(httpRequest.Content);
                }
                return await httpClient.SendAsync(newRequest);
            }
            else
            {
                logger.LogWarning("Token refresh failed in HttpClientService");
            }
        }

        return response;
    }

    private async Task<TokenResponse?> RefreshToken()
    {
        try
        {
            string? tokenSerialized = httpContextAccessor.HttpContext?.Request.Cookies["AuthTokens"];
            if (string.IsNullOrEmpty(tokenSerialized)) return null;

            AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized);
            if (authToken?.RefreshToken == null) return null;

            RefreshRequest request = new RefreshRequest { RefreshToken = authToken.RefreshToken };
            HttpResponseMessage response = await httpClient.PostAsJsonAsync($"{_authServiceUrl}/refresh", request);

            if (response.IsSuccessStatusCode)
            {
                TokenResponse? newTokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
                if (newTokens != null)
                {
                    // Update the cookie with new tokens
                    CookieOptions cookieOptions = new()
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddDays(configuration.GetSection("JwtSettings").GetValue<int>("RefreshTokenLifetimeDays"))
                    };

                    AuthToken updatedAuthToken = new()
                    {
                        Token = newTokens.AccessToken,
                        RefreshToken = newTokens.RefreshToken
                    };

                    httpContextAccessor.HttpContext?.Response.Cookies.Append("AuthTokens", 
                        JsonConvert.SerializeObject(updatedAuthToken), 
                        cookieOptions);

                    return newTokens;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh");
        }

        return null;
    }

    private void AddJwtToken(string? token = null)
    {
        if (token != null)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_bearerPrefix, token);
            return;
        }

        string? tokenSerialized = httpContextAccessor.HttpContext?.Request.Cookies["AuthTokens"];
        if (tokenSerialized != null)
        {
            token = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized)?.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_bearerPrefix, token);
        }
    }

    private static async Task<HttpContent?> CloneContent(HttpContent content)
    {
        MemoryStream ms = new MemoryStream();
        await content.CopyToAsync(ms);
        ms.Position = 0;
        StreamContent clone = new StreamContent(ms);
        foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
        {
            clone.Headers.Add(header.Key, header.Value);
        }
        return clone;
    }

    private sealed class RefreshRequest
    {
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class TokenResponse
    {
        [JsonProperty("token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}