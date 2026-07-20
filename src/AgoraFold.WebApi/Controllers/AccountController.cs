using System.Security.Claims;
using AgoraFold.Core.Entities;
using AgoraFold.WebApi.Filters;
using AgoraFold.WebApi.Models;
using AgoraFold.WebApi.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.WebApi.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateCsrfToken]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new ApiErrorResponse(result.Errors.Select(e => e.Description).ToList()));
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateCsrfToken]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, request.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return BadRequest(new ApiErrorResponse(["Invalid login attempt."]));
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        return Ok(new UserResponse(user!.Id, user.Email!, user.DisplayName));
    }

    [HttpPost("logout")]
    [ValidateCsrfToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }
}
