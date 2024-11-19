using System.Net.Http.Headers;
using Frontend.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Frontend.Services;

public class HttpClientService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<HttpClientService> logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IConfiguration _configuration = configuration;
    private readonly string _authServiceUrl = new ServiceUrl(configuration, logger).GetServiceUrl("Auth");
    private readonly ILogger<HttpClientService> _logger = logger;

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest)
    {
        // Get token from HttpContext.Items first as it's the most up-to-date
        string? token = _httpContextAccessor.HttpContext?.Items["Token"] as string;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            AddJwtToken();
        }

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Check if token was just refreshed by middleware
            token = _httpContextAccessor.HttpContext?.Items["Token"] as string;
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Using newly refreshed token from middleware");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                // Create a new request as the old one is already disposed
                HttpRequestMessage newRequest = new HttpRequestMessage(httpRequest.Method, httpRequest.RequestUri);
                if (httpRequest.Content != null)
                {
                    newRequest.Content = await CloneContent(httpRequest.Content);
                }
                return await _httpClient.SendAsync(newRequest);
            }

            // If no new token from middleware, try to refresh
            TokenResponse? newTokens = await RefreshToken();
            if (newTokens != null)
            {
                _logger.LogInformation("Successfully refreshed token in HttpClientService");
                AddJwtToken(newTokens.AccessToken);
                // Create a new request as the old one is already disposed
                HttpRequestMessage newRequest = new HttpRequestMessage(httpRequest.Method, httpRequest.RequestUri);
                if (httpRequest.Content != null)
                {
                    newRequest.Content = await CloneContent(httpRequest.Content);
                }
                return await _httpClient.SendAsync(newRequest);
            }
            else
            {
                _logger.LogWarning("Token refresh failed in HttpClientService");
            }
        }

        return response;
    }

    private async Task<TokenResponse?> RefreshToken()
    {
        try
        {
            string? tokenSerialized = _httpContextAccessor.HttpContext?.Request.Cookies["AuthTokens"];
            if (string.IsNullOrEmpty(tokenSerialized)) return null;

            AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized);
            if (authToken?.RefreshToken == null) return null;

            RefreshRequest request = new RefreshRequest { RefreshToken = authToken.RefreshToken };
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"{_authServiceUrl}/refresh", request);

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
                        Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetSection("JwtSettings").GetValue<int>("RefreshTokenLifetimeDays"))
                    };

                    AuthToken updatedAuthToken = new()
                    {
                        Token = newTokens.AccessToken,
                        RefreshToken = newTokens.RefreshToken
                    };

                    _httpContextAccessor.HttpContext?.Response.Cookies.Append("AuthTokens", 
                        JsonConvert.SerializeObject(updatedAuthToken), 
                        cookieOptions);

                    return newTokens;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
        }

        return null;
    }

    private void AddJwtToken(string? token = null)
    {
        if (token != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return;
        }

        string? tokenSerialized = _httpContextAccessor.HttpContext?.Request.Cookies["AuthTokens"];
        if (tokenSerialized != null)
        {
            token = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized)?.Token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static async Task<HttpContent?> CloneContent(HttpContent content)
    {
        if (content == null) return null;
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