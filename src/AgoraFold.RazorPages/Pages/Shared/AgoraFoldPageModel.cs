using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgoraFold.RazorPages.Pages.Shared;

public abstract class AgoraFoldPageModel : PageModel
{
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
