using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace AgoraFold.WebApi.Options;

public sealed class ConfigureCorsOptions(IOptionsMonitor<JsClientCorsOptions> jsClientCorsOptions) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options) =>
        options.AddPolicy("JsClients", policy => policy
            .SetIsOriginAllowed(origin => jsClientCorsOptions.CurrentValue.JsClientOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
}
