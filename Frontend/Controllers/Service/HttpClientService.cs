using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Frontend.Models;

namespace Frontend.Controllers.Service;

public class HttpClientService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public void AddJwtToken()
    {
        string? tokenSerialized = _httpContextAccessor.HttpContext?.Request.Cookies["AuthTokens"];

        if (tokenSerialized != null)
        {
            string? token = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized)?.Token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest)
    {
        AddJwtToken();
        return await _httpClient.SendAsync(httpRequest);
    }
}


