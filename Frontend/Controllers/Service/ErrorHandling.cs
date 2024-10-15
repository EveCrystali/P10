using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Frontend.Controllers.Service;

public static class ErrorHandlingUtils
{
    public static async Task HandleErrorResponse(
    ILogger logger,
    ModelStateDictionary modelState,
    ITempDataDictionary tempData,
    string? id = "",
    string? logErrorMessage = "An error occurred.",
    string? modelErrorMessage = "An error occurred while processing your request.",
    HttpResponseMessage? response = null)
    {
        string errorContent = "";

        if (response != null)
        {
            errorContent = await response.Content.ReadAsStringAsync();
        }

        logger.LogError(logErrorMessage, id, response?.StatusCode, errorContent);
        modelState.AddModelError(response?.StatusCode.ToString() ?? string.Empty, modelErrorMessage ?? string.Empty);
        tempData["Error"] = modelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
    }
}

