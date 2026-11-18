using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Realtime;

public static class UpdateScope
{
    public static async Task<int?> AdvisorAsync(ClaimsPrincipal user, ApplicationDbContext db)
    {
        if (user.IsInRole("Advisor") && int.TryParse(user.FindFirstValue("advisorId"), out var advisor)) return advisor;
        if (user.IsInRole("Client") && int.TryParse(user.FindFirstValue("clientId"), out var client))
            return await db.Clients.Where(c => c.ClientId == client).Select(c => (int?)c.AdvisorId).FirstOrDefaultAsync();
        return null;
    }
    public static string Group(int advisor) => $"advisor:{advisor}";
}

// No client-selectable groups or client-callable broadcast methods.
[Authorize(Roles = "Advisor,Client")]
public sealed class UpdatesHub(IDbContextFactory<ApplicationDbContext> factory) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var advisor = await UpdateScope.AdvisorAsync(Context.User!, db);
        if (advisor is null) { Context.Abort(); return; }
        await Groups.AddToGroupAsync(Context.ConnectionId, UpdateScope.Group(advisor.Value));
        await base.OnConnectedAsync();
    }
}
