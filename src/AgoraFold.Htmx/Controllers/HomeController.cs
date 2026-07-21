using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AgoraFold.Htmx.Models;

namespace AgoraFold.Htmx.Controllers;

public class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public new IActionResult StatusCode(int id)
    {
        var (title, message) = id switch
        {
            404 => ("Not found", "We couldn't find what you were looking for."),
            403 => ("Forbidden", "You don't have permission to do that."),
            400 => ("Bad request", "The request couldn't be processed as sent."),
            _ => ("Something went wrong", "An unexpected error occurred."),
        };

        return View(new StatusCodeViewModel(id, title, message));
    }
}
