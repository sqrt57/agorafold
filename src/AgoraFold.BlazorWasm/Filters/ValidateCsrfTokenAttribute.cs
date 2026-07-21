using AgoraFold.BlazorWasm.Client.Api.Dto;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgoraFold.BlazorWasm.Filters;

/// <summary>
/// Validates the X-CSRF-TOKEN header against IAntiforgery directly. The framework's own
/// [ValidateAntiForgeryToken] can't be used here — it resolves services that only
/// AddControllersWithViews/AddMvc register, and this project uses plain AddControllers.
/// </summary>
public sealed class ValidateCsrfTokenAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new ApiErrorResponse(["Invalid or missing CSRF token."]));
            return;
        }

        await next();
    }
}
