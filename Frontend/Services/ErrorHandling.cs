using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Frontend.Controllers.Service;

public static class ErrorHandlingUtils
{
    public static void HandleErrorResponse(
    ILogger logger,
    ModelStateDictionary modelState,
    ITempDataDictionary tempData,
    string? logErrorMessage = "An error occurred.",
    string? modelErrorMessage = "An error occurred while processing your request.",
    HttpResponseMessage? response = null)
    {
        logger.LogError(logErrorMessage, response?.StatusCode, $"Request failed with status code {response?.StatusCode}.");
        modelState.AddModelError(response?.StatusCode.ToString() ?? string.Empty, modelErrorMessage ?? string.Empty);
        tempData["Error"] = modelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
    }
}