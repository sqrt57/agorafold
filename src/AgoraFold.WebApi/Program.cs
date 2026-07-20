using AgoraFold.Core;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Storage;
using AgoraFold.WebApi.Filters;
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

var vueClientOrigin = builder.Configuration["Cors:VueClientOrigin"]
    ?? throw new InvalidOperationException("Cors:VueClientOrigin is not configured.");

builder.Services.AddCors(options =>
    options.AddPolicy("VueClient", policy => policy
        .WithOrigins(vueClientOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

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
app.UseStaticFiles(); // serves wwwroot/uploads/listings, populated at runtime
app.UseRouting();

app.UseCors("VueClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
