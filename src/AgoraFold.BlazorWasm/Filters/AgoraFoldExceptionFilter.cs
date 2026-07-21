using AgoraFold.BlazorWasm.Client.Api.Dto;
using AgoraFold.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgoraFold.BlazorWasm.Filters;

public class AgoraFoldExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            NotFoundException => new NotFoundResult(),
            ForbiddenException => new StatusCodeResult(StatusCodes.Status403Forbidden),
            ValidationException ex => new BadRequestObjectResult(new ApiErrorResponse(ex.Errors)),
            _ => null,
        };

        context.ExceptionHandled = context.Result is not null;
    }
}
