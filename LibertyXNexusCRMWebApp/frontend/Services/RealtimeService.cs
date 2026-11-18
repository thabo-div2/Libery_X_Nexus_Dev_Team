using Microsoft.AspNetCore.SignalR.Client;

namespace frontend.Services;

// One authenticated connection per Blazor circuit; the JWT stays server-side.
public sealed class RealtimeService(TokenStorageService tokens, IConfiguration config,
    ILogger<RealtimeService> logger) : IAsyncDisposable
{
    private HubConnection? connection;
    private readonly SemaphoreSlim gate = new(1);
    private readonly CancellationTokenSource lifetime = new();
    private Task? retryLoop;
    private bool enabled, attempted;
    public event Func<Task>? Changed;
    public async Task StartAsync()
    {
        enabled = true;
        retryLoop ??= RetryAsync();
        await ConnectAsync();
    }
    private async Task ConnectAsync()
    {
        var resync = false;
        await gate.WaitAsync(lifetime.Token);
        try
        {
            if (!enabled || connection?.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting
                || await tokens.GetTokenAsync() is null) return;
            if (connection is not null) await connection.DisposeAsync();
            var reconnecting = attempted;
            attempted = true;
            var api = new Uri(config["ApiSettings:BaseUrl"]!);
            connection = new HubConnectionBuilder().WithUrl(new Uri(api, "/hubs/updates"), options =>
                options.AccessTokenProvider = () => tokens.GetTokenAsync())
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) }).Build();
            connection.On("DataChanged", NotifyAsync);
            connection.Reconnected += _ => NotifyAsync();
            await connection.StartAsync(lifetime.Token);
            resync = reconnecting;
        }
        catch (Exception ex) when (!lifetime.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Realtime connection unavailable; retrying automatically.");
            if (connection is not null) await connection.DisposeAsync();
            connection = null;
        }
        finally { gate.Release(); }
        if (resync) await NotifyAsync();
    }
    private async Task RetryAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
        try { while (await timer.WaitForNextTickAsync(lifetime.Token)) if (enabled) await ConnectAsync(); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }
    public async Task StopAsync()
    {
        enabled = false;
        await gate.WaitAsync();
        try { if (connection is not null) await connection.DisposeAsync(); connection = null; }
        finally { gate.Release(); }
    }
    private async Task NotifyAsync()
    {
        if (!enabled) return;
        foreach (var handler in Changed?.GetInvocationList() ?? [])
        {
            try { await ((Func<Task>)handler)(); }
            catch (Exception ex) { logger.LogWarning(ex, "Realtime screen refresh failed."); }
        }
    }
    public async ValueTask DisposeAsync()
    {
        lifetime.Cancel();
        if (retryLoop is not null) try { await retryLoop; } catch (OperationCanceledException) { }
        await StopAsync();
        lifetime.Dispose();
    }
}
