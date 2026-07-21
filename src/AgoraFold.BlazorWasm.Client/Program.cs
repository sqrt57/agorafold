using AgoraFold.BlazorWasm.Client.Api;
using AgoraFold.BlazorWasm.Client.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// "ApiRaw" has no CsrfHandler attached - CsrfHandler itself uses it to fetch a token without
// looping back through itself. "Api" is what every feature client actually calls through.
builder.Services.AddHttpClient(ApiClientNames.Raw, client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddScoped<CsrfHandler>();
builder.Services.AddHttpClient(ApiClientNames.Api, client =>
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<CsrfHandler>();

builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AccountApiClient>();
builder.Services.AddScoped<CategoryApiClient>();
builder.Services.AddScoped<ListingApiClient>();
builder.Services.AddScoped<ConversationApiClient>();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
builder.Services.AddScoped<CurrentUserAccessor>();

await builder.Build().RunAsync();
