using AgoraFold.BlazorWasm.Client.Api.Dto.Categories;
using AgoraFold.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.BlazorWasm.Controllers;

[ApiController]
[Route("api/categories")]
[AllowAnonymous]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return Ok(categories.Select(c => new CategoryResponse(c.Id, c.Name)).ToList());
    }
}
