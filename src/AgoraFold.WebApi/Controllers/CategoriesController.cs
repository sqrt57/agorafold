using AgoraFold.Core.Services;
using AgoraFold.WebApi.Models.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.WebApi.Controllers;

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
