using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AgoraFold.Mvc.Models;

namespace AgoraFold.Mvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

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
