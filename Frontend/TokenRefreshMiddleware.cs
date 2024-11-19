using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Frontend.Models;
using Frontend.Services;
namespace Frontend;

public class TokenRefreshMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TokenRefreshMiddleware> logger)
{
    private readonly IConfiguration _configuration = configuration;

    private readonly string _authServiceUrl = new ServiceUrl(configuration, logger).GetServiceUrl("Auth");

    private const string _tokenName = "Token";

    private const string _refreshTokenName = "RefreshToken";


    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("TokenRefreshMiddleware : Begin execution");

        await ProcessAuthTokens(context);

        logger.LogInformation("TokenRefreshMiddleware : End execution");
        await next(context);
    }

    private async Task<bool> ProcessAuthTokens(HttpContext context)
    {
        TryExtractTokensFromCookie(context);

        string? accessToken = context.Items[_tokenName] as string;
        string? refreshToken = context.Items[_refreshTokenName] as string;

        LogTokenDebugInfo(accessToken, refreshToken);

        if (IsTokenExpired(accessToken) && !string.IsNullOrEmpty(refreshToken))
        {
            logger.LogInformation("TokenRefreshMiddleware : Access token renewal");
            TokenResponse newTokens = await RefreshTokens(refreshToken);

            if (string.IsNullOrEmpty(newTokens.AccessToken) || string.IsNullOrEmpty(newTokens.RefreshToken))
            {
                HandleFailedRefresh(context);
                return false;
            }

            UpdateTokensAndCookie(context, newTokens);
            return true; // Indique qu'un refresh a été effectué
        }

        UpdateAuthorizationHeaderIfValidToken(context, accessToken);
        return false;
    }

    private void TryExtractTokensFromCookie(HttpContext context)
    {
        string? authTokensCookie = context.Request.Cookies["AuthTokens"];
        if (string.IsNullOrEmpty(authTokensCookie)) return;

        try
        {
            AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(authTokensCookie);
            if (authToken != null)
            {
                context.Items[_tokenName] = authToken.Token;
                context.Items[_refreshTokenName] = authToken.RefreshToken;

                if (!IsTokenExpired(authToken.Token))
                {
                    context.Request.Headers.Authorization = $"Bearer {authToken.Token}";
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error while deserializing AuthTokens cookie");
        }
    }

    private void LogTokenDebugInfo(string? accessToken, string? refreshToken)
    {
        logger.LogDebug("TokenRefreshMiddleware : Current access token : {AccessToken}", accessToken);
        logger.LogDebug("TokenRefreshMiddleware : Current refresh token : {RefreshToken}", refreshToken);
    }

    private void HandleFailedRefresh(HttpContext context)
    {
        context.Response.Cookies.Delete("AuthTokens");
        context.Items[_tokenName] = null;
        context.Items[_refreshTokenName] = null;
        logger.LogDebug("TokenRefreshMiddleware : Tokens cleared due to failed refresh");
    }

    private void UpdateTokensAndCookie(HttpContext context, TokenResponse newTokens)
    {
        context.Items[_tokenName] = newTokens.AccessToken;
        context.Items[_refreshTokenName] = newTokens.RefreshToken;
        context.Request.Headers.Authorization = $"Bearer {newTokens.AccessToken}";

        CookieOptions cookieOptions = CreateCookieOptions();
        AuthToken updatedAuthToken = new AuthToken
        {
            Token = newTokens.AccessToken,
            RefreshToken = newTokens.RefreshToken
        };

        context.Response.Cookies.Append("AuthTokens", JsonConvert.SerializeObject(updatedAuthToken), cookieOptions);
        logger.LogDebug("TokenRefreshMiddleware : Updating cookie with new tokens");
    }

    private CookieOptions CreateCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetSection("JwtSettings").GetValue<int>("RefreshTokenLifetimeDays"))
        };
    }

    private void UpdateAuthorizationHeaderIfValidToken(HttpContext context, string? accessToken)
    {
        if (!string.IsNullOrEmpty(accessToken) && !IsTokenExpired(accessToken))
        {
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }
    }

    private bool IsTokenExpired(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            logger.LogDebug("TokenRefreshMiddleware : The token is empty or null");
            return true;
        }

        JwtSecurityToken? jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        bool result = jwt.ValidTo < DateTime.UtcNow;
        logger.LogInformation("Token expired at {ExpirationTime}", jwt.ValidTo.ToString("yyyy-MM-dd HH:mm:ss")); 
        return result;
    }

    private async Task<TokenResponse> RefreshTokens(string refreshToken)
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient();
            RefreshRequest request = new() { RefreshToken = refreshToken };

            logger.LogDebug("TokenRefreshMiddleware : Sending refresh request with token : {RefreshToken}", refreshToken);

            HttpResponseMessage response = await client.PostAsJsonAsync($"{_authServiceUrl}/refresh", request);
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    TokenResponse? tokens = JsonConvert.DeserializeObject<TokenResponse>(content);
                    if (tokens != null && !string.IsNullOrEmpty(tokens.AccessToken) && !string.IsNullOrEmpty(tokens.RefreshToken))
                    {
                        logger.LogInformation("TokenRefreshMiddleware : Tokens refreshed successfully");
                        return tokens;
                    }
                    logger.LogWarning("TokenRefreshMiddleware : The server response does not contain the expected tokens. Content: {Content}", content);
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "TokenRefreshMiddleware : Error deserializing the response. Content: {Content}", content);
                }
            }
            else
            {
                logger.LogWarning("TokenRefreshMiddleware : Refreshing tokens failed. Status code: {StatusCode}, Content: {Content}",
                    response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TokenRefreshMiddleware : Error refreshing tokens");
        }

        return new TokenResponse();
    }

    private sealed class TokenResponse
    {
        [JsonProperty("token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class RefreshRequest
    {
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}