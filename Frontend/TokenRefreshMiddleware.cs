using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Frontend.Models;
using Frontend.Services;
namespace Frontend;

public class TokenRefreshMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TokenRefreshMiddleware> logger)
{
    private readonly IConfiguration _configuration = configuration;

    private readonly string _authServiceUrl = new ServiceUrl(configuration, logger).GetServiceUrl("Auth");


    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("TokenRefreshMiddleware : Begin execution");

        await ProcessAuthTokens(context);

        logger.LogInformation("TokenRefreshMiddleware : End execution");
        await next(context);
    }

    private async Task ProcessAuthTokens(HttpContext context)
    {
        TryExtractTokensFromCookie(context);

        string? accessToken = context.Items["Token"] as string;
        string? refreshToken = context.Items["RefreshToken"] as string;

        LogTokenDebugInfo(accessToken, refreshToken);

        if (await HandleTokenRefreshIfNeeded(context, accessToken, refreshToken))
        {
            return;
        }

        UpdateAuthorizationHeaderIfValidToken(context, accessToken);
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
                context.Items["Token"] = authToken.Token;
                context.Items["RefreshToken"] = authToken.RefreshToken;

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

    private async Task<bool> HandleTokenRefreshIfNeeded(HttpContext context, string? accessToken, string? refreshToken)
    {
        if (!IsTokenExpired(accessToken) || string.IsNullOrEmpty(refreshToken)) return false;

        logger.LogInformation("TokenRefreshMiddleware : Access token renewal");
        TokenResponse newTokens = await RefreshTokens(refreshToken);

        if (string.IsNullOrEmpty(newTokens.AccessToken) || string.IsNullOrEmpty(newTokens.RefreshToken))
        {
            HandleFailedRefresh(context);
            return true;
        }

        UpdateTokensAndCookie(context, newTokens);
        return false;
    }

    private void HandleFailedRefresh(HttpContext context)
    {
        context.Response.Cookies.Delete("AuthTokens");
        logger.LogDebug("TokenRefreshMiddleware : Redirecting to the login page because the refresh token is invalid");
        context.Response.Redirect("/Auth/Login");
    }

    private void UpdateTokensAndCookie(HttpContext context, TokenResponse newTokens)
    {
        context.Items["Token"] = newTokens.AccessToken;
        context.Items["RefreshToken"] = newTokens.RefreshToken;
        context.Request.Headers.Authorization = $"Bearer {newTokens.AccessToken}";

        var cookieOptions = CreateCookieOptions();
        var updatedAuthToken = new AuthToken
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


    // string? authTokensCookie = context.Request.Cookies["AuthTokens"];
    // if (!string.IsNullOrEmpty(authTokensCookie))
    // {
    //     try 
    //     {
    //         AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(authTokensCookie);
    //         if (authToken != null)
    //         {
    //             context.Items["Token"] = authToken.Token;
    //             context.Items["RefreshToken"] = authToken.RefreshToken;

    //             // Add the token to the Authorization header if it has not expired
    //             if (!IsTokenExpired(authToken.Token))
    //             {
    //                 context.Request.Headers.Authorization = $"Bearer {authToken.Token}";
    //             }
    //         }
    //     }
    //     catch (JsonException ex)
    //     {
    //         logger.LogError(ex, "Error while deserializing AuthTokens cookie");
    //     }
    // }

    // string? accessToken = context.Items["Token"] as string;
    // string? refreshToken = context.Items["RefreshToken"] as string;

    // logger.LogDebug("TokenRefreshMiddleware : Current access token : {AccessToken}", accessToken);
    // logger.LogDebug("TokenRefreshMiddleware : Current refresh token : {RefreshToken}", refreshToken);

    // if (IsTokenExpired(accessToken) && !string.IsNullOrEmpty(refreshToken))
    // {
    //     // Renew the access token
    //     logger.LogInformation("TokenRefreshMiddleware : Access token renewal");
    //     TokenResponse newTokens = await RefreshTokens(refreshToken);
    //     if (!string.IsNullOrEmpty(newTokens.AccessToken) && !string.IsNullOrEmpty(newTokens.RefreshToken))
    //     {
    //         // Update the tokens in HttpContext.Items
    //         context.Items["Token"] = newTokens.AccessToken;
    //         context.Items["RefreshToken"] = newTokens.RefreshToken;

    //         // Add the new access token to the Authorization header
    //         context.Request.Headers.Authorization = $"Bearer {newTokens.AccessToken}";

    //         // Update the cookie with a longer lifetime (7 days like the refresh token)
    //         CookieOptions cookieOptions = new()
    //         {
    //             HttpOnly = true,
    //             Secure = true,
    //             SameSite = SameSiteMode.Strict,
    //             Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetSection("JwtSettings").GetValue<int>("RefreshTokenLifetimeDays"))
    //         };

    //         AuthToken updatedAuthToken = new()
    //         {
    //             Token = newTokens.AccessToken,
    //             RefreshToken = newTokens.RefreshToken
    //         };

    //         context.Response.Cookies.Append("AuthTokens", JsonConvert.SerializeObject(updatedAuthToken), cookieOptions);
    //         logger.LogDebug("TokenRefreshMiddleware : Updating cookie with new tokens");
    //     }
    //     else
    //     {
    //         // Delete the cookie if the refresh has failed
    //         context.Response.Cookies.Delete("AuthTokens");
    //         logger.LogDebug("TokenRefreshMiddleware : Redirecting to the login page because the refresh token is invalid");
    //         context.Response.Redirect("/Auth/Login");
    //         return;
    //     }
    // }
    // else if (!string.IsNullOrEmpty(accessToken) && !IsTokenExpired(accessToken))
    // {
    //     // If the token is valid, add it to the Authorization header
    //     context.Request.Headers.Authorization = $"Bearer {accessToken}";
    // }

    // logger.LogInformation("TokenRefreshMiddleware : Fin de l'exécution");
    // await next(context);
    // }

    private bool IsTokenExpired(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            logger.LogDebug("TokenRefreshMiddleware : The token is empty or null");
            return true;
        }

        JwtSecurityToken? jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        bool result = jwt.ValidTo < DateTime.UtcNow;
        logger.LogDebug($"TokenRefreshMiddleware : The token is expired : {result}");
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