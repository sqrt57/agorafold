using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.BlazorWasm.Controllers;

[ApiController]
[Route("api/antiforgery")]
[AllowAnonymous]
public class AntiforgeryController(IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("token")]
    public IActionResult GetToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}
