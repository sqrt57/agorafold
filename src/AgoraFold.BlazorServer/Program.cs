using AgoraFold.BlazorServer.Account;
using AgoraFold.BlazorServer.Components;
using AgoraFold.Core;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Storage;
using Microsoft.AspNetCore.Components.Authorization;
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

builder.Services.AddAgoraFoldCore();
builder.Services.Configure<ListingImageStorageOptions>(o =>
    o.RootPath = Path.Combine(builder.Environment.WebRootPath, "uploads", "listings"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(o =>
    {
        // InputFile streams over the SignalR circuit, not a multipart POST - default frame size
        // (32 KB) is far below Core's 5 MB/file cap, so raise it with headroom.
        o.MaximumReceiveMessageSize = 6 * 1024 * 1024;
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Explicit, and placed after UseStaticFiles: without it, ASP.NET Core auto-inserts routing at
// the very front of the pipeline, so endpoint matching (including MapStaticAssets' build-time-
// manifest check) would run before UseStaticFiles gets a chance - breaking the runtime-written
// wwwroot/uploads/listings files the same "uploads aren't build-time assets" way AGENTS.md
// documents for the other variants, just via a different mechanism (routing order, not the
// manifest check itself).
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithStaticAssets();

app.MapAdditionalIdentityEndpoints();

app.Run();
