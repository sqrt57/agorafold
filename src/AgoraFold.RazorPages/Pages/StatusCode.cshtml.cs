using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgoraFold.RazorPages.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class StatusCodeModel : PageModel
{
    public int Code { get; set; }

    public string Title { get; set; } = "";

    public string Message { get; set; } = "";

    public void OnGet(int id)
    {
        (Title, Message) = id switch
        {
            404 => ("Not found", "We couldn't find what you were looking for."),
            403 => ("Forbidden", "You don't have permission to do that."),
            400 => ("Bad request", "The request couldn't be processed as sent."),
            _ => ("Something went wrong", "An unexpected error occurred."),
        };

        Code = id;
    }
}
