using DaxaPos.Web;
using DaxaPos.Web.Api;
using DaxaPos.Web.Auth;
using DaxaPos.Web.State;
using DaxaPos.Web.Storage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped<IBrowserStorage, LocalStorageBrowserStorage>();
builder.Services.AddSingleton<IDeviceContextStore, DeviceContextStore>();
builder.Services.AddSingleton<IAuthSessionStore, AuthSessionStore>();
builder.Services.AddSingleton<IBackOfficeSessionStore, BackOfficeSessionStore>();
builder.Services.AddSingleton<IDraftOrderStore, DraftOrderStore>();
builder.Services.AddSingleton<IConnectivityTracker, ConnectivityTracker>();
builder.Services.AddTransient<IDraftPointerWatcher, JsDraftPointerWatcher>();
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddTransient<ConnectivityHandler>();

builder.Services.AddHttpClient<DaxaApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthHeaderHandler>()
    .AddHttpMessageHandler<ConnectivityHandler>();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());

var host = builder.Build();

// PLAN-0006 Milestone A: device context and session must be restored from localStorage before the
// first render, both so the AuthHeaderHandler has something to read synchronously per request and
// so the shell can land on the right page (device-setup/login/home) on first paint instead of
// flashing an unauthenticated state.
var deviceContextStore = host.Services.GetRequiredService<IDeviceContextStore>();
var sessionStore = host.Services.GetRequiredService<IAuthSessionStore>();
var backOfficeSessionStore = host.Services.GetRequiredService<IBackOfficeSessionStore>();
await deviceContextStore.EnsureLoadedAsync();
await sessionStore.EnsureLoadedAsync();
await backOfficeSessionStore.EnsureLoadedAsync();

await host.RunAsync();
