using System.Net;
using Frontend.Controllers;
using Microsoft.AspNetCore.Mvc;
namespace Frontend.Extensions;

public static class ControllerExtensions
{
    public static IActionResult? HandleAuthorizationResponse(this Controller controller, HttpStatusCode statusCode, ILogger logger)
    {
        string controllerAuthName = nameof(AuthController).Replace("Controller", "");

        if (statusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogDebug("User not authenticated. Redirecting to login");
            return controller.RedirectToAction(nameof(AuthController.Login), controllerAuthName);
        }

        if (statusCode == HttpStatusCode.Forbidden)
        {
            logger.LogDebug("Access denied. Status code: {StatusCode}", statusCode);
            return controller.View("AccessDenied");
        }

        return null;
    }
}