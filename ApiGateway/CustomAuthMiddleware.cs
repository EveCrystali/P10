// using System;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.Primitives;
// using Ocelot.Logging;
// using Ocelot.Middleware;


// namespace ApiGateway
// {
//     public class CustomAuthMiddleware
//     {
//         private readonly RequestDelegate _next;
//         private readonly ILogger<CustomAuthMiddleware> _logger;
    
//         public CustomAuthMiddleware(RequestDelegate next, ILogger<CustomAuthMiddleware> logger)
//         {
//             _next = next;
//             _logger = logger;
//         }
    
//         public async Task Invoke(HttpContext context)
//         {
//             if (context.Request.Cookies.ContainsKey("P10AuthCookie"))
//             {
//                 var cookieValue = context.Request.Cookies["P10AuthCookie"];
//                 context.Request.Headers.Append("Cookie", new StringValues($"P10AuthCookie={cookieValue}"));
//                 _logger.LogInformation("Cookie d'authentification ajouté à la requête : {Cookie}", cookieValue);
//             }
//             else
//             {
//                 _logger.LogWarning("Aucun cookie d'authentification trouvé dans la requête entrante.");
//             }
    
//             await _next(context);
//         }
//     }
    
// }