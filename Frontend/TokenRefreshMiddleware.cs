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
        logger.LogInformation("TokenRefreshMiddleware : Début de l'exécution");

        string? authTokensCookie = context.Request.Cookies["AuthTokens"];
        if (!string.IsNullOrEmpty(authTokensCookie))
        {
            try 
            {
                AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(authTokensCookie);
                if (authToken != null)
                {
                    context.Items["Token"] = authToken.Token;
                    context.Items["RefreshToken"] = authToken.RefreshToken;
                    
                    // Ajouter le token à l'en-tête Authorization s'il n'est pas expiré
                    if (!IsTokenExpired(authToken.Token))
                    {
                        context.Request.Headers.Authorization = $"Bearer {authToken.Token}";
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Erreur lors de la désérialisation du cookie AuthTokens");
            }
        }

        string? accessToken = context.Items["Token"] as string;
        string? refreshToken = context.Items["RefreshToken"] as string;

        logger.LogDebug("TokenRefreshMiddleware : Accès token actuel : {AccessToken}", accessToken);
        logger.LogDebug("TokenRefreshMiddleware : Token de rafraîchissement actuel : {RefreshToken}", refreshToken);

        if (IsTokenExpired(accessToken) && !string.IsNullOrEmpty(refreshToken))
        {
            // Renouveler le token d'accès
            logger.LogInformation("TokenRefreshMiddleware : Renouvellement du token d'accès");
            TokenResponse newTokens = await RefreshTokens(refreshToken);
            if (!string.IsNullOrEmpty(newTokens.AccessToken) && !string.IsNullOrEmpty(newTokens.RefreshToken))
            {
                // Mettre à jour les tokens dans HttpContext.Items
                context.Items["Token"] = newTokens.AccessToken;
                context.Items["RefreshToken"] = newTokens.RefreshToken;

                // Ajouter le nouveau access token à l'en-tête Authorization
                context.Request.Headers.Authorization = $"Bearer {newTokens.AccessToken}";

                // Mettre à jour le cookie avec une durée de vie plus longue (7 jours comme le refresh token)
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

                context.Response.Cookies.Append("AuthTokens", JsonConvert.SerializeObject(updatedAuthToken), cookieOptions);
                logger.LogDebug("TokenRefreshMiddleware : Mise à jour du cookie avec les nouveaux tokens");
            }
            else
            {
                // Supprimer le cookie si le refresh a échoué
                context.Response.Cookies.Delete("AuthTokens");
                logger.LogDebug("TokenRefreshMiddleware : Redirection vers la page de connexion car le refresh token est invalide");
                context.Response.Redirect("/Auth/Login");
                return;
            }
        }
        else if (!string.IsNullOrEmpty(accessToken) && !IsTokenExpired(accessToken))
        {
            // Si le token est valide, l'ajouter à l'en-tête Authorization
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }

        logger.LogInformation("TokenRefreshMiddleware : Fin de l'exécution");
        await next(context);
    }

    private bool IsTokenExpired(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            logger.LogDebug("TokenRefreshMiddleware : Le token est vide ou nul");
            return true;
        }

        JwtSecurityToken? jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        bool result = jwt.ValidTo < DateTime.UtcNow;
        logger.LogDebug($"TokenRefreshMiddleware : le token est expiré : {result}");
        return result;
    }
    
    private async Task<TokenResponse> RefreshTokens(string refreshToken)
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient();
            RefreshRequest request = new() { RefreshToken = refreshToken };
            
            logger.LogDebug("TokenRefreshMiddleware : Envoi de la requête de rafraîchissement avec le token : {RefreshToken}", refreshToken);
            
            HttpResponseMessage response = await client.PostAsJsonAsync($"{_authServiceUrl}/refresh", request);
            string content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try 
                {
                    TokenResponse? tokens = JsonConvert.DeserializeObject<TokenResponse>(content);
                    if (tokens != null && !string.IsNullOrEmpty(tokens.AccessToken) && !string.IsNullOrEmpty(tokens.RefreshToken))
                    {
                        logger.LogInformation("TokenRefreshMiddleware : Tokens rafraîchis avec succès");
                        return tokens;
                    }
                    logger.LogWarning("TokenRefreshMiddleware : La réponse du serveur ne contient pas les tokens attendus. Content: {Content}", content);
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "TokenRefreshMiddleware : Erreur lors de la désérialisation de la réponse. Content: {Content}", content);
                }
            }
            else
            {
                logger.LogWarning("TokenRefreshMiddleware : Échec du rafraîchissement des tokens. Status code: {StatusCode}, Content: {Content}", 
                    response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TokenRefreshMiddleware : Erreur lors du rafraîchissement des tokens");
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