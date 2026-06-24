using CitizenPlatform.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CitizenPlatform.Api.Filters;

public sealed class ValidationProblemFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => $"{entry.Key}: {error.ErrorMessage}"))
            .ToArray();

        context.Result = new BadRequestObjectResult(ApiResponse.Fail("Validation failed.", errors));
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
