using AgoraFold.Core;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Storage;
using AgoraFold.Mvc.Filters;
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

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<AgoraFoldExceptionFilter>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Listings}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
