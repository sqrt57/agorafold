using AgoraFold.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (args.Any(argument => argument is "-h" or "--help"))
{
    AdminTui.PrintUsage();
    return 0;
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddAgoraFoldAdmin(builder.Configuration);

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var userService = scope.ServiceProvider.GetRequiredService<AdminUserService>();

return await new AdminTui(userService).RunAsync();
