using AgoraFold.Core;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Storage;
using AgoraFold.LiveChat;
using AgoraFold.LiveChat.Origin;
using AgoraFold.WebApi.Filters;
using AgoraFold.WebApi.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services
    .AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

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
builder.Services.AddLiveChat();
builder.Services.AddSingleton<ILiveChatOriginPolicy>(sp =>
    new ConfiguredOriginPolicy(() => sp.GetRequiredService<IOptionsMonitor<JsClientCorsOptions>>().CurrentValue.JsClientOrigins));
builder.Services.Configure<ListingImageStorageOptions>(o =>
    o.RootPath = Path.Combine(builder.Environment.WebRootPath, "uploads", "listings"));

builder.Services.AddControllers(options =>
    options.Filters.Add<AgoraFoldExceptionFilter>());

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var jsClientOrigins = builder.Configuration.GetSection("Cors:JsClientOrigins").Get<string[]>();
if (jsClientOrigins is not { Length: > 0 })
{
    throw new InvalidOperationException("Cors:JsClientOrigins is not configured.");
}

// Bound via IOptionsMonitor (see ConfigureCorsOptions) rather than a fixed array, so editing
// Cors:JsClientOrigins in appsettings.json takes effect on the next request without restarting.
builder.Services.Configure<JsClientCorsOptions>(builder.Configuration.GetSection("Cors"));
builder.Services.ConfigureOptions<ConfigureCorsOptions>();
builder.Services.AddCors();

builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseWebSockets();
app.UseStaticFiles(); // serves wwwroot/uploads/listings, populated at runtime
app.UseRouting();

app.UseCors("JsClients");

app.UseAuthentication();
app.UseAuthorization();

app.MapConversationLiveChatEndpoint().RequireAuthorization();
app.MapControllers();

app.Run();
