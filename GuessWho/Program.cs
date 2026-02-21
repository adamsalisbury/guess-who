using GuessWho.Components;
using GuessWho.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Game session manager — singleton so all Blazor circuits share the same state
builder.Services.AddSingleton<GameSessionService>();

// Background service that periodically removes ended or abandoned sessions
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
