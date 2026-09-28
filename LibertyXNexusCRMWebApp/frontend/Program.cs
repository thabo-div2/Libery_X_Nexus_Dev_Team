using frontend.Components;
using frontend.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5295/") });
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddSingleton<MessageNotifier>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
