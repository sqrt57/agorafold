namespace AgoraFold.Htmx.Controllers;

/// <summary>HTMX sets this header on every request it issues, letting an action tell an htmx-driven
/// partial request apart from a normal full-page navigation to the same URL.</summary>
public static class HttpRequestExtensions
{
    public static bool IsHtmx(this HttpRequest request) => request.Headers["HX-Request"] == "true";
}
