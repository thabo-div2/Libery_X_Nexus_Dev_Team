using frontend.Components;
using frontend.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddScoped<ProtectedSessionStorage>();
        builder.Services.AddScoped<TokenStorageService>();

        builder.Services.AddScoped<JwtAuthenticationHandler>();

        var apiUrl = builder.Configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            throw new InvalidOperationException(
                "ApiSettings:BaseUrl is missing from appsettings.json.");
        }

        builder.Services.AddScoped(sp =>
        {
            var jwtHandler = sp.GetRequiredService<JwtAuthenticationHandler>();

            jwtHandler.InnerHandler = new HttpClientHandler();

            var httpClient = new HttpClient(jwtHandler)
            {
                BaseAddress = new Uri(apiUrl!)
            };

            return httpClient;
        });

        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<CurrentUserService>();
        builder.Services.AddScoped<AdvisorService>();
        builder.Services.AddScoped<InvitationService>();
        builder.Services.AddScoped<ClientService>();
        builder.Services.AddScoped<MessageService>();
        builder.Services.AddScoped<MeetingService>();
        builder.Services.AddScoped<DocumentService>();
        builder.Services.AddScoped<PolicyService>();

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
    }
}
