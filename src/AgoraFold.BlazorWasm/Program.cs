using AgoraFold.BlazorWasm.Components;
using AgoraFold.BlazorWasm.Filters;
using AgoraFold.Core;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services
    .AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Same-origin SPA: unlike AgoraFold.WebApi (consumed cross-origin by the Vue dev server), the
// WASM client is served by this same host, so a failed [Authorize] would otherwise redirect to
// Identity's default login page instead of returning a JSON-friendly status the client can act on.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAgoraFoldCore();
builder.Services.Configure<ListingImageStorageOptions>(o =>
    o.RootPath = Path.Combine(builder.Environment.WebRootPath, "uploads", "listings"));

builder.Services.AddControllers(options =>
    options.Filters.Add<AgoraFoldExceptionFilter>());

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// Powers the initial server-side AuthorizeRouteView check in Routes.razor (reads HttpContext.User,
// i.e. the real auth cookie) before WASM has booted. Once WASM takes over, the Client project's own
// AddCascadingAuthenticationState() + CookieAuthenticationStateProvider take over client-side.
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseWebAssemblyDebugging();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles(); // serves wwwroot/uploads/listings, populated at runtime

// Explicit, and placed after UseStaticFiles: without it, ASP.NET Core auto-inserts routing at the
// very front of the pipeline, so endpoint matching (including MapStaticAssets' build-time-manifest
// check) would run before UseStaticFiles gets a chance - see design/blazor-server-architecture.md's
// Gotchas section, the same ordering issue applies here.
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AgoraFold.BlazorWasm.Client._Imports).Assembly);

app.Run();
